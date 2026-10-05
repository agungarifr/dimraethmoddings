using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AutoPickupMod
{
    [BepInPlugin("com.custom.autopickupmod", "AutoPickupMod", "1.0.0")]
    public class AutoPickupModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<float> PickupRadius;
        public static ConfigEntry<KeyCode> TriggerKey;
        public static ConfigEntry<bool> HoldToPickup;
        public static ConfigEntry<float> HoldPickupInterval;
        public static ConfigEntry<bool> PickupGold;
        public static ConfigEntry<bool> LogPickups;

        public override void Load()
        {
            Log = base.Log;

            ModEnabled = Config.Bind(
                "General",
                "ModEnabled",
                true,
                "Enable or disable the Auto Pickup Mod (Default: true)"
            );

            PickupRadius = Config.Bind(
                "General",
                "PickupRadius",
                15.0f,
                "Radius in world units around the character to detect and vacuum dropped items (Default: 15.0)"
            );

            TriggerKey = Config.Bind(
                "General",
                "TriggerKey",
                KeyCode.Space,
                "Keyboard key to trigger auto pickup (Default: Space)"
            );

            HoldToPickup = Config.Bind(
                "General",
                "HoldToPickup",
                false,
                "If true, continuously auto-pickups while holding the key instead of requiring individual presses (Default: false)"
            );

            HoldPickupInterval = Config.Bind(
                "General",
                "HoldPickupInterval",
                0.15f,
                "Pulse interval in seconds when HoldToPickup is enabled (Default: 0.15)"
            );

            PickupGold = Config.Bind(
                "General",
                "PickupGold",
                true,
                "Whether to also auto-collect dropped gold piles within the radius (Default: true)"
            );

            LogPickups = Config.Bind(
                "General",
                "LogPickups",
                false,
                "Log auto-pickup collection events to BepInEx console/log (Default: false)"
            );

            Harmony.CreateAndPatchAll(typeof(Patch_PlayerUpdate_Update));

            Log.LogInfo("=================================================");
            Log.LogInfo("AutoPickupMod v1.0.0 (BepInEx 6) Initialized!");
            Log.LogInfo($"Trigger Key: {TriggerKey.Value}");
            Log.LogInfo($"Pickup Radius: {PickupRadius.Value} units");
            Log.LogInfo($"Hold to Pickup: {HoldToPickup.Value}");
            Log.LogInfo($"Auto-collect Gold: {PickupGold.Value}");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");
        }
    }

    /// <summary>
    /// Hooks PlayerUpdate.Update on the local player character.
    /// Detects Space key press and vacuums all dropped items/gold in the configured radius.
    /// </summary>
    [HarmonyPatch(typeof(PlayerUpdate), nameof(PlayerUpdate.Update))]
    public static class Patch_PlayerUpdate_Update
    {
        private static float _lastHoldTriggerTime = 0f;

        public static void Postfix(PlayerUpdate __instance)
        {
            try
            {
                if (AutoPickupModPlugin.ModEnabled == null || !AutoPickupModPlugin.ModEnabled.Value)
                    return;

                if (__instance == null || !__instance.IsOwner)
                    return;

                var player = __instance._player;
                if (player == null || player.transform == null)
                    return;

                // Do not trigger while typing in an on-screen text input (chat, rename, etc.)
                if (IsTypingInInputField())
                    return;

                KeyCode key = AutoPickupModPlugin.TriggerKey != null ? AutoPickupModPlugin.TriggerKey.Value : KeyCode.Space;
                bool shouldTrigger = false;

                if (AutoPickupModPlugin.HoldToPickup != null && AutoPickupModPlugin.HoldToPickup.Value)
                {
                    if (IsKeyHeld(key))
                    {
                        float interval = AutoPickupModPlugin.HoldPickupInterval != null ? AutoPickupModPlugin.HoldPickupInterval.Value : 0.15f;
                        if (Time.time - _lastHoldTriggerTime >= interval)
                        {
                            _lastHoldTriggerTime = Time.time;
                            shouldTrigger = true;
                        }
                    }
                }
                else
                {
                    if (IsKeyPressed(key))
                    {
                        shouldTrigger = true;
                    }
                }

                if (shouldTrigger)
                {
                    PerformAutoPickup(player);
                }
            }
            catch (Exception ex)
            {
                AutoPickupModPlugin.Log.LogError($"Error in Patch_PlayerUpdate_Update: {ex}");
            }
        }

        /// <summary>
        /// Collects all dropped simple items, runes, and gold piles within the radius.
        /// </summary>
        private static void PerformAutoPickup(Player player)
        {
            Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
            float radius = AutoPickupModPlugin.PickupRadius != null ? AutoPickupModPlugin.PickupRadius.Value : 15.0f;
            int itemsCount = 0;
            int goldCount = 0;

            var itemsToPickup = new List<ItemObject>();

            // 1. Gather all dropped SimpleObject instances (weapons, tools, armor, potions, materials, quest items)
            try
            {
                if (SimpleObject.ActiveInstances != null)
                {
                    foreach (var simpleItem in SimpleObject.ActiveInstances)
                    {
                        if (simpleItem == null || simpleItem.gameObject == null || !simpleItem.gameObject.activeInHierarchy)
                            continue;

                        if (simpleItem.PickupRequestPending)
                            continue;

                        if (simpleItem.IsHiddenFromLocalPlayer(player))
                            continue;

                        float dist = Vector2.Distance(playerPos, new Vector2(simpleItem.transform.position.x, simpleItem.transform.position.y));
                        if (dist <= radius)
                        {
                            itemsToPickup.Add(simpleItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AutoPickupModPlugin.Log.LogError($"Error gathering SimpleObject instances: {ex}");
            }

            // 2. Gather all dropped RuneObject instances
            try
            {
                if (RuneObject.ActiveInstances != null)
                {
                    foreach (var runeItem in RuneObject.ActiveInstances)
                    {
                        if (runeItem == null || runeItem.gameObject == null || !runeItem.gameObject.activeInHierarchy)
                            continue;

                        if (runeItem.PickupRequestPending)
                            continue;

                        if (runeItem.IsHiddenFromLocalPlayer(player))
                            continue;

                        float dist = Vector2.Distance(playerPos, new Vector2(runeItem.transform.position.x, runeItem.transform.position.y));
                        if (dist <= radius)
                        {
                            itemsToPickup.Add(runeItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AutoPickupModPlugin.Log.LogError($"Error gathering RuneObject instances: {ex}");
            }

            // 3. Execute item pickups into inventory
            foreach (var item in itemsToPickup)
            {
                try
                {
                    if (item != null && item.gameObject != null && item.gameObject.activeInHierarchy && !item.PickupRequestPending)
                    {
                        item.PickupItem(player);
                        itemsCount++;
                    }
                }
                catch (Exception ex)
                {
                    AutoPickupModPlugin.Log.LogError($"Error picking up item {item?.name}: {ex}");
                }
            }

            // 4. Gather and vacuum dropped Gold piles
            bool pickupGold = AutoPickupModPlugin.PickupGold != null && AutoPickupModPlugin.PickupGold.Value;
            if (pickupGold)
            {
                try
                {
                    if (GoldDropPickup.ActiveInstances != null)
                    {
                        var goldList = new List<GoldDropPickup>();
                        foreach (var gold in GoldDropPickup.ActiveInstances)
                        {
                            if (gold == null || gold.gameObject == null || !gold.gameObject.activeInHierarchy)
                                continue;

                            float dist = Vector2.Distance(playerPos, new Vector2(gold.transform.position.x, gold.transform.position.y));
                            if (dist <= radius)
                            {
                                goldList.Add(gold);
                            }
                        }

                        foreach (var gold in goldList)
                        {
                            try
                            {
                                if (gold != null && gold.gameObject != null)
                                {
                                    // Teleporting the gold pile onto the player immediately satisfies the
                                    // < 2.0f distance trigger threshold in GoldDropPickup.Update(), picking it up!
                                    gold.transform.position = player.transform.position;
                                    try
                                    {
                                        gold.RequestPetPickupServerRpc(player.OwnerClientId);
                                    }
                                    catch { }
                                    goldCount++;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AutoPickupModPlugin.Log.LogError($"Error vacuuming gold: {ex}");
                }
            }

            if (AutoPickupModPlugin.LogPickups != null && AutoPickupModPlugin.LogPickups.Value)
            {
                if (itemsCount > 0 || goldCount > 0)
                {
                    AutoPickupModPlugin.Log.LogInfo($"[AutoPickup] Vacuumed {itemsCount} items and {goldCount} gold piles within {radius:F1}m radius.");
                }
            }
        }

        /// <summary>
        /// Detects single key press across both Legacy Input and the new Unity Input System.
        /// </summary>
        private static bool IsKeyPressed(KeyCode key)
        {
            try
            {
                if (Input.GetKeyDown(key))
                    return true;
            }
            catch { }

            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (key == KeyCode.Space && kb.spaceKey.wasPressedThisFrame)
                        return true;

                    if (Enum.TryParse<UnityEngine.InputSystem.Key>(key.ToString(), true, out var inputKey))
                    {
                        if (kb[inputKey].wasPressedThisFrame)
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Detects key hold across both Legacy Input and the new Unity Input System.
        /// </summary>
        private static bool IsKeyHeld(KeyCode key)
        {
            try
            {
                if (Input.GetKey(key))
                    return true;
            }
            catch { }

            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (key == KeyCode.Space && kb.spaceKey.isPressed)
                        return true;

                    if (Enum.TryParse<UnityEngine.InputSystem.Key>(key.ToString(), true, out var inputKey))
                    {
                        if (kb[inputKey].isPressed)
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Checks if the user is currently typing in an input field (chat window, chest rename, etc.)
        /// so pressing Space won't trigger item pickup while writing text.
        /// </summary>
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
