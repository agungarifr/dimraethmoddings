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
    /// This is the poison/bleed counterpart of the ice-&gt;fire mod PyromancerConverter. It rewrites the skill tree
    /// and tooltips AND (as of [2026-10-16]) the live status stacks the hunter abilities land on enemies: hunter
    /// Poison becomes Burning and hunter Bleeding becomes Chill. Damage types and VFX remain out of scope.
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
        public const string VERSION = "0.2.0"; // [2026-10-16] bumped from 0.1.0 for the live-status conversion feature

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ConvertTree;
        public static ConfigEntry<bool> ConvertTooltips;
        public static ConfigEntry<bool> ConvertStatus;
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

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}, ConvertTree: {ConvertTree.Value}, ConvertTooltips: {ConvertTooltips.Value}, " +
                        $"MatchMode: {MatchMode.Value}, LogSwaps: {LogSwaps.Value}");
            Log.LogInfo("Poison -> Burning, Bleed -> Chill (skill tree + tooltips + hunter live status).");
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
        private static bool MatchesTree(SkillTreeNode node)
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
