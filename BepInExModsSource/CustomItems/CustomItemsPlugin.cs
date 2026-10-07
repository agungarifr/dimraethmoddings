using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CustomItems
{
    // [2026-10-07] Custom item framework: JSON-defined items/recipes registered into
    // ItemManager as the game's own Item/Recipe ScriptableObjects. Flagship item:
    // Satay Madura (10x BeastMeat -> 1x; nourishment + 50% max HP/STA/CON over 30 min;
    // icon = Roasted Droop Core sprite tinted red).

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class CustomItemsPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "com.custom.customitems";
        public const string PLUGIN_NAME = "Custom Items";
        public const string PLUGIN_VERSION = "1.0.0";

        public static CustomItemsPlugin Instance;
        static BepInEx.Logging.ManualLogSource _log;
        static ConfigEntry<KeyCode> _spawnHotkey;
        static ConfigEntry<bool> _debugLogging;

        static bool _registrationDone;
        static bool _unlockTried;

        public override void Load()
        {
            Instance = this;
            _log = Log;

            Config.Bind("General", "Enabled", true, "Master switch for Custom Items.");
            _debugLogging = Config.Bind("General", "DebugLogging", false, "Verbose logging.");
            _spawnHotkey = Config.Bind("Debug", "SpawnHotkey", KeyCode.F9,
                "Spawn one of each custom item at the player (debug/testing).");

            string definitionsPath = Path.Combine(Paths.ConfigPath, "CustomItems", "definitions.json");
            EnsureDefinitionsFile(definitionsPath);

            Harmony harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(typeof(Patches));

            ClassInjector.RegisterTypeInIl2Cpp<CustomItemsTicker>();
            var go = new GameObject("CustomItemsTicker");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<CustomItemsTicker>();
            CustomItemsTicker.Instance = go.GetComponent<CustomItemsTicker>();

            LogInfo($"v{PLUGIN_VERSION} loaded. Definitions: {definitionsPath}");
        }

        static void EnsureDefinitionsFile(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(path))
                {
                    Registry.Load(path, LogInfo, LogWarn);
                    return;
                }

                string defaultSrc = Path.Combine(Path.GetDirectoryName(typeof(CustomItemsPlugin).Assembly.Location), "CustomItems", "default-definitions.json");
                if (File.Exists(defaultSrc))
                {
                    File.Copy(defaultSrc, path);
                    LogInfo("Created definitions.json from default template.");
                }
                else
                {
                    File.WriteAllText(path, "{ \"items\": [], \"recipes\": [] }");
                    LogWarn("default-definitions.json not found; created empty definitions.json.");
                }
                Registry.Load(path, LogInfo, LogWarn);
            }
            catch (Exception ex)
            {
                LogWarn("Failed to prepare definitions file: " + ex.Message);
            }
        }

        // Called from CustomItemsTicker.Update: registers items once ItemManager exists
        // (its Items/Recipes lists are populated in its Awake from game assets), then
        // best-effort unlocks recipes flagged UnlockedByDefault once the player exists.
        public static void PumpRegistration()
        {
            if (!_registrationDone && ItemManager.Singleton != null)
            {
                _registrationDone = true;
                try
                {
                    ItemFactory.RegisterAll(LogInfo, LogWarn);
                }
                catch (Exception ex)
                {
                    LogWarn("Registration failed: " + ex.Message);
                }
            }

            if (_registrationDone && !_unlockTried && DataStorage.Singleton != null && DataStorage.Singleton.Inventory != null)
            {
                _unlockTried = true;
                foreach (var recipe in Registry.Recipes)
                {
                    if (!recipe.UnlockedByDefault) continue;
                    TryUnlockRecipe(recipe.Name);
                    TryUnlockRecipe(recipe.Uuid);
                }
            }

            if (_spawnHotkey.Value != KeyCode.None && Input.GetKeyDown(_spawnHotkey.Value) && ItemManager.Singleton != null)
            {
                foreach (var item in Registry.Items)
                {
                    try { ItemManager.Singleton.SpawnNewServerItem(item.Type, 1); }
                    catch (Exception ex) { LogWarn($"Spawn '{item.Name}' failed: {ex.Message}"); }
                }
            }
        }

        // [2026-10-07] Inventory.UnlockRecipeByName is private; invoke reflectively so a
        // future game update renaming it fails softly instead of breaking the plugin.
        static void TryUnlockRecipe(string name)
        {
            try
            {
                var inv = DataStorage.Singleton.Inventory;
                var method = AccessTools.Method(inv.GetType(), "UnlockRecipeByName");
                if (method == null) { LogOnce("Inventory.UnlockRecipeByName not found; recipe unlock left to game flow."); return; }
                method.Invoke(inv, new object[] { name });
                LogInfo($"Unlocked recipe '{name}' (UnlockedByDefault).");
            }
            catch (Exception ex)
            {
                LogOnce("UnlockRecipeByName failed: " + ex.Message);
            }
        }

        public static void LogInfo(string msg) => _log.LogInfo(msg);
        public static void LogWarn(string msg) => _log.LogWarning(msg);

        static readonly System.Collections.Generic.HashSet<string> _logged = new();
        public static void LogOnce(string msg)
        {
            if (_logged.Add(msg)) _log.LogWarning(msg);
        }

        public static void LogDebug(string msg)
        {
            if (_debugLogging != null && _debugLogging.Value) _log.LogInfo("[dbg] " + msg);
        }
    }
}
