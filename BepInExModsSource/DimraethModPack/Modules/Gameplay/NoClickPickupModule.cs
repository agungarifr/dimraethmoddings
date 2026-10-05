using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DimraethModPack.Modules.Gameplay
{
    public class NoClickPickupModule : ModModuleBase
    {
        public static NoClickPickupModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "No Click Pickup";
        public override string Description => "Automatically pick up nearby items, runes, and gold without clicking";

        public ConfigEntry<float> PickupRadius;
        public ConfigEntry<bool> AutoVacuumAlways;
        public ConfigEntry<KeyCode> ManualPickupKey;
        public ConfigEntry<float> PulseInterval;
        public ConfigEntry<bool> PickupGold;
        public ConfigEntry<bool> LootEquipment;
        public ConfigEntry<bool> LootMaterials;
        public ConfigEntry<bool> LootConsumables;
        public ConfigEntry<bool> LootOtherItems;
        public ConfigEntry<Runes.Rarity> MinRarityFilter;
        public ConfigEntry<bool> LogPickups;

        private static readonly Runes.Rarity[] _rarityOptions = new Runes.Rarity[]
        {
            Runes.Rarity.Common,
            Runes.Rarity.Uncommon,
            Runes.Rarity.Rare,
            Runes.Rarity.Mythical,
            Runes.Rarity.Heroic,
            Runes.Rarity.Ancient
        };

        private static readonly KeyCode[] _availableKeys = new KeyCode[]
        {
            KeyCode.Space,
            KeyCode.V,
            KeyCode.F,
            KeyCode.G,
            KeyCode.X,
            KeyCode.C,
            KeyCode.LeftAlt,
            KeyCode.LeftControl,
            KeyCode.Mouse2,
            KeyCode.Mouse3,
            KeyCode.Mouse4,
            KeyCode.Tab,
            KeyCode.E,
            KeyCode.Q
        };

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.NoClickPickup";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable No Click Pickup Module (Default: false, Vanilla: false)");

            PickupRadius = config.Bind(sec, "PickupRadius", 15.0f,
                "Loot detection radius in meters (Default: 15.0, Vanilla: 0)");

            AutoVacuumAlways = config.Bind(sec, "AutoVacuumAlways", true,
                "Continuously auto-vacuum loot without pressing any keys (Default: true, Vanilla: false)");

            ManualPickupKey = config.Bind(sec, "ManualPickupKey", KeyCode.Space,
                "Key trigger when Always Auto-Vacuum is disabled (Options: Space, V, F, G, X, C, LeftAlt, LeftControl, Mouse3, etc.)");

            PulseInterval = config.Bind(sec, "PulseInterval", 0.5f,
                "Pickup check interval in seconds (Default: 0.5s, Recommended: 0.3s - 0.75s)");

            PickupGold = config.Bind(sec, "PickupGold", true,
                "Automatically vacuum dropped gold piles (Default: true, Vanilla: false)");

            LootEquipment = config.Bind(sec, "LootEquipment", true,
                "Pick up equipment & runes (Weapons, armor, spell runes, tools) (Default: true)");

            LootMaterials = config.Bind(sec, "LootMaterials", true,
                "Pick up materials & resources (Stone, wood, ores, plant fibers, etc.) (Default: true)");

            LootConsumables = config.Bind(sec, "LootConsumables", true,
                "Pick up consumables (Food, drinks, potions, buffs) (Default: true)");

            LootOtherItems = config.Bind(sec, "LootOtherItems", true,
                "Pick up other items like recipes, pets, spellbooks (Default: true)");

            MinRarityFilter = config.Bind(sec, "MinRarityFilter", Runes.Rarity.Common,
                "Minimum equipment & rune rarity threshold (Common = pick up all, Uncommon+, Rare+, Mythical+, Heroic+, Ancient) (Default: Common)");

            LogPickups = config.Bind(sec, "LogPickups", false,
                "Log item pickup notifications to console (Default: false)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawFloatSpinner(x, curY, width, "Pickup Radius", PickupRadius, 1.0f, 1.0f, 50.0f, "Vanilla: 0", labelStyle, btnStyle, "0.0m");
            curY += DrawToggle(x, curY, width, "Always Auto-Vacuum", AutoVacuumAlways, "Default: ON", labelStyle, btnStyle);
            if (!AutoVacuumAlways.Value)
            {
                curY += DrawEnumPicker(x, curY, width, "Manual Trigger Key", ManualPickupKey, _availableKeys, labelStyle, btnStyle);
                GUI.Label(new Rect(x + 10f, curY, width - 10f, 20f), $"<color=#AAAAAA><i>Hold [{ManualPickupKey.Value}] to vacuum nearby loot.</i></color>", labelStyle);
                curY += 24f;
            }
            curY += DrawFloatSpinner(x, curY, width, "Pulse Interval", PulseInterval, 0.05f, 0.1f, 2.0f, "Default: 0.5s", labelStyle, btnStyle, "0.00s");
            curY += DrawToggle(x, curY, width, "Vacuum Gold", PickupGold, "Default: ON", labelStyle, btnStyle);

            curY += 6f;
            GUI.Label(new Rect(x, curY, width, 22f), "<b><color=#77CCFF>--- Item & Rarity Filters ---</color></b>", labelStyle);
            curY += 26f;

            curY += DrawEnumPicker(x, curY, width, "Min Equipment Rarity", MinRarityFilter, _rarityOptions, labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Loot Equipment / Runes", LootEquipment, "Default: ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Loot Materials / Resources", LootMaterials, "Default: ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Loot Consumables (Food/Potion)", LootConsumables, "Default: ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Loot Other Items (Recipe/etc)", LootOtherItems, "Default: ON", labelStyle, btnStyle);

            return curY - y;
        }

        public static class Patches
        {
            private static float _lastPulseTime = 0f;
            private static readonly List<ItemObject> _itemsBuffer = new(64);
            private static readonly List<GoldDropPickup> _goldBuffer = new(64);
            private static readonly Dictionary<int, float> _retryCooldowns = new(64);
            private static readonly List<int> _expiredCooldownsBuffer = new(64);
            private static readonly Dictionary<ItemType, ItemTooltipCategory> _categoryCache = new(128);

            public static void ClearCaches()
            {
                _retryCooldowns.Clear();
                _expiredCooldownsBuffer.Clear();
                _itemsBuffer.Clear();
                _goldBuffer.Clear();
            }

            private static bool ShouldLootSimpleItem(SimpleObject item)
            {
                if (item == null) return false;

                ItemType itemType = item.ItemType;
                try
                {
                    if (item.NetItemType != null)
                        itemType = item.NetItemType.Value;
                }
                catch { }

                if (!_categoryCache.TryGetValue(itemType, out var cat))
                {
                    if (ItemManager.Singleton != null)
                    {
                        var def = ItemManager.Singleton.GetItem(itemType);
                        cat = def != null ? def.ItemTooltipCategory : (ItemTooltipCategory)(-1);
                        _categoryCache[itemType] = cat;
                    }
                    else
                    {
                        return true;
                    }
                }

                switch (cat)
                {
                    case ItemTooltipCategory.Resource:
                        return Instance.LootMaterials == null || Instance.LootMaterials.Value;

                    case ItemTooltipCategory.Food:
                    case ItemTooltipCategory.Drink:
                    case ItemTooltipCategory.HealthPotion:
                    case ItemTooltipCategory.ConcentrationPotion:
                    case ItemTooltipCategory.StaminaPotion:
                    case ItemTooltipCategory.Cure:
                    case ItemTooltipCategory.Potion:
                        return Instance.LootConsumables == null || Instance.LootConsumables.Value;

                    case ItemTooltipCategory.HarvestTool:
                    case ItemTooltipCategory.OffensiveTool:
                        return Instance.LootEquipment == null || Instance.LootEquipment.Value;

                    default:
                        return Instance.LootOtherItems == null || Instance.LootOtherItems.Value;
                }
            }

            private static bool ShouldLootRune(RuneObject rune)
            {
                if (rune == null) return false;

                if (Instance.LootEquipment != null && !Instance.LootEquipment.Value)
                    return false;

                if (Instance.MinRarityFilter != null && rune.Rarity != null)
                {
                    if (rune.Rarity.Value < Instance.MinRarityFilter.Value)
                        return false;
                }

                return true;
            }

            [HarmonyPatch(typeof(PlayerUpdate), nameof(PlayerUpdate.Update))]
            [HarmonyPostfix]
            public static void Postfix_PlayerUpdate(PlayerUpdate __instance)
            {
                try
                {
                    if (Instance == null || !Instance.IsEnabled) return;
                    if (__instance == null || !__instance.IsOwner) return;

                    var player = __instance._player;
                    if (player == null || player.transform == null) return;

                    if (IsTypingInInputField()) return;

                    bool trigger = false;
                    float interval = Instance.PulseInterval?.Value ?? 0.5f;

                    if (Instance.AutoVacuumAlways.Value)
                    {
                        if (Time.time - _lastPulseTime >= interval)
                        {
                            _lastPulseTime = Time.time;
                            trigger = true;
                        }
                    }
                    else
                    {
                        KeyCode key = Instance.ManualPickupKey?.Value ?? KeyCode.Space;
                        if (Input.GetKey(key))
                        {
                            if (Time.time - _lastPulseTime >= interval)
                            {
                                _lastPulseTime = Time.time;
                                trigger = true;
                            }
                        }
                    }

                    if (trigger)
                    {
                        PerformPickup(player);
                    }
                }
                catch { }
            }

            private static void PerformPickup(Player player)
            {
                float now = Time.time;
                Vector3 playerPos = player.transform.position;
                float radius = Instance.PickupRadius?.Value ?? 15.0f;
                float radiusSqr = radius * radius;

                _itemsBuffer.Clear();

                // Periodic purge of old cooldown entries without allocation
                if (_retryCooldowns.Count > 100)
                {
                    _expiredCooldownsBuffer.Clear();
                    foreach (var kvp in _retryCooldowns)
                    {
                        if (now > kvp.Value) _expiredCooldownsBuffer.Add(kvp.Key);
                    }
                    for (int i = 0; i < _expiredCooldownsBuffer.Count; i++) _retryCooldowns.Remove(_expiredCooldownsBuffer[i]);
                }

                // 1. SimpleObject
                try
                {
                    if (SimpleObject.ActiveInstances != null)
                    {
                        foreach (var simpleItem in SimpleObject.ActiveInstances)
                        {
                            try
                            {
                                if (simpleItem == null || !simpleItem || simpleItem.WasCollected)
                                    continue;
                                if (simpleItem.gameObject == null || !simpleItem.gameObject.activeInHierarchy)
                                    continue;
                                if (simpleItem.PickupRequestPending) continue;

                                int id = simpleItem.GetInstanceID();
                                if (_retryCooldowns.TryGetValue(id, out float cd) && now < cd) continue;

                                if (simpleItem.IsHiddenFromLocalPlayer(player)) continue;

                                Vector3 itemPos = simpleItem.transform.position;
                                float dx = playerPos.x - itemPos.x;
                                float dy = playerPos.y - itemPos.y;
                                float dz = playerPos.z - itemPos.z;
                                if (dx * dx + dy * dy + dz * dz <= radiusSqr)
                                {
                                    if (ShouldLootSimpleItem(simpleItem))
                                    {
                                        _itemsBuffer.Add(simpleItem);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // 2. RuneObject
                try
                {
                    if (RuneObject.ActiveInstances != null)
                    {
                        foreach (var runeItem in RuneObject.ActiveInstances)
                        {
                            try
                            {
                                if (runeItem == null || !runeItem || runeItem.WasCollected)
                                    continue;
                                if (runeItem.gameObject == null || !runeItem.gameObject.activeInHierarchy)
                                    continue;
                                if (runeItem.PickupRequestPending) continue;

                                int id = runeItem.GetInstanceID();
                                if (_retryCooldowns.TryGetValue(id, out float cd) && now < cd) continue;

                                if (runeItem.IsHiddenFromLocalPlayer(player)) continue;

                                Vector3 itemPos = runeItem.transform.position;
                                float dx = playerPos.x - itemPos.x;
                                float dy = playerPos.y - itemPos.y;
                                float dz = playerPos.z - itemPos.z;
                                if (dx * dx + dy * dy + dz * dz <= radiusSqr)
                                {
                                    if (ShouldLootRune(runeItem))
                                    {
                                        _itemsBuffer.Add(runeItem);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // Pickup items with cooldown guard against full inventory spam
                int itemsLooted = 0;
                for (int i = 0; i < _itemsBuffer.Count; i++)
                {
                    var item = _itemsBuffer[i];
                    try
                    {
                        if (item != null && item && !item.WasCollected && item.gameObject != null && item.gameObject.activeInHierarchy && !item.PickupRequestPending)
                        {
                            int id = item.GetInstanceID();
                            _retryCooldowns[id] = now + 1.5f;
                            item.PickupItem(player);
                            itemsLooted++;
                        }
                    }
                    catch { }
                }

                if (itemsLooted > 0)
                {
                    DiagnosticsManager.Log("NoClickPickup", $"Vacuumed {itemsLooted} nearby item(s)/rune(s).");
                }

                // 3. Gold
                if (Instance.PickupGold != null && Instance.PickupGold.Value)
                {
                    try
                    {
                        if (GoldDropPickup.ActiveInstances != null)
                        {
                            _goldBuffer.Clear();
                            foreach (var gold in GoldDropPickup.ActiveInstances)
                            {
                                try
                                {
                                    if (gold == null || !gold || gold.WasCollected)
                                        continue;
                                    if (gold.gameObject == null || !gold.gameObject.activeInHierarchy)
                                        continue;

                                    Vector3 goldPos = gold.transform.position;
                                    float dx = playerPos.x - goldPos.x;
                                    float dy = playerPos.y - goldPos.y;
                                    float dz = playerPos.z - goldPos.z;
                                    if (dx * dx + dy * dy + dz * dz <= radiusSqr)
                                    {
                                        _goldBuffer.Add(gold);
                                    }
                                }
                                catch { }
                            }

                            int goldLooted = 0;
                            for (int i = 0; i < _goldBuffer.Count; i++)
                            {
                                var gold = _goldBuffer[i];
                                try
                                {
                                    if (gold != null && gold && !gold.WasCollected && gold.gameObject != null)
                                    {
                                        gold.transform.position = player.transform.position;
                                        try { gold.RequestPetPickupServerRpc(player.OwnerClientId); } catch { }
                                        goldLooted++;
                                    }
                                }
                                catch { }
                            }

                            if (goldLooted > 0)
                            {
                                DiagnosticsManager.Log("NoClickPickup", $"Vacuumed {goldLooted} gold pile(s).");
                            }
                        }
                    }
                    catch { }
                }
            }

            private static bool IsTypingInInputField()
            {
                try
                {
                    var eventSystem = EventSystem.current;
                    if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
                    {
                        var selected = eventSystem.currentSelectedGameObject;
                        if (selected.GetComponent("TMP_InputField") != null ||
                            selected.GetComponent("InputField") != null ||
                            selected.name.ToLower().Contains("input"))
                        {
                            return true;
                        }
                    }
                }
                catch { }
                return false;
            }
        }
    }
}
