using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(LootAndExpMod.LootAndExpMod), "LootAndExpMod", "1.0.0", "CustomMod")]
[assembly: MelonGame("Mudtek", "Dimraeth")]

namespace LootAndExpMod
{
    public class LootAndExpMod : MelonMod
    {
        public static MelonPreferences_Category PrefCategory;
        public static MelonPreferences_Entry<int> ItemMultiplier;
        public static MelonPreferences_Entry<int> GoldMultiplier;
        public static MelonPreferences_Entry<int> ExpMultiplier;
        public static MelonPreferences_Entry<bool> GuaranteeDrops;

        public override void OnInitializeMelon()
        {
            PrefCategory = MelonPreferences.CreateCategory("LootAndExpMod", "Loot & EXP Multiplier Settings");
            ItemMultiplier = PrefCategory.CreateEntry("ItemMultiplier", 10, "Item Drop Multiplier (xVanilla)");
            GoldMultiplier = PrefCategory.CreateEntry("GoldMultiplier", 10, "Gold Drop Multiplier (xVanilla)");
            ExpMultiplier = PrefCategory.CreateEntry("ExpMultiplier", 30, "EXP Multiplier (xVanilla)");
            GuaranteeDrops = PrefCategory.CreateEntry("GuaranteeMonsterDrops", true, "Guarantee 100% Monster Drop Chance");

            LoggerInstance.Msg("=================================================");
            LoggerInstance.Msg("LootAndExpMod v1.1.0 Initialized successfully!");
            LoggerInstance.Msg($"Item Drop Multiplier: {ItemMultiplier.Value}x");
            LoggerInstance.Msg($"Gold Drop Multiplier: {GoldMultiplier.Value}x");
            LoggerInstance.Msg($"EXP Multiplier: {ExpMultiplier.Value}x");
            LoggerInstance.Msg($"Guarantee 100% Monster Drops: {GuaranteeDrops.Value}");
            LoggerInstance.Msg("Zero-lag event-driven architecture active.");
            LoggerInstance.Msg("=================================================");
        }
    }

    /// <summary>
    /// Guarantees 100% drop chance for all monster loot, rune drops, and gold rolls.
    /// In vanilla, Random.Range(0, 100) is compared against ApplyDropChanceModifiers(dropChance).
    /// Returning 100 ensures (roll < chance) is always true.
    /// </summary>
    [HarmonyPatch(typeof(MonsterUtils), nameof(MonsterUtils.ApplyDropChanceModifiers))]
    public static class Patch_ApplyDropChanceModifiers
    {
        public static void Postfix(ref int __result)
        {
            if (LootAndExpMod.GuaranteeDrops != null && LootAndExpMod.GuaranteeDrops.Value)
            {
                __result = 100;
            }
        }
    }

    /// <summary>
    /// Scales monster item drop ranges by 10x so that rolls in ItemDropCheck result in 10x drop quantity.
    /// Uses config instance tracking to scale each monster configuration once without exponential compounding.
    /// </summary>
    [HarmonyPatch(typeof(MonsterUtils), nameof(MonsterUtils.ItemDropCheck))]
    public static class Patch_ItemDropCheck
    {
        private static readonly System.Collections.Generic.HashSet<int> _scaledConfigs = new();

        public static void Prefix(MonsterUtils __instance)
        {
            try
            {
                if (__instance == null || __instance._configuration == null) return;
                var config = __instance._configuration;
                int configId = config.GetInstanceID();
                if (_scaledConfigs.Contains(configId)) return;
                _scaledConfigs.Add(configId);

                var drops = config.ItemDrops;
                if (drops == null) return;

                int mult = LootAndExpMod.ItemMultiplier != null ? LootAndExpMod.ItemMultiplier.Value : 10;
                if (mult <= 1) return;

                for (int i = 0; i < drops.Count; i++)
                {
                    var drop = drops[i];
                    var range = drop.dropRange;
                    range.min = Math.Max(1, range.min) * mult;
                    range.max = Math.Max(1, range.max) * mult;
                    drop.dropRange = range;
                    drops[i] = drop;
                }
                config.ItemDrops = drops;

                if (config.IsRuneDrop && config.RuneDropCount > 0)
                {
                    config.RuneDropCount *= mult;
                }

                Melon<LootAndExpMod>.Logger.Msg($"Scaled item drops for {config.MonsterName} ({config.MonsterType}) by {mult}x.");
            }
            catch (Exception ex)
            {
                Melon<LootAndExpMod>.Logger.Error($"Error in Patch_ItemDropCheck: {ex}");
            }
        }
    }

    /// <summary>
    /// Multiplies dropped gold quantity by 10x.
    /// </summary>
    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateGoldDrop))]
    public static class Patch_CalculateGoldDrop
    {
        public static void Postfix(ref int __result)
        {
            int mult = LootAndExpMod.GoldMultiplier != null ? LootAndExpMod.GoldMultiplier.Value : (LootAndExpMod.ItemMultiplier != null ? LootAndExpMod.ItemMultiplier.Value : 10);
            if (mult > 1)
            {
                __result *= mult;
            }
        }
    }

    /// <summary>
    /// Multiplies combat kill EXP gained from monsters by 10x.
    /// Automatically scales player gain, pet routing, and popups.
    /// </summary>
    [HarmonyPatch(typeof(XP), nameof(XP.CalculateXPGained))]
    public static class Patch_CalculateXPGained
    {
        public static void Postfix(ref int __result)
        {
            int mult = LootAndExpMod.ExpMultiplier != null ? LootAndExpMod.ExpMultiplier.Value : 10;
            if (mult > 1)
            {
                __result *= mult;
            }
        }
    }

    /// <summary>
    /// Multiplies quest, event, and general reward EXP by 10x.
    /// </summary>
    [HarmonyPatch(typeof(XP), nameof(XP.GrantXPToPlayer))]
    public static class Patch_GrantXPToPlayer
    {
        public static void Prefix(Player player, ref int xp, string source, bool showPopup)
        {
            int mult = LootAndExpMod.ExpMultiplier != null ? LootAndExpMod.ExpMultiplier.Value : 10;
            if (mult > 1)
            {
                xp *= mult;
            }
        }
    }

    /// <summary>
    /// Multiplies pet training and growth EXP by 10x.
    /// </summary>
    [HarmonyPatch(typeof(XP), nameof(XP.AddXPToPet))]
    public static class Patch_AddXPToPet
    {
        public static void Prefix(Player player, ref int xp)
        {
            int mult = LootAndExpMod.ExpMultiplier != null ? LootAndExpMod.ExpMultiplier.Value : 10;
            if (mult > 1)
            {
                xp *= mult;
            }
        }
    }
}
