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
    /// This is the poison/bleed counterpart of the ice-&gt;fire mod PyromancerConverter, scoped (per the user's
    /// choice) to the SKILL TREE + TOOLTIPS only: it does NOT rewrite live status stacks, damage types or VFX.
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
        public const string VERSION = "0.1.0";

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ConvertTree;
        public static ConfigEntry<bool> ConvertTooltips;
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

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}, ConvertTree: {ConvertTree.Value}, ConvertTooltips: {ConvertTooltips.Value}, " +
                        $"MatchMode: {MatchMode.Value}, LogSwaps: {LogSwaps.Value}");
            Log.LogInfo("Poison -> Burning, Bleed -> Chill (skill tree + tooltips only).");
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
