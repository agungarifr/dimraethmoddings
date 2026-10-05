using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace UpgradeBonusStatIsNotRandom
{
    [BepInPlugin("com.dimraeth.upgradebonusstatisnotrandom", "Upgrade Bonus Stat Is Not Random", "1.0.0")]
    public class UpgradeBonusStatIsNotRandomPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static UpgradeBonusStatIsNotRandomPlugin Instance { get; private set; }

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<KeyCode> MenuKey;
        public static ConfigEntry<string> TargetStatLevel3;
        public static ConfigEntry<string> TargetStatLevel6;
        public static ConfigEntry<string> TargetStatLevel9;
        public static ConfigEntry<string> TargetStatLevel12;
        public static ConfigEntry<bool> FallbackToVanillaIfDuplicate;

        private Harmony _harmony;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            ModEnabled = Config.Bind("General", "ModEnabled", true, "Enable or disable bonus stat is not random");
            MenuKey = Config.Bind("General", "MenuKey", KeyCode.F7, "Hotkey to toggle the configuration form");
            FallbackToVanillaIfDuplicate = Config.Bind("General", "FallbackToVanillaIfDuplicate", true, "If the chosen stat is already on the item (as primary or existing secondary), fall back cleanly to vanilla RNG instead of breaking");

            TargetStatLevel3 = Config.Bind("Plan", "TargetStatLevel3", "None", "Stat to force at Level +3 (None = Vanilla RNG)");
            TargetStatLevel6 = Config.Bind("Plan", "TargetStatLevel6", "None", "Stat to force at Level +6 (None = Vanilla RNG)");
            TargetStatLevel9 = Config.Bind("Plan", "TargetStatLevel9", "None", "Stat to force at Level +9 (None = Vanilla RNG)");
            TargetStatLevel12 = Config.Bind("Plan", "TargetStatLevel12", "None", "Stat to force at Level +12 (None = Vanilla RNG)");

            _harmony = new Harmony("com.dimraeth.upgradebonusstatisnotrandom");
            _harmony.PatchAll(typeof(Patches.UpgradeRunePatch));

            ClassInjector.RegisterTypeInIl2Cpp<UnrandomizerBehaviour>();
            var go = new GameObject("UpgradeBonusStatIsNotRandomController");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<UnrandomizerBehaviour>();

            Log.LogInfo("Upgrade Bonus Stat Is Not Random v1.0.0 loaded! Press F7 to configure.");
        }
    }

    public class UnrandomizerBehaviour : MonoBehaviour
    {
        public UnrandomizerBehaviour(IntPtr ptr) : base(ptr) { }

        private float _nextToggleTime;
        public static bool IsOpen = false;

        private void Update()
        {
            if (Input.GetKeyDown(UpgradeBonusStatIsNotRandomPlugin.MenuKey.Value) && Time.unscaledTime >= _nextToggleTime)
            {
                _nextToggleTime = Time.unscaledTime + 0.25f;
                IsOpen = !IsOpen;
                if (IsOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void OnGUI()
        {
            if (!IsOpen) return;
            UI.UnrandomizerUI.Draw();
        }
    }
}
