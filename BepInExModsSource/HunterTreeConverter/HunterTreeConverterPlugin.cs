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

namespace HunterTreeConverter
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that re-skins the Human / hunter skill tree (the branch that leads
    /// down to Hunter's Trap, Hail of Arrows and Aimed Shot) away from Poison/Bleed and onto Burning/Chill:
    ///   * Skill-tree nodes that grant a Poison stat grant its Burning equivalent (MaxPoison -> MaxBurning,
    ///     PoisonDamage -> BurningDamage, ...).
    ///   * Skill-tree nodes that grant a Bleed stat grant its Chill equivalent (MaxBleeding -> MaxChill,
    ///     BleedDamage -> ChilledPower, ...).
    ///   * Node/spell tooltip wording follows: "Apply Poison" reads "Apply Burning", "Bleed" reads "Chill".
    ///
    /// This is the poison/bleed counterpart of the ice-&gt;fire mod PyromancerConverter. It rewrites the skill tree,
    /// the tooltips, the live status stacks the hunter abilities land on enemies (hunter Poison -&gt; Burning, hunter
    /// Bleeding -&gt; Chill), the hunter damage type (Thrust -&gt; Fire) and the hunter poison pool (re-spawned as the
    /// game's existing BurningPit / fire-pool VFX - no recolouring).
    /// [2026-10-16] Obsolete (preserved for reference): "Damage types and VFX remain out of scope." is superseded -
    /// the user asked for the damage type (Thrust -&gt; Fire) and for the poison pool to reuse the EXISTING fire-pool
    /// VFX instead of the poison one.
    /// [2026-10-16 14:45] Obsolete (preserved for reference): this class used to document "scoped (per the user's
    /// choice) to the SKILL TREE + TOOLTIPS only: it does NOT rewrite live status stacks, damage types or VFX." That
    /// scope is superseded: the user confirmed enemies still received Poison/Bleed and asked for live-status
    /// conversion too, scoped to hunter sources only.
    ///
    /// Design + enum reference: modding/docs/pyromancer/00-plan.md (same skill-tree model).
    ///
    /// [2026-10-16] Skill-tree node edits are server-authoritative NetworkList data on the host; run the SAME
    /// config on host and client in co-op.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class HunterTreeConverterPlugin : BasePlugin
    {
        public const string GUID = "com.custom.huntertreeconverter";
        public const string NAME = "Hunter Tree Converter";
        // [2026-10-16 13:20] Superseded by 0.4.0: the build now also re-types the hunter spells to Fire at the
        // definition/cast level (so Enflamed 4pc "Everburn" and other "cast a Fire spell" triggers fire) and remaps
        // the hunter consume-side stack reads. Kept for history only.
        // public const string VERSION = "0.3.0"; // [2026-10-16] bumped from 0.2.0 for damage-type (Thrust->Fire) + fire-pool VFX
        // [2026-10-16] Was: public const string VERSION = "0.4.0"; bumped to 0.4.1 for the Thrust -> Fire
        // wording swap on skill-tree nodes (the stat was already converted, the node text still read "Thrust").
        // public const string VERSION = "0.4.0"; // [2026-10-16] bumped from 0.3.0 for fire-typed hunter spells (fire-spell triggers) + consume-stack remap
        public const string VERSION = "0.4.1";

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ConvertTree;
        public static ConfigEntry<bool> ConvertTooltips;
        public static ConfigEntry<bool> ConvertStatus;
        public static ConfigEntry<bool> ConvertDamage;
        public static ConfigEntry<bool> ConvertVfx;
        public static ConfigEntry<bool> LogSwaps;
        public static ConfigEntry<TreeMatchMode> MatchMode;

        /// <summary>
        /// [2026-10-16] Which skill-tree nodes this mod is allowed to rewrite. The user asked for "the whole
        /// Human tree" (the hunter branch toward Trap / Hail Arrow / Aimed Shot). Node tags are authored data we
        /// cannot read offline, so the filter is configurable and every matched node is logged for verification.
        ///   * Either     - Race Human OR Class Shadow (default: most forgiving for "the Human hunter tree").
        ///   * RaceHuman  - only nodes tagged Race.Human.
        ///   * ClassShadow- only nodes tagged Class.Shadow (Hunter's Trap / Hail of Arrows / Aimed Shot live here
        ///                  per ScatterTrapTuner's "Shadow skill-tree" notes).
        ///   * All        - every node regardless of tree (last-resort debug).
        /// </summary>
        public enum TreeMatchMode { Either, RaceHuman, ClassShadow, All }

        public override void Load()
        {
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false nothing is converted. (Default: true)");

            ConvertTree = Config.Bind("General", "ConvertTree", true,
                "Remap skill-tree Poison stats (MaxPoison, PoisonDamage, DamageVsPoison, ...) to their Burning " +
                "equivalents, and Bleed stats (MaxBleeding, BleedDamage, ...) to their Chill equivalents. (Default: true)");

            ConvertTooltips = Config.Bind("General", "ConvertTooltips", true,
                "Rewrite Poison/Bleed wording to Burn/Chill in skill-node and spell tooltips (\"Apply Poison\" -> " +
                "\"Apply Burning\", \"Bleed\" -> \"Chill\"). (Default: true)");

            ConvertStatus = Config.Bind("General", "ConvertStatus", true,
                "Convert the live status stacks the hunter abilities land on enemies: hunter Poison -> Burning, " +
                "hunter Bleeding -> Chill. Scoped to hunter sources (Hunter's Trap / Hail of Arrows / Aimed Shot + " +
                "their poison pool, mists and sub-traps). (Default: true)");

            ConvertDamage = Config.Bind("General", "ConvertDamage", true,
                "Convert the hunter's base damage type from Thrust to Fire (the arrows/traps/shots pierce for Thrust; " +
                "rewritten so the hunter theme reads as burn). Scoped to hunter sources. (Default: true)");

            ConvertVfx = Config.Bind("General", "ConvertVfx", true,
                "Make the hunter's poison pool use the game's EXISTING fire pool (BurningPit) instead of the poison " +
                "pool - i.e. call PoisonPoolPrefab.ConvertToBurningPit, no recolouring of other VFX. (Default: true)");

            MatchMode = Config.Bind("General", "MatchMode", TreeMatchMode.Either,
                "Which skill-tree nodes to convert: 'Either' (Race Human OR Class Shadow), 'RaceHuman', " +
                "'ClassShadow', or 'All'. (Default: Either)");

            LogSwaps = Config.Bind("Diagnostics", "LogSwaps", true,
                "Log every matched node (id/class/race/stats) and every stat conversion to the BepInEx console. " +
                "Turn off once the conversion set is verified. (Default: true)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_SkillTree_LoadSkillTreeInitial));
            PatchOrLog(harmony, typeof(Patch_SkillTree_LoadNodeEffects));
            PatchOrLog(harmony, typeof(Patch_SkillTree_ApplySingleNodeEffect));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedName));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedEffects));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_SendDamageToTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_SpawnElementalPool));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetStacksFromTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetStackSourceFromTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_RemoveStacksOnTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_CleanseStacksOnTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_CollapseStacksOnTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetLastConsumedAmount));
            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_Passives_RegisterSpellCast));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}, ConvertTree: {ConvertTree.Value}, ConvertTooltips: {ConvertTooltips.Value}, " +
                        $"ConvertStatus: {ConvertStatus.Value}, ConvertDamage: {ConvertDamage.Value}, ConvertVfx: {ConvertVfx.Value}, " +
                        $"MatchMode: {MatchMode.Value}, LogSwaps: {LogSwaps.Value}");
            Log.LogInfo("Poison -> Burning, Bleed -> Chill, Thrust -> Fire (tree + tooltips + hunter live status + damage + fire pool).");
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

        internal static bool IsActive => Enabled != null && Enabled.Value;

        /// <summary>
        /// [2026-10-16] Spells/prefabs whose Poison and Bleed output this mod rewrites to Burning and Chill. Scoped
        /// (per the user's choice) to the hunter tree: Hunter's Trap, Hail of Arrows, Aimed Shot plus their derived
        /// prefabs (poison pool, poison/bleed mists, scatter sub-traps). Contagion (Spell.Contagion = 281) and monster
        /// poison/bleed are deliberately left alone, so a poison-stack tuner on another spell (e.g. ContagionTuner)
        /// keeps working untouched.
        /// </summary>
        internal static readonly HashSet<Spell> HunterSpells = new HashSet<Spell>
        {
            Spell.HuntersTrap,      // 105 - Hunter's Trap (bleed mist + scatter sub-traps)
            Spell.HailOfArrows,     // 106 - Hail of Arrows (poison)
            Spell.AimedShot,        // 107 - Aimed Shot (poison)
            Spell.PoisonPool,       // 125 - the poison pool the Hail of Arrows arrows leave behind
            Spell.PoisonMistPlayer, // 157 - the player poison/bleed mist
        };

        // [2026-10-16] Status swaps/skips already reported once (AddStacksToTarget fires every tick of a pool/mist);
        // keeps the log readable. Keyed by source + effect (+ ">skip").
        internal static readonly HashSet<string> LoggedStatus = new HashSet<string>();

        // [2026-10-16] Damage swaps/skips and pool conversions already reported once (SendDamageToTarget fires per
        // hit/tick, SpawnElementalPool per arrow); keeps the log readable.
        internal static readonly HashSet<string> LoggedDamage = new HashSet<string>();
        internal static readonly HashSet<Spell> LoggedPool = new HashSet<Spell>();

        // [2026-10-16] Hunter spell definitions re-typed to Fire (ApplyDefinition) and cast components stamped to
        // Fire (RegisterSpellCast), each reported once.
        internal static readonly HashSet<Spell> LoggedDefinition = new HashSet<Spell>();
        internal static readonly HashSet<Spell> LoggedCastStamp = new HashSet<Spell>();

        /// <summary>
        /// True when this prefab is a hunter source whose Poison/Bleed we rewrite. Derived pool/mist/sub-trap prefabs
        /// either carry their own Spell id (PoisonPool / PoisonMistPlayer) or inherit the parent spell, so the Spell
        /// set catches them; a prefab-type-name fallback catches any sub-prefab whose <c>_spell</c> is unset.
        /// </summary>
        internal static bool ShouldConvert(BaseSpellLibrary prefab)
        {
            if (prefab == null || prefab.Pointer == IntPtr.Zero) return false;
            if (HunterSpells.Contains(prefab._spell)) return true;

            // Fallback for derived sub-prefabs with an unset/None _spell: match by the hunter prefab type name.
            try
            {
                string type = prefab.GetIl2CppType().Name;
                return type != null && (
                    type.StartsWith("HuntersTrap") || type.StartsWith("HailOfArrows") ||
                    type.StartsWith("AimedShot") || type.StartsWith("PoisonPool") || type.StartsWith("PoisonMist"));
            }
            catch { return false; }
        }

        /// <summary>Log a converted status swap once per (source spell, from-effect).</summary>
        internal static void LogStatusSwap(BaseSpellLibrary prefab, StackingEffect from, StackingEffect to)
        {
            if (!LogSwaps.Value) return;
            Spell spell = prefab != null ? prefab._spell : Spell.None;
            if (LoggedStatus.Add(spell + ">" + from))
            {
                Log.LogInfo($"[status] {spell}: {from} -> {to}");
            }
        }

        /// <summary>
        /// Log a player-applied Poison/Bleed we did NOT convert (once per source). Player-owned non-hunter sources
        /// (e.g. Shadow Daggers, Contagion) surface here so the user can decide whether to widen HunterSpells; monster
        /// sources are skipped to keep the log quiet. Turns "did it leak?" into a readable line.
        /// </summary>
        internal static void LogStatusSkipped(BaseSpellLibrary prefab, StackingEffect effect)
        {
            if (!LogSwaps.Value) return;
            try
            {
                if (prefab == null || prefab._owner == null || !prefab._owner.IsPlayer) return;
                Spell spell = prefab._spell;
                string type = prefab.GetIl2CppType().Name;
                if (LoggedStatus.Add(spell + ">" + effect + ">skip"))
                {
                    Log.LogInfo($"[status] skip {spell} ({type}): {effect} -> left as-is (not in hunter scope)");
                }
            }
            catch { }
        }

        /// <summary>Log a converted damage-type swap once per (source spell, from-type).</summary>
        internal static void LogDamageSwap(BaseSpellLibrary prefab, DamageType from, DamageType to)
        {
            if (!LogSwaps.Value) return;
            Spell spell = prefab != null ? prefab._spell : Spell.None;
            if (LoggedDamage.Add(spell + ">" + from))
            {
                Log.LogInfo($"[damage] {spell}: {from} -> {to}");
            }
        }

        /// <summary>
        /// Log a player-applied Thrust hit we did NOT convert (once per source). Player-owned non-hunter sources
        /// (e.g. Shadow Daggers) surface here so the user can decide whether to widen HunterSpells; monster sources
        /// are skipped to keep the log quiet. Turns "did the hunter's thrust really get converted?" into a line.
        /// </summary>
        internal static void LogDamageSkipped(BaseSpellLibrary prefab, DamageType type)
        {
            if (!LogSwaps.Value) return;
            try
            {
                if (prefab == null || prefab._owner == null || !prefab._owner.IsPlayer) return;
                Spell spell = prefab._spell;
                string typeName = prefab.GetIl2CppType().Name;
                if (LoggedDamage.Add(spell + ">" + type + ">skip"))
                {
                    Log.LogInfo($"[damage] skip {spell} ({typeName}): {type} -> left as-is (not in hunter scope)");
                }
            }
            catch { }
        }

        /// <summary>
        /// [2026-10-16] Poison -&gt; Burning, Bleeding -&gt; Chill for a stack effect; anything else is unchanged.
        /// Shared by the status-add patch and the stack read/consume patches.
        /// </summary>
        internal static StackingEffect MapEffect(StackingEffect e)
        {
            if (e == StackingEffect.Poison) return StackingEffect.Burning;
            if (e == StackingEffect.Bleeding) return StackingEffect.Chill;
            return e;
        }

        /// <summary>
        /// [2026-10-16] Remap a stack-effect argument on a hunter source so the read/consume side of a mechanic
        /// looks for Burning/Chill instead of the now-empty Poison/Bleeding. Called from the stack helper prefixes
        /// (<c>GetStacksFromTarget</c>, <c>RemoveStacksOnTarget</c>, ...). The value is edited in place via a Harmony
        /// <c>ref</c> parameter. Gated by ShouldConvert so other sources are untouched. Without this the Hunter's
        /// Trap kept asking for Poison after we moved the stacks to Burning, so nothing was consumed.
        /// </summary>
        internal static void RemapStackEffect(BaseSpellLibrary prefab, string op, ref StackingEffect effect)
        {
            try
            {
                if (!IsActive || !ConvertStatus.Value) return;
                if (effect != StackingEffect.Poison && effect != StackingEffect.Bleeding) return;
                if (!ShouldConvert(prefab)) return;

                StackingEffect to = MapEffect(effect);
                if (to == effect) return;

                if (LogSwaps.Value && LoggedStatus.Add("consume:" + op + ">" + (prefab != null ? prefab._spell : Spell.None) + ">" + effect))
                {
                    Log.LogInfo($"[status] {op} {(prefab != null ? prefab._spell : Spell.None)}: {effect} -> {to}");
                }

                effect = to;
            }
            catch (Exception ex)
            {
                Log?.LogError($"[status] remap {op} {ex}");
            }
        }
    }

    /// <summary>Prefix on <c>SkillTree.LoadSkillTreeInitial</c>: convert every loaded target node before the tree
    /// aggregates its effects.</summary>
    [HarmonyPatch(typeof(SkillTree), "LoadSkillTreeInitial")]
    public static class Patch_SkillTree_LoadSkillTreeInitial
    {
        public static void Prefix()
        {
            if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertTree.Value) return;
            TreeConverter.ConvertAllLoaded();
        }
    }

    /// <summary>Belt-and-braces: convert each target node just before the tree reads its effects.</summary>
    [HarmonyPatch(typeof(SkillTree), "LoadNodeEffects")]
    public static class Patch_SkillTree_LoadNodeEffects
    {
        public static void Prefix(SkillTreeNode node)
        {
            if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertTree.Value) return;
            TreeConverter.ConvertNode(node);
        }
    }

    /// <summary>Belt-and-braces: convert each target node just before a point is applied.</summary>
    [HarmonyPatch(typeof(SkillTree), "ApplySingleNodeEffect")]
    public static class Patch_SkillTree_ApplySingleNodeEffect
    {
        public static void Prefix(SkillTreeNode node)
        {
            if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertTree.Value) return;
            TreeConverter.ConvertNode(node);
        }
    }

    [HarmonyPatch(typeof(SkillTreeNode), "GetLocalizedName")]
    public static class Patch_SkillTreeNode_GetLocalizedName
    {
        // [2026-10-16] Was: public static void Postfix(ref string __result). Now takes __instance so the
        // Thrust -> Fire wording swap can be gated to the converted tree (see TooltipText.ConvertNode).
        public static void Postfix(SkillTreeNode __instance, ref string __result)
        {
            __result = TooltipText.ConvertNode(__result, __instance);
        }
    }

    [HarmonyPatch(typeof(SkillTreeNode), "GetLocalizedDescription")]
    public static class Patch_SkillTreeNode_GetLocalizedDescription
    {
        // [2026-10-16] Was: public static void Postfix(ref string __result). Same rewrite as the name patch -
        // the description ("Increase your Thrust Damage by 3%") must follow the stat (ThrustDamage -> FireDamage).
        public static void Postfix(SkillTreeNode __instance, ref string __result)
        {
            __result = TooltipText.ConvertNode(__result, __instance);
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
            if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertTooltips.Value) return;
            if (__result == null) return;

            for (int i = 0; i < __result.Count; i++)
            {
                __result[i] = TooltipText.Convert(__result[i]);
            }
        }
    }

    /// <summary>
    /// Prefix on <c>BaseSpellLibrary.AddStacksToTarget</c>. Hunter Poison is re-dispatched as Burning and hunter
    /// Bleeding as Chill, so the status that lands on the enemy matches the converted tree/tooltip. The effect
    /// parameter is by value, so we return false and replay with the converted effect; the re-entrant call sees
    /// Burning/Chill and runs the original body exactly once (same proven pattern as PyromancerConverter).
    ///
    /// Scoped to hunter sources only (see ShouldConvert). The hunter tuners (ScatterTrapTuner, BarrageOfArrowsTuner,
    /// AimedShotChargeTuner) never patch this method - they only raise how many traps/arrows/pools spawn - so they
    /// cannot override this swap; their extra stacks just land as Burning/Chill instead of Poison/Bleed.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "AddStacksToTarget")]
    public static class Patch_BaseSpellLibrary_AddStacksToTarget
    {
        public static bool Prefix(BaseSpellLibrary __instance, ObjectsCommon target, StackingEffect effect, int amount, float time)
        {
            try
            {
                if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertStatus.Value) return true;
                if (effect != StackingEffect.Poison && effect != StackingEffect.Bleeding) return true;

                if (!HunterTreeConverterPlugin.ShouldConvert(__instance))
                {
                    HunterTreeConverterPlugin.LogStatusSkipped(__instance, effect);
                    return true;
                }

                StackingEffect to = effect == StackingEffect.Poison ? StackingEffect.Burning : StackingEffect.Chill;
                __instance.AddStacksToTarget(target, to, amount, time);
                HunterTreeConverterPlugin.LogStatusSwap(__instance, effect, to);
                return false;
            }
            catch (Exception ex)
            {
                HunterTreeConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_AddStacksToTarget] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Prefix on <c>BaseSpellLibrary.SendDamageToTarget</c>. The hunter's base damage type is Thrust (the arrows,
    /// traps and shots pierce); the user asked for it to read as Fire so the hunter theme is burn, matching the
    /// converted tree. The <c>damageType</c> parameter is passed by value (confirmed against the interop), so a
    /// plain prefix cannot edit it; instead we return false and replay the call with Fire, forwarding the original's
    /// return value via <c>__result</c>. The re-entrant call sees Fire and runs the original body exactly once (same
    /// proven pattern as PyromancerConverter).
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
                if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertDamage.Value) return true;
                if (damageType != DamageType.Thrust) return true;

                if (!HunterTreeConverterPlugin.ShouldConvert(__instance))
                {
                    HunterTreeConverterPlugin.LogDamageSkipped(__instance, damageType);
                    return true;
                }

                __result = __instance.SendDamageToTarget(
                    target, DamageType.Fire, multiplier, isDamageOverTime, isThirdAttack, stunBuildupBonusPercent);

                HunterTreeConverterPlugin.LogDamageSwap(__instance, damageType, DamageType.Fire);
                return false;
            }
            catch (Exception ex)
            {
                HunterTreeConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_SendDamageToTarget] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Postfix on <c>BaseSpellLibrary.SpawnElementalPool</c>. The Hail of Arrows poison-pool upgrade leaves a
    /// PoisonPoolPrefab behind; the user asked for the hunter's pool to use the game's EXISTING fire-pool VFX instead
    /// of the poison one (no recolouring). The game already ships exactly that conversion as
    /// <c>PoisonPoolPrefab.ConvertToBurningPit</c>, so we simply call it on the freshly spawned pool. Gated by
    /// ShouldConvert on the CALLER, so a non-hunter poison pool (e.g. BlightedTrail) is left untouched even though it
    /// shares the same Spell id.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "SpawnElementalPool")]
    public static class Patch_BaseSpellLibrary_SpawnElementalPool
    {
        public static void Postfix(BaseSpellLibrary __instance, GameObject __result)
        {
            try
            {
                if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertVfx.Value) return;
                if (__result == null) return;
                if (!HunterTreeConverterPlugin.ShouldConvert(__instance)) return;

                PoisonPoolPrefab pool = __result.GetComponentInChildren<PoisonPoolPrefab>();
                if (pool == null) return;

                pool.ConvertToBurningPit(null);
                if (HunterTreeConverterPlugin.LogSwaps.Value && HunterTreeConverterPlugin.LoggedPool.Add(__instance._spell))
                {
                    HunterTreeConverterPlugin.Log.LogInfo(
                        $"[vfx] {__instance._spell}: poison pool -> BurningPit (existing fire pool)");
                }
            }
            catch (Exception ex)
            {
                HunterTreeConverterPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_SpawnElementalPool] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-16] Status-round-trip companions. Because the hunter's Poison/Bleeding stacks now live under the
    /// effects Burning/Chill (see <see cref="Patch_BaseSpellLibrary_AddStacksToTarget"/>), any mechanic that READS or
    /// CONSUMES the old effect must be re-pointed at the new one. The player reported the Hunter's Trap no longer
    /// draining the Burning it should consume - it was still asking for Poison, which is now always empty. These
    /// prefixes remap the effect for hunter sources (the enum arg is edited in place via a Harmony <c>ref</c>).
    /// All are gated by ShouldConvert, so non-hunter sources (and enemy stacks) are left alone.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetStacksFromTarget")]
    public static class Patch_BaseSpellLibrary_GetStacksFromTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, ref StackingEffect stackType)
            => HunterTreeConverterPlugin.RemapStackEffect(__instance, "read", ref stackType);
    }

    /// <summary>See <see cref="Patch_BaseSpellLibrary_GetStacksFromTarget"/>; returns the stack's source object.</summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetStackSourceFromTarget")]
    public static class Patch_BaseSpellLibrary_GetStackSourceFromTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, ref StackingEffect stackType)
            => HunterTreeConverterPlugin.RemapStackEffect(__instance, "read-src", ref stackType);
    }

    /// <summary>See <see cref="Patch_BaseSpellLibrary_GetStacksFromTarget"/>; this is the actual stack removal.</summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "RemoveStacksOnTarget")]
    public static class Patch_BaseSpellLibrary_RemoveStacksOnTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, ref StackingEffect stackType)
            => HunterTreeConverterPlugin.RemapStackEffect(__instance, "remove", ref stackType);
    }

    /// <summary>See <see cref="Patch_BaseSpellLibrary_GetStacksFromTarget"/>; clears all of a stack type.</summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "CleanseStacksOnTarget")]
    public static class Patch_BaseSpellLibrary_CleanseStacksOnTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, ref StackingEffect stackType)
            => HunterTreeConverterPlugin.RemapStackEffect(__instance, "cleanse", ref stackType);
    }

    /// <summary>See <see cref="Patch_BaseSpellLibrary_GetStacksFromTarget"/>; collapses a stack pool to a budget.</summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "CollapseStacksOnTarget")]
    public static class Patch_BaseSpellLibrary_CollapseStacksOnTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, ref StackingEffect stackType)
            => HunterTreeConverterPlugin.RemapStackEffect(__instance, "collapse", ref stackType);
    }

    /// <summary>See <see cref="Patch_BaseSpellLibrary_GetStacksFromTarget"/>; "how many did I just consume".</summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetLastConsumedAmount")]
    public static class Patch_BaseSpellLibrary_GetLastConsumedAmount
    {
        public static void Prefix(BaseSpellLibrary __instance, ref StackingEffect stackingEffect)
            => HunterTreeConverterPlugin.RemapStackEffect(__instance, "last-consumed", ref stackingEffect);
    }

    /// <summary>
    /// Postfix on <c>SpellLibrary.ApplyDefinition</c>. Re-types each hunter spell's DEFINITION to Fire so it reads as
    /// fire everywhere the definition is inspected (the <c>DmgType</c> backing field at 0xF0, the element, fire
    /// passives). Mirrors PyromancerConverter. This alone does not change the damage a prefab sends at its call site
    /// - that is the SendDamageToTarget patch - but it is what "cast a Fire spell" triggers look at.
    /// </summary>
    [HarmonyPatch(typeof(SpellLibrary), "ApplyDefinition")]
    public static class Patch_SpellLibrary_ApplyDefinition
    {
        public static void Postfix(SpellLibrary __instance)
        {
            try
            {
                if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertDamage.Value) return;
                if (__instance == null) return;
                Spell spell = __instance.Spell;
                if (spell == Spell.None) return;
                if (!HunterTreeConverterPlugin.HunterSpells.Contains(spell)) return;

                Element element = __instance.Element;
                DamageType dmg = __instance.DmgType;
                if (dmg == DamageType.Fire && element == Element.Fire) return;

                __instance.SetDamageType(DamageType.Fire);
                __instance.SetElement(Element.Fire);

                if (HunterTreeConverterPlugin.LogSwaps.Value && HunterTreeConverterPlugin.LoggedDefinition.Add(spell))
                {
                    HunterTreeConverterPlugin.Log.LogInfo(
                        $"[damage] def {spell}: element {element}, damage {dmg} -> Fire (fire-spell trigger)");
                }
            }
            catch (Exception ex)
            {
                HunterTreeConverterPlugin.Log?.LogError($"[Patch_SpellLibrary_ApplyDefinition] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>Passives.RegisterSpellCast</c>. The Enflamed 4pc "Everburn" passive (Passives.EverburnCheck)
    /// fires its mortar only when the CAST spell's <c>DmgType</c> (backing field at 0xF0) is DamageType.Fire - the
    /// method compares it to 7 and quietly returns otherwise. The hunter abilities send Thrust at their call site, so
    /// stamping the cast component to Fire right before the check runs is what makes the mortar go off.
    /// </summary>
    [HarmonyPatch(typeof(Passives), "RegisterSpellCast")]
    public static class Patch_Passives_RegisterSpellCast
    {
        public static void Prefix(Spells castSpell)
        {
            try
            {
                if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertDamage.Value) return;
                if (castSpell == null) return;
                if (!HunterTreeConverterPlugin.HunterSpells.Contains(castSpell.Spell)) return;

                if (castSpell.DmgType != DamageType.Fire) castSpell.SetDamageType(DamageType.Fire);
                if (castSpell.Element != Element.Fire) castSpell.SetElement(Element.Fire);

                if (HunterTreeConverterPlugin.LogSwaps.Value && HunterTreeConverterPlugin.LoggedCastStamp.Add(castSpell.Spell))
                {
                    HunterTreeConverterPlugin.Log.LogInfo(
                        $"[damage] cast {castSpell.Spell}: stamped Fire (Everburn trigger armed)");
                }
            }
            catch (Exception ex)
            {
                HunterTreeConverterPlugin.Log?.LogError($"[Patch_Passives_RegisterSpellCast] {ex}");
            }
        }
    }

    /// <summary>
    /// Remaps target skill-tree nodes from Poison/Bleed to Burning/Chill. Only Poison* and Bleed* stats are
    /// touched (nothing else in the tree is affected), and the mapping is idempotent (no target is itself a key),
    /// so the repeated calls from <c>LoadNodeEffects</c>/<c>ApplySingleNodeEffect</c> are safe.
    /// </summary>
    internal static class TreeConverter
    {
        // [2026-10-16] Runes.Stat Poison/Bleed family -> Burning/Chill family. Mirrors the PyromancerConverter
        // Chill/Ice->Burning/Fire table, retargeted for the hunter tree. PoisonPiercing maps to FirePiercing
        // because there is no BurningPiercing stat; FirePiercing is the burn-theme penetration stat.
        private static readonly Dictionary<Runes.Stat, Runes.Stat> Map = new Dictionary<Runes.Stat, Runes.Stat>
        {
            // Poison -> Burning
            { Runes.Stat.MaxPoison, Runes.Stat.MaxBurning },
            { Runes.Stat.PoisonPiercing, Runes.Stat.FirePiercing },
            { Runes.Stat.PoisonDuration, Runes.Stat.BurningDuration },
            { Runes.Stat.PoisonDamage, Runes.Stat.BurningDamage },
            { Runes.Stat.DamageVsPoison, Runes.Stat.DamageVsBurning },
            { Runes.Stat.CriticalChanceVsPoison, Runes.Stat.CriticalChanceVsBurning },
            { Runes.Stat.CriticalDamageVsPoison, Runes.Stat.CriticalDamageVsBurning },

            // Bleed -> Chill
            { Runes.Stat.MaxBleeding, Runes.Stat.MaxChill },
            { Runes.Stat.BleedDuration, Runes.Stat.ChilledDuration },
            { Runes.Stat.BleedDamage, Runes.Stat.ChilledPower },
            { Runes.Stat.DamageVsBleeding, Runes.Stat.DamageVsChilled },
            { Runes.Stat.CriticalChanceVsBleeding, Runes.Stat.CriticalChanceVsChilled },
            { Runes.Stat.CriticalDamageVsBleeding, Runes.Stat.CriticalDamageVsChilled },

            // Thrust -> Fire. [2026-10-16] The hunter tree's base damage is Thrust (the arrows / traps / shots
            // pierce); the user asked for it to read as Fire damage so the whole hunter theme is burn, matching the
            // damage-type swap and fire-pool VFX. ThrustPiercing maps to FirePiercing (a FirePiercing stat exists).
            { Runes.Stat.ThrustDamage, Runes.Stat.FireDamage },
            { Runes.Stat.ThrustPiercing, Runes.Stat.FirePiercing },
        };

        // [2026-10-16] Nodes already reported once (ConvertNode runs per node many times); keeps the log readable.
        private static readonly HashSet<string> LoggedNodes = new HashSet<string>();

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
                HunterTreeConverterPlugin.Log?.LogError($"[tree] enumerate {ex}");
            }
        }

        /// <summary>
        /// True when this node belongs to the tree the mod is allowed to rewrite (see
        /// <see cref="HunterTreeConverterPlugin.TreeMatchMode"/>). Defaults to the Human / hunter tree.
        /// </summary>
        // [2026-10-16] Was: private static bool MatchesTree(SkillTreeNode node). Made internal so the tree-aware
        // tooltip wording swap (TooltipText.ConvertNode) reuses the exact same tree gate as the stat conversion.
        internal static bool MatchesTree(SkillTreeNode node)
        {
            switch (HunterTreeConverterPlugin.MatchMode.Value)
            {
                case HunterTreeConverterPlugin.TreeMatchMode.All:
                    return true;
                case HunterTreeConverterPlugin.TreeMatchMode.RaceHuman:
                    return node.Race == Race.Human;
                case HunterTreeConverterPlugin.TreeMatchMode.ClassShadow:
                    return node.Class == Class.Shadow;
                default: // Either
                    return node.Race == Race.Human || node.Class == Class.Shadow;
            }
        }

        internal static void ConvertNode(SkillTreeNode node)
        {
            try
            {
                if (node == null) return;
                if (!MatchesTree(node)) return;

                Il2CppSystem.Collections.Generic.List<SkillTreeStatImprovement> list = node.StatImprovements;

                // [2026-10-16] Discovery log (once per node): the node's id, tree tags and full stat list, so the
                // conversion set can be verified from the BepInEx log and MatchMode tightened if needed.
                if (HunterTreeConverterPlugin.LogSwaps.Value)
                {
                    string key = node.NodeId;
                    if (string.IsNullOrEmpty(key))
                    {
                        try { key = "#" + node.Pointer; } catch { key = "?" + LoggedNodes.Count; }
                    }
                    if (LoggedNodes.Add(key))
                    {
                        string stats = "";
                        if (list != null)
                        {
                            for (int j = 0; j < list.Count; j++) stats += list[j].Stat + " ";
                        }
                        HunterTreeConverterPlugin.Log.LogInfo(
                            $"[node] {node.NodeId} class={node.Class} race={node.Race} stats=[{stats.Trim()}]");
                    }
                }

                if (list == null || list.Count == 0) return;

                IntPtr elemClass = Il2CppClassPointerStore<SkillTreeStatImprovement>.NativeClassPtr;
                IntPtr listPtr = ((Il2CppObjectBase)list).Pointer;

                for (int i = 0; i < list.Count; i++)
                {
                    Runes.Stat current = list[i].Stat;
                    if (!Map.TryGetValue(current, out Runes.Stat to)) continue;

                    // Direct native write: SkillTreeStatImprovement is a value type, so the interop list indexer
                    // copies rather than mutates (same technique as PyromancerConverter / NecroSkelly).
                    Marshal.WriteInt32(
                        RawStruct.Elem(listPtr, elemClass, i) + RawStruct.Off(elemClass, "Stat"),
                        (int)to);

                    if (HunterTreeConverterPlugin.LogSwaps.Value)
                    {
                        HunterTreeConverterPlugin.Log.LogInfo($"[tree] {node.NodeId}[{i}] {current} -> {to}");
                    }
                }
            }
            catch (Exception ex)
            {
                HunterTreeConverterPlugin.Log?.LogError($"[tree] node {ex}");
            }
        }
    }

    /// <summary>
    /// Approximate cosmetic wording swap. Kept deliberately small: it only touches the poison/bleed words so the
    /// converted node/spell text reads as burn/chill. Order matters - longer/more specific forms first so the
    /// short root does not clobber them ("Poisoned" before "Poison", "Bleeding" before "Bleed").
    /// </summary>
    internal static class TooltipText
    {
        internal static string Convert(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertTooltips.Value) return s;

            s = s.Replace("Poisoned", "Burning").Replace("Poison", "Burning");
            s = s.Replace("Bleeding", "Chilled").Replace("Bleed", "Chill");
            return s;
        }

        /// <summary>
        /// [2026-10-16] Tree-aware variant for SKILL-TREE nodes. The hunter damage-type nodes keep their
        /// authored localization key after <see cref="TreeConverter"/> retargets the node's Stat
        /// (ThrustDamage -> FireDamage), so the node header/description still read "Thrust Damage Increase",
        /// "Thrust Piercing", ... even though the granted stat is Fire. This swap closes that gap. It is gated
        /// on <see cref="TreeConverter.MatchesTree"/> because "Thrust" is a generic word (unlike Poison/Bleed)
        /// and must not leak into trees the mod is not converting.
        /// </summary>
        internal static string ConvertNode(string s, SkillTreeNode node)
        {
            s = Convert(s);
            if (string.IsNullOrEmpty(s)) return s;
            if (!HunterTreeConverterPlugin.IsActive || !HunterTreeConverterPlugin.ConvertTooltips.Value) return s;
            if (node == null || !TreeConverter.MatchesTree(node)) return s;

            // Order matters: swap the all-caps UI form first, then the normal/lower-case forms.
            return s.Replace("THRUST", "FIRE").Replace("Thrust", "Fire").Replace("thrust", "fire");
        }
    }

    /// <summary>Direct native-memory access to an IL2CPP list of blittable structs. Needed because the game's
    /// <c>SkillTreeStatImprovement</c> is a value type and writing the <c>Stat</c> enum through the interop
    /// list indexer copies rather than mutates the element. Copied from the proven PyromancerConverter helper.</summary>
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
