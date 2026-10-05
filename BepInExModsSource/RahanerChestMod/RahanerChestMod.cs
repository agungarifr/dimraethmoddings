using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RahanerChestMod
{
    [BepInPlugin("com.custom.rahanerchestmod", "AnyChestAndInventoryMod", "2.0.0")]
    public class RahanerChestModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        // Chest settings
        public static ConfigEntry<bool> ApplyToAllChests;
        public static ConfigEntry<float> ChestSlotMultiplier;
        public static ConfigEntry<int> ChestCustomSlots;
        public static ConfigEntry<bool> TargetOnlyRahanerChest;

        // Player inventory settings
        public static ConfigEntry<bool> EnablePlayerInventoryExpansion;
        public static ConfigEntry<float> PlayerSlotMultiplier;
        public static ConfigEntry<int> PlayerCustomSlots;

        // Diagnostics
        public static ConfigEntry<bool> EnableLogging;

        public override void Load()
        {
            Log = base.Log;

            // Chest configuration
            ApplyToAllChests = Config.Bind(
                "Chests",
                "ApplyToAllChests",
                true,
                "If true, expands all chests in the game, including player-crafted chests (pinewood chest, oak cabinet, heart oak chest, barrel, etc.) and basic chests. If false, only modifies the Guildhall chest. (Default: true)"
            );

            ChestSlotMultiplier = Config.Bind(
                "Chests",
                "ChestSlotMultiplier",
                3.0f,
                "Multiplier applied to chest tile slots (e.g. 2.0 = 36 slots, 3.0 = 54 slots, 4.0 = 72 slots, 5.0 = 90 slots). Rounds up to full 6-slot rows. (Default: 3.0)"
            );

            ChestCustomSlots = Config.Bind(
                "Chests",
                "ChestCustomSlots",
                -1,
                "Directly set the exact number of tile slots for chests (e.g. 36, 54, 72, 90). Set to -1 to use ChestSlotMultiplier instead. (Default: -1)"
            );

            TargetOnlyRahanerChest = Config.Bind(
                "Chests",
                "TargetOnlyRahanerChest",
                false,
                "Backward compatibility option: if true, forces targeting ONLY the Guildhall chest next to Rahaner. (Default: false)"
            );

            // Player inventory configuration
            EnablePlayerInventoryExpansion = Config.Bind(
                "PlayerInventory",
                "EnablePlayerInventoryExpansion",
                true,
                "If true, expands the player character's inventory tile slots. (Default: true)"
            );

            PlayerSlotMultiplier = Config.Bind(
                "PlayerInventory",
                "PlayerSlotMultiplier",
                2.0f,
                "Multiplier applied to player character backpack slots (e.g. 1.5 = 1.5x, 2.0 = 2x, etc.). (Default: 2.0)"
            );

            PlayerCustomSlots = Config.Bind(
                "PlayerInventory",
                "PlayerCustomSlots",
                -1,
                "Directly set the exact number of tile slots for player inventory (e.g. 48, 60, 72, 84, 96). Set to -1 to use PlayerSlotMultiplier instead. (Default: -1)"
            );

            // Logging
            EnableLogging = Config.Bind(
                "General",
                "EnableLogging",
                true,
                "Enable informational logs in the console to diagnose chest openings and inventory expansion. (Default: true)"
            );

            // Legacy config migration
            var legacyMult = Config.Bind("General", "TileSlotMultiplier", -1.0f, "Legacy chest multiplier");
            var legacyCustom = Config.Bind("General", "CustomSlotCount", -1, "Legacy custom chest slots");
            if (legacyMult.Value > 0f && Math.Abs(ChestSlotMultiplier.Value - 3.0f) < 0.01f)
            {
                ChestSlotMultiplier.Value = legacyMult.Value;
            }
            if (legacyCustom.Value > 0 && ChestCustomSlots.Value <= 0)
            {
                ChestCustomSlots.Value = legacyCustom.Value;
            }

            Harmony.CreateAndPatchAll(typeof(ChestAndInventoryPatches));

            Log.LogInfo("=================================================");
            Log.LogInfo("AnyChestAndInventoryMod v2.0.0 (BepInEx 6) Initialized!");
            Log.LogInfo($"Apply To All Chests: {ApplyToAllChests.Value} (TargetOnlyRahanerChest: {TargetOnlyRahanerChest.Value})");
            Log.LogInfo($"Chest Multiplier: {ChestSlotMultiplier.Value}x (Custom: {ChestCustomSlots.Value})");
            Log.LogInfo($"Player Inventory Expansion: {EnablePlayerInventoryExpansion.Value} (Multiplier: {PlayerSlotMultiplier.Value}x, Custom: {PlayerCustomSlots.Value})");
            Log.LogInfo("=================================================");
        }

        public static int GetChestTargetSlotCount(Storage storage)
        {
            if (ChestCustomSlots != null && ChestCustomSlots.Value > 0)
            {
                int custom = ChestCustomSlots.Value;
                custom = Mathf.CeilToInt(custom / 6f) * 6;
                return Math.Clamp(custom, 6, 216);
            }

            float mult = ChestSlotMultiplier != null ? ChestSlotMultiplier.Value : 3.0f;
            if (mult < 0.1f) mult = 0.1f;

            int baseSlots = 18;
            try
            {
                if (storage != null && storage.StorageData != null && storage.StorageData.StorageSize > 0)
                {
                    baseSlots = storage.StorageData.StorageSize;
                }
            }
            catch { }

            int calculated = (int)Math.Round(baseSlots * mult);
            calculated = Mathf.CeilToInt(calculated / 6f) * 6;
            return Math.Clamp(calculated, 6, 216);
        }

        public static int GetPlayerTargetSlotCount(int currentBase = 36)
        {
            if (PlayerCustomSlots != null && PlayerCustomSlots.Value > 0)
            {
                int custom = PlayerCustomSlots.Value;
                custom = Mathf.CeilToInt(custom / 6f) * 6;
                return Math.Clamp(custom, 36, 180);
            }

            float mult = PlayerSlotMultiplier != null ? PlayerSlotMultiplier.Value : 2.0f;
            if (mult < 1.0f) mult = 1.0f;

            int calculated = (int)Math.Round(currentBase * mult);
            calculated = Mathf.CeilToInt(calculated / 6f) * 6;
            return Math.Clamp(calculated, 36, 180);
        }

        public static bool IsInGuildhall(Storage storage)
        {
            if (storage == null) return false;

            try
            {
                if (storage.Scene != null && (int)storage.Scene.Value == 24)
                    return true;
            }
            catch { }

            try
            {
                if (storage.StorageData != null && (int)storage.StorageData.Scene == 24)
                    return true;
            }
            catch { }

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
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    string aName = activeScene.name;
                    if (!string.IsNullOrEmpty(aName) && aName.IndexOf("Guild", StringComparison.OrdinalIgnoreCase) >= 0)
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

            // Direct Rahaner enum tag
            if (storage.Container == StorageContainers.Rahaner)
                return true;

            // If configured to only target the Guildhall / Rahaner chest:
            if ((TargetOnlyRahanerChest != null && TargetOnlyRahanerChest.Value) ||
                (ApplyToAllChests != null && !ApplyToAllChests.Value))
            {
                return IsInGuildhall(storage);
            }

            // Target all chest types (crafted, basic, cabinet, barrel, rahaner)
            if (storage.Container == StorageContainers.ChestBasic ||
                storage.Container == StorageContainers.ChestBarrel ||
                storage.Container == StorageContainers.PinewoodChest ||
                storage.Container == StorageContainers.OakCabinet ||
                storage.Container == StorageContainers.HeartOakChest)
            {
                return true;
            }

            try
            {
                if (storage.IsChestContainer) return true;
            }
            catch { }

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
                        if (EnableLogging != null && EnableLogging.Value)
                        {
                            Log.LogInfo($"Expanding StorageSize from {storage.StorageData.StorageSize} to {targetSlots} on {storage.Container}");
                        }
                        storage.StorageData.StorageSize = targetSlots;
                    }

                    if (storage.StorageData.Entries != null)
                    {
                        while (storage.StorageData.Entries.Count < targetSlots)
                        {
                            storage.StorageData.Entries.Add(InventoryEntry.Empty);
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
            catch (Exception ex)
            {
                Log.LogError($"Error in EnsureStorageCapacity: {ex.Message}");
            }
        }

        public static void EnsureStorageViewSlots(StorageView view, int targetCount)
        {
            if (view == null || view._storageSlots == null || view._storageSlots.Count == 0)
                return;

            int current = view._storageSlots.Count;
            if (current >= targetCount)
                return;

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
                if (templateBg != null)
                {
                    bgParent = templateBg.transform.parent;
                }
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

            if (EnableLogging != null && EnableLogging.Value)
            {
                Log.LogInfo($"Expanded {view.GetIl2CppType().Name} UI slots from {current} to {view._storageSlots.Count}");
            }
        }

        public static void EnsurePlayerInventoryCapacity(Player player, int targetCount)
        {
            if (player == null || player.Inventory == null) return;

            try
            {
                if (player.Inventory.Count < targetCount)
                {
                    int added = 0;
                    while (player.Inventory.Count < targetCount)
                    {
                        player.Inventory.Add(InventoryEntry.Empty);
                        added++;
                    }
                    if (EnableLogging != null && EnableLogging.Value && added > 0)
                    {
                        Log.LogInfo($"Padded Player.Inventory with {added} empty entries (now {player.Inventory.Count}).");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in EnsurePlayerInventoryCapacity: {ex.Message}");
            }
        }

        public static void EnsureInventoryPaneViewSlots(InventoryPaneView paneView, int targetSlots)
        {
            if (paneView == null) return;

            try
            {
                // 1. Expand BackpackSlots
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
                        if (trigger != null)
                        {
                            paneView.WireSlotClickAndHover(trigger, i);
                        }

                        paneView.BackpackSlots.Add(newSlot);

                        if (templateBg != null && bgParent != null && paneView.BackpackBackgroundSlots != null)
                        {
                            var newBg = UnityEngine.Object.Instantiate(templateBg, bgParent);
                            newBg.name = $"BackpackBackgroundSlot_{i}";
                            newBg.SetActive(true);
                            paneView.BackpackBackgroundSlots.Add(newBg);
                        }
                    }

                    if (EnableLogging != null && EnableLogging.Value)
                    {
                        Log.LogInfo($"Expanded InventoryPaneView BackpackSlots from {current} to {paneView.BackpackSlots.Count}");
                    }
                }

                // 2. Expand CraftingBackpackSlots if present
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
                        if (trigger != null)
                        {
                            paneView.WireSlotClickAndHover(trigger, i);
                        }

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

                // 3. Ensure scroll content height
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
            catch (Exception ex)
            {
                Log.LogError($"Error in EnsureInventoryPaneViewSlots: {ex.Message}");
            }
        }
    }

    public static class ChestAndInventoryPatches
    {
        // -------------------------------------------------------------
        // CHEST HOOK: Intercept OpenChest for all chests
        // -------------------------------------------------------------
        [HarmonyPatch(typeof(Storage), nameof(Storage.OpenChest))]
        [HarmonyPrefix]
        public static bool Prefix_OpenChest(Storage __instance)
        {
            try
            {
                if (__instance == null) return true;

                bool isTarget = RahanerChestModPlugin.IsTargetStorage(__instance);

                if (RahanerChestModPlugin.EnableLogging != null && RahanerChestModPlugin.EnableLogging.Value)
                {
                    string activeScene = SceneManager.GetActiveScene().name;
                    string objScene = __instance.gameObject != null ? __instance.gameObject.scene.name : "null";
                    int currentSize = __instance.StorageData != null ? __instance.StorageData.StorageSize : -1;
                    int invCount = __instance.Inventory != null ? __instance.Inventory.Count : -1;
                    RahanerChestModPlugin.Log.LogInfo($"OpenChest: Container={__instance.Container}, ObjScene='{objScene}', ActiveScene='{activeScene}', Name='{__instance.name}', StorageSize={currentSize}, InvCount={invCount}, Match={isTarget}");
                }

                if (isTarget)
                {
                    int targetSlots = RahanerChestModPlugin.GetChestTargetSlotCount(__instance);
                    RahanerChestModPlugin.EnsureStorageCapacity(__instance, targetSlots);

                    var ds = DataStorage.Singleton;
                    if (ds != null)
                    {
                        StorageView viewToUse = null;
                        if (targetSlots > 18 && ds.LargeChest != null)
                        {
                            viewToUse = ds.LargeChest;
                        }
                        else if (ds.Chest != null)
                        {
                            viewToUse = ds.Chest;
                        }

                        if (viewToUse != null)
                        {
                            RahanerChestModPlugin.EnsureStorageViewSlots(viewToUse, targetSlots);
                            viewToUse.SetStorage(__instance);
                            ds.ActiveChestView = viewToUse;
                            if (ds.PSM != null)
                            {
                                ds.PSM.SwapToNewState(PlayerUIState.Storage);
                            }

                            if (RahanerChestModPlugin.EnableLogging != null && RahanerChestModPlugin.EnableLogging.Value)
                            {
                                RahanerChestModPlugin.Log.LogInfo($"Opened chest ({targetSlots} slots) via {viewToUse.GetIl2CppType().Name} view.");
                            }
                            return false; // Handled custom open
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RahanerChestModPlugin.Log.LogError($"Exception in Prefix_OpenChest: {ex}");
            }

            return true;
        }

        // -------------------------------------------------------------
        // PLAYER INVENTORY HOOKS
        // -------------------------------------------------------------
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetBackpackSlotLimit))]
        [HarmonyPostfix]
        public static void Postfix_GetBackpackSlotLimit(Inventory __instance, ref int __result)
        {
            try
            {
                if (RahanerChestModPlugin.EnablePlayerInventoryExpansion == null ||
                    !RahanerChestModPlugin.EnablePlayerInventoryExpansion.Value)
                    return;

                int target = RahanerChestModPlugin.GetPlayerTargetSlotCount(__result);
                if (target > __result)
                {
                    __result = target;
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CalculateVisibleBackpackSlots))]
        [HarmonyPostfix]
        public static void Postfix_CalculateVisibleBackpackSlots(Inventory __instance, int limit, ref int __result)
        {
            try
            {
                if (RahanerChestModPlugin.EnablePlayerInventoryExpansion == null ||
                    !RahanerChestModPlugin.EnablePlayerInventoryExpansion.Value)
                    return;

                if (limit > __result)
                {
                    __result = limit;
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(InventoryPaneView), nameof(InventoryPaneView.EnsureBackpackGridSpawned))]
        [HarmonyPostfix]
        public static void Postfix_EnsureBackpackGridSpawned(InventoryPaneView __instance)
        {
            try
            {
                if (RahanerChestModPlugin.EnablePlayerInventoryExpansion == null ||
                    !RahanerChestModPlugin.EnablePlayerInventoryExpansion.Value)
                    return;

                int target = RahanerChestModPlugin.GetPlayerTargetSlotCount();
                RahanerChestModPlugin.EnsureInventoryPaneViewSlots(__instance, target);
            }
            catch (Exception ex)
            {
                RahanerChestModPlugin.Log.LogError($"Exception in Postfix_EnsureBackpackGridSpawned: {ex}");
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.UpdateVisibleBackpackSlots))]
        [HarmonyPrefix]
        public static void Prefix_UpdateVisibleBackpackSlots(Inventory __instance)
        {
            try
            {
                if (RahanerChestModPlugin.EnablePlayerInventoryExpansion == null ||
                    !RahanerChestModPlugin.EnablePlayerInventoryExpansion.Value)
                    return;

                int target = RahanerChestModPlugin.GetPlayerTargetSlotCount();

                // 1. Ensure Player.Inventory has capacity
                if (__instance != null && __instance._player != null)
                {
                    RahanerChestModPlugin.EnsurePlayerInventoryCapacity(__instance._player, target);
                }

                // 2. Ensure Inventory.BackpackSlots list has enough GameObjects
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
            catch (Exception ex)
            {
                RahanerChestModPlugin.Log.LogError($"Exception in Prefix_UpdateVisibleBackpackSlots: {ex}");
            }
        }
    }
}
