using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace DimraethModPack.Modules.Loot
{
    public class LootModV2Module : ModModuleBase
    {
        public static LootModV2Module Instance { get; private set; }

        public override string Category => "Loot";
        public override string Name => "Loot Mod V2";
        public override string Description => "Configurable drop multipliers for monsters, gold, resource nodes, and map interactables";

        public ConfigEntry<int> ItemMultiplier;
        public ConfigEntry<int> GoldMultiplier;
        public ConfigEntry<int> HarvestMultiplier;
        public ConfigEntry<int> InteractableMultiplier;
        // [2026-10-06 12:40] Dedicated crop slider (user: "crops not nodes"). Crops were previously
        // riding HarvestMultiplier — the RESOURCE-NODE slider (wood/stone/plants, often set very high),
        // which blew farm-plot yields up (e.g. apple tree -> 300 at x50). Crops now have their own knob.
        public ConfigEntry<int> CropMultiplier;
        public ConfigEntry<bool> GuaranteeDrops;
        public ConfigEntry<bool> ForceHighestRarity;

        // [2026-09-30 09:15] "Drop table khusus equipment" (disetujui user): jumlah drop equipment
        // (rune) per tipe lewat form nested. Lihat EquipmentDropCounts (global > slot > set > set+slot).
        public ConfigEntry<int> EquipmentCountGlobal;
        public ConfigEntry<string> EquipmentCountSlots;
        public ConfigEntry<string> EquipmentCountSets;
        public ConfigEntry<string> EquipmentCountSetSlots;

        public static bool IsSpawningChestLoot { get; set; } = false;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Loot.LootModV2";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Loot Mod V2 (Default: false, Vanilla: false)");

            ItemMultiplier = config.Bind(sec, "ItemMultiplier", 10,
                "Multiplier for monster item drops (Default: 10, Vanilla: 1)");

            GoldMultiplier = config.Bind(sec, "GoldMultiplier", 10,
                "Multiplier for dropped gold amounts (Default: 10, Vanilla: 1)");

            HarvestMultiplier = config.Bind(sec, "HarvestMultiplier", 10,
                "Multiplier for harvesting resource nodes (wood, stone, plants) (Default: 10, Vanilla: 1)");

            InteractableMultiplier = config.Bind(sec, "InteractableMultiplier", 10,
                "Multiplier for map interactions, bodies, and loot boxes (Default: 10, Vanilla: 1)");

            // [2026-10-06 12:40] Crops get their OWN multiplier, separate from resource nodes.
            // Default 1 = vanilla so enabling the module never surprises farm-plot yields;
            // raise it to taste. (User: "i meant crops not nodes".)
            CropMultiplier = config.Bind(sec, "CropMultiplier", 1,
                "Multiplier for harvesting farm-plot crops (apple tree, flax, etc.) (Default: 1, Vanilla: 1)");

            GuaranteeDrops = config.Bind(sec, "GuaranteeDrops", true,
                "Guarantees 100% monster item drop chance (Default: true, Vanilla: false)");

            ForceHighestRarity = config.Bind(sec, "ForceHighestRarity", true,
                "Equipment drops always roll the highest rarity vanilla permits for that drop (Default: true, Vanilla: false)");

            // [2026-09-30 09:15] Form nested jumlah drop equipment (absolute, tepat N keping).
            EquipmentCountGlobal = config.Bind(sec, "EquipmentDropCount", 1,
                "Equipment (rune) drop count, absolute keping per drop (Default: 1, Vanilla: 1)");

            EquipmentCountSlots = config.Bind(sec, "EquipmentDropCount.Slots", "",
                "Per slot-type override, format: Undercoat=3,Belt=2 (0/kosong = ikut global)");

            EquipmentCountSets = config.Bind(sec, "EquipmentDropCount.Sets", "",
                "Per rune-set override, format: GoblinCaller=2 (0/kosong = ikut level atas)");

            EquipmentCountSetSlots = config.Bind(sec, "EquipmentDropCount.SetSlots", "",
                "Per set+slot override (paling spesifik), format: GoblinCaller/Undercoat=5");

            EquipmentDropCounts.Bind(EquipmentCountGlobal, EquipmentCountSlots, EquipmentCountSets, EquipmentCountSetSlots);
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patch_DropChance));
            harmony.PatchAll(typeof(Patch_ItemDropCheck));
            harmony.PatchAll(typeof(Patch_CalculateGoldDrop));
            harmony.PatchAll(typeof(Patch_HarvestCalculate));
            harmony.PatchAll(typeof(Patch_HarvestBonus));
            // [2026-10-06 12:20] BUGFIX: Patch_RequestHarvestClientRpc was defined but never
            // registered here, so the sanctum farm-plot (crop) yield was never multiplied.
            harmony.PatchAll(typeof(Patch_RequestHarvestClientRpc));
            harmony.PatchAll(typeof(Patch_ReturnRandomRuneData));
            harmony.PatchAll(typeof(Patch_InteractableReward));
            harmony.PatchAll(typeof(Patch_DungeonChest_TrySpawn));
            harmony.PatchAll(typeof(Patch_ChestBasic_SpawnStarter));
            harmony.PatchAll(typeof(Patch_StorageData_AddItem));
            harmony.PatchAll(typeof(Patch_CreateNewStorageData));

            // [2026-09-30 09:15] "Drop table khusus equipment": pool generate, flag sumber loot,
            // dan drain keping ekstra di jalur add rune.
            harmony.PatchAll(typeof(Patch_Pools_ReturnRandomRuneData));
            harmony.PatchAll(typeof(Patch_Pools_GenerateRandomRune));
            harmony.PatchAll(typeof(Patch_SrcFlag_RuneDropCheck));
            harmony.PatchAll(typeof(Patch_SrcFlag_SpawnLoot));
            harmony.PatchAll(typeof(Patch_SrcFlag_SpawnPersonalLoot));
            harmony.PatchAll(typeof(Patch_SrcFlag_RewardPlayer));
            harmony.PatchAll(typeof(Patch_SrcFlag_GrantRuneReward));
            // [2026-09-30 11:10] Obsolete & BERBAHAYA (CRASH): patch HarmonyX pada
            // ServerRPC.AddRuneToWorldServerRpc memicu StackOverflow tanpa batas. Di IL2CPP,
            // HarmonyX (Il2CppInterop.HarmonySupport) tidak bisa memanggil "original" dari method
            // [ServerRpc] ini -- jalur il2cpp_runtime_invoke milik detour berputar balik ke detour
            // sendiri (termasuk lewat [HarmonyReversePatch] kita, karena reverse patch memakai
            // jalur CopyOriginal yang sama). Bukti: BepInEx\ErrorLog.log 2026-09-30 10:55 ->
            // DMD<ServerRPC::AddRuneToWorldServerRpc> muncul 289x, DrainWorld 1x, CallOriginal 1x.
            // Semua sumber (monster/chest/quest) yang men-spawn rune ke dunia lewat RPC ini.
            // Jumlah drop kini ditangani AMAN lewat field native MonsterConfiguration.RuneDropCount
            // (loop multi-drop milik game) di Patch_SrcFlag_RuneDropCheck -> ResolveForConfig().
            // harmony.PatchAll(typeof(Patch_AddRuneToWorld));
            // harmony.PatchAll(typeof(Patch_AddRuneToInventory));
            // [2026-09-30 11:15] Penanda build (satu baris, bukan spam) agar versi aktif bisa diverifikasi.
            // [2026-09-30 12:45] Diagnostik [LootDrop] dimatikan setelah verifikasi; tinggal penanda ini.
            DimraethModPackPlugin.Log?.LogInfo("[Loot] Rune-drop count: tepat-N per (set,slot) via kunci-tipe loop native (opsi c') @2026-09-30 12:45");
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntSpinner(x, curY, width, "Item Drop Multiplier", ItemMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            curY += DrawIntSpinner(x, curY, width, "Gold Multiplier", GoldMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            curY += DrawIntSpinner(x, curY, width, "Harvest Multiplier", HarvestMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            curY += DrawIntSpinner(x, curY, width, "Interactable Multiplier", InteractableMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            // [2026-10-06 12:40] Dedicated crop slider (farm plots), kept separate from resource nodes.
            curY += DrawIntSpinner(x, curY, width, "Crop Harvest Multiplier", CropMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Guarantee Drops (100%)", GuaranteeDrops, "Vanilla: OFF", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Force Highest Rarity", ForceHighestRarity, "Vanilla: OFF", labelStyle, btnStyle);

            // [2026-09-30 09:15] Form nested jumlah drop equipment (global > slot > set > set+slot).
            curY += DrawIntSpinner(x, curY, width, "Equipment Drop Count (Global)", EquipmentCountGlobal, 1, 1, 99, "Vanilla: 1", labelStyle, btnStyle);
            curY += EquipmentDropCounts.DrawNested(x, curY, width, labelStyle, btnStyle);
            return curY - y;
        }

        public static void MultiplyHarvestList(Il2CppSystem.Collections.Generic.List<HarvestedItem> list)
        {
            if (list == null || list.Count == 0) return;
            int mult = Instance != null ? Instance.HarvestMultiplier.Value : 10;
            if (mult <= 1) return;
            try
            {
                var temp = new List<HarvestedItem>();
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    // [2026-09-30 09:15] Recipe tidak pernah di-multiplier (tetap drop 1) — permintaan user.
                    if (IsRecipe(item.ItemType)) { temp.Add(item); continue; }
                    item.ItemCount = Math.Max(1, item.ItemCount) * mult;
                    list[i] = item;
                    temp.Add(item);
                }
                list.Clear();
                for (int i = 0; i < temp.Count; i++)
                {
                    list.Add(temp[i]);
                }
            }
            catch { }
        }

        // [2026-09-30 09:15] Recipe (scroll unlock — ItemType diawali "Recipe", ~80+ entry seperti
        // RecipePinewoodChest / RecipeWeakHealthExtract) TIDAK pernah di-multiplier mana pun:
        // user "kita exclude saja untuk recipee jadi tetap drop 1 saja". Nama di-cache sekali jalan.
        private static HashSet<ItemType> _recipeItems;

        public static bool IsRecipe(ItemType item)
        {
            if (_recipeItems == null)
            {
                _recipeItems = new HashSet<ItemType>();
                foreach (ItemType v in Enum.GetValues(typeof(ItemType)))
                {
                    if (v.ToString().StartsWith("Recipe", StringComparison.Ordinal)) _recipeItems.Add(v);
                }
            }
            return _recipeItems.Contains(item);
        }
    }

    [HarmonyPatch(typeof(MonsterUtils), nameof(MonsterUtils.ApplyDropChanceModifiers))]
    public static class Patch_DropChance
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            if (LootModV2Module.Instance.GuaranteeDrops.Value) __result = 100;
        }
    }

    [HarmonyPatch(typeof(MonsterUtils), nameof(MonsterUtils.ItemDropCheck))]
    public static class Patch_ItemDropCheck
    {
        private static readonly HashSet<int> _scaledConfigs = new();

        public static void ClearCache() => _scaledConfigs.Clear();

        [HarmonyPrefix]
        public static void Prefix(MonsterUtils __instance)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            try
            {
                if (__instance == null || __instance._configuration == null) return;
                var cfg = __instance._configuration;
                int id = cfg.GetInstanceID();
                if (_scaledConfigs.Contains(id)) return;
                _scaledConfigs.Add(id);

                var drops = cfg.ItemDrops;
                if (drops == null) return;

                int mult = LootModV2Module.Instance.ItemMultiplier.Value;
                if (mult <= 1) return;

                for (int i = 0; i < drops.Count; i++)
                {
                    var drop = drops[i];
                    // [2026-09-30 09:15] Recipe di-exclude dari multiplier — tetap drop 1 (permintaan user).
                    if (LootModV2Module.IsRecipe(drop.item)) continue;
                    var range = drop.dropRange;
                    range.min = Math.Max(1, range.min) * mult;
                    range.max = Math.Max(1, range.max) * mult;
                    drop.dropRange = range;
                    drops[i] = drop;
                }
                cfg.ItemDrops = drops;

                /* [2026-09-30 09:15] Obsolete: RuneDropCount (jumlah equipment) tidak lagi dikali
                   ItemMultiplier. Keputusan user "Form = otoritas penuh": jumlah equipment kini
                   ditentukan SEMATA oleh form nested EquipmentDropCounts (absolute, per tipe) yang
                   dieksekusi EquipmentDropBoost di titik add rune. Kode lama:
                if (cfg.IsRuneDrop && cfg.RuneDropCount > 0)
                {
                    cfg.RuneDropCount *= mult;
                }
                */
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateGoldDrop))]
    public static class Patch_CalculateGoldDrop
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            int mult = LootModV2Module.Instance.GoldMultiplier.Value;
            if (mult > 1) __result *= mult;
        }
    }

    [HarmonyPatch(typeof(HarvestClient), nameof(HarvestClient.CalculateItemsForDamage))]
    public static class Patch_HarvestCalculate
    {
        [HarmonyPostfix]
        public static void Postfix(Il2CppSystem.Collections.Generic.List<HarvestedItem> __result)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            LootModV2Module.MultiplyHarvestList(__result);
        }
    }

    [HarmonyPatch(typeof(HarvestClient), nameof(HarvestClient.RollBonusItems))]
    public static class Patch_HarvestBonus
    {
        [HarmonyPostfix]
        public static void Postfix(Il2CppSystem.Collections.Generic.List<HarvestedItem> __result)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            LootModV2Module.MultiplyHarvestList(__result);
        }
    }

    // [2026-10-06 10:30] Sanctum farm-plot nodes (apple tree, flax, etc. — BuildItemType.FlaxPlot=27,
    // AppleTreePlot=45; FarmingPlantType.Flax=4, AppleTree=5) are NOT HarvestClient. They are
    // FarmingClient interactables, and their yield is granted server-side through
    // PlayerBaseManager.RequestHarvestServerRpc -> RequestHarvestClientRpc(itemType, amount).
    // The older HarvestMultiplier patch (Patch_HarvestCalculate/Bonus) only covers wild HarvestClient
    // nodes, so farm-plot yields were never multiplied. This patch applies its own CropMultiplier to that
    // give-items path (user clarified: "i meant crops not nodes" — crops are harvested plants).
    // [2026-10-06 12:20] BUGFIX: this class was previously defined but never registered in ApplyPatches,
    // so it never ran — hence the feature "didn't work".
    // [2026-10-06 12:40] Now uses the DEDICATED CropMultiplier. Previously it rode HarvestMultiplier
    // (the RESOURCE-NODE slider), so a high node value (e.g. 50) turned one apple tree into 300 apples.
    // The server loops the drop list and calls this RPC once PER drop entry, so each entry is scaled;
    // keeping crops on their own low-default slider makes the result predictable (Vanilla x1).
    [HarmonyPatch(typeof(PlayerBaseManager), nameof(PlayerBaseManager.RequestHarvestClientRpc))]
    public static class Patch_RequestHarvestClientRpc
    {
        // [2026-10-06 12:20] One-time diagnostic so an in-game test can confirm the crop harvest
        // path is intercepted (the earlier build never registered this patch).
        private static bool _loggedFirst;

        [HarmonyPrefix]
        public static void Prefix(ItemType itemType, ref int amount)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            // [2026-09-30 09:15] Recipe di-exclude dari multiplier — tetap drop 1 (permintaan user).
            if (LootModV2Module.IsRecipe(itemType)) return;
            // [2026-10-06 12:40] Crops use their own dedicated CropMultiplier (farm-plot harvests only),
            // deliberately NOT HarvestMultiplier (that one targets resource nodes and is often set high).
            int mult = LootModV2Module.Instance.CropMultiplier.Value;
            if (mult <= 1 || amount <= 0) return;
            try
            {
                int before = amount;
                amount = Math.Max(1, amount) * mult;
                if (!_loggedFirst)
                {
                    _loggedFirst = true;
                    DimraethModPackPlugin.Log?.LogInfo($"[Loot] crop harvest {itemType}: {before} -> {amount} (x{mult})");
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Runes), nameof(Runes.ReturnRandomRuneData))]
    public static class Patch_ReturnRandomRuneData
    {
        [HarmonyPrefix]
        public static void Prefix(ref Il2CppSystem.Collections.Generic.List<Runes.Rarity> rarities)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            try
            {
                if (!LootModV2Module.Instance.ForceHighestRarity.Value) return;
                if (rarities == null || rarities.Count <= 1) return;

                Runes.Rarity max = rarities[0];
                for (int i = 1; i < rarities.Count; i++)
                {
                    if (rarities[i] > max) max = rarities[i];
                }

                var only = new Il2CppSystem.Collections.Generic.List<Runes.Rarity>();
                only.Add(max);
                rarities = only;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(InteractableReward), nameof(InteractableReward.ApplyReward))]
    public static class Patch_InteractableReward
    {
        [HarmonyPrefix]
        public static void Prefix(InteractableRewardEntry entry, out int __state)
        {
            __state = 0;
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled || entry == null) return;
            int mult = LootModV2Module.Instance.InteractableMultiplier.Value;
            if (mult <= 1) return;
            try
            {
                if (entry.Type == InteractableRewardType.GrantItem && entry.GrantedItemAmount > 0
                    // [2026-09-30 09:15] Recipe di-exclude dari multiplier — tetap drop 1 (permintaan user).
                    && !LootModV2Module.IsRecipe(entry.GrantedItem))
                {
                    __state = entry.GrantedItemAmount;
                    entry.GrantedItemAmount = Math.Max(1, entry.GrantedItemAmount) * mult;
                }
            }
            catch { }
        }

        [HarmonyPostfix]
        public static void Postfix(InteractableRewardEntry entry, int __state)
        {
            if (entry != null && __state > 0)
            {
                try
                {
                    entry.GrantedItemAmount = __state;
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(DungeonChest), nameof(DungeonChest.TrySpawn))]
    public static class Patch_DungeonChest_TrySpawn
    {
        [HarmonyPrefix]
        public static void Prefix(ItemDrop drop, out IntRange __state)
        {
            __state = default;
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled || drop == null) return;
            // [2026-09-30 09:15] Recipe di-exclude dari multiplier — tetap drop 1 (permintaan user).
            if (LootModV2Module.IsRecipe(drop.item)) return;
            int mult = LootModV2Module.Instance.InteractableMultiplier.Value;
            if (mult <= 1) return;
            try
            {
                __state = drop.dropRange;
                var range = drop.dropRange;
                range.min = Math.Max(1, range.min) * mult;
                range.max = Math.Max(1, range.max) * mult;
                drop.dropRange = range;
            }
            catch { }
        }

        [HarmonyPostfix]
        public static void Postfix(ItemDrop drop, IntRange __state)
        {
            if (drop != null && __state.max > 0)
            {
                try
                {
                    drop.dropRange = __state;
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(ChestBasic), nameof(ChestBasic.SpawnStarterItemsIntoStorage))]
    public static class Patch_ChestBasic_SpawnStarter
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            LootModV2Module.IsSpawningChestLoot = true;
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            LootModV2Module.IsSpawningChestLoot = false;
        }
    }

    [HarmonyPatch(typeof(StorageData), nameof(StorageData.AddItemToStorage))]
    public static class Patch_StorageData_AddItem
    {
        [HarmonyPrefix]
        public static void Prefix(StorageData __instance, ItemType item, ref int amount, int slot)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            if (!LootModV2Module.IsSpawningChestLoot) return;
            // [2026-09-30 09:15] Recipe di-exclude dari multiplier — tetap drop 1 (permintaan user).
            if (LootModV2Module.IsRecipe(item)) return;
            int mult = LootModV2Module.Instance.InteractableMultiplier.Value;
            if (mult <= 1 || amount <= 0) return;
            try
            {
                amount = Math.Max(1, amount) * mult;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(SceneHandler), nameof(SceneHandler.CreateNewStorageData))]
    public static class Patch_CreateNewStorageData
    {
        private static readonly HashSet<IntPtr> _scaledStorageSpawns = new();

        public static void ClearCache() => _scaledStorageSpawns.Clear();

        [HarmonyPrefix]
        public static void Prefix(StorageSpawn store)
        {
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            int mult = LootModV2Module.Instance.InteractableMultiplier.Value;
            if (mult <= 1 || store == null || store.StartingItems == null) return;

            IntPtr id = store.Pointer;
            if (_scaledStorageSpawns.Contains(id)) return;
            _scaledStorageSpawns.Add(id);

            try
            {
                var items = store.StartingItems;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    // [2026-09-30 09:15] Recipe di-exclude dari multiplier — tetap drop 1 (permintaan user).
                    if (LootModV2Module.IsRecipe(item.Item)) continue;
                    if (item.Amount > 0)
                    {
                        item.Amount = Math.Max(1, item.Amount) * mult;
                        items[i] = item;
                    }
                }
            }
            catch { }
        }
    }

    // =====================================================================
    // [2026-09-30 09:15] "Drop table khusus equipment" — patch pendukung EquipmentDropBoost:
    // (1) stash pool rarity/stars dari generate, (2) flag sumber loot (monster/chest/quest),
    // (3) drain N-1 keping ekstra di jalur add rune. Lihat EquipmentDropBoost.cs.
    // =====================================================================

    [HarmonyPatch(typeof(Runes), nameof(Runes.ReturnRandomRuneData))]
    public static class Patch_Pools_ReturnRandomRuneData
    {
        [HarmonyPrefix]
        public static void Prefix(
            ref Il2CppSystem.Collections.Generic.List<Runes.RuneSet> runeSets,
            ref Il2CppSystem.Collections.Generic.List<Runes.SlotType> slotTypes,
            Il2CppSystem.Collections.Generic.List<Runes.Rarity> rarities,
            Il2CppSystem.Collections.Generic.List<Runes.Stars> stars,
            bool empowered)
        {
            EquipmentDropBoost.NotePools(rarities, stars, empowered);
            // [2026-10-06 09:00] Paksa tipe (set,slot) sesuai rencana "semua slot satu set".
            EquipmentDropBoost.ApplyPlannedType(ref runeSets, ref slotTypes);
            // [2026-09-30 11:15] Diagnostik drop: dipanggil sekali per keping rune yang digenerate.
            // [2026-09-30 11:40] Lewati saat sampling tipe (bukan keping nyata).
            // [2026-09-30 12:45] OBSOLETE — diagnostik per-keping hanya untuk verifikasi awal dan sudah
            // terverifikasi (350 keping; tiap blok = N, 2N saat empowered). Dihilangkan agar LogOutput.log
            // tidak banjir. Activate via uncomment bila perlu verifikasi ulang.
            // if (EquipmentDropBoost.Enabled && EquipmentDropBoost.Active && !EquipmentDropBoost.Sampling)
            //     DimraethModPackPlugin.Log?.LogInfo("[LootDrop] rune piece generated (sumber loot aktif)");
        }
    }

    [HarmonyPatch(typeof(Runes), nameof(Runes.GenerateRandomRune))]
    public static class Patch_Pools_GenerateRandomRune
    {
        [HarmonyPrefix]
        public static void Prefix(RuneDrop runeDrop)
        {
            EquipmentDropBoost.NotePools(runeDrop);
        }
    }

    // [2026-09-30 09:15] Flag sumber loot (BeginSource/EndSource). Nama method pakai string karena
    // sebagian privat (RPC/loot internal) — lookup Harmony via AccessTools tetap menemukannya.

    [HarmonyPatch(typeof(MonsterUtils), "RuneDropCheck")]
    public static class Patch_SrcFlag_RuneDropCheck
    {
        // [2026-09-30 11:10] Jumlah drop equipment di-set di sini lewat field native
        // MonsterConfiguration.RuneDropCount. Game sendiri sudah punya loop multi-drop
        // (MonsterUtils.RuneDropCheck) yang men-spawn sampai RuneDropCount keping.
        // [2026-10-06 09:00] UBAH: tiap keping kini dipaksa ke tipe (set,slot) dari rencana
        // "semua slot satu set" via prefix Runes.ReturnRandomRuneData (lihat EquipmentDropBoost).
        // Karena itu kita tidak perlu (dan tidak boleh) menyentuh ServerRPC-nya.
        // Obsolete (dipindah dari drain di jalur add rune): lihat ApplyPatches() + EquipmentDropBoost.
        // Catatan: nilai asli tiap config disimpan agar tidak "nyangkut" saat config diturunkan.
        static readonly System.Collections.Generic.Dictionary<int, int> _origRuneCount = new();
        [HarmonyPrefix]
        public static void Prefix(MonsterUtils __instance)
        {
            EquipmentDropBoost.BeginSource();
            if (LootModV2Module.Instance == null || !LootModV2Module.Instance.IsEnabled) return;
            try
            {
                var cfg = __instance != null ? __instance._configuration : null;
                if (cfg == null || !cfg.IsRuneDrop) return;
                int id = cfg.GetInstanceID();
                if (!_origRuneCount.TryGetValue(id, out int orig))
                {
                    orig = cfg.RuneDropCount;
                    _origRuneCount[id] = orig;
                }
                int n = EquipmentDropBoost.PlanRuneDrop(cfg);
                // [2026-10-06 09:00] ApplyNarrowing (kunci SATU tipe) -> ApplyPlan (semua slot satu set).
                // EquipmentDropBoost.ApplyNarrowing(cfg, n, orig);
                EquipmentDropBoost.ApplyPlan(cfg, n, orig);
                // [2026-09-30 11:15] Diagnostik drop: log nilai + tipe terkunci agar bisa diverifikasi user.
                // [2026-09-30 12:45] OBSOLETE — sudah terverifikasi, dihilangkan agar tidak banjir tiap drop.
                // Nilai N tetap diterapkan lewat ApplyPlan(cfg, n, orig) di atas.
                // DimraethModPackPlugin.Log?.LogInfo($"[LootDrop] RuneDropCheck '{cfg.MonsterName}': N={n}, RuneDropCount={cfg.RuneDropCount}, tipe={EquipmentDropBoost.ForcedLabel}");
            }
            catch { }
        }

        [HarmonyPostfix] public static void Postfix() => EquipmentDropBoost.EndSource();
    }

    [HarmonyPatch(typeof(DungeonChest), "SpawnLoot")]
    public static class Patch_SrcFlag_SpawnLoot
    {
        [HarmonyPrefix] public static void Prefix() => EquipmentDropBoost.BeginSource();
        [HarmonyPostfix] public static void Postfix() => EquipmentDropBoost.EndSource();
    }

    [HarmonyPatch(typeof(DungeonChest), "SpawnPersonalLootForPlayer")]
    public static class Patch_SrcFlag_SpawnPersonalLoot
    {
        [HarmonyPrefix] public static void Prefix() => EquipmentDropBoost.BeginSource();
        [HarmonyPostfix] public static void Postfix() => EquipmentDropBoost.EndSource();
    }

    [HarmonyPatch(typeof(QuestManager), "RewardPlayerServerRpc")]
    public static class Patch_SrcFlag_RewardPlayer
    {
        [HarmonyPrefix] public static void Prefix() => EquipmentDropBoost.BeginSource();
        [HarmonyPostfix] public static void Postfix() => EquipmentDropBoost.EndSource();
    }

    [HarmonyPatch(typeof(QuestManager), "GrantRuneRewardClientRpc")]
    public static class Patch_SrcFlag_GrantRuneReward
    {
        [HarmonyPrefix] public static void Prefix() => EquipmentDropBoost.BeginSource();
        [HarmonyPostfix] public static void Postfix() => EquipmentDropBoost.EndSource();
    }

    [HarmonyPatch(typeof(ServerRPC), nameof(ServerRPC.AddRuneToWorldServerRpc))]
    public static class Patch_AddRuneToWorld
    {
        [HarmonyPrefix]
        public static void Prefix(ServerRPC __instance, ref Rune rune, Areas.Scene scene, Vector2 position,
            ulong networkId, bool enforcePersonalLoot, bool neverShare)
        {
            if (!EquipmentDropBoost.TryBeginBoost(ref rune, out int count)) return;
            EquipmentDropBoost.DrainWorld(__instance, rune, count, scene, position, networkId, enforcePersonalLoot, neverShare);
        }

        // [2026-09-30 10:42] Reverse patch: memanggil method ASLI AddRuneToWorldServerRpc TANPA
        // melewati prefix di atas lagi. Dipakai EquipmentDropBoost.DrainWorld untuk spawn keping ekstra.
        // Sebelumnya DrainWorld memanggil `rpc.AddRuneToWorldServerRpc(...)` langsung; di IL2CPP panggilan
        // itu lewat interop proxy (il2cpp_runtime_invoke) yang diarahkan ke detour HarmonyX lagi, sehingga
        // method memanggil dirinya sendiri tanpa henti -> StackOverflow (BepInEx\ErrorLog.log, 2026-09-30).
        // Aturan: jangan panggil method yang di-patch dari dalam patch-nya sendiri; gunakan stub ini.
        [HarmonyReversePatch]
        [HarmonyPatch(nameof(ServerRPC.AddRuneToWorldServerRpc))]
        public static void CallOriginal(ServerRPC __instance, Rune rune, Areas.Scene scene, Vector2 position,
            ulong networkId, bool enforcePersonalLoot, bool neverShare)
        {
            throw new NotImplementedException("Harmony ReversePatch stub - isi digantikan oleh Harmony.");
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddRuneToInventory))]
    public static class Patch_AddRuneToInventory
    {
        [HarmonyPrefix]
        public static void Prefix(Inventory __instance, ref Rune rune)
        {
            if (!EquipmentDropBoost.TryBeginBoost(ref rune, out int count)) return;
            EquipmentDropBoost.DrainInventory(__instance, rune, count);
        }

        // [2026-09-30 10:42] Alasan sama dengan Patch_AddRuneToWorld.CallOriginal: DrainInventory
        // dulu memanggil `inv.AddRuneToInventory(copy)` langsung sehingga masuk lagi ke detour HarmonyX.
        // Sekarang memakai method asli lewat stub reverse patch ini.
        [HarmonyReversePatch]
        [HarmonyPatch(nameof(Inventory.AddRuneToInventory))]
        public static void CallOriginal(Inventory __instance, Rune rune)
        {
            throw new NotImplementedException("Harmony ReversePatch stub - isi digantikan oleh Harmony.");
        }
    }
}
