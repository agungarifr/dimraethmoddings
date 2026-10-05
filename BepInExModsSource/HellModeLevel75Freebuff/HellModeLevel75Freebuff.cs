using System;
using System.IO;
using System.Reflection;
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

namespace HellModeLevel75Freebuff
{
    [BepInPlugin("com.freebuff.hellmodelevel75", "HellModeLevel75Freebuff", "2.2.2")]
    public class HellModeLevel75FreebuffPlugin : BasePlugin
    {
        public static HellModeLevel75FreebuffPlugin Instance { get; private set; }
        internal static new ManualLogSource Log;

        // Config Entries
        public static ConfigEntry<bool> ForceHellMode;
        public static ConfigEntry<float> MonsterHealthMult;
        public static ConfigEntry<float> MonsterDamageMult;
        public static ConfigEntry<float> CombatPaceMult;
        public static ConfigEntry<float> MonsterSpeedMult;
        public static ConfigEntry<float> EmpowermentChanceMult;
        public static ConfigEntry<int> MonsterBonusLevelOverPlayer;
        public static ConfigEntry<int> MaxLevelCap;
        public static ConfigEntry<int> MaxAttributeCap;
        public static ConfigEntry<float> ExpMultiplier;
        public static ConfigEntry<bool> EnableNativeBytePatch;
        public static ConfigEntry<bool> EnableLevelSync;
        public static ConfigEntry<float> IdleCheckIntervalSeconds;

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
                    Log?.LogInfo($"[HellLevel75FB] Config reloaded from disk ({configPath})! Active values:");
                    Log?.LogInfo($"  - Monster Health Multiplier: {MonsterHealthMult?.Value}x");
                    Log?.LogInfo($"  - Monster Damage Multiplier: {MonsterDamageMult?.Value}x");
                    Log?.LogInfo($"  - Combat Pace Multiplier: {CombatPaceMult?.Value}x (Turn delay = base / {CombatPaceMult?.Value})");
                    Log?.LogInfo($"  - Monster Move Speed Multiplier: {MonsterSpeedMult?.Value}x");
                    Log?.LogInfo($"  - Elite / Empowerment Chance Multiplier: {EmpowermentChanceMult?.Value}x");
                    Log?.LogInfo($"  - Monster Bonus Level: PlayerLevel + {MonsterBonusLevelOverPlayer?.Value}");
                    Log?.LogInfo($"  - Max Level Cap: {MaxLevelCap?.Value}");
                    Log?.LogInfo($"  - Max Attribute Cap: {MaxAttributeCap?.Value}");
                    Log?.LogInfo($"  - Bonus EXP Multiplier: {ExpMultiplier?.Value}x");
                    Log?.LogInfo($"  - Native Byte Patch Enabled: {EnableNativeBytePatch?.Value}");
                    Log?.LogInfo($"  - Level-Sync Service (level 75 enabler): {EnableLevelSync?.Value} (idle safety-net every {IdleCheckIntervalSeconds?.Value}s, 0=off)");
                    Log?.LogInfo("=========================================================");

                    CheckAndUpdateLevelCap();
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
            MaxLevelCap = Config.Bind("Progression", "MaxLevelCap", 75, "Max character level cap in Hell Mode (Default: 75). Set to 25 to restore vanilla behavior.");
            MaxAttributeCap = Config.Bind("Progression", "MaxAttributeCap", 75, "Max per-attribute level cap in Hell Mode (Default: 75). The engine natively supports up to 99 per attribute.");
            ExpMultiplier = Config.Bind("Progression", "ExpMultiplier", 3.0f, "Bonus EXP multiplier while playing in Hell Mode to keep progression engaging (Default: 3.0x)");
            EnableNativeBytePatch = Config.Bind("General", "EnableNativeBytePatch", true, "Also patch the level-25 immediate compares in GameAssembly.dll (best-effort, byte-verified). Keep as a secondary layer; the primary cap lift is the Level-Sync service.");
            EnableLevelSync = Config.Bind("LevelSync", "EnableLevelSync", true, "PRIMARY mechanism for Level 75: emulates the vanilla level-up loop from your accumulated XP and writes Level/HighestLevel/SkillPoints directly (never above what the server would grant for the new level, so the over-grant protection stays happy). This is what un-sticks a save already sitting at 25 with millions of XP banked.");
            IdleCheckIntervalSeconds = Config.Bind("LevelSync", "IdleCheckIntervalSeconds", 30f, "Safety-net re-check interval in seconds while playing (real time). Event-driven sync on XP gain and the 3x world-load bootstrap work regardless. Set 0 to disable idle checks entirely for minimal background activity.");

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
            // (GameConfig cap patching is NOT a Harmony patch - it is applied via
            // GameConfigCapPatcher.Apply() from CheckAndUpdateLevelCap() below.)
            SafePatch(typeof(Patch_DifficultySlider));
            SafePatch(typeof(Patch_WorldCreationPresenter));
            SafePatch(typeof(Patch_FinalSelection));
            SafePatch(typeof(Patch_WorldSelection));
            SafePatch(typeof(Patch_WorldLifecycle));
            SafePatch(typeof(Patch_DifficultyDials));
            SafePatch(typeof(Patch_MonsterLevelScaling));
            SafePatch(typeof(Patch_LevelCapAndProgression));

            Log.LogInfo("=================================================");
            Log.LogInfo("HellModeLevel75Freebuff v2.0.1 (BepInEx 6 IL2CPP) Initialized!");
            Log.LogInfo("Freebuff Edition: rebuilt level-cap architecture.");
            Log.LogInfo($"Monster Health: {MonsterHealthMult.Value}x");
            Log.LogInfo($"Monster Damage: {MonsterDamageMult.Value}x");
            Log.LogInfo($"Combat Pace: {CombatPaceMult.Value}x (Turn delay = base / {CombatPaceMult.Value})");
            Log.LogInfo($"Monster Move Speed: {MonsterSpeedMult.Value}x");
            Log.LogInfo($"Elite / Empowerment Chance: {EmpowermentChanceMult.Value}x");
            Log.LogInfo($"Dynamic Monster Level: PlayerLevel + {MonsterBonusLevelOverPlayer.Value}");
            Log.LogInfo($"Hell Mode Level Cap: {MaxLevelCap.Value} | Attribute Cap: {MaxAttributeCap.Value} (Vanilla 25 preserved on Easy/Moderate/Hard)");
            Log.LogInfo($"Hell Mode Bonus EXP: {ExpMultiplier.Value}x (Gold & Loot vanilla 1.0x)");
            Log.LogInfo($"Native Byte Patch: {(EnableNativeBytePatch.Value ? "ENABLED" : "disabled")} | GameConfig Runtime Data Patch: ALWAYS ON (primary)");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");

            // Apply initial cap state (restores vanilla 25 until a Hell world is loaded)
            CheckAndUpdateLevelCap();
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
                CheckAndUpdateLevelCap();
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
                CheckAndUpdateLevelCap();
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
            LevelSyncService.ResetForNewWorld();
            ReloadConfigIfChanged(true);
            Log?.LogInfo($"Active World set to '{worldName}' - Hell Mode: {isHell}");
            CheckAndUpdateLevelCap();
        }

        public static void CheckAndUpdateLevelCap()
        {
            bool hellActive = IsHellModeActive;
            int cap = MaxLevelCap != null ? MaxLevelCap.Value : 75;
            int attrCap = MaxAttributeCap != null ? MaxAttributeCap.Value : 75;

            // PRIMARY mechanism: patch the engine's GameConfig data (MaxLevel / MaxAttributeLevel)
            GameConfigCapPatcher.Apply(hellActive, cap, attrCap);

            // SECONDARY mechanism (optional): byte-patch the 25-immediates inside known methods
            if (EnableNativeBytePatch != null && EnableNativeBytePatch.Value)
            {
                NativeBytePatcher.SetNativeLevelCap(hellActive, cap);
            }
            else
            {
                NativeBytePatcher.RestoreVanillaIfPatched();
            }
        }

        public static void LogGameConfigState()
        {
            try
            {
                var (maxLevel, maxAttr) = GameConfigCapPatcher.ReadCurrentValues();
                if (maxLevel < 0 && maxAttr < 0)
                {
                    Log?.LogWarning("[HellLevel75FB] Diagnostics: GameConfig values not readable (data patch unavailable, relying on secondary layers).");
                    return;
                }
                Log?.LogInfo($"[HellLevel75FB] Diagnostics: GameConfig live values -> MaxLevel={maxLevel}, MaxAttributeLevel={maxAttr} (vanilla 25/25; Hell patches to {MaxLevelCap?.Value}/{MaxAttributeCap?.Value})");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"[HellLevel75FB] Diagnostics error reading GameConfig: {ex.Message}");
            }
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
    /// PRIMARY LEVEL CAP MECHANISM (v2.0.1 - Freebuff Edition).
    ///
    /// The vanilla game enforces its Level 25 cap by reading GameConfig.MaxLevel
    /// (a STATIC data value read by the XP level-up loop and the upgrade UI).
    /// That is why v1's fixed-RVA byte patching reported "[8/8 hooks]" yet never
    /// uncapped anything: the cap lives in static data, not in the patched bytes.
    ///
    /// v2.0.0 lesson learned: writing through the interop property wrapper
    /// (GameConfig.MaxLevel = 75) does NOT persist to the native static storage.
    /// v2.0.1 therefore resolves the il2cpp class/field metadata directly and
    /// writes into the native static field data block in memory:
    ///   class ptr  = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "GameConfig")
    ///   field ptr  = IL2CPP.GetIl2CppClassField(class, "MaxLevel")
    ///   offset     = IL2CPP.il2cpp_field_get_offset(field)
    ///   base       = IL2CPP.il2cpp_class_get_static_field_data(class)
    ///   *(int*)(base + offset) = 75
    /// This is version-resilient (no hardcoded RVAs) and verified by read-back.
    /// </summary>
    public static class GameConfigCapPatcher
    {
        private static bool? _currentState = null;
        private static int _currentLevelCap = -1;
        private static int _currentAttrCap = -1;

        // Native il2cpp metadata pointers
        private static IntPtr _nativeClassPtr = IntPtr.Zero;
        private static IntPtr _maxLevelFieldPtr = IntPtr.Zero;
        private static IntPtr _maxAttrFieldPtr = IntPtr.Zero;
        private static int _originalMaxLevel = 25;
        private static int _originalMaxAttr = 99;
        private static bool _originalsCaptured = false;
        private static bool _initFailed = false;

        /// <summary>Reads an int static field through the engine's own field API.</summary>
        private static unsafe int ReadStaticInt(IntPtr fieldPtr)
        {
            IntPtr buf = Marshal.AllocHGlobal(4);
            try
            {
                IL2CPP.il2cpp_field_static_get_value(fieldPtr, (void*)buf);
                return Marshal.ReadInt32(buf);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <summary>Writes an int static field through the engine's own field API, then verifies by reading back.</summary>
        private static unsafe bool WriteStaticInt(IntPtr fieldPtr, int value, out int after)
        {
            IntPtr buf = Marshal.AllocHGlobal(4);
            try
            {
                Marshal.WriteInt32(buf, value);
                IL2CPP.il2cpp_field_static_set_value(fieldPtr, (void*)buf);
                IL2CPP.il2cpp_field_static_get_value(fieldPtr, (void*)buf); // read back through the engine
                after = Marshal.ReadInt32(buf);
                return after == value;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        private static bool TryInit()
        {
            if (_initFailed) return false;
            if (_originalsCaptured && _maxLevelFieldPtr != IntPtr.Zero) return true;

            try
            {
                _nativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "GameConfig");
                if (_nativeClassPtr == IntPtr.Zero)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogWarning("[HellLevel75FB] Native GameConfig class not found - primary cap patch unavailable.");
                    _initFailed = true;
                    return false;
                }

                // Ensure the class .cctor ran and static storage is initialized
                try { IL2CPP.il2cpp_runtime_class_init(_nativeClassPtr); } catch { }

                _maxLevelFieldPtr = IL2CPP.GetIl2CppField(_nativeClassPtr, "MaxLevel");
                _maxAttrFieldPtr = IL2CPP.GetIl2CppField(_nativeClassPtr, "MaxAttributeLevel");
                if (_maxLevelFieldPtr == IntPtr.Zero)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogWarning("[HellLevel75FB] Native field 'GameConfig.MaxLevel' not found - primary cap patch unavailable.");
                    _initFailed = true;
                    return false;
                }

                // Capture vanilla values exactly once (before we ever write)
                if (!_originalsCaptured)
                {
                    _originalMaxLevel = ReadStaticInt(_maxLevelFieldPtr);
                    _originalMaxAttr = _maxAttrFieldPtr != IntPtr.Zero ? ReadStaticInt(_maxAttrFieldPtr) : -1;
                    _originalsCaptured = true;
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] GameConfig static fields resolved via il2cpp field API: MaxLevel = {_originalMaxLevel}, MaxAttributeLevel = {_originalMaxAttr}");
                }

                return true;
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogWarning($"[HellLevel75FB] GameConfig native init failed: {ex.Message} - relying on secondary layers.");
                _initFailed = true;
                return false;
            }
        }

        /// <summary>Reads the live native GameConfig values for diagnostics (best-effort).</summary>
        public static (int maxLevel, int maxAttributeLevel) ReadCurrentValues()
        {
            int maxLevel = -1, maxAttr = -1;
            if (TryInit())
            {
                try { maxLevel = ReadStaticInt(_maxLevelFieldPtr); } catch { }
                try { if (_maxAttrFieldPtr != IntPtr.Zero) maxAttr = ReadStaticInt(_maxAttrFieldPtr); } catch { }
            }
            if (maxLevel < 0) { try { maxLevel = GameConfig.MaxLevel; } catch { } }
            if (maxAttr < 0) { try { maxAttr = GameConfig.MaxAttributeLevel; } catch { } }
            return (maxLevel, maxAttr);
        }

        /// <summary>
        /// Writes the Hell level cap into GameConfig's static storage via the engine's field API (or restores vanilla).
        /// MaxAttributeLevel is only raised when the configured attribute cap exceeds the native
        /// vanilla value (native is 99, so the default config never touches it).
        /// </summary>
        public static void Apply(bool hellActive, int levelCap, int attributeCap)
        {
            if (!TryInit())
            {
                // Keep state machine open so a later successful init still applies
                _currentState = null;
                return;
            }

            int targetLevelCap = hellActive ? Math.Max(1, levelCap) : _originalMaxLevel;

            // Attribute policy: never lower below vanilla; only raise when configured above vanilla.
            int targetAttrCap = _originalMaxAttr;
            bool touchAttr = hellActive && attributeCap > _originalMaxAttr && _maxAttrFieldPtr != IntPtr.Zero;
            if (touchAttr)
            {
                targetAttrCap = attributeCap;
            }

            if (_currentState.HasValue && _currentState.Value == hellActive
                && _currentLevelCap == targetLevelCap && _currentAttrCap == targetAttrCap)
            {
                return; // Already in desired state
            }

            try
            {
                int beforeLevel = ReadStaticInt(_maxLevelFieldPtr);
                bool levelOk = WriteStaticInt(_maxLevelFieldPtr, targetLevelCap, out int afterLevel);

                int beforeAttr = -1, afterAttr = -1;
                bool attrOk = true;
                if (touchAttr)
                {
                    beforeAttr = ReadStaticInt(_maxAttrFieldPtr);
                    attrOk = WriteStaticInt(_maxAttrFieldPtr, targetAttrCap, out afterAttr);
                }
                else if (_maxAttrFieldPtr != IntPtr.Zero)
                {
                    afterAttr = ReadStaticInt(_maxAttrFieldPtr);
                }

                _currentState = hellActive;
                _currentLevelCap = targetLevelCap;
                _currentAttrCap = targetAttrCap;

                HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] GameConfig.MaxLevel (static): {beforeLevel} -> {afterLevel} ({(hellActive ? $"Hell cap {targetLevelCap}" : "vanilla restored")}){(levelOk ? " [verified]" : " [WRITE MISMATCH!]")}");
                if (_maxAttrFieldPtr != IntPtr.Zero)
                {
                    string attrDesc = touchAttr ? $"{beforeAttr} -> {afterAttr}" : $"untouched at {afterAttr} (engine native value >= configured cap)";
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] GameConfig.MaxAttributeLevel (static): {attrDesc}");
                }

                if (!levelOk || !attrOk)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogWarning("[HellLevel75FB] GameConfig static write verification FAILED - relying on secondary layers (byte patch + Harmony).");
                }
                else
                {
                    string attrCapDesc = touchAttr ? targetAttrCap.ToString() : $"native({_originalMaxAttr})";
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] GameConfig data patch applied & verified: HellMode={hellActive} LevelCap={targetLevelCap} AttrCap={attrCapDesc}");
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"[HellLevel75FB] GameConfig data patch failed: {ex} - relying on secondary layers.");
            }
        }
    }

    /// <summary>
    /// SECONDARY mechanism (optional, config-gated): byte-verified patches of the
    /// level-25 immediate compares inside known hot methods of GameAssembly.dll.
    /// v1 relied solely on this and missed both the GameConfig data source and the
    /// attribute-cap compares inside PlayerStats.ApplyUpgradeAttributeInternal.
    /// v2 adds the two missed ApplyUpgradeAttributeInternal sites and keeps the
    /// whole layer best-effort: each site is only written when its expected byte
    /// is still present, so a game update degrades gracefully instead of crashing.
    /// </summary>
    public static class NativeBytePatcher
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private static readonly (long rva, int offset, byte originalByte, string site)[] PatchTargets = new[]
        {
            (0x114C32BL, 2, (byte)0x19, "XP.IsXPGainBlocked #1"),
            (0x114C361L, 2, (byte)0x19, "XP.IsXPGainBlocked #2"),
            (0x114C368L, 1, (byte)0x19, "XP.IsXPGainBlocked #3"),
            (0x92C0ADL,  1, (byte)0x19, "Player.InitializeHighestLevel #1"),
            (0x92C312L,  2, (byte)0x19, "Player.InitializeHighestLevel #2"),
            (0x92C317L,  1, (byte)0x19, "Player.InitializeHighestLevel #3"),
            (0xA21964L,  2, (byte)0x19, "PlayerStats.ApplyUpgradeAttributeInternal #1 (v2 NEW)"),
            (0xA21D36L,  2, (byte)0x19, "PlayerStats.ApplyUpgradeAttributeInternal #2 (v2 NEW)"),
            (0xA2DB55L,  2, (byte)0x19, "PlayerStats.UpgradeAttributeServerRpc #1"),
            (0xA2DED6L,  2, (byte)0x19, "PlayerStats.UpgradeAttributeServerRpc #2"),
        };

        private static bool? _currentPatchedState = null;
        private static int _currentCapValue = 25;

        public static void RestoreVanillaIfPatched()
        {
            if (_currentPatchedState == true)
            {
                SetNativeLevelCap(false, 25);
            }
        }

        public static void SetNativeLevelCap(bool enableHellCap, int capValue = 75)
        {
            if (_currentPatchedState.HasValue && _currentPatchedState.Value == enableHellCap && _currentCapValue == capValue)
            {
                return;
            }

            IntPtr gaBase = GetModuleHandle("GameAssembly.dll");
            if (gaBase == IntPtr.Zero)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogWarning("GameAssembly.dll base address not found, skipping memory patching.");
                return;
            }

            byte targetByte = enableHellCap ? (byte)capValue : (byte)0x19;
            int patchCount = 0;

            foreach (var (rva, offset, originalByte, site) in PatchTargets)
            {
                IntPtr addr = IntPtr.Add(gaBase, (int)(rva + offset));
                byte current = Marshal.ReadByte(addr);

                if (current == originalByte || current == (byte)_currentCapValue || current == (byte)capValue || current == 0x4B)
                {
                    if (VirtualProtect(addr, (UIntPtr)1, 0x40 /* PAGE_EXECUTE_READWRITE */, out uint oldProtect))
                    {
                        Marshal.WriteByte(addr, targetByte);
                        VirtualProtect(addr, (UIntPtr)1, oldProtect, out _);
                        patchCount++;
                    }
                }
                else
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogWarning($"Skipping byte patch '{site}' (RVA 0x{rva:X} + {offset}): byte is 0x{current:X2} (expected 0x{originalByte:X2}) - game update? Degrading gracefully.");
                }
            }

            _currentPatchedState = enableHellCap;
            _currentCapValue = enableHellCap ? capValue : 25;
            HellModeLevel75FreebuffPlugin.Log?.LogInfo($"Native byte layer: {(enableHellCap ? $"Uncapped to {capValue} (Hell Mode active)" : "Restored to 25 (Standard mode active)")} [{patchCount}/{PatchTargets.Length} sites] (primary mechanism: GameConfig data patch)");
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

                    HellModeLevel75FreebuffPlugin.Log?.LogInfo("DifficultySlider UI successfully revamped to 4 difficulties (EASY, MODERATE, HARD, HELL) within window boundaries!");
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in Patch_DifficultySlider.Start_Postfix: {ex}");
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
                HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex = index;
                if (index == 3)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo("World creation slider set to Hell Mode (Difficulty Index 3).");
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
                bool isHell = HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex == 3 || (int)difficulty == 3;
                if (__instance != null && __instance._view != null && __instance._view._worldDifficultySlider != null)
                {
                    if (Mathf.RoundToInt(__instance._view._worldDifficultySlider.value) == 3)
                    {
                        isHell = true;
                    }
                }

                if (isHell)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"Creating new Hell Mode world '{worldName}'!");
                    HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
                    // Pass Hard difficulty to base game engine so it serializes without error
                    difficulty = Difficulty.Hard;
                }
                else
                {
                    if (HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName))
                    {
                        HellModeLevel75FreebuffPlugin.UnregisterWorldAsHellMode(worldName);
                        HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(false, worldName);
                    }
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in CreateNewWorld_Prefix: {ex}");
            }
        }

        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.CreateNewWorld))]
        [HarmonyPostfix]
        public static void CreateNewWorld_Postfix(WorldCreationPresenter __instance, string worldName, Difficulty difficulty)
        {
            try
            {
                if (HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex == 3 || HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName))
                {
                    HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
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
                           || HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex == 3;

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
                if (!isHell && HellModeLevel75FreebuffPlugin.CurrentWorldIsHellMode && string.Equals(HellModeLevel75FreebuffPlugin.CurrentWorldName, worldName, StringComparison.OrdinalIgnoreCase))
                {
                    isHell = true;
                }

                // If already registered as Hell and not explicitly non-Hell
                if (!isHell && HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName))
                {
                    if (HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex == 3 || (int)difficulty == 3 || HellModeLevel75FreebuffPlugin.CurrentWorldIsHellMode)
                    {
                        isHell = true;
                    }
                }

                if (isHell)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"Creating default Hell Mode world '{worldName}'!");
                    HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
                    difficulty = Difficulty.Hard;
                }
                else
                {
                    if (HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName))
                    {
                        HellModeLevel75FreebuffPlugin.UnregisterWorldAsHellMode(worldName);
                        HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(false, worldName);
                    }
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in CreateDefaultWorld_Prefix: {ex}");
            }
        }

        [HarmonyPatch(typeof(WorldCreationPresenter), nameof(WorldCreationPresenter.CreateDefaultWorld))]
        [HarmonyPostfix]
        public static void CreateDefaultWorld_Postfix(string worldName, Difficulty difficulty)
        {
            try
            {
                if (HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex == 3 
                    || (HellModeLevel75FreebuffPlugin.CurrentWorldIsHellMode && string.Equals(HellModeLevel75FreebuffPlugin.CurrentWorldName, worldName, StringComparison.OrdinalIgnoreCase))
                    || HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName))
                {
                    HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
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
                HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex = rounded;
                string worldName = GetFinalSelectionWorldName(__instance);

                if (rounded == 3)
                {
                    __instance.SetSelectedDifficulty((Difficulty)3);
                    if (!string.IsNullOrWhiteSpace(worldName))
                    {
                        HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                        HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
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
                        if (!string.IsNullOrWhiteSpace(worldName) && HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName))
                        {
                            HellModeLevel75FreebuffPlugin.UnregisterWorldAsHellMode(worldName);
                            HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(false, worldName);
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
                            HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                            HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
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
                bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName);
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

                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"FinalSelection loaded Hell Mode world '{worldName}', set slider to position 3.");
                }
                else
                {
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(false, worldName);
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in FinalSelection.FillInWorldValues_Postfix: {ex}");
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
                bool isRegisteredHell = !string.IsNullOrWhiteSpace(worldName) && HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName);

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

                HellModeLevel75FreebuffPlugin.SelectedCreationDifficultyIndex = diff;

                if (diff == 3 || isRegisteredHell)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"FinalSelection starting Hell Mode world '{worldName}'!");
                    HellModeLevel75FreebuffPlugin.RegisterWorldAsHellMode(worldName);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);

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
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"FinalSelection world '{worldName}' difficulty changed from Hell to {__instance._selectedDifficulty}.");
                        HellModeLevel75FreebuffPlugin.UnregisterWorldAsHellMode(worldName);
                        HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(false, worldName);
                    }
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in FinalSelection.StartGame_Prefix: {ex}");
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
                bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName);
                if (isHell)
                {
                    if (__instance._worldDifficultyText != null)
                    {
                        __instance._worldDifficultyText.text = "<color=#FF2222>Hell</color>";
                    }
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(true, worldName);
                    HellModeLevel75FreebuffPlugin.Log?.LogInfo($"WorldSelection: selected Hell Mode world '{worldName}'.");
                }
                else
                {
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(false, worldName);
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in WorldSelection.SelectWorld_Postfix: {ex}");
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
                    HellModeLevel75FreebuffPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(worldName);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(isHell, worldName);
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
                    HellModeLevel75FreebuffPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(name);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(isHell, name);
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
                    HellModeLevel75FreebuffPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(__instance.Name);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(isHell, __instance.Name);
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
                    HellModeLevel75FreebuffPlugin.ReloadConfigIfChanged(true);
                    bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(__instance.Name);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(isHell, __instance.Name);
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
                HellModeLevel75FreebuffPlugin.ReloadConfigIfChanged(true);
                if (ServerPersistentData.Singleton != null && !string.IsNullOrEmpty(ServerPersistentData.Singleton.Name))
                {
                    bool isHell = HellModeLevel75FreebuffPlugin.IsWorldRegisteredAsHell(ServerPersistentData.Singleton.Name);
                    HellModeLevel75FreebuffPlugin.SetActiveWorldHellMode(isHell, ServerPersistentData.Singleton.Name);
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
                    HellModeLevel75FreebuffPlugin.CachedPlayer = __instance;
                    HellModeLevel75FreebuffPlugin.ReloadConfigIfChanged(true);
                    HellModeLevel75FreebuffPlugin.CheckAndUpdateLevelCap();
                    HellModeLevel75FreebuffPlugin.LogGameConfigState();

                    if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
                    {
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo("=========================================================");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo("[HellLevel75FB] HELL MODE ACTIVE ON THIS WORLD!");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Monster Health Multiplier: {HellModeLevel75FreebuffPlugin.MonsterHealthMult?.Value}x");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Monster Damage Multiplier: {HellModeLevel75FreebuffPlugin.MonsterDamageMult?.Value}x");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Combat Pace Multiplier: {HellModeLevel75FreebuffPlugin.CombatPaceMult?.Value}x (Turn delay = base / {HellModeLevel75FreebuffPlugin.CombatPaceMult?.Value})");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Monster Move Speed Multiplier: {HellModeLevel75FreebuffPlugin.MonsterSpeedMult?.Value}x");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Elite / Empowerment Chance Multiplier: {HellModeLevel75FreebuffPlugin.EmpowermentChanceMult?.Value}x");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Dynamic Monster Level: PlayerLevel + {HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer?.Value}");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Max Level Cap: {HellModeLevel75FreebuffPlugin.MaxLevelCap?.Value} | Attribute Cap: {HellModeLevel75FreebuffPlugin.MaxAttributeCap?.Value} (Uncapped)");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"  - Bonus EXP Multiplier: {HellModeLevel75FreebuffPlugin.ExpMultiplier?.Value}x | Gold & Loot: Vanilla 1.0x");
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo("=========================================================");
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
                    HellModeLevel75FreebuffPlugin.CachedPlayer = __instance;
                    HellModeLevel75FreebuffPlugin.CheckAndUpdateLevelCap();
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
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive && difficulty == Difficulty.Hard)
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
    /// Dynamically applies latest user-configured multipliers from the mod config.
    /// </summary>
    public static class Patch_DifficultyDials
    {
        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterHealthMultiplier))]
        [HarmonyPostfix]
        public static void GetMonsterHealthMultiplier_Postfix(Monster monster, ref float __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                __result = HellModeLevel75FreebuffPlugin.MonsterHealthMult != null ? HellModeLevel75FreebuffPlugin.MonsterHealthMult.Value : 10.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterDamageDealtMultiplier))]
        [HarmonyPostfix]
        public static void GetMonsterDamageDealtMultiplier_Postfix(Monster monster, ref float __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                __result = HellModeLevel75FreebuffPlugin.MonsterDamageMult != null ? HellModeLevel75FreebuffPlugin.MonsterDamageMult.Value : 5.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetCombatPaceMultiplier))]
        [HarmonyPostfix]
        public static void GetCombatPaceMultiplier_Postfix(ref float __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                __result = HellModeLevel75FreebuffPlugin.CombatPaceMult != null ? HellModeLevel75FreebuffPlugin.CombatPaceMult.Value : 2.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterSpeedMultiplier))]
        [HarmonyPostfix]
        public static void GetMonsterSpeedMultiplier_Postfix(ObjectsCommon obj, ref float __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                __result = HellModeLevel75FreebuffPlugin.MonsterSpeedMult != null ? HellModeLevel75FreebuffPlugin.MonsterSpeedMult.Value : 1.5f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetEmpowermentChanceMultiplier))]
        [HarmonyPostfix]
        public static void GetEmpowermentChanceMultiplier_Postfix(MonsterConfiguration config, ref float __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                __result = HellModeLevel75FreebuffPlugin.EmpowermentChanceMult != null ? HellModeLevel75FreebuffPlugin.EmpowermentChanceMult.Value : 3.0f;
            }
        }

        [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetXPMultiplier))]
        [HarmonyPostfix]
        public static void GetXPMultiplier_Postfix(MonsterConfiguration config, ref float __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                __result = HellModeLevel75FreebuffPlugin.ExpMultiplier != null ? HellModeLevel75FreebuffPlugin.ExpMultiplier.Value : 3.0f;
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
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                int pLvl = HellModeLevel75FreebuffPlugin.GetPlayerLevel();
                int bonusOffset = HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer != null ? HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer.Value : 20;
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
                if (!HellModeLevel75FreebuffPlugin.IsHellModeActive || __instance == null || __instance._monster == null) return;
                var monster = __instance._monster;
                if (monster.Level != null)
                {
                    int pLvl = HellModeLevel75FreebuffPlugin.GetPlayerLevel();
                    int bonusOffset = HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer != null ? HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer.Value : 20;
                    int targetLvl = pLvl + bonusOffset;
                    monster.Level.Value = Math.Max(1, targetLvl);
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in ApplyDifficultyProfile_Postfix: {ex}");
            }
        }

        [HarmonyPatch(typeof(MonsterSetup), nameof(MonsterSetup.InitializeMonster))]
        [HarmonyPostfix]
        public static void InitializeMonster_Postfix(MonsterSetup __instance)
        {
            try
            {
                if (!HellModeLevel75FreebuffPlugin.IsHellModeActive || __instance == null || __instance._monster == null) return;
                var monster = __instance._monster;
                if (monster.Level != null)
                {
                    int pLvl = HellModeLevel75FreebuffPlugin.GetPlayerLevel();
                    int bonusOffset = HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer != null ? HellModeLevel75FreebuffPlugin.MonsterBonusLevelOverPlayer.Value : 20;
                    int targetLvl = pLvl + bonusOffset;
                    monster.Level.Value = Math.Max(1, targetLvl);
                }
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogError($"Error in InitializeMonster_Postfix: {ex}");
            }
        }
    }

    /// <summary>
    /// Implements Level Cap progression guards (dynamically configured via MaxLevelCap, default 75),
    /// while preserving vanilla 25 cap on Easy, Moderate, and Hard.
    /// The heavy lifting is done by GameConfigCapPatcher; these Harmony patches keep the
    /// managed-side checks (XP blocking, attribute upgrade RPC guard) in sync with the cap.
    /// </summary>
    /// <summary>
    /// PRIMARY level-75 mechanism.
    /// </summary>
    /// <remarks>
    /// Why this exists:
    /// - The engine's level-up loop (XP.AddXPToPlayer -> XP.GrantXPToPlayer) refuses to level past
    ///   GameConfig.MaxLevel (25). That constant is baked into native code (writing the static field
    ///   via il2cpp_field_static_set_value never persists), so the vanilla loop can NEVER go past 25.
    /// - But XP still accumulates on the Player (Player.XP / AccumulatedXP keep growing thanks to the
    ///   IsXPGainBlocked Harmony prefix), so a save can sit at level 25 with millions of banked XP.
    /// - Attribute upgrades (Intelligence 37 in the user's screenshot) prove instance field writes
    ///   through the interop layer persist and replicate fine.
    ///
    /// Therefore: emulate the vanilla level-up loop ourselves. Compute the level the player SHOULD
    /// be at given Player.XP using the game's own XPRequiredForLevelUp table, then write
    /// Player.Level / Player.HighestLevel / Player.SkillPoints consistently. Skill points granted
    /// stay within MaxGrantableSkillPoints() so the server-side over-grant protection
    /// (SkillPointOverGrantPersists / ReportLocalCheatServerRpc) is never tripped.
    ///
    /// Cost model (limited-device friendly): per frame this service costs one null check plus two
    /// float compares - no allocations, no GC pressure, no GPU work. Real work fires only on
    /// (a) XP-gain events (throttled to 1x / 0.5s), (b) 3x world-load bootstrap (t+2s/7s/15s),
    /// and (c) the idle safety-net (default 30s; set IdleCheckIntervalSeconds=0 to disable).
    /// </remarks>
    public static class LevelSyncService
    {
        private static Player _queuedPlayer = null;
        private static float _syncNotBefore = 0f;
        private static float _nextBootstrapCheck = 0f;
        private static int _bootstrapChecksLeft = 0;
        private static float _nextIdleCheck = 0f;
        private static float _lastFieldWarningTime = -60f;
        private static int _lastSyncedLevel = -1;
        private const float EventThrottleSeconds = 0.5f;
        private const float FieldWarningCooldownSeconds = 10f;

        /// <summary>
        /// Queues an event-driven sync (called when the game grants XP). Throttled; performs no work itself.
        /// </summary>
        public static void RequestSync(Player player)
        {
            if (player == null) return;
            float now = Time.unscaledTime;
            if (now < _syncNotBefore) return; // throttled: the next XP event will retry anyway
            _syncNotBefore = now + EventThrottleSeconds;
            _queuedPlayer = player;
        }

        /// <summary>
        /// Per-frame pump called from the PlayerStats.Update postfix. Steady-state cost: one null
        /// check plus two float compares, zero allocations, no GPU work. Heavy path only on real work.
        /// </summary>
        public static void Tick(Player statsPlayer)
        {
            var queued = _queuedPlayer;
            if (queued == null)
            {
                TryScheduledChecks(statsPlayer);
                return;
            }
            _queuedPlayer = null;
            RunSync(queued, "xp-gain");
        }

        private static void TryScheduledChecks(Player fallbackPlayer)
        {
            bool bootstrapDue = _bootstrapChecksLeft > 0;
            float idleInterval = HellModeLevel75FreebuffPlugin.IdleCheckIntervalSeconds != null ? HellModeLevel75FreebuffPlugin.IdleCheckIntervalSeconds.Value : 30f;
            if (!bootstrapDue && idleInterval <= 0f) return; // fully dormant

            float now = Time.unscaledTime;
            if (bootstrapDue && now >= _nextBootstrapCheck)
            {
                _bootstrapChecksLeft--;
                _nextBootstrapCheck = now + (_bootstrapChecksLeft == 1 ? 8f : 5f); // fires at ~t+2s, t+7s, t+15s
                var p = fallbackPlayer ?? HellModeLevel75FreebuffPlugin.CachedPlayer;
                if (p != null) RunSync(p, $"world-load bootstrap ({3 - _bootstrapChecksLeft}/3)");
            }
            else if (idleInterval > 0f && now >= _nextIdleCheck)
            {
                _nextIdleCheck = now + idleInterval;
                var p = fallbackPlayer ?? HellModeLevel75FreebuffPlugin.CachedPlayer;
                if (p != null) RunSync(p, "idle safety-net");
            }
        }

        private static void RunSync(Player player, string reason)
        {
            try
            {
                if (player == null) return;
                if (!HellModeLevel75FreebuffPlugin.EnableLevelSync.Value) return;
                if (!HellModeLevel75FreebuffPlugin.IsHellModeActive) return;

                var levelNet = player.Level;               // inherited from ObjectsCommon (character level)
                var xpNet = player.XP;                     // current spendable/banked XP
                var highestNet = player.HighestLevel;
                var spNet = player.SkillPoints;
                if (levelNet == null || xpNet == null || highestNet == null || spNet == null)
                {
                    float now = Time.unscaledTime;
                    if (now - _lastFieldWarningTime >= FieldWarningCooldownSeconds)
                    {
                        _lastFieldWarningTime = now;
                        HellModeLevel75FreebuffPlugin.Log?.LogWarning("[HellLevel75FB] LevelSync: Player network fields not ready yet (Level/XP/HighestLevel/SkillPoints).");
                    }
                    return;
                }

                int curLevel = levelNet.Value;
                int curXP = xpNet.Value;
                int curHighest = highestNet.Value;
                int curSP = spNet.Value;

                int cap = HellModeLevel75FreebuffPlugin.MaxLevelCap != null ? HellModeLevel75FreebuffPlugin.MaxLevelCap.Value : 75;

                // Compute the level the player SHOULD be at by spending banked XP, using the game's own cost table.
                int targetLevel = curLevel;
                int xpSim = curXP;
                while (targetLevel < cap)
                {
                    int cost;
                    try { cost = PlayerStats.XPRequiredForLevelUp(targetLevel); }
                    catch { break; }
                    if (cost <= 0) break;
                    if (xpSim < cost) break;
                    xpSim -= cost;
                    targetLevel++;
                }

                if (targetLevel <= curLevel)
                {
                    _lastSyncedLevel = curLevel; // nothing to do, stay quiet
                    return;
                }

                int levelsGained = targetLevel - curLevel;

                // Skill points: mirror the vanilla grant (1 per level), but never exceed what the
                // server would allow for the new level (avoids the anti-overgrant cheat detector).
                int maxSP;
                try { maxSP = player.MaxGrantableSkillPoints(); }
                catch { maxSP = targetLevel; }

                int newSP = Math.Min(curSP + levelsGained, Math.Max(maxSP, curSP));

                // Write through the interop layer (proven to persist by the attribute upgrades).
                levelNet.Value = targetLevel;
                xpNet.Value = xpSim;
                if (targetLevel > curHighest) highestNet.Value = targetLevel;
                spNet.Value = newSP;

                // Keep the game's XP ledger balanced. The anti-cheat validates:
                //   AllTimeXP == SumLevels + AccumXP (tolerance 75)
                // Granting levels raises SumLevels by exactly the consumed XP cost, so adding the
                // same amount to AllTimeXP keeps the books balanced and the check passes honestly.
                int consumed = curXP - xpSim;
                try
                {
                    var allTimeNet = player.AllTimeXP;
                    if (allTimeNet != null && consumed > 0)
                    {
                        int beforeAllTime = allTimeNet.Value;
                        allTimeNet.Value = beforeAllTime + consumed;
                        HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] LevelSync: AllTimeXP {beforeAllTime} -> {allTimeNet.Value} (+{consumed}, keeps XP ledger balanced for anti-cheat validation)");
                    }
                }
                catch (Exception allTimeEx)
                {
                    HellModeLevel75FreebuffPlugin.Log?.LogWarning($"[HellLevel75FB] LevelSync: could not update AllTimeXP ledger: {allTimeEx.Message}");
                }

                // Mirror the game's own post-change ritual so any cached XP-state signature stays current.
                try { player.UpdateXPStateSignature(); } catch { }

                HellModeLevel75FreebuffPlugin.Log?.LogInfo("=========================================================");
                HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] LevelSync ({reason}): LEVEL UP! {curLevel} -> {targetLevel} (+{levelsGained} levels, consumed {curXP - xpSim} banked XP, {xpSim} XP left)");
                HellModeLevel75FreebuffPlugin.Log?.LogInfo($"[HellLevel75FB] LevelSync: SkillPoints {curSP} -> {newSP} (maxGrantable={maxSP}) | HighestLevel -> {Math.Max(curHighest, targetLevel)} | cap={cap}");
                HellModeLevel75FreebuffPlugin.Log?.LogInfo("=========================================================");

                _lastSyncedLevel = targetLevel;
            }
            catch (Exception ex)
            {
                HellModeLevel75FreebuffPlugin.Log?.LogWarning($"[HellLevel75FB] LevelSync error ({reason}): {ex.Message}");
                _syncNotBefore = Time.unscaledTime + 5f; // back off on errors
            }
        }

        public static void ResetForNewWorld()
        {
            float now = Time.unscaledTime;
            _queuedPlayer = null;
            _syncNotBefore = 0f;
            _bootstrapChecksLeft = 3;
            _nextBootstrapCheck = now + 2f;
            _nextIdleCheck = now + 30f;
            _lastFieldWarningTime = -60f;
            _lastSyncedLevel = -1;
        }
    }

    public static class Patch_LevelCapAndProgression
    {
        [HarmonyPatch(typeof(XP), nameof(XP.IsXPGainBlocked))]
        [HarmonyPrefix]
        public static bool IsXPGainBlocked_Prefix(Player player, ref bool __result)
        {
            if (player == null || player.Level == null) return true;

            int curLevel = player.Level.Value;
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                int maxCap = HellModeLevel75FreebuffPlugin.MaxLevelCap != null ? HellModeLevel75FreebuffPlugin.MaxLevelCap.Value : 75;
                if (curLevel >= maxCap)
                {
                    __result = true; // XP gain blocked at configured level cap
                    return false;
                }
                __result = false; // Allow XP gain up to maxCap!
                return false;
            }
            else
            {
                if (curLevel >= 25)
                {
                    __result = true; // Enforce strict 25 cap on normal modes
                    return false;
                }
                return true; // Run vanilla checks
            }
        }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.UpgradeAttributeServerRpc))]
        [HarmonyPrefix]
        public static bool UpgradeAttributeServerRpc_Prefix(PlayerStats __instance, string attributeString)
        {
            if (__instance != null && __instance._player != null && __instance._player.Level != null)
            {
                int curLevel = __instance._player.Level.Value;
                if (!HellModeLevel75FreebuffPlugin.IsHellModeActive)
                {
                    if (curLevel >= 25)
                    {
                        return false; // Prevent upgrading attributes past 25 in normal modes
                    }
                }
                else
                {
                    int maxCap = HellModeLevel75FreebuffPlugin.MaxLevelCap != null ? HellModeLevel75FreebuffPlugin.MaxLevelCap.Value : 75;
                    if (curLevel >= maxCap)
                    {
                        return false; // Prevent upgrading attributes past configured cap in Hell Mode
                    }
                }
            }
            return true;
        }

        /// <summary>Per-frame pump. Steady-state cost: one null check + two float compares (no allocations, no GPU work).</summary>
        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.Update))]
        [HarmonyPostfix]
        public static void PlayerStats_Update_Postfix(PlayerStats __instance)
        {
            try { LevelSyncService.Tick(__instance != null ? __instance._player : null); } catch { }
        }

        /// <summary>Event-driven trigger: queue a sync right after the game grants XP (throttled inside the service).</summary>
        [HarmonyPatch(typeof(XP), nameof(XP.GrantXPToPlayer))]
        [HarmonyPostfix]
        public static void GrantXPToPlayer_Postfix(Player player)
        {
            try { LevelSyncService.RequestSync(player); } catch { }
        }

        /// <summary>
        /// Emergency safety-net (Hell Mode worlds ONLY). The LevelSync ledger balancing keeps the
        /// XP invariant honest (AllTimeXP == SumLevels + AccumXP), but if any future/unknown check
        /// trips on a modded world, report valid instead of kicking the player mid-session.
        /// Non-Hell worlds keep the vanilla anti-cheat untouched.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.ValidateXPState))]
        [HarmonyPrefix]
        public static bool ValidateXPState_Prefix(ref bool __result)
        {
            if (!HellModeLevel75FreebuffPlugin.IsHellModeActive) return true;
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ValidateXPInvariants))]
        [HarmonyPrefix]
        public static bool ValidateXPInvariants_Prefix(ref bool __result)
        {
            if (!HellModeLevel75FreebuffPlugin.IsHellModeActive) return true;
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(XP), nameof(XP.CalculateXPGained))]
        [HarmonyPostfix]
        public static void CalculateXPGained_Postfix(ref int __result)
        {
            if (HellModeLevel75FreebuffPlugin.IsHellModeActive)
            {
                float mult = HellModeLevel75FreebuffPlugin.ExpMultiplier != null ? HellModeLevel75FreebuffPlugin.ExpMultiplier.Value : 3.0f;
                if (mult > 1.0f)
                {
                    __result = (int)Math.Round(__result * mult);
                }
            }
        }
    }
}
