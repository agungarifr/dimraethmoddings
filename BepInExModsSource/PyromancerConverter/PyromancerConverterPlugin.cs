using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace PyromancerConverter
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that converts the Magician class's Cold/Ice identity into
    /// Fire/Burning:
    ///   * Magician ice spells (Blizzard, Frost Javelin, ...) deal Fire instead of Ice.
    ///   * The Chill they apply becomes Burning (and the Frostbound self-buff becomes Kindled).
    ///   * Magician skill-tree nodes that grant Chill/Ice stats grant their Burning/Fire equivalent.
    ///
    /// Design + reverse-engineering notes: modding/docs/pyromancer/00-plan.md
    ///
    /// Co-op: damage and status are server-computed, so run the SAME config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class PyromancerConverterPlugin : BasePlugin
    {
        public const string GUID = "com.custom.pyromancerconverter";
        public const string NAME = "Pyromancer Converter";
        public const string VERSION = "0.1.0";

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ConvertDamage;
        public static ConfigEntry<bool> ConvertStatus;
        public static ConfigEntry<bool> ConvertTree;
        public static ConfigEntry<bool> ConvertTooltips;
        public static ConfigEntry<bool> LogSwaps;
        // [2026-10-06] Deep audit: logs every stack actually applied to a target through the low-level
        // Stacking.AddStacksToTarget path. This is how a bypassing caller (e.g. the damage system
        // auto-applying a spell definition's StackDatas, or a passive) would sneak Chill past the
        // BaseSpellLibrary patch, so it is the authoritative way to find the source of a cold icon.
        public static ConfigEntry<bool> DiagnosticLogging;
        // [2026-10-06] Phase 3 — VFX. Recolor the converted spells' ice effects (blue) to fire (orange) at
        // runtime by shifting the ParticleSystem color modules and sprite/trail/line renderer colors. Only
        // converted spells are touched, so monster/pet ice VFX are left alone.
        public static ConfigEntry<bool> ConvertVfx;
        public static ConfigEntry<bool> LogVfx;

        /// <summary>Spells whose Ice/Chill output this mod rewrites. Seeded with the known Magician ice
        /// spells and extended at runtime by <see cref="Patch_SpellLibrary_ApplyDefinition"/>.</summary>
        internal static readonly HashSet<Spell> Converted = new HashSet<Spell>();

        internal static readonly HashSet<Spell> LoggedDamage = new HashSet<Spell>();
        // [2026-10-05] Diagnostic roster: every Ice-typed spell definition seen, with its class, logged once.
        internal static readonly HashSet<Spell> IceRoster = new HashSet<Spell>();
        // [2026-10-05] Was HashSet<Spell>: one line per spell hid the fact that a single spell applies
        // several distinct StackingEffects (Blizzard applies Chill AND Frostbound). Now keyed by
        // (spell, effect) so every pair is reported once.
        // internal static readonly HashSet<Spell> LoggedStacks = new HashSet<Spell>();
        internal static readonly HashSet<long> LoggedStacks = new HashSet<long>();

        // [2026-10-06] Audit keys so the deep diagnostics report each distinct application once.
        internal static readonly HashSet<string> AuditedTargetStacks = new HashSet<string>();
        internal static readonly HashSet<long> AuditedBaseStacks = new HashSet<long>();
        internal static readonly HashSet<Spell> AuditedIceDamage = new HashSet<Spell>();

        internal static long StackKey(Spell spell, StackingEffect effect)
        {
            return ((long)(int)spell << 8) | (byte)effect;
        }

        /// <summary>Log a converted stack swap once per (spell, source effect).</summary>
        internal static void LogSwap(Spell spell, StackingEffect from, StackingEffect to)
        {
            if (!LogSwaps.Value) return;
            if (LoggedStacks.Add(StackKey(spell, from)))
            {
                Log.LogInfo($"[status] {spell}: {from} -> {to}");
            }
        }

        /// <summary>
        /// [2026-10-06] Deep audit of a stack that is actually being applied to a target through the
        /// low-level Stacking component. Reports each distinct (path, effect, source) once.
        /// </summary>
        internal static void AuditTargetStack(string via, Stacking target, StackingEffect effect, int stacks, float duration, ulong sourceId)
        {
            if (!DiagnosticLogging.Value) return;
            string key = via + "|" + (int)effect + "|" + sourceId;
            if (!AuditedTargetStacks.Add(key)) return;
            string obj = "?";
            try { obj = target != null && target.gameObject != null ? target.gameObject.name : "?"; } catch { }
            Log.LogInfo($"[audit-target] {via}: effect={effect} stacks={stacks} dur={duration:0.##} source={sourceId} on={obj}");
        }

        /// <summary>
        /// [2026-10-06] Deep audit of a BaseSpellLibrary stack call, once per (spell, effect), with the
        /// caster's player/monster flags so it is obvious whether a cold effect came from the player.
        /// </summary>
        internal static void AuditBaseStack(BaseSpellLibrary prefab, string via, StackingEffect effect, int amount)
        {
            if (!DiagnosticLogging.Value) return;
            Spell spell = prefab != null ? prefab._spell : Spell.None;
            if (!AuditedBaseStacks.Add(StackKey(spell, effect))) return;
            ObjectsCommon owner = prefab != null ? prefab._owner : null;
            bool ownerPlayer = owner != null && owner.IsPlayer;
            bool ownerMonster = owner != null && owner.IsMonster;
            Log.LogInfo($"[audit-bl] {via}: spell={spell} effect={effect} amount={amount} ownerPlayer={ownerPlayer} ownerMonster={ownerMonster}");
        }

        /// <summary>[2026-10-06] Deep audit of an Ice damage instance that this mod did NOT convert.</summary>
        internal static void AuditIceDamage(BaseSpellLibrary prefab, string via)
        {
            if (!DiagnosticLogging.Value) return;
            Spell spell = prefab != null ? prefab._spell : Spell.None;
            if (!AuditedIceDamage.Add(spell)) return;
            ObjectsCommon owner = prefab != null ? prefab._owner : null;
            bool ownerPlayer = owner != null && owner.IsPlayer;
            bool ownerMonster = owner != null && owner.IsMonster;
            Log.LogInfo($"[audit-dmg] {via}: spell={spell} Ice ownerPlayer={ownerPlayer} ownerMonster={ownerMonster} (not converted)");
        }

        // Insurance: spells that must convert even if their definition does not report Element/DamageType Ice
        // (e.g. the prefab hard-codes Ice). Blizzard + Frost Javelin are the core Magician ice spells.
        private static readonly Spell[] KnownMagicianIce = { Spell.Blizzard, Spell.FrostJavelin };

        public override void Load()
        {
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false nothing is converted. (Default: true)");

            ConvertDamage = Config.Bind("General", "ConvertDamage", true,
                "Magician ice spells deal Fire instead of Ice. (Default: true)");

            ConvertStatus = Config.Bind("General", "ConvertStatus", true,
                "Chill applied by Magician ice spells becomes Burning; the Frostbound self-buff becomes Kindled. (Default: true)");

            ConvertTree = Config.Bind("General", "ConvertTree", true,
                "Remap Magician skill-tree Chill/Ice stats (MaxChill, ChilledDuration, DamageVsChilled, IceDamage, ...) to their Burning/Fire equivalents. (Default: true)");

            ConvertTooltips = Config.Bind("General", "ConvertTooltips", true,
                "Rewrite Chill/Frost/Ice wording to Burn/Fire in spell and skill-node tooltips. (Default: true)");

            LogSwaps = Config.Bind("Diagnostics", "LogSwaps", true,
                "Log every spell/node conversion to the BepInEx console. Turn off once the conversion set is known. (Default: true)");

            // [2026-10-06] Default flipped true -> false: the deep audit did its job. It proved the only
            // remaining Chill came from the player's PET (SavageFrostbite/ChillwingDescent, ownerPlayer=False
            // ownerMonster=False) while every Magician cold source (Blizzard + its IcePool) converts cleanly.
            // Pets are intentionally left cold, so the [audit-target] spew is no longer wanted in normal play.
            // The old line is kept for reference; set DiagnosticLogging=true in the config to re-enable.
            // DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", true,
            //     "Deep audit: log every stack applied at the target (Stacking.AddStacksToTarget) and every BaseSpellLibrary stack call, once per (spell/effect/source). Use to find cold sources that bypass the normal path. (Default: true)");
            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Deep audit: log every stack applied at the target (Stacking.AddStacksToTarget) and every BaseSpellLibrary stack call, once per (spell/effect/source). Re-enable only when hunting an unexplained cold source. (Default: false)");

            ConvertVfx = Config.Bind("General", "ConvertVfx", true,
                "Recolor the converted spells' ice VFX (blue) to fire (orange) at runtime, by shifting the particle color modules and sprite/trail/line renderer colors. (Default: true)");

            LogVfx = Config.Bind("Diagnostics", "LogVfx", true,
                "Log every VFX element recolored (type, object, before -> after), once per (spell, type, object). Turn off once the VFX pass is verified. (Default: true)");

            foreach (Spell s in KnownMagicianIce)
            {
                Converted.Add(s);
            }

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_SendDamageToTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToCaster));
            // [2026-10-06] Phase 3 — VFX recoloring hooks.
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_Start_Vfx));
            PatchOrLog(harmony, typeof(Patch_AreaOfEffect_Start_Vfx));
            PatchOrLog(harmony, typeof(Patch_SkillShot_Start_Vfx));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_SpawnVFXAtOffset));
            PatchOrLog(harmony, typeof(Patch_Stacking_AddStacksToTarget_StackData));
            PatchOrLog(harmony, typeof(Patch_Stacking_AddStacksToTarget_List));
            PatchOrLog(harmony, typeof(Patch_SkillTree_LoadSkillTreeInitial));
            PatchOrLog(harmony, typeof(Patch_SkillTree_LoadNodeEffects));
            PatchOrLog(harmony, typeof(Patch_SkillTree_ApplySingleNodeEffect));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedName));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedEffects));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}, ConvertDamage: {ConvertDamage.Value}, ConvertStatus: {ConvertStatus.Value}, " +
                        $"ConvertTree: {ConvertTree.Value}, ConvertTooltips: {ConvertTooltips.Value}, ConvertVfx: {ConvertVfx.Value}, " +
                        $"LogSwaps: {LogSwaps.Value}, LogVfx: {LogVfx.Value}, DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo($"Seeded Magician ice spells: {string.Join(", ", KnownMagicianIce)}");
            Log.LogInfo("=================================================");
        }

        private void PatchOrLog(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.PatchAll(patchType);
            }
            catch (Exception ex)
            {
                Log.LogError($"[{NAME}] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        /// <summary>True when the prefab belongs to a spell this mod rewrites.</summary>
        internal static bool IsConverted(BaseSpellLibrary prefab)
        {
            return prefab != null && Converted.Contains(prefab._spell);
        }

        /// <summary>
        /// True when cold output from this prefab should become fire/burn.
        /// [2026-10-05] The converted-set gate alone left Chill in play: Blizzard/FrostGlide spawn an
        /// Ice Pool whose SpellLibrary is not flagged ClassRequirement=Magician, so the pool's Chill was
        /// never rewritten. Anything a PLAYER-owned spell emits is therefore converted; cold from a
        /// monster (e.g. SavageFrostbite) is left alone because that prefab's _owner.IsPlayer is false.
        /// </summary>
        internal static bool ShouldConvert(BaseSpellLibrary prefab)
        {
            if (prefab == null) return false;
            if (Converted.Contains(prefab._spell)) return true;
            ObjectsCommon owner = prefab._owner;
            return owner != null && owner.IsPlayer;
        }
    }

    /// <summary>
    /// Postfix on the private <c>SpellLibrary.ApplyDefinition</c>. Records every spell whose definition is
    /// Ice-typed as "converted" (so the damage/status prefixes can gate on it), and swaps its cosmetic
    /// Element / DamageType to Fire. This does NOT change the damage the prefab actually sends (Blizzard
    /// hard-codes Ice at the call site) — that is handled by the SendDamageToTarget prefix.
    /// </summary>
    [HarmonyPatch(typeof(SpellLibrary), "ApplyDefinition")]
    public static class Patch_SpellLibrary_ApplyDefinition
    {
        public static void Postfix(SpellLibrary __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!PyromancerConverterPlugin.Enabled.Value) return;

                Spell spell = __instance.Spell;
                if (spell == Spell.None) return;

                Element element = __instance.Element;
                DamageType dmg = __instance.DmgType;
                bool ice = element == Element.Ice || dmg == DamageType.Ice;
                if (!ice) return;

                Class classReq = __instance.ClassRequirement;

                // [2026-10-05] Diagnostic: name every Ice-typed spell and its class requirement once, so
                // the true Magician ice roster can be confirmed from the log instead of guessed.
                if (PyromancerConverterPlugin.LogSwaps.Value &&
                    PyromancerConverterPlugin.IceRoster.Add(spell))
                {
                    PyromancerConverterPlugin.Log.LogInfo(
                        $"[ice] {spell}: element {element}, damage {dmg}, classReq {classReq}");
                }

                bool known = PyromancerConverterPlugin.Converted.Contains(spell);
                if (!known && classReq != Class.Magician) return;

                bool newly = PyromancerConverterPlugin.Converted.Add(spell);

                if (PyromancerConverterPlugin.ConvertDamage.Value)
                {
                    if (element == Element.Ice) __instance.SetElement(Element.Fire);
                    if (dmg == DamageType.Ice) __instance.SetDamageType(DamageType.Fire);
                }

                if (newly && PyromancerConverterPlugin.LogSwaps.Value)
                {
                    PyromancerConverterPlugin.Log.LogInfo(
                        $"[discover] {spell}: element {element}, damage {dmg}, classReq {classReq} -> converted to Fire/Burning");
                }
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[Patch_SpellLibrary_ApplyDefinition] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>BaseSpellLibrary.SendDamageToTarget</c>. Ice damage from a converted spell is re-dispatched
    /// as Fire. The parameter is passed by value (confirmed against the interop), so a plain prefix cannot edit
    /// it; instead we return false and replay the call with Fire, forwarding the original's return value via
    /// <c>__result</c>. The re-entrant call sees Fire and runs the original body exactly once.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "SendDamageToTarget")]
    public static class Patch_BaseSpellLibrary_SendDamageToTarget
    {
        public static bool Prefix(
            BaseSpellLibrary __instance,
            ObjectsCommon target,
            DamageType damageType,
            float multiplier,
            bool isDamageOverTime,
            bool isThirdAttack,
            float stunBuildupBonusPercent,
            ref Damage __result)
        {
            try
            {
                if (!PyromancerConverterPlugin.Enabled.Value) return true;
                if (!PyromancerConverterPlugin.ConvertDamage.Value) return true;
                if (damageType != DamageType.Ice) return true;
                if (!PyromancerConverterPlugin.ShouldConvert(__instance))
                {
                    PyromancerConverterPlugin.AuditIceDamage(__instance, "SendDamageToTarget");
                    return true;
                }

                __result = __instance.SendDamageToTarget(
                    target, DamageType.Fire, multiplier, isDamageOverTime, isThirdAttack, stunBuildupBonusPercent);

                if (PyromancerConverterPlugin.LogSwaps.Value &&
                    PyromancerConverterPlugin.LoggedDamage.Add(__instance._spell))
                {
                    PyromancerConverterPlugin.Log.LogInfo($"[damage] {__instance._spell}: Ice -> Fire");
                }

                return false;
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_SendDamageToTarget] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Prefix on <c>BaseSpellLibrary.AddStacksToTarget</c>. Chill from a converted spell is re-dispatched as
    /// Burning. The effect parameter is by value, so we return false and replay with Burning; the re-entrant
    /// call sees Burning and runs the original body exactly once.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "AddStacksToTarget")]
    public static class Patch_BaseSpellLibrary_AddStacksToTarget
    {
        public static bool Prefix(BaseSpellLibrary __instance, ObjectsCommon target, StackingEffect effect, int amount, float time)
        {
            try
            {
                PyromancerConverterPlugin.AuditBaseStack(__instance, "AddStacksToTarget", effect, amount);
                if (!PyromancerConverterPlugin.Enabled.Value) return true;
                if (!PyromancerConverterPlugin.ConvertStatus.Value) return true;

                // Only the cold effects are interesting here; anything else passes straight through.
                if (effect != StackingEffect.Chill && effect != StackingEffect.Frostbound) return true;

                if (!PyromancerConverterPlugin.ShouldConvert(__instance))
                {
                    // [2026-10-05] A cold stack from a spell that is NOT in the converted set means the
                    // discovery step (or the seed list) missed an ice spell. Name it once so the set can be
                    // widened instead of silently leaving Chill on the target.
                    if (PyromancerConverterPlugin.LogSwaps.Value &&
                        PyromancerConverterPlugin.LoggedStacks.Add(PyromancerConverterPlugin.StackKey(__instance._spell, effect)))
                    {
                        PyromancerConverterPlugin.Log.LogInfo(
                            $"[unconverted] {__instance._spell}: {effect} via AddStacksToTarget (not in converted set)");
                    }
                    return true;
                }

                if (effect == StackingEffect.Chill)
                {
                    __instance.AddStacksToTarget(target, StackingEffect.Burning, amount, time);
                    PyromancerConverterPlugin.LogSwap(__instance._spell, effect, StackingEffect.Burning);
                    return false;
                }

                // [2026-10-05] Blizzard's ally aura applies Frostbound(16) through AddStacksToTarget, not
                // AddStacksToCaster, so the Frostbound->Kindled swap must also happen on this path.
                __instance.AddStacksToTarget(target, StackingEffect.Kindled, amount, time);
                PyromancerConverterPlugin.LogSwap(__instance._spell, effect, StackingEffect.Kindled);
                return false;
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_AddStacksToTarget] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Prefix on <c>BaseSpellLibrary.AddStacksToCaster</c>. The Frostbound self-buff (the ice counterpart of
    /// Kindled) becomes Kindled on a converted spell.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "AddStacksToCaster")]
    public static class Patch_BaseSpellLibrary_AddStacksToCaster
    {
        public static bool Prefix(BaseSpellLibrary __instance, StackingEffect effect, int amount, float time)
        {
            try
            {
                PyromancerConverterPlugin.AuditBaseStack(__instance, "AddStacksToCaster", effect, amount);
                if (!PyromancerConverterPlugin.Enabled.Value) return true;
                if (!PyromancerConverterPlugin.ConvertStatus.Value) return true;
                if (effect != StackingEffect.Frostbound) return true;

                if (!PyromancerConverterPlugin.ShouldConvert(__instance))
                {
                    if (PyromancerConverterPlugin.LogSwaps.Value &&
                        PyromancerConverterPlugin.LoggedStacks.Add(PyromancerConverterPlugin.StackKey(__instance._spell, effect)))
                    {
                        PyromancerConverterPlugin.Log.LogInfo(
                            $"[unconverted] {__instance._spell}: Frostbound via AddStacksToCaster (not in converted set)");
                    }
                    return true;
                }

                __instance.AddStacksToCaster(StackingEffect.Kindled, amount, time);
                PyromancerConverterPlugin.LogSwap(__instance._spell, effect, StackingEffect.Kindled);
                return false;
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_AddStacksToCaster] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// [2026-10-06] Deep audit on the target-side stack entry point. This is where a caller that bypasses
    /// BaseSpellLibrary (e.g. a spell definition's StackDatas applied by the damage system, or a passive)
    /// would apply Chill directly, so it is the ground truth for which effect icon lands on a target.
    /// Diagnostic only: it never alters behaviour.
    /// </summary>
    [HarmonyPatch(typeof(Stacking), "AddStacksToTarget", new Type[] { typeof(StackData) })]
    public static class Patch_Stacking_AddStacksToTarget_StackData
    {
        public static void Prefix(Stacking __instance, StackData stack)
        {
            try
            {
                PyromancerConverterPlugin.AuditTargetStack(
                    "Stacking.AddStacksToTarget(StackData)", __instance, stack.Effect, stack.Stacks, stack.Duration, stack.SourceId);
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[Patch_Stacking_AddStacksToTarget_StackData] {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Stacking), "AddStacksToTarget", new Type[] { typeof(Il2CppSystem.Collections.Generic.List<StackData>) })]
    public static class Patch_Stacking_AddStacksToTarget_List
    {
        public static void Prefix(Stacking __instance, Il2CppSystem.Collections.Generic.List<StackData> stacks)
        {
            try
            {
                if (stacks == null) return;
                for (int i = 0; i < stacks.Count; i++)
                {
                    StackData s = stacks[i];
                    PyromancerConverterPlugin.AuditTargetStack(
                        "Stacking.AddStacksToTarget(List)", __instance, s.Effect, s.Stacks, s.Duration, s.SourceId);
                }
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[Patch_Stacking_AddStacksToTarget_List] {ex}");
            }
        }
    }

    /// <summary>Prefix on <c>SkillTree.LoadSkillTreeInitial</c>: convert every loaded Magician node before the
    /// tree aggregates its effects.</summary>
    [HarmonyPatch(typeof(SkillTree), "LoadSkillTreeInitial")]
    public static class Patch_SkillTree_LoadSkillTreeInitial
    {
        public static void Prefix()
        {
            if (!PyromancerConverterPlugin.Enabled.Value || !PyromancerConverterPlugin.ConvertTree.Value) return;
            TreeConverter.ConvertAllLoaded();
        }
    }

    /// <summary>Belt-and-braces: convert each Magician node just before the tree reads its effects.</summary>
    [HarmonyPatch(typeof(SkillTree), "LoadNodeEffects")]
    public static class Patch_SkillTree_LoadNodeEffects
    {
        public static void Prefix(SkillTreeNode node)
        {
            if (!PyromancerConverterPlugin.Enabled.Value || !PyromancerConverterPlugin.ConvertTree.Value) return;
            TreeConverter.ConvertNode(node);
        }
    }

    /// <summary>Belt-and-braces: convert each Magician node just before a point is applied.</summary>
    [HarmonyPatch(typeof(SkillTree), "ApplySingleNodeEffect")]
    public static class Patch_SkillTree_ApplySingleNodeEffect
    {
        public static void Prefix(SkillTreeNode node)
        {
            if (!PyromancerConverterPlugin.Enabled.Value || !PyromancerConverterPlugin.ConvertTree.Value) return;
            TreeConverter.ConvertNode(node);
        }
    }

    [HarmonyPatch(typeof(SkillTreeNode), "GetLocalizedName")]
    public static class Patch_SkillTreeNode_GetLocalizedName
    {
        public static void Postfix(ref string __result)
        {
            __result = TooltipText.Convert(__result);
        }
    }

    [HarmonyPatch(typeof(SkillTreeNode), "GetLocalizedDescription")]
    public static class Patch_SkillTreeNode_GetLocalizedDescription
    {
        public static void Postfix(ref string __result)
        {
            __result = TooltipText.Convert(__result);
        }
    }

    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedDescription")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedDescription
    {
        public static void Postfix(ref string __result)
        {
            __result = TooltipText.Convert(__result);
        }
    }

    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedEffects")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedEffects
    {
        public static void Postfix(ref Il2CppSystem.Collections.Generic.List<string> __result)
        {
            if (!PyromancerConverterPlugin.Enabled.Value || !PyromancerConverterPlugin.ConvertTooltips.Value) return;
            if (__result == null) return;

            for (int i = 0; i < __result.Count; i++)
            {
                __result[i] = TooltipText.Convert(__result[i]);
            }
        }
    }

    /// <summary>
    /// [2026-10-06] Phase 3 — VFX pass 1 (base spell spawn). A postfix on <c>BaseSpellLibrary.Start</c> runs after
    /// the prefab's virtual <c>OnStart</c> has configured its visuals, so the whole spell hierarchy can be
    /// recolored in one shot. Gated by <see cref="PyromancerConverterPlugin.ShouldConvert"/>, so monster/pet ice
    /// VFX are left alone.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "Start")]
    public static class Patch_BaseSpellLibrary_Start_Vfx
    {
        public static void Postfix(BaseSpellLibrary __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!PyromancerConverterPlugin.ShouldConvert(__instance)) return;
                VfxRecolor.Convert(__instance.gameObject, $"spell:{__instance._spell}");
            }
            catch (Exception ex) { PyromancerConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_Start_Vfx] {ex}"); }
        }
    }

    /// <summary>[2026-10-06] VFX pass 1b — AreaOfEffect overrides Start; its postfix runs once the AoE is set up.</summary>
    [HarmonyPatch(typeof(AreaOfEffect), "Start")]
    public static class Patch_AreaOfEffect_Start_Vfx
    {
        public static void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!PyromancerConverterPlugin.ShouldConvert(__instance)) return;
                VfxRecolor.Convert(__instance.gameObject, $"aoe:{__instance._spell}");
            }
            catch (Exception ex) { PyromancerConverterPlugin.Log?.LogError($"[Patch_AreaOfEffect_Start_Vfx] {ex}"); }
        }
    }

    /// <summary>[2026-10-06] VFX pass 1c — SkillShot projectiles (e.g. Frost Javelin) that override Start.</summary>
    [HarmonyPatch(typeof(SkillShot), "Start")]
    public static class Patch_SkillShot_Start_Vfx
    {
        public static void Postfix(SkillShot __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!PyromancerConverterPlugin.ShouldConvert(__instance)) return;
                VfxRecolor.Convert(__instance.gameObject, $"skillshot:{__instance._spell}");
            }
            catch (Exception ex) { PyromancerConverterPlugin.Log?.LogError($"[Patch_SkillShot_Start_Vfx] {ex}"); }
        }
    }

    /// <summary>
    /// [2026-10-06] VFX pass 2 — VFX that the prefab instantiates as a separate object (cast FX, blast FX) rather
    /// than authoring as a child. Recolor the returned instance right after it spawns.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "SpawnVFXAtOffset")]
    public static class Patch_BaseSpellLibrary_SpawnVFXAtOffset
    {
        public static void Postfix(BaseSpellLibrary __instance, GameObject __result)
        {
            try
            {
                if (__result == null) return;
                if (!PyromancerConverterPlugin.ShouldConvert(__instance)) return;
                VfxRecolor.Convert(__result, $"spawned:{__instance._spell}");
            }
            catch (Exception ex) { PyromancerConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_SpawnVFXAtOffset] {ex}"); }
        }
    }

    /// <summary>
    /// [2026-10-06] Phase 3 — runtime VFX recolor. The game's effects use the All In 1 VFX Toolkit, whose shader
    /// tints a (grayscale) texture by the ParticleSystem vertex color; there is no runtime color component
    /// (AllIn1VfxComponent is editor-only). So the reliable levers are the ParticleSystem color modules
    /// (<c>main.startColor</c>, <c>colorOverLifetime.color</c>) plus SpriteRenderer / TrailRenderer /
    /// LineRenderer / Light colors. Cold hues (cyan..violet) are remapped onto a fire range (red-orange..amber)
    /// while preserving saturation, brightness and alpha, so gradients and fades survive. The shift is guarded by
    /// a cold test, so it is idempotent and never touches already-warm colors.
    /// </summary>
    internal static class VfxRecolor
    {
        private const float ColdHueMin = 0.45f;   // cyan
        private const float ColdHueMax = 0.78f;   // violet-blue
        private const float FireHueMin = 0.02f;   // red-orange
        private const float FireHueMax = 0.12f;   // amber
        private const float FireMinValue = 0.80f; // brighten fire a touch
        private const float FireMinSaturation = 0.60f;

        private static readonly HashSet<string> LoggedVfx = new HashSet<string>();

        internal static bool IsCold(Color c)
        {
            if (c.a <= 0.01f) return false;
            // Blue dominant over red, with enough blue to be a real tint (skips grays/whites/blacks).
            return c.b > c.r + 0.03f && c.b > 0.15f;
        }

        internal static Color ToFire(Color c)
        {
            if (!IsCold(c)) return c;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float t = Mathf.InverseLerp(ColdHueMin, ColdHueMax, h);
            float nh = Mathf.Lerp(FireHueMin, FireHueMax, t);
            float ns = Mathf.Clamp(s, FireMinSaturation, 1f);
            float nv = Mathf.Max(v, FireMinValue);
            Color fire = Color.HSVToRGB(nh, ns, nv);
            fire.a = c.a;
            return fire;
        }

        internal static void Convert(GameObject root, string tag)
        {
            if (root == null) return;
            if (!PyromancerConverterPlugin.Enabled.Value || !PyromancerConverterPlugin.ConvertVfx.Value) return;

            int changed = 0;
            try
            {
                foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (RecolorParticleSystem(ps, tag)) changed++;
                }

                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null) continue;

                    SpriteRenderer sr = r.TryCast<SpriteRenderer>();
                    if (sr != null)
                    {
                        if (RecolorColor(sr, sr.color, c => sr.color = c, tag, "Sprite")) changed++;
                        continue;
                    }

                    TrailRenderer tr = r.TryCast<TrailRenderer>();
                    if (tr != null)
                    {
                        bool a = RecolorColor(tr, tr.startColor, c => tr.startColor = c, tag, "Trail.start");
                        bool b = RecolorColor(tr, tr.endColor, c => tr.endColor = c, tag, "Trail.end");
                        if (a || b) changed++;
                        continue;
                    }

                    LineRenderer lr = r.TryCast<LineRenderer>();
                    if (lr != null)
                    {
                        bool a = RecolorColor(lr, lr.startColor, c => lr.startColor = c, tag, "Line.start");
                        bool b = RecolorColor(lr, lr.endColor, c => lr.endColor = c, tag, "Line.end");
                        if (a || b) changed++;
                        continue;
                    }
                }

                foreach (Light l in root.GetComponentsInChildren<Light>(true))
                {
                    if (l == null) continue;
                    if (RecolorColor(l, l.color, c => l.color = c, tag, "Light")) changed++;
                }
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[vfx] {tag} {ex}");
            }

            if (changed > 0 && PyromancerConverterPlugin.LogVfx.Value)
            {
                PyromancerConverterPlugin.Log.LogInfo($"[vfx] {tag}: recolored {changed} element(s) on '{root.name}'");
            }
        }

        private static bool RecolorColor(UnityEngine.Object owner, Color before, Action<Color> set, string tag, string kind)
        {
            Color after = ToFire(before);
            if (after == before) return false;
            set(after);
            LogVfx(tag, kind, owner != null ? owner.name : "?", before, after);
            return true;
        }

        private static bool RecolorParticleSystem(ParticleSystem ps, string tag)
        {
            bool changed = false;

            var main = ps.main;
            ParticleSystem.MinMaxGradient sc = main.startColor;
            // [2026-10-06] The interop exposes MinMaxGradient.color/gradient as read-only and
            // ColorOverLifetimeModule.color as write-only, so the only safe lever is main.startColor:
            // read it, build a shifted copy through a constructor, and assign that back. colorOverLifetime
            // is usually just an alpha fade (white RGB), so shifting startColor recolors the effect while
            // preserving the fade.
            ParticleSystem.MinMaxGradient shifted;
            if (TryShiftGradient(sc, out shifted))
            {
                main.startColor = shifted;
                changed = true;
            }

            if (changed) LogVfx(tag, "ParticleSystem", ps != null ? ps.name : "?", new Color(-1f, -1f, -1f, -1f), Color.white);
            return changed;
        }

        private static bool TryShiftGradient(ParticleSystem.MinMaxGradient g, out ParticleSystem.MinMaxGradient result)
        {
            result = g;
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color:
                    {
                        Color c = g.color, n = ToFire(c);
                        if (n == c) return false;
                        result = new ParticleSystem.MinMaxGradient(n);
                        return true;
                    }
                case ParticleSystemGradientMode.TwoColors:
                    {
                        Color a = g.colorMin, b = g.colorMax;
                        Color na = ToFire(a), nb = ToFire(b);
                        if (na == a && nb == b) return false;
                        result = new ParticleSystem.MinMaxGradient(na, nb);
                        return true;
                    }
                case ParticleSystemGradientMode.Gradient:
                case ParticleSystemGradientMode.RandomColor:
                    {
                        Gradient shiftedG;
                        if (!TryShiftGradientCopy(g.gradient, out shiftedG)) return false;
                        result = new ParticleSystem.MinMaxGradient(shiftedG);
                        return true;
                    }
                case ParticleSystemGradientMode.TwoGradients:
                    {
                        Gradient a, b;
                        bool ca = TryShiftGradientCopy(g.gradientMin, out a);
                        bool cb = TryShiftGradientCopy(g.gradientMax, out b);
                        if (!ca && !cb) return false;
                        result = new ParticleSystem.MinMaxGradient(ca ? a : g.gradientMin, cb ? b : g.gradientMax);
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Copies a gradient (so the authored asset is never mutated) and shifts every color key cold -> fire.
        /// Returns false when nothing was cold.
        /// </summary>
        private static bool TryShiftGradientCopy(Gradient g, out Gradient result)
        {
            result = g;
            if (g == null) return false;

            var keys = g.colorKeys;
            if (keys == null || keys.Length == 0) return false;

            var alpha = g.alphaKeys;
            bool changed = false;
            for (int i = 0; i < keys.Length; i++)
            {
                Color c = keys[i].color;
                Color n = ToFire(c);
                if (n != c)
                {
                    keys[i] = new GradientColorKey(n, keys[i].time);
                    changed = true;
                }
            }
            if (!changed) return false;

            Gradient copy = new Gradient();
            copy.SetKeys(keys, alpha);
            copy.mode = g.mode;
            result = copy;
            return true;
        }

        private static void LogVfx(string tag, string kind, string name, Color before, Color after)
        {
            if (!PyromancerConverterPlugin.LogVfx.Value) return;
            string key = tag + "|" + kind + "|" + name;
            if (!LoggedVfx.Add(key)) return;
            PyromancerConverterPlugin.Log.LogInfo($"[vfx] {tag}: {kind} '{name}' {Hex(before)} -> {Hex(after)}");
        }

        private static string Hex(Color c)
        {
            if (c.r < 0f) return "(particles)";
            return "#" + Mathf.RoundToInt(c.r * 255f).ToString("X2")
                       + Mathf.RoundToInt(c.g * 255f).ToString("X2")
                       + Mathf.RoundToInt(c.b * 255f).ToString("X2")
                       + Mathf.RoundToInt(c.a * 255f).ToString("X2");
        }
    }

    /// <summary>Remaps Magician (<c>Class.Magician</c>) skill-tree stat improvements from Chill/Ice to
    /// Burning/Fire. The mapping is idempotent (no target is itself a key), so repeated calls are safe.</summary>
    internal static class TreeConverter
    {
        private static readonly Dictionary<Runes.Stat, Runes.Stat> Map = new Dictionary<Runes.Stat, Runes.Stat>
        {
            { Runes.Stat.MaxChill, Runes.Stat.MaxBurning },
            { Runes.Stat.ChilledDuration, Runes.Stat.BurningDuration },
            { Runes.Stat.ChilledPower, Runes.Stat.BurningDamage },
            { Runes.Stat.DamageVsChilled, Runes.Stat.DamageVsBurning },
            { Runes.Stat.CriticalChanceVsChilled, Runes.Stat.CriticalChanceVsBurning },
            { Runes.Stat.CriticalDamageVsChilled, Runes.Stat.CriticalDamageVsBurning },
            { Runes.Stat.IceDamage, Runes.Stat.FireDamage },
            { Runes.Stat.IcePiercing, Runes.Stat.FirePiercing },
            { Runes.Stat.IceResistance, Runes.Stat.FireResistance },
            { Runes.Stat.MaxFrostbound, Runes.Stat.MaxKindled },
            { Runes.Stat.FrostboundDuration, Runes.Stat.KindledDuration },
        };

        internal static void ConvertAllLoaded()
        {
            try
            {
                foreach (SkillTreeNode node in Resources.FindObjectsOfTypeAll<SkillTreeNode>())
                {
                    ConvertNode(node);
                }
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[tree] enumerate {ex}");
            }
        }

        internal static void ConvertNode(SkillTreeNode node)
        {
            try
            {
                if (node == null) return;
                if (node.Class != Class.Magician) return;

                Il2CppSystem.Collections.Generic.List<SkillTreeStatImprovement> list = node.StatImprovements;
                if (list == null || list.Count == 0) return;

                IntPtr elemClass = Il2CppClassPointerStore<SkillTreeStatImprovement>.NativeClassPtr;
                IntPtr listPtr = ((Il2CppObjectBase)list).Pointer;

                for (int i = 0; i < list.Count; i++)
                {
                    Runes.Stat current = list[i].Stat;
                    if (!Map.TryGetValue(current, out Runes.Stat to)) continue;

                    Marshal.WriteInt32(
                        RawStruct.Elem(listPtr, elemClass, i) + RawStruct.Off(elemClass, "Stat"),
                        (int)to);

                    if (PyromancerConverterPlugin.LogSwaps.Value)
                    {
                        PyromancerConverterPlugin.Log.LogInfo($"[tree] {node.NodeId}[{i}] {current} -> {to}");
                    }
                }
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[tree] node {ex}");
            }
        }
    }

    /// <summary>Approximate cosmetic wording swap. Kept deliberately small: it only touches the four cold
    /// adjectives so the converted node/spell text reads as fire/burn.</summary>
    internal static class TooltipText
    {
        internal static string Convert(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (!PyromancerConverterPlugin.Enabled.Value || !PyromancerConverterPlugin.ConvertTooltips.Value) return s;

            // Order matters: plural/participle before the root, Frostbound before Frost.
            s = s.Replace("Chilled", "Burning").Replace("Chill", "Burn");
            s = s.Replace("Frostbound", "Kindled").Replace("Frost", "Fire").Replace("Ice", "Fire");
            return s;
        }
    }

    /// <summary>Direct native-memory access to an IL2CPP list of blittable structs. Needed because the game's
    /// <c>SkillTreeStatImprovement</c> is a value type and writing the <c>Stat</c> enum through the interop
    /// list indexer copies rather than mutates the element. Copied from the proven NecroSkelly helpers.</summary>
    internal static class RawStruct
    {
        private const int ArrayHeader = 32;

        private static IntPtr Field(IntPtr klass, string name)
        {
            IntPtr field = IL2CPP.il2cpp_class_get_field_from_name(klass, name);
            if (field == IntPtr.Zero) throw new MissingFieldException(name);
            return field;
        }

        public static IntPtr Elem(IntPtr list, IntPtr elemKlass, int k)
        {
            IntPtr klass = IL2CPP.il2cpp_object_get_class(list);
            int size = Marshal.ReadInt32(list + (int)IL2CPP.il2cpp_field_get_offset(Field(klass, "_size")));
            if (k < 0 || k >= size) throw new ArgumentOutOfRangeException(nameof(k), k + " of " + size);

            IntPtr items = Marshal.ReadIntPtr(list + (int)IL2CPP.il2cpp_field_get_offset(Field(klass, "_items")));
            uint align = 0u;
            int valueSize = IL2CPP.il2cpp_class_value_size(elemKlass, ref align);
            return items + ArrayHeader + k * valueSize;
        }

        public static int Off(IntPtr elemKlass, string field)
        {
            return (int)(IL2CPP.il2cpp_field_get_offset(Field(elemKlass, field)) - 16);
        }
    }
}
