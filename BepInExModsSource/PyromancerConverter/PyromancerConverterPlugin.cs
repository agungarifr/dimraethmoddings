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

        /// <summary>Spells whose Ice/Chill output this mod rewrites. Seeded with the known Magician ice
        /// spells and extended at runtime by <see cref="Patch_SpellLibrary_ApplyDefinition"/>.</summary>
        internal static readonly HashSet<Spell> Converted = new HashSet<Spell>();

        internal static readonly HashSet<Spell> LoggedDamage = new HashSet<Spell>();
        // [2026-10-05] Was HashSet<Spell>: one line per spell hid the fact that a single spell applies
        // several distinct StackingEffects (Blizzard applies Chill AND Frostbound). Now keyed by
        // (spell, effect) so every pair is reported once.
        // internal static readonly HashSet<Spell> LoggedStacks = new HashSet<Spell>();
        internal static readonly HashSet<long> LoggedStacks = new HashSet<long>();

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

            foreach (Spell s in KnownMagicianIce)
            {
                Converted.Add(s);
            }

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_SendDamageToTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToTarget));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToCaster));
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
                        $"ConvertTree: {ConvertTree.Value}, ConvertTooltips: {ConvertTooltips.Value}, LogSwaps: {LogSwaps.Value}");
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

        /// <summary>True when the prefab belongs to a spell this mod rewrites and the master/feature gates allow it.</summary>
        internal static bool IsConverted(BaseSpellLibrary prefab)
        {
            return prefab != null && Converted.Contains(prefab._spell);
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

                bool known = PyromancerConverterPlugin.Converted.Contains(spell);
                Class classReq = __instance.ClassRequirement;
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
                if (!PyromancerConverterPlugin.IsConverted(__instance)) return true;

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
                if (!PyromancerConverterPlugin.Enabled.Value) return true;
                if (!PyromancerConverterPlugin.ConvertStatus.Value) return true;

                // Only the cold effects are interesting here; anything else passes straight through.
                if (effect != StackingEffect.Chill && effect != StackingEffect.Frostbound) return true;

                if (!PyromancerConverterPlugin.IsConverted(__instance))
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
                if (!PyromancerConverterPlugin.Enabled.Value) return true;
                if (!PyromancerConverterPlugin.ConvertStatus.Value) return true;
                if (effect != StackingEffect.Frostbound) return true;

                if (!PyromancerConverterPlugin.IsConverted(__instance))
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
