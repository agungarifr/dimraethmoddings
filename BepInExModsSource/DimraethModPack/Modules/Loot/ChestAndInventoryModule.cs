using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DimraethModPack.Modules.Loot
{
    public class ChestAndInventoryModule : ModModuleBase
    {
        public static ChestAndInventoryModule Instance { get; private set; }

        public override string Category => "Loot";
        public override string Name => "Any Chest & Inventory";
        public override string Description => "Expands storage capacity for chests and player backpack inventory";

        public ConfigEntry<bool> ApplyToAllChests;
        public ConfigEntry<float> ChestSlotMultiplier;
        public ConfigEntry<int> ChestCustomSlots;
        public ConfigEntry<bool> TargetOnlyRahanerChest;

        public ConfigEntry<bool> EnablePlayerInventoryExpansion;
        public ConfigEntry<float> PlayerSlotMultiplier;
        public ConfigEntry<int> PlayerCustomSlots;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Loot.ChestAndInventory";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Any Chest & Inventory Module (Default: false, Vanilla: false)");

            ApplyToAllChests = config.Bind(sec, "ApplyToAllChests", true,
                "Apply to all chests including crafted containers (pinewood, oak, cabinet, barrel) (Default: true, Vanilla: false)");

            ChestSlotMultiplier = config.Bind(sec, "ChestSlotMultiplier", 3.0f,
                "Multiplier for chest slot capacity (3.0 = 54 slots) (Default: 3.0, Vanilla: 1.0)");

            ChestCustomSlots = config.Bind(sec, "ChestCustomSlots", -1,
                "Exact manual slot count for chests (-1 = use multiplier) (Default: -1, Vanilla: -1)");

            TargetOnlyRahanerChest = config.Bind(sec, "TargetOnlyRahanerChest", false,
                "Only modify Guildhall / Rahaner chest (Default: false, Vanilla: false)");

            EnablePlayerInventoryExpansion = config.Bind(sec, "EnablePlayerInventoryExpansion", true,
                "Expand player backpack inventory slots (Default: true, Vanilla: false)");

            PlayerSlotMultiplier = config.Bind(sec, "PlayerSlotMultiplier", 2.0f,
                "Multiplier for player backpack inventory capacity (2.0 = 2x) (Default: 2.0, Vanilla: 1.0)");

            PlayerCustomSlots = config.Bind(sec, "PlayerCustomSlots", -1,
                "Exact manual slot count for player backpack (-1 = use multiplier) (Default: -1, Vanilla: -1)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Apply To All Chests", ApplyToAllChests, "Vanilla: OFF", labelStyle, btnStyle);
            curY += DrawFloatSpinner(x, curY, width, "Chest Multiplier", ChestSlotMultiplier, 0.5f, 1.0f, 6.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawToggle(x, curY, width, "Expand Player Inventory", EnablePlayerInventoryExpansion, "Vanilla: OFF", labelStyle, btnStyle);
            if (EnablePlayerInventoryExpansion.Value)
            {
                curY += DrawFloatSpinner(x, curY, width, "Player Inv Multiplier", PlayerSlotMultiplier, 0.5f, 1.0f, 4.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            }
            return curY - y;
        }

        public static int GetChestTargetSlotCount(Storage storage)
        {
            // [2026-10-01 12:00] Replaced the inline multiplier/custom-slot math below with a delegation to
            // GetChestTargetSlotCountFromBase so chest-open uses the same clamped, idempotent scaling
            // (the inline version re-scaled StorageData.StorageSize, which may already hold the expanded
            // value, compounding the multiplier: 18 -> 108 -> 216).
            // if (Instance.ChestCustomSlots != null && Instance.ChestCustomSlots.Value > 0)
            // {
            //     int custom = Instance.ChestCustomSlots.Value;
            //     custom = Mathf.CeilToInt(custom / 6f) * 6;
            //     return Math.Clamp(custom, 6, 216);
            // }
            //
            // float mult = Instance.ChestSlotMultiplier != null ? Instance.ChestSlotMultiplier.Value : 3.0f;
            // if (mult < 0.1f) mult = 0.1f;

            int baseSlots = 18;
            try
            {
                if (storage != null && storage.StorageData != null && storage.StorageData.StorageSize > 0)
                {
                    baseSlots = storage.StorageData.StorageSize;
                }
            }
            catch { }

            // int calculated = (int)Math.Round(baseSlots * mult);
            // calculated = Mathf.CeilToInt(calculated / 6f) * 6;
            // return Math.Clamp(calculated, 6, 216);
            return GetChestTargetSlotCountFromBase(baseSlots);
        }

        // Same chest scaling as GetChestTargetSlotCount, but driven by a raw base slot count.
        // Used for the pet's chest directory, whose rows only carry the vanilla-authored
        // slot total (DEFAULT_CHEST_SLOTS = 18) rather than a live Storage reference.
        public static int GetChestTargetSlotCountFromBase(int baseSlots)
        {
            if (Instance.ChestCustomSlots != null && Instance.ChestCustomSlots.Value > 0)
            {
                int custom = Instance.ChestCustomSlots.Value;
                custom = Mathf.CeilToInt(custom / 6f) * 6;
                return Math.Clamp(custom, 6, 216);
            }

            float mult = Instance.ChestSlotMultiplier != null ? Instance.ChestSlotMultiplier.Value : 3.0f;
            if (mult < 0.1f) mult = 0.1f;

            if (baseSlots <= 0 || baseSlots > 18) baseSlots = 18;
            // [2026-10-01 12:00] Clamp the multiplier base to the vanilla chest size (was: if (baseSlots <= 0) baseSlots = 18;).
            // EnsureStorageCapacity/EnsureStorageDataCapacity write the already-expanded count back into
            // StorageData.StorageSize, so re-reading/re-scaling it compounds the multiplier (18 -> 108 -> 216).
            // Chests author 18 slots (StorageDirectory.DEFAULT_CHEST_SLOTS; module docs say 3.0x = 54),
            // so treating any base above 18 as an already-expanded value keeps expansion idempotent.

            int calculated = (int)Math.Round(baseSlots * mult);
            calculated = Mathf.CeilToInt(calculated / 6f) * 6;
            return Math.Clamp(calculated, 6, 216);
        }

        // StorageData-only counterpart of EnsureStorageCapacity (no live Storage/UISlot list yet).
        public static void EnsureStorageDataCapacity(StorageData data, int targetSlots)
        {
            if (data == null) return;
            try
            {
                if (data.StorageSize < targetSlots)
                {
                    data.StorageSize = targetSlots;
                }
                if (data.Entries != null)
                {
                    while (data.Entries.Count < targetSlots)
                    {
                        data.Entries.Add(InventoryEntry.Empty);
                    }
                }
                // [2026-10-01 12:30] Keep the parallel Runes list in lockstep with Entries/StorageSize.
                // Vanilla keeps Runes.Count == Entries.Count == StorageSize (StorageData ctor initialises all
                // three to storageSize; Storage.SyncWithStorageData syncs StorageData.Runes -> Storage.Runes
                // bounded by both counts). Growing only Entries/StorageSize left Runes at the authored 18,
                // which breaks that invariant (observed: size=216, entries=216, runes=18 in ars's World.jrwf)
                // and risks length mismatch on save/load. Pad Runes exactly like the ctor does.
                if (data.Runes != null)
                {
                    while (data.Runes.Count < targetSlots)
                    {
                        data.Runes.Add(Rune.Empty);
                    }
                }
            }
            catch { }
        }

        public static int GetPlayerTargetSlotCount(int currentBase = 36)
        {
            if (Instance.PlayerCustomSlots != null && Instance.PlayerCustomSlots.Value > 0)
            {
                int custom = Instance.PlayerCustomSlots.Value;
                custom = Mathf.CeilToInt(custom / 6f) * 6;
                return Math.Clamp(custom, 36, 180);
            }

            float mult = Instance.PlayerSlotMultiplier != null ? Instance.PlayerSlotMultiplier.Value : 2.0f;
            if (mult < 1.0f) mult = 1.0f;

            int calculated = (int)Math.Round(currentBase * mult);
            calculated = Mathf.CeilToInt(calculated / 6f) * 6;
            return Math.Clamp(calculated, 36, 180);
        }

        public static bool IsInGuildhall(Storage storage)
        {
            if (storage == null) return false;
            try { if (storage.Scene != null && (int)storage.Scene.Value == 24) return true; } catch { }
            try { if (storage.StorageData != null && (int)storage.StorageData.Scene == 24) return true; } catch { }
            try
            {
                if (storage.gameObject != null && storage.gameObject.scene.IsValid())
                {
                    string sceneName = storage.gameObject.scene.name;
                    if (!string.IsNullOrEmpty(sceneName) && sceneName.IndexOf("Guild", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(storage.UniqueID) &&
                    (storage.UniqueID.IndexOf("Guild", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     storage.UniqueID.IndexOf("Rahaner", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;

                if (!string.IsNullOrEmpty(storage.name) &&
                    (storage.name.IndexOf("Guild", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     storage.name.IndexOf("Rahaner", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            }
            catch { }
            return false;
        }

        public static bool IsTargetStorage(Storage storage)
        {
            if (storage == null) return false;
            if (storage.Container == StorageContainers.Rahaner) return true;

            if ((Instance.TargetOnlyRahanerChest != null && Instance.TargetOnlyRahanerChest.Value) ||
                (Instance.ApplyToAllChests != null && !Instance.ApplyToAllChests.Value))
            {
                return IsInGuildhall(storage);
            }

            if (storage.Container == StorageContainers.ChestBasic ||
                storage.Container == StorageContainers.ChestBarrel ||
                storage.Container == StorageContainers.PinewoodChest ||
                storage.Container == StorageContainers.OakCabinet ||
                storage.Container == StorageContainers.HeartOakChest)
            {
                return true;
            }

            try { if (storage.IsChestContainer) return true; } catch { }

            string name = storage.name ?? "";
            string uid = storage.UniqueID ?? "";
            if (name.IndexOf("chest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("cabinet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("barrel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                uid.IndexOf("chest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return IsInGuildhall(storage);
        }

        public static void EnsureStorageCapacity(Storage storage, int targetSlots)
        {
            if (storage == null) return;
            try
            {
                if (storage.StorageData != null)
                {
                    if (storage.StorageData.StorageSize < targetSlots)
                    {
                        storage.StorageData.StorageSize = targetSlots;
                    }
                    if (storage.StorageData.Entries != null)
                    {
                        while (storage.StorageData.Entries.Count < targetSlots)
                        {
                            storage.StorageData.Entries.Add(InventoryEntry.Empty);
                        }
                    }
                    // [2026-10-01 12:30] Keep the parallel Runes list in lockstep with Entries/StorageSize
                    // (see EnsureStorageDataCapacity for the full rationale). Prior code expanded Entries and
                    // StorageSize only, leaving Runes short of StorageSize and violating the vanilla invariant.
                    if (storage.StorageData.Runes != null)
                    {
                        while (storage.StorageData.Runes.Count < targetSlots)
                        {
                            storage.StorageData.Runes.Add(Rune.Empty);
                        }
                    }
                }
                if (storage.Inventory != null)
                {
                    while (storage.Inventory.Count < targetSlots)
                    {
                        storage.Inventory.Add(InventoryEntry.Empty);
                    }
                }
            }
            catch { }
        }

        public static void EnsureStorageViewSlots(StorageView view, int targetCount)
        {
            if (view == null || view._storageSlots == null || view._storageSlots.Count == 0) return;
            int current = view._storageSlots.Count;
            if (current >= targetCount) return;

            var templateSlot = view._storageSlots[current - 1];
            if (templateSlot == null) templateSlot = view._storageSlots[0];
            if (templateSlot == null) return;

            var slotParent = templateSlot.transform.parent;
            if (slotParent == null) return;

            Chest chest = view.TryCast<Chest>();
            GameObject templateBg = null;
            Transform bgParent = null;
            if (chest != null && chest._storageBackgrounds != null && chest._storageBackgrounds.Count > 0)
            {
                templateBg = chest._storageBackgrounds[chest._storageBackgrounds.Count - 1];
                if (templateBg != null) bgParent = templateBg.transform.parent;
            }

            int toAdd = targetCount - current;
            for (int i = 0; i < toAdd; i++)
            {
                var newSlot = UnityEngine.Object.Instantiate(templateSlot, slotParent);
                newSlot.name = $"ModSlot_{view._storageSlots.Count}";
                newSlot.SetActive(true);
                view._storageSlots.Add(newSlot);

                if (chest != null && templateBg != null && bgParent != null && chest._storageBackgrounds != null)
                {
                    var newBg = UnityEngine.Object.Instantiate(templateBg, bgParent);
                    newBg.name = $"ModBg_{chest._storageBackgrounds.Count}";
                    newBg.SetActive(true);
                    chest._storageBackgrounds.Add(newBg);
                }
            }

            if (chest != null)
            {
                chest.SetupChestSlotEventTriggers();
            }
        }

        public static void EnsurePlayerInventoryCapacity(Player player, int targetCount)
        {
            if (player == null || player.Inventory == null) return;
            try
            {
                if (!player.IsSpawned) return;
                if (!player.IsServer && !player.IsOwner) return;
                if (player.Inventory.Count < targetCount)
                {
                    while (player.Inventory.Count < targetCount)
                    {
                        player.Inventory.Add(InventoryEntry.Empty);
                    }
                }
            }
            catch { }
        }

        public static void EnsureInventoryPaneViewSlots(InventoryPaneView paneView, int targetSlots)
        {
            if (paneView == null) return;
            try
            {
                if (paneView.BackpackSlots != null && paneView.BackpackSlots.Count > 0 && paneView.BackpackSlots.Count < targetSlots)
                {
                    int current = paneView.BackpackSlots.Count;
                    var templateSlot = paneView.BackpackSlots[current - 1];
                    var slotParent = templateSlot.transform.parent;

                    GameObject templateBg = null;
                    Transform bgParent = null;
                    if (paneView.BackpackBackgroundSlots != null && paneView.BackpackBackgroundSlots.Count > 0)
                    {
                        templateBg = paneView.BackpackBackgroundSlots[paneView.BackpackBackgroundSlots.Count - 1];
                        if (templateBg != null) bgParent = templateBg.transform.parent;
                    }

                    for (int i = current; i < targetSlots; i++)
                    {
                        var newSlot = UnityEngine.Object.Instantiate(templateSlot, slotParent);
                        newSlot.name = $"BackpackSlot_{i}";
                        newSlot.SetActive(true);

                        var trigger = newSlot.GetComponent<EventTrigger>();
                        if (trigger != null) paneView.WireSlotClickAndHover(trigger, i);

                        paneView.BackpackSlots.Add(newSlot);

                        if (templateBg != null && bgParent != null && paneView.BackpackBackgroundSlots != null)
                        {
                            var newBg = UnityEngine.Object.Instantiate(templateBg, bgParent);
                            newBg.name = $"BackpackBackgroundSlot_{i}";
                            newBg.SetActive(true);
                            paneView.BackpackBackgroundSlots.Add(newBg);
                        }
                    }
                }

                if (paneView.CraftingBackpackSlots != null && paneView.CraftingBackpackSlots.Count > 0 && paneView.CraftingBackpackSlots.Count < targetSlots)
                {
                    int currentCraft = paneView.CraftingBackpackSlots.Count;
                    var templateCraftSlot = paneView.CraftingBackpackSlots[currentCraft - 1];
                    var craftParent = templateCraftSlot.transform.parent;

                    GameObject templateCraftBg = null;
                    Transform craftBgParent = null;
                    if (paneView.CraftingBackgroundSlots != null && paneView.CraftingBackgroundSlots.Count > 0)
                    {
                        templateCraftBg = paneView.CraftingBackgroundSlots[paneView.CraftingBackgroundSlots.Count - 1];
                        if (templateCraftBg != null) craftBgParent = templateCraftBg.transform.parent;
                    }

                    for (int i = currentCraft; i < targetSlots; i++)
                    {
                        var newCraftSlot = UnityEngine.Object.Instantiate(templateCraftSlot, craftParent);
                        newCraftSlot.name = $"CraftingBackpackSlot_{i}";
                        newCraftSlot.SetActive(true);

                        var trigger = newCraftSlot.GetComponent<EventTrigger>();
                        if (trigger != null) paneView.WireSlotClickAndHover(trigger, i);

                        paneView.CraftingBackpackSlots.Add(newCraftSlot);

                        if (templateCraftBg != null && craftBgParent != null && paneView.CraftingBackgroundSlots != null)
                        {
                            var newCraftBg = UnityEngine.Object.Instantiate(templateCraftBg, craftBgParent);
                            newCraftBg.name = $"CraftingBackgroundSlot_{i}";
                            newCraftBg.SetActive(true);
                            paneView.CraftingBackgroundSlots.Add(newCraftBg);
                        }
                    }
                }

                if (paneView._backpackScrollContent != null)
                {
                    var grid = paneView._backpackScrollContent.GetComponent<GridLayoutGroup>();
                    if (grid != null)
                    {
                        int rows = Mathf.CeilToInt(targetSlots / 6f);
                        float totalHeight = rows * (grid.cellSize.y + grid.spacing.y) + grid.padding.top + grid.padding.bottom + 20f;
                        if (paneView._backpackScrollContent.sizeDelta.y < totalHeight)
                        {
                            paneView._backpackScrollContent.sizeDelta = new Vector2(paneView._backpackScrollContent.sizeDelta.x, totalHeight);
                        }
                    }
                }
            }
            catch { }
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(Storage), nameof(Storage.OpenChest))]
            [HarmonyPrefix]
            public static bool Prefix_OpenChest(Storage __instance)
            {
                if (Instance == null || !Instance.IsEnabled) return true;
                try
                {
                    if (__instance == null) return true;
                    if (IsTargetStorage(__instance))
                    {
                        int targetSlots = GetChestTargetSlotCount(__instance);
                        EnsureStorageCapacity(__instance, targetSlots);

                        var ds = DataStorage.Singleton;
                        if (ds != null)
                        {
                            StorageView viewToUse = null;
                            if (targetSlots > 18 && ds.LargeChest != null)
                                viewToUse = ds.LargeChest;
                            else if (ds.Chest != null)
                                viewToUse = ds.Chest;

                            if (viewToUse != null)
                            {
                                EnsureStorageViewSlots(viewToUse, targetSlots);
                                viewToUse.SetStorage(__instance);
                                ds.ActiveChestView = viewToUse;
                                if (ds.PSM != null)
                                {
                                    ds.PSM.SwapToNewState(PlayerUIState.Storage);
                                }
                                return false; // Handled
                            }
                        }
                    }
                }
                catch { }
                return true;
            }

            // The pet panel's chest list ("Send To" menu) is built by StorageDirectory.BuildFor,
            // which caps each row's TotalSlots at the *authored* build slot count (default 18).
            // Growing StorageData.StorageSize on chest-open never lifts that cap, so the list kept
            // showing xx/18. Scale the row total here so the list reflects the modded capacity, and
            // grow the backing storage so the displayed slots are real.
            [HarmonyPatch(typeof(StorageDirectory), nameof(StorageDirectory.BuildFor))]
            [HarmonyPostfix]
            /* [2026-10-01 14:40] FIX (load-time HarmonyX [Error]). Obsolete old signature kept for reference:
                 public static void Postfix_BuildFor(List<ChestDirectoryEntry> __result)
               Why obsolete: StorageDirectory.BuildFor returns the IL2CPP generic list
               Il2CppSystem.Collections.Generic.List<ChestDirectoryEntry>, NOT the managed
               System.Collections.Generic.List<ChestDirectoryEntry>. HarmonyX failed to emit the patch with
               "Cannot assign method return type Il2CppSystem... to __result type System.Collections...",
               which threw out of PatchAll and produced a red error during game load. Other modules already
               use the Il2Cpp list type (LootModV2Module, EquipmentDropBoost, AntiCheatBypassModule). */
            public static void Postfix_BuildFor(Il2CppSystem.Collections.Generic.List<ChestDirectoryEntry> __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (__result == null) return;
                    for (int i = 0; i < __result.Count; i++)
                    {
                        var row = __result[i];
                        if (row.TotalSlots <= 0) continue;

                        Storage live = null;
                        try { live = StorageDirectory.FindLive(row.StorageName); } catch { }

                        // Respect ApplyToAllChests / TargetOnlyRahanerChest when the live storage is known.
                        if (live != null && !IsTargetStorage(live)) continue;

                        int target = GetChestTargetSlotCountFromBase(row.TotalSlots);
                        if (target <= row.TotalSlots) continue;

                        if (live != null)
                        {
                            EnsureStorageCapacity(live, target);
                        }
                        else
                        {
                            try { EnsureStorageDataCapacity(StorageDirectory.FindData(row.StorageName), target); } catch { }
                        }

                        row.TotalSlots = target;
                        __result[i] = row;
                    }
                }
                catch { }
            }

            // Couriers deposit through StorageDirectory.TryResolveDeliverable, whose slotLimit comes
            // from the same authored cap (18). Raise it to the modded target so a pet can actually
            // fill the expanded chest, not just display it.
            [HarmonyPatch(typeof(StorageDirectory), nameof(StorageDirectory.TryResolveDeliverable))]
            [HarmonyPostfix]
            public static void Postfix_TryResolveDeliverable(bool __result, StorageData record, Storage live, ref int slotLimit)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!__result) return;
                try
                {
                    if (live != null && !IsTargetStorage(live)) return;

                    int target = GetChestTargetSlotCountFromBase(slotLimit);
                    if (target <= slotLimit) return;

                    if (record != null) EnsureStorageDataCapacity(record, target);
                    if (live != null) EnsureStorageCapacity(live, target);
                    slotLimit = target;
                }
                catch { }
            }

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetBackpackSlotLimit))]
            [HarmonyPostfix]
            public static void Postfix_GetBackpackSlotLimit(Inventory __instance, ref int __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (Instance.EnablePlayerInventoryExpansion != null && Instance.EnablePlayerInventoryExpansion.Value)
                    {
                        int target = GetPlayerTargetSlotCount(__result);
                        if (target > __result) __result = target;
                    }
                }
                catch { }
            }

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.CalculateVisibleBackpackSlots))]
            [HarmonyPostfix]
            public static void Postfix_CalculateVisibleBackpackSlots(Inventory __instance, int limit, ref int __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (Instance.EnablePlayerInventoryExpansion != null && Instance.EnablePlayerInventoryExpansion.Value)
                    {
                        if (limit > __result) __result = limit;
                    }
                }
                catch { }
            }

            [HarmonyPatch(typeof(InventoryPaneView), nameof(InventoryPaneView.EnsureBackpackGridSpawned))]
            [HarmonyPostfix]
            public static void Postfix_EnsureBackpackGridSpawned(InventoryPaneView __instance)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (Instance.EnablePlayerInventoryExpansion != null && Instance.EnablePlayerInventoryExpansion.Value)
                    {
                        int target = GetPlayerTargetSlotCount();
                        EnsureInventoryPaneViewSlots(__instance, target);
                    }
                }
                catch { }
            }

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.UpdateVisibleBackpackSlots))]
            [HarmonyPrefix]
            public static void Prefix_UpdateVisibleBackpackSlots(Inventory __instance)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (Instance.EnablePlayerInventoryExpansion != null && Instance.EnablePlayerInventoryExpansion.Value)
                    {
                        int target = GetPlayerTargetSlotCount();
                        if (__instance != null && __instance._player != null)
                        {
                            EnsurePlayerInventoryCapacity(__instance._player, target);
                        }

                        if (__instance != null && __instance.BackpackSlots != null && __instance.BackpackSlots.Count > 0 && __instance.BackpackSlots.Count < target)
                        {
                            int current = __instance.BackpackSlots.Count;
                            var templateSlot = __instance.BackpackSlots[current - 1];
                            var slotParent = templateSlot.transform.parent;

                            GameObject templateBg = null;
                            Transform bgParent = null;
                            if (__instance.BackpackBackgroundSlots != null && __instance.BackpackBackgroundSlots.Count > 0)
                            {
                                templateBg = __instance.BackpackBackgroundSlots[__instance.BackpackBackgroundSlots.Count - 1];
                                if (templateBg != null) bgParent = templateBg.transform.parent;
                            }

                            for (int i = current; i < target; i++)
                            {
                                var newSlot = UnityEngine.Object.Instantiate(templateSlot, slotParent);
                                newSlot.name = $"BackpackSlot_{i}";
                                newSlot.SetActive(true);
                                __instance.BackpackSlots.Add(newSlot);

                                if (templateBg != null && bgParent != null && __instance.BackpackBackgroundSlots != null)
                                {
                                    var newBg = UnityEngine.Object.Instantiate(templateBg, bgParent);
                                    newBg.name = $"BackpackBackgroundSlot_{i}";
                                    newBg.SetActive(true);
                                    __instance.BackpackBackgroundSlots.Add(newBg);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
