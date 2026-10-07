using System;
using System.IO;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    public class HellModeModule : ModModuleBase
    {
        public static HellModeModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Hell Mode";
        public override string Description => "High-difficulty combat mode with custom monster level scaling, HP, and damage multipliers";

        public ConfigEntry<bool> ForceHellMode;
        public ConfigEntry<float> MonsterHealthMult;
        public ConfigEntry<float> MonsterDamageMult;
        public ConfigEntry<float> CombatPaceMult;
        public ConfigEntry<float> MonsterSpeedMult;
        public ConfigEntry<float> EmpowermentChanceMult;
        public ConfigEntry<int> MonsterBonusLevelOverPlayer;

        public static bool CurrentWorldIsHellMode = false;
        public static string CurrentWorldName = string.Empty;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.HellMode";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Hell Mode feature (Default: false, Vanilla: false)");

            ForceHellMode = config.Bind(sec, "ForceHellMode", false,
                "Force Hell Mode active on all worlds (Default: false, Vanilla: false)");

            MonsterHealthMult = config.Bind(sec, "MonsterHealthMultiplier", 5.0f,
                "Monster HP multiplier in Hell Mode (Default: 5.0, Vanilla: 1.0)");

            MonsterDamageMult = config.Bind(sec, "MonsterDamageMultiplier", 3.0f,
                "Monster attack damage multiplier (Default: 3.0, Vanilla: 1.0)");

            CombatPaceMult = config.Bind(sec, "CombatPaceMultiplier", 1.5f,
                "Monster combat aggressiveness and attack pace multiplier (Default: 1.5, Vanilla: 1.0)");

            MonsterSpeedMult = config.Bind(sec, "MonsterSpeedMultiplier", 1.3f,
                "Monster movement and chase speed multiplier (Default: 1.3, Vanilla: 1.0)");

            EmpowermentChanceMult = config.Bind(sec, "EmpowermentChanceMultiplier", 2.0f,
                "Elite / empowered monster spawn chance multiplier (Default: 2.0, Vanilla: 1.0)");

            MonsterBonusLevelOverPlayer = config.Bind(sec, "MonsterBonusLevelOverPlayer", 10,
                "Monster bonus level above player level (Default: 10, Vanilla: 0)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        private static string _lastCheckedWorld = null;
        private static bool _lastCheckedIsHell = false;

        public static void ResetWorldCache()
        {
            CurrentWorldIsHellMode = false;
            CurrentWorldName = string.Empty;
            _lastCheckedWorld = null;
            _lastCheckedIsHell = false;
        }

        public static bool IsHellActive
        {
            get
            {
                if (Instance == null || !Instance.IsEnabled) return false;
                if (Instance.ForceHellMode.Value) return true;
                if (CurrentWorldIsHellMode) return true;

                try
                {
                    if (ServerPersistentData.Singleton != null && !string.IsNullOrEmpty(ServerPersistentData.Singleton.Name))
                    {
                        string wName = ServerPersistentData.Singleton.Name;
                        if (string.Equals(_lastCheckedWorld, wName, StringComparison.OrdinalIgnoreCase))
                        {
                            return _lastCheckedIsHell;
                        }

                        _lastCheckedWorld = wName;
                        _lastCheckedIsHell = IsWorldRegisteredAsHell(wName);
                        if (_lastCheckedIsHell)
                        {
                            CurrentWorldIsHellMode = true;
                            CurrentWorldName = wName;
                            return true;
                        }
                        return false;
                    }
                }
                catch { }

                return false;
            }
        }

        public static int GetPlayerLevel()
        {
            try
            {
                if (DataStorage.Singleton != null && DataStorage.Singleton.Player != null && DataStorage.Singleton.Player.Level != null)
                {
                    return DataStorage.Singleton.Player.Level.Value;
                }
            }
            catch { }
            return 1;
        }

        public static bool IsWorldRegisteredAsHell(string worldName)
        {
            try
            {
                string dir = GetHellWorldsDirectory();
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
                string clean = worldName.Trim();
                if (clean.EndsWith(".jrwf", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(0, clean.Length - 5);
                string file = Path.Combine(dir, $"{clean}.hell");
                return File.Exists(file);
            }
            catch { return false; }
        }

        public static string GetHellWorldsDirectory()
        {
            try
            {
                string localLow = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";
                string worldsDir = Path.Combine(localLow, "Mudtek", "Dimraeth", "Worlds");
                if (!Directory.Exists(worldsDir)) Directory.CreateDirectory(worldsDir);
                return worldsDir;
            }
            catch { return string.Empty; }
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Force Hell Mode All Worlds", ForceHellMode, "Vanilla: OFF", labelStyle, btnStyle);
            // [2026-10-07 12:00] Changed: all Hell Mode float multiplier caps raised from their old
            // per-stat ceilings (HP 20.0, Damage 10.0, Pace 5.0, Speed 3.0, Elite 10.0) to a uniform
            // 999.0 max so every multiplier can reach the requested 999x ceiling. The old capped
            // DrawFloatSpinner lines are preserved here per repo rule (obsolete because they limited
            // the UI spinner to values below 999):
            // curY += DrawFloatSpinner(x, curY, width, "Monster HP Multiplier", MonsterHealthMult, 0.5f, 1.0f, 20.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            // curY += DrawFloatSpinner(x, curY, width, "Monster Damage Mult", MonsterDamageMult, 0.5f, 1.0f, 10.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            // curY += DrawFloatSpinner(x, curY, width, "Combat Pace Mult", CombatPaceMult, 0.2f, 1.0f, 5.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            // curY += DrawFloatSpinner(x, curY, width, "Monster Move Speed", MonsterSpeedMult, 0.1f, 1.0f, 3.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            // curY += DrawFloatSpinner(x, curY, width, "Elite Spawn Chance", EmpowermentChanceMult, 0.5f, 1.0f, 10.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Monster HP Multiplier", MonsterHealthMult, 0.5f, 1.0f, 999.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Monster Damage Mult", MonsterDamageMult, 0.5f, 1.0f, 999.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Combat Pace Mult", CombatPaceMult, 0.2f, 1.0f, 999.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Monster Move Speed", MonsterSpeedMult, 0.1f, 1.0f, 999.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Elite Spawn Chance", EmpowermentChanceMult, 0.5f, 1.0f, 999.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawIntSpinner(x, curY, width, "Monster Bonus Level", MonsterBonusLevelOverPlayer, 1, 0, 50, "Vanilla: +0", labelStyle, btnStyle);
            return curY - y;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterHealthMultiplier))]
            [HarmonyPostfix]
            public static void GetMonsterHealthMultiplier_Postfix(ref float __result)
            {
                if (IsHellActive)
                {
                    __result = Instance.MonsterHealthMult.Value;
                }
            }

            [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterDamageDealtMultiplier))]
            [HarmonyPostfix]
            public static void GetMonsterDamageDealtMultiplier_Postfix(ref float __result)
            {
                if (IsHellActive)
                {
                    __result = Instance.MonsterDamageMult.Value;
                }
            }

            [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetCombatPaceMultiplier))]
            [HarmonyPostfix]
            public static void GetCombatPaceMultiplier_Postfix(ref float __result)
            {
                if (IsHellActive)
                {
                    __result = Instance.CombatPaceMult.Value;
                }
            }

            [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterSpeedMultiplier))]
            [HarmonyPostfix]
            public static void GetMonsterSpeedMultiplier_Postfix(ref float __result)
            {
                if (IsHellActive)
                {
                    __result = Instance.MonsterSpeedMult.Value;
                }
            }

            [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetEmpowermentChanceMultiplier))]
            [HarmonyPostfix]
            public static void GetEmpowermentChanceMultiplier_Postfix(ref float __result)
            {
                if (IsHellActive)
                {
                    __result = Instance.EmpowermentChanceMult.Value;
                }
            }

            [HarmonyPatch(typeof(DifficultyManager), nameof(DifficultyManager.GetMonsterBonusLevel))]
            [HarmonyPostfix]
            public static void GetMonsterBonusLevel_Postfix(MonsterConfiguration config, ref int __result)
            {
                if (IsHellActive)
                {
                    int pLvl = GetPlayerLevel();
                    int bonusOffset = Instance.MonsterBonusLevelOverPlayer.Value;
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
                    if (!IsHellActive || __instance == null || __instance._monster == null) return;
                    var monster = __instance._monster;
                    if (monster.IsSpawned && monster.IsServer && monster.Level != null)
                    {
                        int pLvl = GetPlayerLevel();
                        int bonusOffset = Instance.MonsterBonusLevelOverPlayer.Value;
                        monster.Level.Value = Math.Max(1, pLvl + bonusOffset);
                    }
                }
                catch { }
            }
        }
    }
}
