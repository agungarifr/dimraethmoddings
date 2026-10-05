using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace DayNightToggleMod
{
    public enum VisualMode
    {
        MidDay,
        Midnight,
        Vanilla
    }

    [BepInPlugin("com.custom.daynighttogglemod", "DayNightToggleMod", "1.0.0")]
    public class DayNightToggleModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static DayNightToggleModPlugin Instance { get; private set; }

        // General Configuration
        public static ConfigEntry<VisualMode> ConfigVisualMode;
        public static ConfigEntry<KeyCode> ToggleKey;
        public static ConfigEntry<bool> AdjustStreetLights;
        public static ConfigEntry<bool> AdjustEnvironmentColor;
        public static ConfigEntry<bool> ShowNotifications;

        // Day Visual Configuration
        public static ConfigEntry<float> DaySunMultiplier;
        public static ConfigEntry<float> DayAmbientMultiplier;
        public static ConfigEntry<float> CustomDaySunIntensity;
        public static ConfigEntry<float> CustomDayAmbientIntensity;
        public static ConfigEntry<string> DayColorHex;

        // Night Visual Configuration
        public static ConfigEntry<float> NightSunMultiplier;
        public static ConfigEntry<float> NightAmbientMultiplier;
        public static ConfigEntry<float> CustomNightSunIntensity;
        public static ConfigEntry<float> CustomNightAmbientIntensity;
        public static ConfigEntry<string> NightColorHex;

        // Active runtime mode
        public static VisualMode CurrentMode { get; set; } = VisualMode.MidDay;

        // Cached parsed colors
        public static Color ParsedDayColor = Color.white;
        public static Color ParsedNightColor = new Color(0.36f, 0.44f, 0.60f, 1.0f);

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            // --- General Settings ---
            ConfigVisualMode = Config.Bind(
                "General",
                "Mode",
                VisualMode.MidDay,
                "The forced lighting visual mode. MidDay = Bright as 12:00 PM, Midnight = Dark as 00:00 AM, Vanilla = Normal game progression."
            );

            ToggleKey = Config.Bind(
                "General",
                "ToggleKey",
                KeyCode.F7,
                "Keyboard hotkey to cycle through visual modes (MidDay -> Midnight -> Vanilla) in real-time."
            );

            AdjustStreetLights = Config.Bind(
                "General",
                "AdjustStreetLights",
                true,
                "If true, street lamps and outdoor lanterns are forced off during MidDay and forced on during Midnight."
            );

            AdjustEnvironmentColor = Config.Bind(
                "General",
                "AdjustEnvironmentColor",
                true,
                "If true, environment color curves are adjusted to match daylight/midnight ambient color tints."
            );

            ShowNotifications = Config.Bind(
                "General",
                "ShowNotifications",
                true,
                "Log mode changes to BepInEx log and game console when toggled."
            );

            // --- Day Visuals ---
            DaySunMultiplier = Config.Bind(
                "DayVisuals",
                "DaySunMultiplier",
                1.0f,
                new ConfigDescription("Multiplier applied to direct sunlight intensity during MidDay.", new AcceptableValueRange<float>(0.0f, 5.0f))
            );

            DayAmbientMultiplier = Config.Bind(
                "DayVisuals",
                "DayAmbientMultiplier",
                1.0f,
                new ConfigDescription("Multiplier applied to ambient/environment light intensity during MidDay.", new AcceptableValueRange<float>(0.0f, 5.0f))
            );

            CustomDaySunIntensity = Config.Bind(
                "DayVisuals",
                "CustomDaySunIntensity",
                -1.0f,
                "Absolute direct sun intensity override for MidDay. Set to -1 to use scene's default daytime level * DaySunMultiplier."
            );

            CustomDayAmbientIntensity = Config.Bind(
                "DayVisuals",
                "CustomDayAmbientIntensity",
                -1.0f,
                "Absolute ambient light intensity override for MidDay. Set to -1 to use scene's default daytime level * DayAmbientMultiplier."
            );

            DayColorHex = Config.Bind(
                "DayVisuals",
                "DayColorHex",
                "#FFFFFF",
                "Ambient tint color in hex code for MidDay mode."
            );

            // --- Night Visuals ---
            NightSunMultiplier = Config.Bind(
                "NightVisuals",
                "NightSunMultiplier",
                1.0f,
                new ConfigDescription("Multiplier applied to direct sunlight intensity during Midnight.", new AcceptableValueRange<float>(0.0f, 5.0f))
            );

            NightAmbientMultiplier = Config.Bind(
                "NightVisuals",
                "NightAmbientMultiplier",
                1.0f,
                new ConfigDescription("Multiplier applied to ambient/environment light intensity during Midnight.", new AcceptableValueRange<float>(0.0f, 5.0f))
            );

            CustomNightSunIntensity = Config.Bind(
                "NightVisuals",
                "CustomNightSunIntensity",
                -1.0f,
                "Absolute direct sun intensity override for Midnight. Set to -1 to use scene's default nighttime level * NightSunMultiplier."
            );

            CustomNightAmbientIntensity = Config.Bind(
                "NightVisuals",
                "CustomNightAmbientIntensity",
                -1.0f,
                "Absolute ambient light intensity override for Midnight. Set to -1 to use scene's default nighttime level * NightAmbientMultiplier."
            );

            NightColorHex = Config.Bind(
                "NightVisuals",
                "NightColorHex",
                "#5C7099",
                "Ambient tint color in hex code for Midnight mode."
            );

            // Initialize mode from config
            CurrentMode = ConfigVisualMode.Value;
            UpdateParsedColors();

            // Register Harmony patches
            Harmony.CreateAndPatchAll(typeof(DayNightToggleModPlugin).Assembly);

            Log.LogInfo("=================================================");
            Log.LogInfo("DayNightToggleMod v1.0.0 Initialized!");
            Log.LogInfo($"Initial Visual Mode: {CurrentMode}");
            Log.LogInfo($"Toggle Hotkey: {ToggleKey.Value}");
            Log.LogInfo($"Adjust Street Lights: {AdjustStreetLights.Value}");
            Log.LogInfo($"Adjust Environment Color: {AdjustEnvironmentColor.Value}");
            Log.LogInfo("Time progress, NPC schedules, and quests remain vanilla.");
            Log.LogInfo("=================================================");
        }

        public static void UpdateParsedColors()
        {
            if (ColorUtility.TryParseHtmlString(DayColorHex.Value, out var dayColor))
                ParsedDayColor = dayColor;
            else
                ParsedDayColor = Color.white;

            if (ColorUtility.TryParseHtmlString(NightColorHex.Value, out var nightColor))
                ParsedNightColor = nightColor;
            else
                ParsedNightColor = new Color(0.36f, 0.44f, 0.60f, 1.0f);
        }

        public static void CycleVisualMode()
        {
            switch (CurrentMode)
            {
                case VisualMode.MidDay:
                    CurrentMode = VisualMode.Midnight;
                    break;
                case VisualMode.Midnight:
                    CurrentMode = VisualMode.Vanilla;
                    break;
                case VisualMode.Vanilla:
                default:
                    CurrentMode = VisualMode.MidDay;
                    break;
            }

            ConfigVisualMode.Value = CurrentMode;
            Instance.Config.Save();

            if (ShowNotifications.Value)
            {
                Log.LogInfo($"[DayNightToggleMod] Visual Mode switched to: >>> {CurrentMode} <<<");
            }

            if (CurrentMode == VisualMode.Vanilla)
            {
                RestoreVanillaLighting();
            }
            else
            {
                ApplyForcedLighting();
            }
        }

        public static float AdjustSunIntensity(float original)
        {
            if (CurrentMode == VisualMode.Vanilla)
                return original;

            if (CurrentMode == VisualMode.MidDay)
            {
                if (CustomDaySunIntensity.Value >= 0f)
                    return CustomDaySunIntensity.Value;
                return original * DaySunMultiplier.Value;
            }
            else // Midnight
            {
                if (CustomNightSunIntensity.Value >= 0f)
                    return CustomNightSunIntensity.Value;
                return original * NightSunMultiplier.Value;
            }
        }

        public static float AdjustEnvIntensity(float original)
        {
            if (CurrentMode == VisualMode.Vanilla)
                return original;

            if (CurrentMode == VisualMode.MidDay)
            {
                if (CustomDayAmbientIntensity.Value >= 0f)
                    return CustomDayAmbientIntensity.Value;
                return original * DayAmbientMultiplier.Value;
            }
            else // Midnight
            {
                if (CustomNightAmbientIntensity.Value >= 0f)
                    return CustomNightAmbientIntensity.Value;
                return original * NightAmbientMultiplier.Value;
            }
        }

        public static void ApplyForcedLighting(TimeManager timeMgr = null)
        {
            if (CurrentMode == VisualMode.Vanilla)
                return;

            if (timeMgr == null)
                timeMgr = TimeManager.Singleton;
            if (timeMgr == null)
                return;

            var lm = LightManager.Singleton;
            if (lm == null)
                return;

            GameTime fakeTime = (CurrentMode == VisualMode.MidDay)
                ? new GameTime(1, 1, 1, 12, 0)
                : new GameTime(1, 1, 1, 0, 0);

            float sun = timeMgr.CalculateTargetIntensity(fakeTime);
            float env = timeMgr.CalculateTargetIntensityForSecondLight(fakeTime);

            if (lm.SunLight != null)
            {
                lm.SunLight.intensity = sun;
            }

            if (lm.EnvironmentLight != null)
            {
                lm.EnvironmentLight.intensity = env;
            }
        }

        public static void RestoreVanillaLighting(TimeManager timeMgr = null)
        {
            if (timeMgr == null)
                timeMgr = TimeManager.Singleton;
            if (timeMgr == null)
                return;

            var lm = LightManager.Singleton;
            if (lm == null)
                return;

            GameTime realTime = timeMgr.CurrentGameTime;
            float sun = timeMgr.CalculateTargetIntensity(realTime);
            float env = timeMgr.CalculateTargetIntensityForSecondLight(realTime);

            if (lm.SunLight != null)
            {
                lm.SunLight.intensity = sun;
            }

            if (lm.EnvironmentLight != null)
            {
                lm.EnvironmentLight.intensity = env;
            }
        }
    }

    /// <summary>
    /// Hooks PlayerUpdate.Update for hotkey toggling and per-frame lighting reinforcement.
    /// </summary>
    [HarmonyPatch(typeof(PlayerUpdate), nameof(PlayerUpdate.Update))]
    public static class Patch_PlayerUpdate_Update
    {
        public static void Postfix(PlayerUpdate __instance)
        {
            try
            {
                if (__instance == null || !__instance.IsOwner)
                    return;

                // Check hotkey toggle
                if (IsToggleKeyPressed())
                {
                    DayNightToggleModPlugin.CycleVisualMode();
                }

                // Reinforce lighting every frame if in forced mode
                if (DayNightToggleModPlugin.CurrentMode != VisualMode.Vanilla)
                {
                    DayNightToggleModPlugin.ApplyForcedLighting();
                }
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in Patch_PlayerUpdate_Update: {ex}");
            }
        }

        private static bool IsToggleKeyPressed()
        {
            // Do not trigger while typing in text input fields (chat, chest names, etc.)
            if (IsTypingInInputField())
                return false;

            KeyCode key = DayNightToggleModPlugin.ToggleKey != null
                ? DayNightToggleModPlugin.ToggleKey.Value
                : KeyCode.F7;

            // Check Unity Legacy Input
            try
            {
                if (Input.GetKeyDown(key))
                    return true;
            }
            catch { }

            // Check Unity Input System (New)
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
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

    /// <summary>
    /// Hooks TimeManager.DayNightCycleUpdate which is called every 0.5s by LightUpdateCoroutine.
    /// Reapplies forced lighting targets.
    /// </summary>
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.DayNightCycleUpdate))]
    public static class Patch_TimeManager_DayNightCycleUpdate
    {
        public static void Postfix(TimeManager __instance)
        {
            try
            {
                if (DayNightToggleModPlugin.CurrentMode != VisualMode.Vanilla)
                {
                    DayNightToggleModPlugin.ApplyForcedLighting(__instance);
                }
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in Patch_TimeManager_DayNightCycleUpdate: {ex}");
            }
        }
    }

    /// <summary>
    /// Hooks TimeManager.CalculateTargetIntensity (primary sun light calculation).
    /// </summary>
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.CalculateTargetIntensity))]
    public static class Patch_TimeManager_CalculateTargetIntensity
    {
        public static bool Prefix(TimeManager __instance, GameTime gameTime, ref float __result)
        {
            try
            {
                if (DayNightToggleModPlugin.CurrentMode == VisualMode.Vanilla)
                    return true;

                float forced = (DayNightToggleModPlugin.CurrentMode == VisualMode.MidDay) ? 1.0f : 0.05f;
                __result = DayNightToggleModPlugin.AdjustSunIntensity(forced);
                return false;
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in CalculateTargetIntensity: {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Hooks TimeManager.CalculateTargetIntensityForSecondLight (environment/ambient light calculation).
    /// </summary>
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.CalculateTargetIntensityForSecondLight))]
    public static class Patch_TimeManager_CalculateTargetIntensityForSecondLight
    {
        public static bool Prefix(TimeManager __instance, GameTime gameTime, ref float __result)
        {
            try
            {
                if (DayNightToggleModPlugin.CurrentMode == VisualMode.Vanilla)
                    return true;

                float forced = (DayNightToggleModPlugin.CurrentMode == VisualMode.MidDay) ? 0.8f : 0.15f;
                __result = DayNightToggleModPlugin.AdjustEnvIntensity(forced);
                return false;
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in CalculateTargetIntensityForSecondLight: {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Hooks TimeManager.CalculateTargetIntensityForNewScene used during scene transitions.
    /// </summary>
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.CalculateTargetIntensityForNewScene))]
    public static class Patch_TimeManager_CalculateTargetIntensityForNewScene
    {
        public static bool Prefix(TimeManager __instance, SceneHandler handler, GameTime gameTime, ref float __result)
        {
            try
            {
                if (DayNightToggleModPlugin.CurrentMode == VisualMode.Vanilla)
                    return true;

                float forced = (DayNightToggleModPlugin.CurrentMode == VisualMode.MidDay) ? 1.0f : 0.05f;
                __result = DayNightToggleModPlugin.AdjustSunIntensity(forced);
                return false;
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in CalculateTargetIntensityForNewScene: {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Hooks TimeManager.CalculateTargetEnvIntensityForNewScene used during scene transitions.
    /// </summary>
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.CalculateTargetEnvIntensityForNewScene))]
    public static class Patch_TimeManager_CalculateTargetEnvIntensityForNewScene
    {
        public static bool Prefix(TimeManager __instance, SceneHandler handler, GameTime gameTime, ref float __result)
        {
            try
            {
                if (DayNightToggleModPlugin.CurrentMode == VisualMode.Vanilla)
                    return true;

                float forced = (DayNightToggleModPlugin.CurrentMode == VisualMode.MidDay) ? 0.8f : 0.15f;
                __result = DayNightToggleModPlugin.AdjustEnvIntensity(forced);
                return false;
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in CalculateTargetEnvIntensityForNewScene: {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Hooks DayNightLightAdjuster.Update to control street lights / lanterns.
    /// Keeps lamps off during forced MidDay and on during forced Midnight.
    /// </summary>
    [HarmonyPatch(typeof(DayNightLightAdjuster), nameof(DayNightLightAdjuster.Update))]
    public static class Patch_DayNightLightAdjuster_Update
    {
        public static bool Prefix(DayNightLightAdjuster __instance)
        {
            try
            {
                if (!DayNightToggleModPlugin.AdjustStreetLights.Value || DayNightToggleModPlugin.CurrentMode == VisualMode.Vanilla)
                {
                    return true; // Execute normal game behavior
                }

                bool shouldBeOn = (DayNightToggleModPlugin.CurrentMode == VisualMode.Midnight);
                if (__instance._areLightsOn != shouldBeOn)
                {
                    __instance.SetChildObjectsActive(shouldBeOn);
                    __instance._areLightsOn = shouldBeOn;
                }

                if (shouldBeOn)
                {
                    __instance.AdjustLightIntensities();
                }

                return false; // Suppress vanilla Update while in forced mode
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in Patch_DayNightLightAdjuster_Update: {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Hooks EnvironmentColor.Update to tint ambient lights matching daylight/midnight curves.
    /// </summary>
    [HarmonyPatch(typeof(EnvironmentColor), nameof(EnvironmentColor.Update))]
    public static class Patch_EnvironmentColor_Update
    {
        public static bool Prefix(EnvironmentColor __instance)
        {
            try
            {
                if (!DayNightToggleModPlugin.AdjustEnvironmentColor.Value || DayNightToggleModPlugin.CurrentMode == VisualMode.Vanilla)
                {
                    return true; // Normal vanilla behavior
                }

                if (__instance._lights != null)
                {
                    Color targetColor = (DayNightToggleModPlugin.CurrentMode == VisualMode.MidDay)
                        ? DayNightToggleModPlugin.ParsedDayColor
                        : DayNightToggleModPlugin.ParsedNightColor;

                    for (int i = 0; i < __instance._lights.Count; i++)
                    {
                        var light = __instance._lights[i];
                        if (light != null)
                        {
                            light.color = targetColor;
                        }
                    }
                }

                return false; // Suppress vanilla hourly curve evaluation
            }
            catch (Exception ex)
            {
                DayNightToggleModPlugin.Log.LogError($"Error in Patch_EnvironmentColor_Update: {ex}");
                return true;
            }
        }
    }
}
