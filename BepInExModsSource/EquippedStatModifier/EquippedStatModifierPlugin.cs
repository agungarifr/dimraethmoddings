using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using EquippedStatModifier.Core;
using EquippedStatModifier.UI;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace EquippedStatModifier
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class EquippedStatModifierPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "com.dimraeth.equippedstatmodifier";
        public const string PLUGIN_NAME = "Equipped Stat Modifier";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static EquippedStatModifierPlugin Instance { get; private set; }

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<KeyCode> MenuKey;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            ModEnabled = Config.Bind("General", "ModEnabled", true,
                "Enable or disable Equipped Stat Modifier");
            MenuKey = Config.Bind("General", "MenuKey", KeyCode.F8,
                "Keyboard shortcut to toggle the in-game equipped stat editor UI (Default: F8)");

            RuneMemory.LogInfo = m => Log.LogInfo(m);
            RuneMemory.LogWarn = m => Log.LogWarning(m);
            RuneMemory.LogError = m => Log.LogError(m);

            ClassInjector.RegisterTypeInIl2Cpp<EquippedStatModifierBehaviour>();
            var go = new GameObject("EquippedStatModifierController");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<EquippedStatModifierBehaviour>();

            Log.LogInfo("=================================================");
            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded successfully!");
            Log.LogInfo($"Press {MenuKey.Value} in-game to open the equipped gear editor");
            Log.LogInfo("=================================================");
        }
    }

    public class EquippedStatModifierBehaviour : MonoBehaviour
    {
        public EquippedStatModifierBehaviour(IntPtr ptr) : base(ptr) { }

        private float _nextToggleTime;

        private void Update()
        {
            if (!EquippedStatModifierPlugin.ModEnabled.Value) return;

            try
            {
                KeyCode toggleKey = EquippedStatModifierPlugin.MenuKey != null
                    ? EquippedStatModifierPlugin.MenuKey.Value
                    : KeyCode.F8;

                if (Input.GetKeyDown(toggleKey) && Time.unscaledTime >= _nextToggleTime)
                {
                    _nextToggleTime = Time.unscaledTime + 0.25f;
                    EquippedStatModifierUI.Toggle();
                }
                else if (EquippedStatModifierUI.IsOpen && Input.GetKeyDown(KeyCode.Escape))
                {
                    EquippedStatModifierUI.Toggle();
                }
            }
            catch (Exception ex)
            {
                EquippedStatModifierPlugin.Log.LogError($"[Update Error] {ex}");
            }
        }

        private void OnGUI()
        {
            if (!EquippedStatModifierPlugin.ModEnabled.Value) return;

            try
            {
                EquippedStatModifierUI.Draw();
            }
            catch (Exception ex)
            {
                EquippedStatModifierPlugin.Log.LogError($"[OnGUI Error] {ex}");
            }
        }
    }
}
