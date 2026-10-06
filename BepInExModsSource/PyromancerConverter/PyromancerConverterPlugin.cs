using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
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

            // [2026-10-06 16:05] Inject the behaviour that drains the deferred VFX texture-probe queue one
            // texture per frame, so the first cast of a converted spell no longer stalls. Previously there was
            // no behaviour at all (the texture work ran inline on the first cast).
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<VfxPrewarmBehaviour>();
                var prewarmGo = new GameObject("PyromancerConverter.VfxPrewarm");
                UnityEngine.Object.DontDestroyOnLoad(prewarmGo);
                prewarmGo.AddComponent<VfxPrewarmBehaviour>();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[{NAME}] Could not start VFX prewarm behaviour: {ex.Message}");
            }

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
        private const float FireHueMin = 0.00f;   // pure red (user wanted more red)
        private const float FireHueMax = 0.045f;  // orange-red
        private const float FireMinValue = 0.80f; // brighten fire a touch
        private const float FireMinSaturation = 0.75f;

        private static readonly HashSet<string> LoggedVfx = new HashSet<string>();
        private static readonly HashSet<string> LoggedMats = new HashSet<string>();
        // [2026-10-06] Cache of hue-shifted texture copies keyed by source instance id. A null value means the
        // source texture was already warm (nothing to shift), so we don't re-scan it.
        private static readonly Dictionary<int, Texture2D> ShiftedTextures = new Dictionary<int, Texture2D>();
        private static readonly string[] TexProps = { "_MainTex", "_BaseMap", "_Texture", "_MainTexture", "_EmissionMap" };

        // [2026-10-06] The ice colour is baked into the renderer material (particle startColor logs as #FFFFFF),
        // so we tint every common colour property the shader exposes. Names below were collected from the actual
        // shader property dumps: URP Particles/Unlit uses _BaseColor/_Color; Shader Graph Basic_FadeMultiply uses
        // _Main_Color; Trail_01/Trail_02 use _Color_HDR_1/_Color_HDR_2 (the javelin projectile trail);
        // Dissolve/Outline shaders use _Main_Color/_edge_color/_Outline_Color; FF_Fire uses _Emission.
        private static readonly string[] ColorProps =
        {
            "_Color", "_TintColor", "_BaseColor", "_MainColor", "_Main_Color",
            "_EmissionColor", "_Emission", "_emission",
            "_GlowColor", "_RimColor", "_Tint", "_MainTint", "_OverlayColor",
            "_Color_HDR_1", "_Color_HDR_2", "_color_edge",
            "_edge_color", "_edge_color_2", "_Outline_Color", "_OutlineColor",
            "_Color1", "_Color2", "_Color_Tint",
            "_AllIn1VfxColor", "_AllIn1VfxMainColor", "_AllIn1VfxTintColor",
            "_All1VfxColor", "_All1VfxMainColor", "_AllIn1VfxEmissionColor",
        };

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
                        if (TintRendererMaterial(r, tag, "Sprite")) changed++;
                        continue;
                    }

                    TrailRenderer tr = r.TryCast<TrailRenderer>();
                    if (tr != null)
                    {
                        bool a = RecolorColor(tr, tr.startColor, c => tr.startColor = c, tag, "Trail.start");
                        bool b = RecolorColor(tr, tr.endColor, c => tr.endColor = c, tag, "Trail.end");
                        if (a || b) changed++;
                        if (TintRendererMaterial(r, tag, "Trail")) changed++;
                        continue;
                    }

                    LineRenderer lr = r.TryCast<LineRenderer>();
                    if (lr != null)
                    {
                        bool a = RecolorColor(lr, lr.startColor, c => lr.startColor = c, tag, "Line.start");
                        bool b = RecolorColor(lr, lr.endColor, c => lr.endColor = c, tag, "Line.end");
                        if (a || b) changed++;
                        if (TintRendererMaterial(r, tag, "Line")) changed++;
                        continue;
                    }

                    // [2026-10-06] Particle startColor logs as #FFFFFF for almost every ice emitter, so the
                    // actual blue is in the renderer material. Tint that as well.
                    ParticleSystemRenderer pr = r.TryCast<ParticleSystemRenderer>();
                    if (pr != null)
                    {
                        if (TintRendererMaterial(pr, tag, "Particle")) changed++;
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
            bool changed = after != before;
            if (changed) set(after);
            // [2026-10-06] Log the inventory even when unchanged (e.g. a white SpriteRenderer tinted by its
            // material), so the recon shows where each color actually lives.
            LogVfx(tag, kind, owner != null ? owner.name : "?", before, after);
            return changed;
        }

        /// <summary>
        /// [2026-10-06] Tint the material(s) a renderer draws with. This is where the ice colour actually lives
        /// (particle startColor is white). ParticleSystemRenderer also has a separate trailMaterial.
        /// </summary>
        private static bool TintRendererMaterial(Renderer r, string tag, string kind)
        {
            bool changed = false;
            try
            {
                Material m = null;
                try { m = r.material; } catch { }
                if (m != null) changed |= TintMaterial(m, tag, kind + ":" + r.name);

                ParticleSystemRenderer pr = r.TryCast<ParticleSystemRenderer>();
                if (pr != null)
                {
                    Material tm = null;
                    try { tm = pr.trailMaterial; } catch { }
                    if (tm != null) changed |= TintMaterial(tm, tag, kind + "Trail:" + r.name);
                }
            }
            catch (Exception ex) { PyromancerConverterPlugin.Log?.LogError($"[vfx] mat {tag} {ex}"); }
            return changed;
        }

        private static bool TintMaterial(Material m, string tag, string owner)
        {
            if (m == null) return false;

            string shaderName = "?";
            try { shaderName = m.shader != null ? m.shader.name : "?"; } catch { }
            string texName = "?";
            try { Texture t = m.mainTexture; if (t != null) texName = t.name; } catch { }
            LogMaterialOnce(tag, m, owner, shaderName, texName);

            bool changed = false;
            foreach (string prop in ColorProps)
            {
                bool has = false;
                try { has = m.HasProperty(prop); } catch { }
                if (!has) continue;

                Color before;
                try { before = m.GetColor(prop); } catch { continue; }
                Color after = ToFire(before);
                if (after != before)
                {
                    try { m.SetColor(prop, after); } catch { }
                    changed = true;
                }
                LogVfx(tag, "Mat." + prop, owner, before, after);
            }

            // [2026-10-06] Some ice materials carry a white tint and a blue texture (e.g. "Fire Anim - blue"),
            // so also hue-shift the texture itself.
            changed |= ShiftMaterialTextures(m, tag, owner);
            return changed;
        }

        private static bool ShiftMaterialTextures(Material m, string tag, string owner)
        {
            bool changed = false;
            foreach (string prop in TexProps)
            {
                bool has = false;
                try { has = m.HasProperty(prop); } catch { }
                if (!has) continue;

                Texture t = null;
                try { t = m.GetTexture(prop); } catch { }
                Texture2D tex = t != null ? t.TryCast<Texture2D>() : null;
                if (tex == null) continue;

                Texture2D shifted = GetShiftedTexture(tex, tag);
                if (shifted == null) continue;

                try { m.SetTexture(prop, shifted); changed = true; } catch { }
                LogVfx(tag, "Tex." + prop, owner, Color.white, Color.white);
            }
            return changed;
        }

        /* [2026-10-06 16:05] ===== DEFERRED TEXTURE WORK (first-cast hitch fix) =====
           The old design ran Graphics.Blit + ReadPixels + GetPixels + a per-pixel HSV scan INLINE, the
           first time each texture was seen - i.e. during the first cast of the spell - which stalled that
           cast. The log proved it never actually changed any texture (there were zero "shifted texture"
           lines; the ice blue lives in the MATERIAL tint, not the texture), so it was pure wasted cost:
           it probed every texture once and cached "nothing to do". Now GetShiftedTexture enqueues the probe
           and returns null immediately; VfxPrewarmBehaviour runs ONE texture per frame. This is invisible
           because the fire recolour is applied from the material colour synchronously - only the (usually
           no-op) texture copy is deferred - and later casts hit the cache.

           Obsolete inline body (kept per repo rule): it computed and cached the shifted texture here in one go:
             int id; try { id = src.GetInstanceID(); } catch { return null; }
             if (ShiftedTextures.TryGetValue(id, out Texture2D cached)) return cached;   // cache check now in GetShiftedTexture
             Texture2D result = null; try { [Blit + ReadPixels + GetPixels + HSV loop + SetPixels/Apply] } ... */
        private struct PendingTex { public int Id; public Texture2D Src; public string Tag; }
        private static readonly Queue<PendingTex> PendingTextures = new Queue<PendingTex>();
        private static readonly HashSet<int> PendingTextureIds = new HashSet<int>();

        internal static int ProcessOnePendingTexture()
        {
            if (PendingTextures.Count == 0) return 0;
            PendingTex job = PendingTextures.Dequeue();
            PendingTextureIds.Remove(job.Id);
            Texture2D result = null;
            try
            {
                if (job.Src != null) result = ComputeShiftedTexture(job.Src, job.Tag);
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[vfx] tex {job.Tag} {ex.Message}");
            }
            ShiftedTextures[job.Id] = result; // null == already warm / nothing to do; cached so we never re-probe
            return 1;
        }

        /// <summary>
        /// Returns a hue-shifted copy of <paramref name="src"/> (cold pixels -> fire), or null when the texture is
        /// already warm. If the texture has not been probed yet the work is queued for the prewarm behaviour and
        /// null is returned now, so the calling cast never blocks. Results are cached per source texture.
        /// </summary>
        private static Texture2D GetShiftedTexture(Texture2D src, string tag)
        {
            int id;
            try { id = src.GetInstanceID(); } catch { return null; }
            if (ShiftedTextures.TryGetValue(id, out Texture2D cached)) return cached;

            if (PendingTextureIds.Add(id))
            {
                PendingTextures.Enqueue(new PendingTex { Id = id, Src = src, Tag = tag });
            }
            return null; // not ready yet; the next cast will read the cached result
        }

        private static Texture2D ComputeShiftedTexture(Texture2D src, string tag)
        {
            int id;
            try { id = src.GetInstanceID(); } catch { return null; }

            Texture2D result = null;
            try
            {
                int w = src.width, h = src.height;
                if (w <= 0 || h <= 0 || w > 4096 || h > 4096)
                {
                    ShiftedTextures[id] = null;
                    return null;
                }

                // Blit through a RenderTexture so we can read textures that are not CPU-readable.
                RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
                copy.Apply();

                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                Color[] px = copy.GetPixels();
                bool any = false;
                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    Color n = ToFire(c);
                    if (n != c) { px[i] = n; any = true; }
                }

                if (!any)
                {
                    UnityEngine.Object.Destroy(copy);
                    ShiftedTextures[id] = null;
                    return null;
                }

                copy.SetPixels(px);
                copy.Apply();
                ShiftedTextures[id] = copy;
                result = copy;

                if (PyromancerConverterPlugin.LogVfx.Value)
                    PyromancerConverterPlugin.Log.LogInfo($"[vfx] {tag}: shifted texture '{src.name}' ({w}x{h})");
            }
            catch (Exception ex)
            {
                PyromancerConverterPlugin.Log?.LogError($"[vfx] tex {tag} {ex.Message}");
                ShiftedTextures[id] = null;
            }
            return result;
        }

        private static void LogMaterialOnce(string tag, Material m, string owner, string shaderName, string texName)
        {
            if (!PyromancerConverterPlugin.LogVfx.Value) return;
            string key = tag + "|mat|" + owner + "|" + m.name;
            if (!LoggedMats.Add(key)) return;

            string props = "";
            try
            {
                int pc = m.shader != null ? m.shader.GetPropertyCount() : 0;
                var names = new List<string>();
                for (int i = 0; i < pc; i++)
                {
                    try { names.Add(m.shader.GetPropertyName(i)); } catch { }
                }
                props = string.Join(",", names);
            }
            catch { }

            PyromancerConverterPlugin.Log.LogInfo(
                $"[vfx] {tag}: material '{m.name}' shader '{shaderName}' tex '{texName}' props [{props}]");
        }

        private static bool RecolorParticleSystem(ParticleSystem ps, string tag)
        {
            // [2026-10-06] The interop exposes MinMaxGradient.color/gradient as read-only and
            // ColorOverLifetimeModule.color as write-only, so the only safe lever is main.startColor:
            // read it, build a shifted copy through a constructor, and assign that back. colorOverLifetime
            // is usually just an alpha fade (white RGB), so shifting startColor recolors the effect while
            // preserving the fade.
            var main = ps.main;
            ParticleSystem.MinMaxGradient sc = main.startColor;

            ParticleSystem.MinMaxGradient shifted;
            bool changed = TryShiftGradient(sc, out shifted);
            if (changed) main.startColor = shifted;

            // [2026-10-06] Log the inventory even when nothing changed, so a blue effect whose color lives in
            // the material/texture (not startColor) is still visible in the recon.
            Color before = RepresentativeColor(sc);
            Color after = changed ? RepresentativeColor(shifted) : before;
            LogVfx(tag, "ParticleSystem", ps != null ? ps.name : "?", before, after);
            return changed;
        }

        private static Color RepresentativeColor(ParticleSystem.MinMaxGradient g)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return g.color;
                case ParticleSystemGradientMode.TwoColors:
                    return g.colorMin;
                case ParticleSystemGradientMode.Gradient:
                case ParticleSystemGradientMode.RandomColor:
                    return g.gradient != null ? g.gradient.Evaluate(0.5f) : Color.white;
                case ParticleSystemGradientMode.TwoGradients:
                    return g.gradientMin != null ? g.gradientMin.Evaluate(0.5f) : Color.white;
            }
            return Color.white;
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

    /// <summary>
    /// [2026-10-06 16:05] Runs the deferred VFX texture probes, ONE per frame, so the first cast of a spell
    /// never stalls on Graphics.Blit/ReadPixels/GetPixels. One texture per frame is invisible because the
    /// fire recolour is applied from the material colour synchronously; only the (usually no-op) texture copy
    /// is deferred. Injected at load, mirroring the modpack's injected behaviours.
    /// </summary>
    public class VfxPrewarmBehaviour : MonoBehaviour
    {
        public VfxPrewarmBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            try { VfxRecolor.ProcessOnePendingTexture(); }
            catch { /* prewarm is best-effort */ }
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
