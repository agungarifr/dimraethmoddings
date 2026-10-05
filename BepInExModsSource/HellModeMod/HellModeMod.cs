using System;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HellModeMod
{
    [BepInPlugin("com.custom.hellmodemod", "HellModeMod", "1.0.0")]
    public class HellModeModPlugin : BasePlugin
    {
        public static HellModeModPlugin Instance { get; private set; }
        internal static new ManualLogSource Log;

        // Config Entries
        public static ConfigEntry<bool> ForceHellMode;
        public static ConfigEntry<float> MonsterHealthMult;
        public static ConfigEntry<float> MonsterDamageMult;
        public static ConfigEntry<float> CombatPaceMult;
        public static ConfigEntry<float> MonsterSpeedMult;
        public static ConfigEntry<float> EmpowermentChanceMult;
        public static ConfigEntry<int> MonsterBonusLevelOverPlayer;
        public static ConfigEntry<float> ExpMultiplier;

        // Runtime State
        public static int SelectedCreationDifficultyIndex = 1; // Default to Moderate
        public static bool CurrentWorldIsHellMode = false;
        public static string CurrentWorldName = string.Empty;
        public static Player CachedPlayer = null;

        private static DateTime _lastConfigWriteTime = DateTime.MinValue;
        private static float _lastConfigCheckTime = -10f;

        public static void ReloadConfigIfChanged(bool force = false)
        {
            try
            {
                if (Instance == null || Instance.Config == null) return;
                string configPath = Instance.Config.ConfigFilePath;
                if (!File.Exists(configPath)) return;

                DateTime lastWrite = File.GetLastWriteTimeUtc(configPath);
                if (force || lastWrite > _lastConfigWriteTime || _lastConfigWriteTime == DateTime.MinValue)
                {
                    _lastConfigWriteTime = lastWrite;
                    Instance.Config.Reload();
                    Log?.LogInfo("=========================================================");
                    Log?.LogInfo($"[HellModeMod] Config reloaded from disk ({configPath})! Active values:");
                    Log?.LogInfo($"  - Monster Health Multiplier: {MonsterHealthMult?.Value}x");
                    Log?.LogInfo($"  - Monster Damage Multiplier: {MonsterDamageMult?.Value}x");
                    Log?.LogInfo($"  - Combat Pace Multiplier: {CombatPaceMult?.Value}x (Turn delay = base / {CombatPaceMult?.Value})");
                    Log?.LogInfo($"  - Monster Move Speed Multiplier: {MonsterSpeedMult?.Value}x");
                    Log?.LogInfo($"  - Elite / Empowerment Chance Multiplier: {EmpowermentChanceMult?.Value}x");
                    Log?.LogInfo($"  - Monster Bonus Level: PlayerLevel + {MonsterBonusLevelOverPlayer?.Value}");
                    Log?.LogInfo($"  - Bonus EXP Multiplier: {ExpMultiplier?.Value}x");
                    Log?.LogInfo("=========================================================");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error reloading config: {ex}");
            }
        }

        public static void CheckConfigUpdate()
        {
            try
            {
                if (Time.unscaledTime - _lastConfigCheckTime > 1.0f)
                {
                    _lastConfigCheckTime = Time.unscaledTime;
                    ReloadConfigIfChanged(false);
                }
            }
            catch { }
        }

        public static bool IsHellModeActive
        {
            get
            {
                CheckConfigUpdate();

                if (ForceHellMode != null && ForceHellMode.Value) return true;
                if (CurrentWorldIsHellMode) return true;

                try
                {
                    if (ServerPersistentData.Singleton != null && !string.IsNullOrEmpty(ServerPersistentData.Singleton.Name))
                    {
                        if (IsWorldRegisteredAsHell(ServerPersistentData.Singleton.Name))
                        {
                            CurrentWorldIsHellMode = true;
                            CurrentWorldName = ServerPersistentData.Singleton.Name;
                            return true;
                        }
                    }
                }
                catch { }

                return false;
            }
        }

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            // Bind Configurations
            ForceHellMode = Config.Bind("General", "ForceHellMode", false, "Force Hell Mode active on all worlds regardless of world creation selection (Default: false)");
            MonsterHealthMult = Config.Bind("DifficultyDials", "MonsterHealthMultiplier", 10.0f, "Hell Mode monster health multiplier (Default: 10.0x)");
            MonsterDamageMult = Config.Bind("DifficultyDials", "MonsterDamageDealtMultiplier", 5.0f, "Hell Mode monster damage dealt multiplier (Default: 5.0x)");
            CombatPaceMult = Config.Bind("DifficultyDials", "CombatPaceMultiplier", 2.0f, "Hell Mode combat pace and squad aggression multiplier (squad turn delay = base / mult) (Default: 2.0x)");
            MonsterSpeedMult = Config.Bind("DifficultyDials", "MonsterSpeedMultiplier", 1.5f, "Hell Mode monster move speed multiplier (Default: 1.5x)");
            EmpowermentChanceMult = Config.Bind("DifficultyDials", "EmpowermentChanceMultiplier", 3.0f, "Hell Mode monster empowerment / elite chance multiplier (Default: 3.0x)");
            MonsterBonusLevelOverPlayer = Config.Bind("DifficultyDials", "MonsterBonusLevelOverPlayer", 20, "Dynamic bonus levels above player level for monsters in Hell Mode (Default: 20)");
            ExpMultiplier = Config.Bind("Progression", "ExpMultiplier", 3.0f, "Bonus EXP multiplier while playing in Hell Mode to keep progression engaging (Default: 3.0x)");

            try
            {
                string configPath = Config.ConfigFilePath;
                if (File.Exists(configPath))
                {
                    _lastConfigWriteTime = File.GetLastWriteTimeUtc(configPath);
                }
            }
            catch { }

            // Apply Harmony Patches safely
            SafePatch(typeof(Patch_DifficultySlider));
            SafePatch(typeof(Patch_WorldCreationPresenter));
            SafePatch(typeof(Patch_FinalSelection));
            SafePatch(typeof(Patch_WorldSelection));
            SafePatch(typeof(Patch_WorldLifecycle));
            SafePatch(typeof(Patch_DifficultyDials));
            SafePatch(typeof(Patch_MonsterLevelScaling));
            SafePatch(typeof(Patch_Progression));

            Log.LogInfo("=================================================");
            Log.LogInfo("HellModeMod v1.1.0 (BepInEx 6 IL2CPP) Initialized!");
            Log.LogInfo($"Monster Health: {MonsterHealthMult.Value}x");
            Log.LogInfo($"Monster Damage: {MonsterDamageMult.Value}x");
            Log.LogInfo($"Combat Pace: {CombatPaceMult.Value}x (Turn delay = base / {CombatPaceMult.Value})");
            Log.LogInfo($"Monster Move Speed: {MonsterSpeedMult.Value}x");
            Log.LogInfo($"Elite / Empowerment Chance: {EmpowermentChanceMult.Value}x");
            Log.LogInfo($"Dynamic Monster Level: PlayerLevel + {MonsterBonusLevelOverPlayer.Value}");
            Log.LogInfo("Level cap is handled by ConfigurableLevelCapFreebuff (global, up to 99).");
            Log.LogInfo($"Hell Mode Bonus EXP: {ExpMultiplier.Value}x (Gold & Loot vanilla 1.0x)");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");
        }

        private static void SafePatch(Type patchType)
        {
            try
            {
                Harmony.CreateAndPatchAll(patchType);
                Log.LogInfo($"Successfully applied patch: {patchType.Name}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        public static string GetHellWorldsDirectory()
        {
            try
            {
                string localLow = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";
                string worldsDir = Path.Combine(localLow, "Mudtek", "Dimraeth", "Worlds");
                if (!Directory.Exists(worldsDir))
                {
                    Directory.CreateDirectory(worldsDir);
                }
                return worldsDir;
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error resolving Worlds directory: {ex.Message}");
                return string.Empty;
            }
        }

        public static void RegisterWorldAsHellMode(string worldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(worldName)) return;
                string dir = GetHellWorldsDirectory();
                if (string.IsNullOrEmpty(dir)) return;

                string cleanName = worldName.Trim();
                if (cleanName.EndsWith(".jrwf", StringComparison.OrdinalIgnoreCase))
                {
                    cleanName = cleanName.Substring(0, cleanName.Length - 5).Trim();
                }

                string markerFile = Path.Combine(dir, $"{cleanName}.hell");
                File.WriteAllText(markerFile, $"HellMode=true\nCreatedAt={DateTime.UtcNow:O}");
                Log?.LogInfo($"Registered world '{cleanName}' as Hell Mode (marker: {markerFile})");
                CurrentWorldIsHellMode = true;
                CurrentWorldName = cleanName;
                ReloadConfigIfChanged(true);
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error registering Hell Mode world '{worldName}': {ex}");
            }
        }

        public static void UnregisterWorldAsHellMode(string worldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(worldName)) return;
                string dir = GetHellWorldsDirectory();
                if (string.IsNullOrEmpty(dir)) return;

                string cleanName = worldName.Trim();
                if (cleanName.EndsWith(".jrwf", StringComparison.OrdinalIgnoreCase))
                {
                    cleanName = cleanName.Substring(0, cleanName.Length - 5).Trim();
                }

                string markerFile = Path.Combine(dir, $"{cleanName}.hell");
                if (File.Exists(markerFile))
                {
                    File.Delete(markerFile);
                    Log?.LogInfo($"Unregistered world '{cleanName}' from Hell Mode (removed: {markerFile})");
                }

                // Also check case-insensitive files
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.hell");
                    foreach (var f in files)
                    {
                        string baseName = Path.GetFileNameWithoutExtension(f).Trim();
                        if (string.Equals(baseName, cleanName, StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.Delete(f); } catch { }
                            Log?.LogInfo($"Unregistered world (case-match) '{f}' from Hell Mode.");
                        }
                    }
                }

                CurrentWorldIsHellMode = false;
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error unregistering Hell Mode world '{worldName}': {ex}");
            }
        }

        public static bool IsWorldRegisteredAsHell(string worldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(worldName)) return false;
                string dir = GetHellWorldsDirectory();
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;

                string cleanName = worldName.Trim();
                if (cleanName.EndsWith(".jrwf", StringComparison.OrdinalIgnoreCase))
                {
                    cleanName = cleanName.Substring(0, cleanName.Length - 5).Trim();
                }

                string markerFile = Path.Combine(dir, $"{cleanName}.hell");
                if (File.Exists(markerFile)) return true;

                // Case-insensitive directory enumeration
                var files = Directory.GetFiles(dir, "*.hell");
                foreach (var f in files)
                {
                    string baseName = Path.GetFileNameWithoutExtension(f).Trim();
                    if (string.Equals(baseName, cleanName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void SetActiveWorldHellMode(bool isHell, string worldName)
        {
            CurrentWorldIsHellMode = isHell;
            CurrentWorldName = worldName ?? string.Empty;
            ReloadConfigIfChanged(true);
            Log?.LogInfo($"Active World set to '{worldName}' - Hell Mode: {isHell}");
        }

        public static int GetPlayerLevel()
        {
            try
            {
                if (CachedPlayer != null && CachedPlayer.Level != null)
                {
                    return Math.Max(1, CachedPlayer.Level.Value);
                }

                var playerObj = UnityEngine.Object.FindObjectOfType<Player>();
                if (playerObj != null && playerObj.Level != null)
                {
                    CachedPlayer = playerObj;
                    return Math.Max(1, playerObj.Level.Value);
                }
            }
            catch { }
            return 1;
        }
    }

    /// <summary>
    /// Expands Difficulty Sliders (World Creation and World Selection/Final Selection)
    /// to support 4 modes (EASY, MODERATE, HARD, HELL) evenly fitted across the existing
    /// visual slider track boundaries, ensuring no labels or knobs extend beyond the window frame.
    /// </summary>
    public static class Patch_DifficultySlider
    {
        [HarmonyPatch(typeof(DifficultySlider), nameof(DifficultySlider.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(DifficultySlider __instance)
        {
            try
            {
                if (__instance == null) return;

                if (__instance._difficultySlider != null)
                {
                    __instance._difficultySlider.minValue = 0f;
                    __instance._difficultySlider.maxValue = 3f;
                }

                if (__instance.difficultyLabels == null) return;

                if (__instance.difficultyLabels.Length >= 4)
                {
                    try
                    {
                        var existingHell = __instance.difficultyLabels[3];
                        if (existingHell != null)
                        {
                            existingHell.text = "HELL";
                        }
                    }
                    catch { }
                    return; // Already expanded
                }

                if (__instance.difficultyLabels.Length == 3)
                {
                    var easyLabel = __instance.difficultyLabels[0];
                    var modLabel = __instance.difficultyLabels[1];
                    var hardLabel = __instance.difficultyLabels[2];

                    if (easyLabel == null || modLabel == null || hardLabel == null) return;
                    if (easyLabel.transform == null || hardLabel.transform == null || hardLabel.gameObject == null) return;

                    var parentTransform = hardLabel.transform.parent;
                    if (parentTransform == null) return;

                    // Keep total bar span strictly between left tick (Easy) and right tick (Hard's original spot)
                    float leftX = easyLabel.transform.localPosition.x;
                    float rightX = hardLabel.transform.localPosition.x;
                    float totalWidth = rightX - leftX;
                    float y = hardLabel.transform.localPosition.y;
                    float z = hardLabel.transform.localPosition.z;

                    // Reposition labels to fit 4 positions evenly across the existing visual bar:
                    // 0: EASY (left end: 0%)
                    // 1: MODERATE (at 33.33%)
                    // 2: HARD (at 66.67%)
                    // 3: HELL (right end: 100%, where Hard originally was!)
                    float step = totalWidth / 3f;
                    easyLabel.transform.localPosition = new Vector3(leftX, y, z);
                    modLabel.transform.localPosition = new Vector3(leftX + step, y, z);
                    hardLabel.transform.localPosition = new Vector3(leftX + step * 2f, y, z);

                    // Instantiate clone of Hard label for Hell at rightX (100%)
                    var hellObj = UnityEngine.Object.Instantiate(hardLabel.gameObject, parentTransform);
                    if (hellObj == null) return;
                    hellObj.name = "HellLabel";

                    var hellTmp = hellObj.GetComponent<TextMeshProUGUI>();
                    if (hellTmp == null) return;

                    // Strip any localization / translation scripts copied from Hard label
                    try
                    {
                        var monoBehaviours = hellObj.GetComponents<MonoBehaviour>();
                        if (monoBehaviours != null)
                        {
                            foreach (var mb in monoBehaviours)
                            {
                                if (mb != null && mb.Pointer != hellTmp.Pointer)
                                {
                                    UnityEngine.Object.DestroyImmediate(mb);
                                }
                            }
                        }
                    }
                    catch { }

                    hellTmp.text = "HELL";
                    hellTmp.color = new Color(0.85f, 0.25f, 0.25f, 0.45f);
                    hellObj.transform.localPosition = new Vector3(rightX, y, z);

                    // Align center for clean visual balance under slider ticks
                    easyLabel.alignment = TextAlignmentOptions.Center;
                    modLabel.alignment = TextAlignmentOptions.Center;
                    hardLabel.alignment = TextAlignmentOptions.Center;
                    hellTmp.alignment = TextAlignmentOptions.Center;

                    // Expand difficultyLabels array to 4
                    var newLabels = new Il2CppReferenceArray<TextMeshProUGUI>(4);
                    newLabels[0] = easyLabel;
                    newLabels[1] = modLabel;
                    newLabels[2] = hardLabel;
                    newLabels[3] = hellTmp;
                    __instance.difficultyLabels = newLabels;

                    // Expand slider snap positions to 4
                    var newPositions = new Il2CppStructArray<float>(4);
                    newPositions[0] = 0f;
                    newPositions[1] = 1f;
                    newPositions[2] = 2f;
                    newPositions[3] = 3f;
                    __instance._sliderPositions = newPositions;

                    HellModeModPlugin.Log?.LogInfo("DifficultySlider UI successfully revamped to 4 difficulties (EASY, MODERATE, HARD, HELL) within window boundaries!");
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in Patch_DifficultySlider.Start_Postfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(DifficultySlider), nameof(DifficultySlider.UpdateDifficultyText))]
        [HarmonyPostfix]
        public static void UpdateDifficultyText_Postfix(DifficultySlider __instance, float value)
        {
            try
            {
                if (__instance == null || __instance.difficultyLabels == null) return;
                int rounded = Mathf.RoundToInt(value);
                if (__instance.difficultyLabels.Length >= 4)
                {
                    var hellTmp = __instance.difficultyLabels[3];
                    if (hellTmp != null)
                    {
                        hellTmp.text = "HELL"; // Always enforce "HELL"
                        if (rounded == 3)
                        {
                            hellTmp.color = new Color(1.0f, 0.15f, 0.15f, 1.0f); // Bright red glow
                            hellTmp.fontSize = __instance.MaxFontSize;
                            for (int i = 0; i < 3; i++)
                            {
                                if (__instance.difficultyLabels[i] != null)
                                {
                                    __instance.difficultyLabels[i].color = __instance.MinColor;
                                    __instance.difficultyLabels[i].fontSize = __instance.MinFontSize;
                                }
                            }
                        }
                        else
                        {
                            hellTmp.color = new Color(0.85f, 0.25f, 0.25f, 0.45f);
                            hellTmp.fontSize = __instance.MinFontSize;
                        }
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Intercepts World Creation UI selections and handles Hell Mode creation and persistence.
    /// </summary>
    public static class Patch_WorldCreationPresenter
    {
        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.OnDifficultyChanged))]
        [HarmonyPostfix]
        public static void OnDifficultyChanged_Postfix(WorldCreationPresenter __instance, float value)
        {
            try
            {
                int index = Mathf.RoundToInt(value);
                HellModeModPlugin.SelectedCreationDifficultyIndex = index;
                if (index == 3)
                {
                    HellModeModPlugin.Log?.LogInfo("World creation slider set to Hell Mode (Difficulty Index 3).");
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.CreateNewWorld))]
        [HarmonyPrefix]
        public static void CreateNewWorld_Prefix(WorldCreationPresenter __instance, string worldName, ref Difficulty difficulty)
        {
            try
            {
                bool isHell = HellModeModPlugin.SelectedCreationDifficultyIndex == 3 || (int)difficulty == 3;
                if (__instance != null && __instance._view != null && __instance._view._worldDifficultySlider != null)
                {
                    if (Mathf.RoundToInt(__instance._view._worldDifficultySlider.value) == 3)
                    {
                        isHell = true;
                    }
                }

                if (isHell)
                {
                    HellModeModPlugin.Log?.LogInfo($"Creating new Hell Mode world '{worldName}'!");
                    HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                    // Pass Hard difficulty to base game engine so it serializes without error
                    difficulty = Difficulty.Hard;
                }
                else
                {
                    if (HellModeModPlugin.IsWorldRegisteredAsHell(worldName))
                    {
                        HellModeModPlugin.UnregisterWorldAsHellMode(worldName);
                        HellModeModPlugin.SetActiveWorldHellMode(false, worldName);
                    }
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in CreateNewWorld_Prefix: {ex}");
            }
        }

        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.CreateNewWorld))]
        [HarmonyPostfix]
        public static void CreateNewWorld_Postfix(WorldCreationPresenter __instance, string worldName, Difficulty difficulty)
        {
            try
            {
                if (HellModeModPlugin.SelectedCreationDifficultyIndex == 3 || HellModeModPlugin.IsWorldRegisteredAsHell(worldName))
                {
                    HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.CreateDefaultWorld))]
        [HarmonyPrefix]
        public static void CreateDefaultWorld_Prefix(WorldCreationPresenter __instance, string worldName, ref Difficulty difficulty)
        {
            try
            {
                // In Method 2 (Character Creation -> FinalSelection simple world creation),
                // the user configures the world in FinalSelection, NOT WorldCreationPresenter.
                bool isHell = (int)difficulty == 3 
                           || HellModeModPlugin.SelectedCreationDifficultyIndex == 3;

                // Check FinalSelection directly if active
                if (FinalSelection.Singleton != null && FinalSelection.Singleton._isNewWorldMode)
                {
                    if (FinalSelection.Singleton._simpleWorldDifficultySlider != null)
                    {
                        int sVal = Mathf.RoundToInt(FinalSelection.Singleton._simpleWorldDifficultySlider.value);
                        if (sVal == 3) isHell = true;
                        else if (sVal >= 0 && sVal <= 2) isHell = false;
                    }
                    if ((int)FinalSelection.Singleton._selectedDifficulty == 3)
                    {
                        isHell = true;
                    }
                }

                // If already marked active as Hell by FinalSelection.StartGame_Prefix
                if (!isHell && HellModeModPlugin.CurrentWorldIsHellMode && string.Equals(HellModeModPlugin.CurrentWorldName, worldName, StringComparison.OrdinalIgnoreCase))
                {
                    isHell = true;
                }

                // If already registered as Hell and not explicitly non-Hell
                if (!isHell && HellModeModPlugin.IsWorldRegisteredAsHell(worldName))
                {
                    if (HellModeModPlugin.SelectedCreationDifficultyIndex == 3 || (int)difficulty == 3 || HellModeModPlugin.CurrentWorldIsHellMode)
                    {
                        isHell = true;
                    }
                }

                if (isHell)
                {
                    HellModeModPlugin.Log?.LogInfo($"Creating default Hell Mode world '{worldName}'!");
                    HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                    difficulty = Difficulty.Hard;
                }
                else
                {
                    if (HellModeModPlugin.IsWorldRegisteredAsHell(worldName))
                    {
                        HellModeModPlugin.UnregisterWorldAsHellMode(worldName);
                        HellModeModPlugin.SetActiveWorldHellMode(false, worldName);
                    }
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in CreateDefaultWorld_Prefix: {ex}");
            }
        }

        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.CreateDefaultWorld))]
        [HarmonyPostfix]
        public static void CreateDefaultWorld_Postfix(string worldName, Difficulty difficulty)
        {
            try
            {
                if (HellModeModPlugin.SelectedCreationDifficultyIndex == 3 
                    || (HellModeModPlugin.CurrentWorldIsHellMode && string.Equals(HellModeModPlugin.CurrentWorldName, worldName, StringComparison.OrdinalIgnoreCase))
                    || HellModeModPlugin.IsWorldRegisteredAsHell(worldName))
                {
                    HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Integrates Hell Mode with FinalSelection (the Character & World Selection screen).
    /// Safely retains Hell Mode on existing worlds, displays the Hell label correctly,
    /// and ensures config updates are immediately applied upon loading.
    /// </summary>
    public static class Patch_FinalSelection
    {
        public static string GetFinalSelectionWorldName(FinalSelection fs)
        {
            if (fs == null) return string.Empty;
            try
            {
                if (fs._isNewWorldMode)
                {
                    if (fs._simpleWorldNameInput != null && !string.IsNullOrWhiteSpace(fs._simpleWorldNameInput.text))
                    {
                        return fs._simpleWorldNameInput.text.Trim();
                    }
                    if (fs._playerFile != null && fs._playerFile.playerData != null && !string.IsNullOrWhiteSpace(fs._playerFile.playerData.characterName))
                    {
                        return $"{fs._playerFile.playerData.characterName}'s World".Trim();
                    }
                }
                else
                {
                    if (fs._worldFile != null && fs._worldFile.worldData != null && !string.IsNullOrWhiteSpace(fs._worldFile.worldData.Name))
                    {
                        return fs._worldFile.worldData.Name.Trim();
                    }
                    if (fs._simpleWorldNameInput != null && !string.IsNullOrWhiteSpace(fs._simpleWorldNameInput.text))
                    {
                        return fs._simpleWorldNameInput.text.Trim();
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        [HarmonyPatch(typeof(FinalSelection), nameof(FinalSelection.ReadSliderDifficulty))]
        [HarmonyPostfix]
        public static void ReadSliderDifficulty_Postfix(FinalSelection __instance, ref Difficulty __result)
        {
            try
            {
                if (__instance != null && __instance._simpleWorldDifficultySlider != null)
                {
                    int val = Mathf.RoundToInt(__instance._simpleWorldDifficultySlider.value);
                    if (val == 3)
                    {
                        __result = (Difficulty)3;
                    }
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(FinalSelection), nameof(FinalSelection.OnDifficultySliderChanged))]
        [HarmonyPostfix]
        public static void OnDifficultySliderChanged_Postfix(FinalSelection __instance)
        {
            try
            {
                if (__instance == null || __instance._simpleWorldDifficultySlider == null) return;
                int rounded = Mathf.RoundToInt(__instance._simpleWorldDifficultySlider.value);
                HellModeModPlugin.SelectedCreationDifficultyIndex = rounded;
                string worldName = GetFinalSelectionWorldName(__instance);

                if (rounded == 3)
                {
                    __instance.SetSelectedDifficulty((Difficulty)3);
                    if (!string.IsNullOrWhiteSpace(worldName))
                    {
                        HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                        HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                    }
                }
                else
                {
                    // CRITICAL FIX: Only unregister if user is in New World Mode!
                    // If browsing/selecting existing worlds (_isNewWorldMode == false),
                    // vanilla FillInWorldValues temporarily sets slider to 2 (Hard),
                    // which MUST NOT delete the world's .hell marker!
                    if (__instance._isNewWorldMode)
                    {
                        if (!string.IsNullOrWhiteSpace(worldName) && HellModeModPlugin.IsWorldRegisteredAsHell(worldName))
                        {
                            HellModeModPlugin.UnregisterWorldAsHellMode(worldName);
                            HellModeModPlugin.SetActiveWorldHellMode(false, worldName);
                        }
                    }
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(FinalSelection), nameof(FinalSelection.OnSimpleWorldNameUpdate))]
        [HarmonyPostfix]
        public static void OnSimpleWorldNameUpdate_Postfix(FinalSelection __instance)
        {
            try
            {
                if (__instance == null || !__instance._isNewWorldMode) return;
                if (__instance._simpleWorldDifficultySlider != null)
                {
                    int rounded = Mathf.RoundToInt(__instance._simpleWorldDifficultySlider.value);
                    if (rounded == 3)
                    {
                        string worldName = GetFinalSelectionWorldName(__instance);
                        if (!string.IsNullOrWhiteSpace(worldName))
                        {
                            HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                            HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                        }
                    }
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(FinalSelection), nameof(FinalSelection.FillInWorldValues))]
        [HarmonyPostfix]
        public static void FillInWorldValues_Postfix(FinalSelection __instance)
        {
            try
            {
                if (__instance == null) return;
                if (__instance._isNewWorldMode) return;

                if (__instance._worldFile == null || __instance._worldFile.worldData == null) return;
                string worldName = __instance._worldFile.worldData.Name;
                bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(worldName);
                if (isHell)
                {
                    if (__instance._simpleWorldDifficultySlider != null)
                    {
                        __instance._simpleWorldDifficultySlider.value = 3f;
                        var diffSlider = __instance._simpleWorldDifficultySlider.GetComponent<DifficultySlider>();
                        if (diffSlider != null)
                        {
                            diffSlider.UpdateDifficultyText(3f);
                        }
                    }
                    __instance.SetSelectedDifficulty((Difficulty)3);

                    if (__instance._worldDifficultyTexts != null)
                    {
                        foreach (var txt in __instance._worldDifficultyTexts)
                        {
                            if (txt != null)
                            {
                                txt.text = "<color=#FF2222>Hell</color>";
                            }
                        }
                    }

                    if (__instance._characterDifficultyPreview != null)
                    {
                        __instance._characterDifficultyPreview.text = "<color=#FF2222>Hell</color>";
                    }

                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                    HellModeModPlugin.Log?.LogInfo($"FinalSelection loaded Hell Mode world '{worldName}', set slider to position 3.");
                }
                else
                {
                    HellModeModPlugin.SetActiveWorldHellMode(false, worldName);
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in FinalSelection.FillInWorldValues_Postfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(FinalSelection), nameof(FinalSelection.StartGame))]
        [HarmonyPrefix]
        public static void StartGame_Prefix(FinalSelection __instance)
        {
            try
            {
                if (__instance == null) return;

                string worldName = GetFinalSelectionWorldName(__instance);
                bool isRegisteredHell = !string.IsNullOrWhiteSpace(worldName) && HellModeModPlugin.IsWorldRegisteredAsHell(worldName);

                int diff = (int)__instance._selectedDifficulty;
                if (__instance._simpleWorldDifficultySlider != null)
                {
                    int sliderVal = Mathf.RoundToInt(__instance._simpleWorldDifficultySlider.value);
                    if (sliderVal == 3) diff = 3;
                    else if (sliderVal >= 0 && sliderVal <= 2) diff = sliderVal;
                }

                // If existing world is already registered as Hell, preserve Hell Mode!
                if (!__instance._isNewWorldMode && isRegisteredHell)
                {
                    diff = 3;
                }

                HellModeModPlugin.SelectedCreationDifficultyIndex = diff;

                if (diff == 3 || isRegisteredHell)
                {
                    HellModeModPlugin.Log?.LogInfo($"FinalSelection starting Hell Mode world '{worldName}'!");
                    HellModeModPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);

                    // For new world mode, keeping selectedDifficulty as 3 ensures CreateDefaultWorld receives 3,
                    // where CreateDefaultWorld_Prefix will safely convert it to Hard for engine serialization.
                    // For existing world mode, set it to Hard.
                    if (__instance._isNewWorldMode)
                    {
                        __instance.SetSelectedDifficulty((Difficulty)3);
                    }
                    else
                    {
                        __instance._selectedDifficulty = Difficulty.Hard;
                    }
                }
                else if (diff >= 0 && diff <= 2 && __instance._isNewWorldMode)
                {
                    if (isRegisteredHell)
                    {
                        HellModeModPlugin.Log?.LogInfo($"FinalSelection world '{worldName}' difficulty changed from Hell to {__instance._selectedDifficulty}.");
                        HellModeModPlugin.UnregisterWorldAsHellMode(worldName);
                        HellModeModPlugin.SetActiveWorldHellMode(false, worldName);
                    }
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in FinalSelection.StartGame_Prefix: {ex}");
            }
        }
    }

    /// <summary>
    /// Hooks WorldSelection screen so selecting a world updates the difficulty text to Hell.
    /// </summary>
    public static class Patch_WorldSelection
    {
        [HarmonyPatch(typeof(WorldSelection), nameof(WorldSelection.SelectWorld))]
        [HarmonyPostfix]
        public static void SelectWorld_Postfix(WorldSelection __instance, int x)
        {
            try
            {
                if (__instance == null || __instance.CurrentWorldFile == null || __instance.CurrentWorldFile.worldData == null) return;
                string worldName = __instance.CurrentWorldFile.worldData.Name;
                bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(worldName);
                if (isHell)
                {
                    if (__instance._worldDifficultyText != null)
                    {
                        __instance._worldDifficultyText.text = "<color=#FF2222>Hell</color>";
                    }
                    HellModeModPlugin.SetActiveWorldHellMode(true, worldName);
                    HellModeModPlugin.Log?.LogInfo($"WorldSelection: selected Hell Mode world '{worldName}'.");
                }
                else
                {
                    HellModeModPlugin.SetActiveWorldHellMode(false, worldName);
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in WorldSelection.SelectWorld_Postfix: {ex}");
            }
        }
    }

    /// <summary>
    /// Detects world loading and applies Hell Mode state, dynamic config reloading, and localization.
    /// </summary>
    public static class Patch_WorldLifecycle
    {
        [HarmonyPatch(typeof(SaveSystem), nameof(SaveSystem.LoadWorld))]
        [HarmonyPostfix]
        public static void LoadWorld_Postfix(string worldName, WorldSaveFile __result)
        {
            try
            {
                if (__result != null && !string.IsNullOrEmpty(worldName))
                {
                    HellModeModPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(worldName);
                    HellModeModPlugin.SetActiveWorldHellMode(isHell, worldName);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(SaveSystem), nameof(SaveSystem.TryLoadWorldWithFallback))]
        [HarmonyPostfix]
        public static void TryLoadWorldWithFallback_Postfix(string name, WorldSaveFile __result)
        {
            try
            {
                if (__result != null && !string.IsNullOrEmpty(name))
                {
                    HellModeModPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(name);
                    HellModeModPlugin.SetActiveWorldHellMode(isHell, name);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(ServerPersistentData), nameof(ServerPersistentData.LoadWorldData))]
        [HarmonyPostfix]
        public static void LoadWorldData_Postfix(ServerPersistentData __instance)
        {
            try
            {
                if (__instance != null && !string.IsNullOrEmpty(__instance.Name))
                {
                    HellModeModPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(__instance.Name);
                    HellModeModPlugin.SetActiveWorldHellMode(isHell, __instance.Name);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(ServerPersistentData), nameof(ServerPersistentData.Awake))]
        [HarmonyPostfix]
        public static void ServerPersistentData_Awake_Postfix(ServerPersistentData __instance)
        {
            try
            {
                if (__instance != null && !string.IsNullOrEmpty(__instance.Name))
                {
                    HellModeModPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(__instance.Name);
                    HellModeModPlugin.SetActiveWorldHellMode(isHell, __instance.Name);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.Awake))]
        [HarmonyPostfix]
        public static void DifficultyManager_Awake_Postfix(DifficultyManager __instance)
        {
            try
            {
                HellModeModPlugin.ReloadConfigIfChanged(true);
                if (ServerPersistentData.Singleton != null && !string.IsNullOrEmpty(ServerPersistentData.Singleton.Name))
                {
                    bool isHell = HellModeModPlugin.IsWorldRegisteredAsHell(ServerPersistentData.Singleton.Name);
                    HellModeModPlugin.SetActiveWorldHellMode(isHell, ServerPersistentData.Singleton.Name);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        [HarmonyPostfix]
        public static void Player_Awake_Postfix(Player __instance)
        {
            try
            {
                if (__instance != null)
                {
                    HellModeModPlugin.CachedPlayer = __instance;
                    HellModeModPlugin.ReloadConfigIfChanged(true);

                    if (HellModeModPlugin.IsHellModeActive)
                    {
                        HellModeModPlugin.Log?.LogInfo("=========================================================");
                        HellModeModPlugin.Log?.LogInfo("[HellModeMod] HELL MODE ACTIVE ON THIS WORLD!");
                        HellModeModPlugin.Log?.LogInfo($"  - Monster Health Multiplier: {HellModeModPlugin.MonsterHealthMult?.Value}x");
                        HellModeModPlugin.Log?.LogInfo($"  - Monster Damage Multiplier: {HellModeModPlugin.MonsterDamageMult?.Value}x");
                        HellModeModPlugin.Log?.LogInfo($"  - Combat Pace Multiplier: {HellModeModPlugin.CombatPaceMult?.Value}x (Turn delay = base / {HellModeModPlugin.CombatPaceMult?.Value})");
                        HellModeModPlugin.Log?.LogInfo($"  - Monster Move Speed Multiplier: {HellModeModPlugin.MonsterSpeedMult?.Value}x");
                        HellModeModPlugin.Log?.LogInfo($"  - Elite / Empowerment Chance Multiplier: {HellModeModPlugin.EmpowermentChanceMult?.Value}x");
                        HellModeModPlugin.Log?.LogInfo($"  - Dynamic Monster Level: PlayerLevel + {HellModeModPlugin.MonsterBonusLevelOverPlayer?.Value}");
                        HellModeModPlugin.Log?.LogInfo($"  - Level Cap: handled by ConfigurableLevelCapFreebuff (up to 99)");
                        HellModeModPlugin.Log?.LogInfo($"  - Bonus EXP Multiplier: {HellModeModPlugin.ExpMultiplier?.Value}x | Gold & Loot: Vanilla 1.0x");
                        HellModeModPlugin.Log?.LogInfo("=========================================================");
                    }
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnNetworkSpawn))]
        [HarmonyPostfix]
        public static void Player_OnNetworkSpawn_Postfix(Player __instance)
        {
            try
            {
                if (__instance != null)
                {
                    HellModeModPlugin.CachedPlayer = __instance;
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetDifficultyName))]
        [HarmonyPrefix]
        public static bool GetDifficultyName_Prefix(Difficulty difficulty, ref string __result)
        {
            if ((int)difficulty == 3)
            {
                __result = "<color=#FF2222>Hell</color>";
                return false;
            }
            if (HellModeModPlugin.IsHellModeActive && difficulty == Difficulty.Hard)
            {
                // In game saves, Hell Mode is stored as Hard, so display Hell for this world
                __result = "<color=#FF2222>Hell</color>";
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Implements Hell Mode difficulty dials:
    /// Dynamically applies latest user-configured multipliers from com.custom.hellmodemod.cfg.
    /// </summary>
    public static class Patch_DifficultyDials
    {
        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterHealthMultiplier))]
        [HarmonyPostfix]
        public static void GetMonsterHealthMultiplier_Postfix(Monster monster, ref float __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                __result = HellModeModPlugin.MonsterHealthMult != null ? HellModeModPlugin.MonsterHealthMult.Value : 10.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterDamageDealtMultiplier))]
        [HarmonyPostfix]
        public static void GetMonsterDamageDealtMultiplier_Postfix(Monster monster, ref float __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                __result = HellModeModPlugin.MonsterDamageMult != null ? HellModeModPlugin.MonsterDamageMult.Value : 5.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetCombatPaceMultiplier))]
        [HarmonyPostfix]
        public static void GetCombatPaceMultiplier_Postfix(ref float __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                __result = HellModeModPlugin.CombatPaceMult != null ? HellModeModPlugin.CombatPaceMult.Value : 2.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterSpeedMultiplier))]
        [HarmonyPostfix]
        public static void GetMonsterSpeedMultiplier_Postfix(ObjectsCommon obj, ref float __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                __result = HellModeModPlugin.MonsterSpeedMult != null ? HellModeModPlugin.MonsterSpeedMult.Value : 1.5f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetEmpowermentChanceMultiplier))]
        [HarmonyPostfix]
        public static void GetEmpowermentChanceMultiplier_Postfix(MonsterConfiguration config, ref float __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                __result = HellModeModPlugin.EmpowermentChanceMult != null ? HellModeModPlugin.EmpowermentChanceMult.Value : 3.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetXPMultiplier))]
        [HarmonyPostfix]
        public static void GetXPMultiplier_Postfix(MonsterConfiguration config, ref float __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                __result = HellModeModPlugin.ExpMultiplier != null ? HellModeModPlugin.ExpMultiplier.Value : 3.0f;
            }
        }
    }

    /// <summary>
    /// Dynamically scales monster level to PlayerLevel + MonsterBonusLevelOverPlayer in Hell Mode.
    /// E.g. Player Level 1 -> Monster Level 21; Player Level 75 -> Monster Level 95.
    /// </summary>
    public static class Patch_MonsterLevelScaling
    {
        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterBonusLevel))]
        [HarmonyPostfix]
        public static void GetMonsterBonusLevel_Postfix(MonsterConfiguration config, ref int __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                int pLvl = HellModeModPlugin.GetPlayerLevel();
                int bonusOffset = HellModeModPlugin.MonsterBonusLevelOverPlayer != null ? HellModeModPlugin.MonsterBonusLevelOverPlayer.Value : 20;
                int baseLvl = config != null ? config.MonsterLevel : 1;
                int targetLvl = pLvl + bonusOffset;
                __result = Math.Max(bonusOffset, targetLvl - baseLvl);
            }
        }

        [HarmonyPatch(typeof(MonsterSetup), nameof(MonsterSetup.ApplyDifficultyProfile))]
        [HarmonyPostfix]
        public static void ApplyDifficultyProfile_Postfix(MonsterSetup __instance)
        {
            try
            {
                if (!HellModeModPlugin.IsHellModeActive || __instance == null || __instance._monster == null) return;
                var monster = __instance._monster;
                if (monster.Level != null)
                {
                    int pLvl = HellModeModPlugin.GetPlayerLevel();
                    int bonusOffset = HellModeModPlugin.MonsterBonusLevelOverPlayer != null ? HellModeModPlugin.MonsterBonusLevelOverPlayer.Value : 20;
                    int targetLvl = pLvl + bonusOffset;
                    monster.Level.Value = Math.Max(1, targetLvl);
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in ApplyDifficultyProfile_Postfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(MonsterSetup), nameof(MonsterSetup.InitializeMonster))]
        [HarmonyPostfix]
        public static void InitializeMonster_Postfix(MonsterSetup __instance)
        {
            try
            {
                if (!HellModeModPlugin.IsHellModeActive || __instance == null || __instance._monster == null) return;
                var monster = __instance._monster;
                if (monster.Level != null)
                {
                    int pLvl = HellModeModPlugin.GetPlayerLevel();
                    int bonusOffset = HellModeModPlugin.MonsterBonusLevelOverPlayer != null ? HellModeModPlugin.MonsterBonusLevelOverPlayer.Value : 20;
                    int targetLvl = pLvl + bonusOffset;
                    monster.Level.Value = Math.Max(1, targetLvl);
                }
            }
            catch (Exception ex)
            {
                HellModeModPlugin.Log?.LogError($"Error in InitializeMonster_Postfix: {ex}");
            }
        }
    }

    /// <summary>
    /// Hell Mode bonus EXP multiplier.
    /// NOTE: The level cap is intentionally NOT managed here. ConfigurableLevelCapFreebuff
    /// handles the global cap (up to 99) via its native patch, so there is no per-mode cap
    /// gate and no memory patching in this mod — this avoids any conflict with that mod.
    /// </summary>
    public static class Patch_Progression
    {
        [HarmonyPatch(typeof(XP), nameof(XP.CalculateXPGained))]
        [HarmonyPostfix]
        public static void CalculateXPGained_Postfix(ref int __result)
        {
            if (HellModeModPlugin.IsHellModeActive)
            {
                float mult = HellModeModPlugin.ExpMultiplier != null ? HellModeModPlugin.ExpMultiplier.Value : 3.0f;
                if (mult > 1.0f)
                {
                    __result = (int)Math.Round(__result * mult);
                }
            }
        }
    }
}
