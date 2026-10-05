using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace LootModV2.Patches
{
    // ============================================================
    // MONSTER LOOT (dipertahankan dari LootAndExpMod)
    // ============================================================

    /// <summary>Paksa drop chance monster 100%.</summary>
    [HarmonyPatch(typeof(MonsterUtils), nameof(MonsterUtils.ApplyDropChanceModifiers))]
    public static class Patch_ApplyDropChanceModifiers
    {
        public static void Postfix(ref int __result)
        {
            if (LootModV2Plugin.GuaranteeDrops != null && LootModV2Plugin.GuaranteeDrops.Value)
            {
                __result = 100;
            }
        }
    }

    /// <summary>
    /// Perbesar rentang jumlah drop monster sesuai ItemMultiplier.
    /// Dipakai di awal (prefix) dan melacak konfigurasi yang sudah pernah
    /// diskalakan agar tidak berlipat-ganda (exponential compounding).
    /// </summary>
    [HarmonyPatch(typeof(MonsterUtils), nameof(MonsterUtils.ItemDropCheck))]
    public static class Patch_ItemDropCheck
    {
        private static readonly HashSet<int> _scaledConfigs = new();

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

                int mult = LootModV2Plugin.ItemMultiplier != null ? LootModV2Plugin.ItemMultiplier.Value : 10;
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

                LootModV2Plugin.Log.LogDebug($"Scaled item drops for {config.MonsterName} ({config.MonsterType}) by {mult}x.");
            }
            catch (Exception ex)
            {
                LootModV2Plugin.Log.LogError($"Error in Patch_ItemDropCheck: {ex}");
            }
        }
    }

    /// <summary>Perbesar jumlah gold yang di-drop monster sesuai GoldMultiplier.</summary>
    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateGoldDrop))]
    public static class Patch_GoldDrop
    {
        public static void Postfix(ref int __result)
        {
            int mult = LootModV2Plugin.GoldMultiplier != null ? LootModV2Plugin.GoldMultiplier.Value
                        : (LootModV2Plugin.ItemMultiplier != null ? LootModV2Plugin.ItemMultiplier.Value : 10);
            if (mult > 1) __result *= mult;
        }
    }

    // ============================================================
    // NODE PANEN (stone, wood/log, rumput, bush, plant, dll)
    // Semua tipe node lewat Harvest.CalculateItemsForDamage /
    // RollBonusItems yang mengembalikan List<HarvestedItem>.
    // ============================================================

    internal static class HarvestMultiplication
    {
        internal static int Mult => LootModV2Plugin.HarvestMultiplier != null ? LootModV2Plugin.HarvestMultiplier.Value : 10;

        internal static void Multiply(Il2CppSystem.Collections.Generic.List<HarvestedItem> list)
        {
            int mult = Mult;
            if (list == null || mult <= 1) return;
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    item.ItemCount = Math.Max(1, item.ItemCount) * mult;
                    list[i] = item;
                }
                LootModV2Plugin.Log.LogDebug($"Harvest: scaled {list.Count} item(s) by {mult}x.");
            }
            catch (Exception ex)
            {
                LootModV2Plugin.Log.LogError($"Error scaling harvest list: {ex}");
            }
        }
    }

    /// <summary>Item utama hasil panen node dikali HarvestMultiplier (10x).</summary>
    [HarmonyPatch(typeof(HarvestClient), nameof(HarvestClient.CalculateItemsForDamage))]
    public static class Patch_HarvestCalculateItems
    {
        public static void Postfix(Il2CppSystem.Collections.Generic.List<HarvestedItem> __result)
        {
            HarvestMultiplication.Multiply(__result);
        }
    }

    /// <summary>Item bonus (chance) hasil panen node dikali HarvestMultiplier.</summary>
    [HarmonyPatch(typeof(HarvestClient), nameof(HarvestClient.RollBonusItems))]
    public static class Patch_HarvestBonusItems
    {
        public static void Postfix(Il2CppSystem.Collections.Generic.List<HarvestedItem> __result)
        {
            HarvestMultiplication.Multiply(__result);
        }
    }

    // ============================================================
    // EQUIPMENT (RUNE) RARITY — patch randomness saja
    // Semua sumber equipment (monster, chest, deed, craft) lewat
    // Runes.ReturnRandomRuneData(..., List<Rarity> rarities, ...)
    // yang memilih 1 rarity acak dari daftar yg diizinkan vanilla.
    // Kita ganti daftar tsb menjadi [rarity tertinggi yg diizinkan]
    // -> hasil selalu rarity max yg diperbolehkan game (bukan Ancient).
    // ============================================================

    /// <summary>
    /// Paksa roll equipment memakai rarity tertinggi yang DIIZINKAN VANILLA
    /// untuk drop tsb (max dari list rarities). Tidak meng-inject Ancient
    /// bila vanilla tidak mengizinkannya. Gunakan `ref` + list baru agar
    /// list milik pemanggil TIDAK ikut termutasi (side-effect free).
    /// </summary>
    [HarmonyPatch(typeof(Runes), nameof(Runes.ReturnRandomRuneData))]
    public static class Patch_MaxEquipmentRarity
    {
        public static void Prefix(ref Il2CppSystem.Collections.Generic.List<Runes.Rarity> rarities)
        {
            try
            {
                if (LootModV2Plugin.ForceHighestRarity == null || !LootModV2Plugin.ForceHighestRarity.Value) return;
                if (rarities == null || rarities.Count <= 1) return;

                // Cari rarity tertinggi yang vanilla izinkan di daftar ini.
                Runes.Rarity max = rarities[0];
                for (int i = 1; i < rarities.Count; i++)
                {
                    if (rarities[i] > max) max = rarities[i];
                }

                // Ganti daftar dengan [max] -> roll selalu kembali ke max.
                var only = new Il2CppSystem.Collections.Generic.List<Runes.Rarity>();
                only.Add(max);
                rarities = only;

                LootModV2Plugin.Log.LogDebug($"MaxEquipmentRarity: forced roll to {max} (highest vanilla-allowed).");
            }
            catch (Exception ex)
            {
                LootModV2Plugin.Log.LogError($"Error in Patch_MaxEquipmentRarity: {ex}");
            }
        }
    }

    // ============================================================
    // INTERACTABLE DI MAP (dead body / box / tekan C)
    // InteractableReward.ApplyReward(entry, player) memberi item
    // dengan jumlah entry.GrantedItemAmount.
    // ============================================================

    /// <summary>Reward item dari interactable dikali InteractableMultiplier.</summary>
    [HarmonyPatch(typeof(InteractableReward), nameof(InteractableReward.ApplyReward))]
    public static class Patch_InteractableReward
    {
        public static void Prefix(InteractableRewardEntry entry)
        {
            try
            {
                int mult = LootModV2Plugin.InteractableMultiplier != null ? LootModV2Plugin.InteractableMultiplier.Value : 10;
                if (mult <= 1 || entry == null) return;
                if (entry.Type == InteractableRewardType.GrantItem && entry.GrantedItemAmount > 0)
                {
                    entry.GrantedItemAmount = Math.Max(1, entry.GrantedItemAmount) * mult;
                    LootModV2Plugin.Log.LogDebug($"InteractableReward: granted item amount scaled by {mult}x.");
                }
            }
            catch (Exception ex)
            {
                LootModV2Plugin.Log.LogError($"Error in Patch_InteractableReward: {ex}");
            }
        }
    }
}