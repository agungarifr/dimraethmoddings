using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LootModV2.Patches;

namespace LootModV2
{
    /// <summary>
    /// LootModV2 — versi baru dari LootAndExpMod:
    ///   • LOOT SAJA (tanpa EXP modifier)
    ///   • Drop loot 10x dari vanilla untuk MONSTER (item + gold)
    ///   • Baru: drop 10x untuk NODE / objek panen (stone→pickaxe, wood/log→kapak,
    ///     rumput/bush/plant→glove) via kelas Harvest
    ///   • Baru: reward 10x untuk INTERACTABLE di map (dead body / box / tekan C)
    ///     via kelas InteractableReward
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class LootModV2Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static ConfigEntry<int> ItemMultiplier;
        public static ConfigEntry<int> GoldMultiplier;
        public static ConfigEntry<int> HarvestMultiplier;
        public static ConfigEntry<int> InteractableMultiplier;
        public static ConfigEntry<bool> GuaranteeDrops;
        public static ConfigEntry<bool> ForceHighestRarity;

        public override void Load()
        {
            Log = base.Log;

            // ---- Konfigurasi (BepInEx/config) ----
            ItemMultiplier = Config.Bind("General", "ItemMultiplier", 10, "Monster item drop quantities xN (Default: 10)");
            GoldMultiplier = Config.Bind("General", "GoldMultiplier", 10, "Dropped gold quantities xN (Default: 10)");
            HarvestMultiplier = Config.Bind("General", "HarvestMultiplier", 10, "Harvest node drops (stone/wood/log/bush/grass/plant) xN (Default: 10)");
            InteractableMultiplier = Config.Bind("General", "InteractableMultiplier", 10, "Interactable reward amounts (dead body/box/press-C) xN (Default: 10)");
            GuaranteeDrops = Config.Bind("General", "GuaranteeMonsterDrops", true, "Force 100% monster drop chance (Default: true)");
            ForceHighestRarity = Config.Bind("General", "ForceHighestRarity", true, "Equipment drops (monster/chest/craft) always pick the HIGHEST rarity vanilla allows for that drop — not forced to Ancient (Default: true)");

            // ---- Pasang patch ----
            Harmony.CreateAndPatchAll(typeof(Patch_ApplyDropChanceModifiers));
            Harmony.CreateAndPatchAll(typeof(Patch_ItemDropCheck));
            Harmony.CreateAndPatchAll(typeof(Patch_GoldDrop));
            Harmony.CreateAndPatchAll(typeof(Patch_HarvestCalculateItems));
            Harmony.CreateAndPatchAll(typeof(Patch_HarvestBonusItems));
            Harmony.CreateAndPatchAll(typeof(Patch_InteractableReward));
            Harmony.CreateAndPatchAll(typeof(Patch_MaxEquipmentRarity));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} initialized (BepInEx 6)");
            Log.LogInfo($"Monster Item Drop  : {ItemMultiplier.Value}x");
            Log.LogInfo($"Gold Drop          : {GoldMultiplier.Value}x");
            Log.LogInfo($"Harvest Node Drop  : {HarvestMultiplier.Value}x");
            Log.LogInfo($"Interactable Reward: {InteractableMultiplier.Value}x");
            Log.LogInfo($"Guarantee drop 100%: {GuaranteeDrops.Value}");
            Log.LogInfo("=================================================");
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.custom.lootmodv2";
        public const string PLUGIN_NAME = "LootModV2 (10x Loot + Nodes + Interactables)";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}