using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Stats
{
    public class UpgradeBonusStatIsNotRandomModule : ModModuleBase
    {
        public static UpgradeBonusStatIsNotRandomModule Instance { get; private set; }

        public override string Category => "Stats";
        public override string Name => "Upgrade Bonus Stat Is Not Random";
        public override string Description => "Guarantee & plan bonus stat rolls for Level +3, +6, +9, and +12 at the Forge";

        public ConfigEntry<string> TargetStatLevel3;
        public ConfigEntry<string> TargetStatLevel6;
        public ConfigEntry<string> TargetStatLevel9;
        public ConfigEntry<string> TargetStatLevel12;
        public ConfigEntry<string> InstantNextRollOverride;
        public ConfigEntry<bool> FallbackToVanillaIfDuplicate;

        // Alphabetically sorted A-Z catalog
        public static readonly string[] SortedStatNames;
        public static readonly char[] AvailableLetters;
        private static readonly Dictionary<char, string[]> StatsByLetter;
        private static readonly Dictionary<string, Runes.Stat> NameToStat;

        // Dropdown state
        private static int _activeDropdownSlot = -1; // 3, 6, 9, 12, 99 (instant), or -1 (closed)
        private static char _selectedLetter = 'A';

        static UpgradeBonusStatIsNotRandomModule()
        {
            var values = (Runes.Stat[])Enum.GetValues(typeof(Runes.Stat));
            var rawNames = new List<string>(values.Length);
            NameToStat = new Dictionary<string, Runes.Stat>(values.Length, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < values.Length; i++)
            {
                var val = values[i];
                string name = val.ToString();
                rawNames.Add(name);
                NameToStat[name] = val;
            }

            // Sort alphabetically from A to Z
            rawNames.Sort(StringComparer.OrdinalIgnoreCase);

            var fullList = new List<string>(rawNames.Count + 1) { "None" };
            fullList.AddRange(rawNames);
            SortedStatNames = fullList.ToArray();

            // Group by starting letter
            var letterDict = new Dictionary<char, List<string>>();
            for (int i = 0; i < rawNames.Count; i++)
            {
                string s = rawNames[i];
                if (string.IsNullOrEmpty(s)) continue;
                char c = char.ToUpperInvariant(s[0]);
                if (!letterDict.TryGetValue(c, out var list))
                {
                    list = new List<string>();
                    letterDict[c] = list;
                }
                list.Add(s);
            }

            var letterKeys = new List<char>(letterDict.Keys);
            letterKeys.Sort();
            AvailableLetters = letterKeys.ToArray();

            StatsByLetter = new Dictionary<char, string[]>(letterKeys.Count);
            foreach (var kvp in letterDict)
            {
                StatsByLetter[kvp.Key] = kvp.Value.ToArray();
            }
        }

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Stats.UpgradeBonusStatIsNotRandom";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable or disable Upgrade Bonus Stat Is Not Random at the Forge (Default: true)");

            FallbackToVanillaIfDuplicate = config.Bind(sec, "FallbackToVanillaIfDuplicate", true,
                "If the chosen stat is already on the equipment piece, fall back cleanly to vanilla RNG instead of breaking (Default: true)");

            TargetStatLevel3 = config.Bind(sec, "TargetStatLevel3", "None",
                "Bonus secondary stat to guarantee at Level +3 upgrade (None = Vanilla RNG)");

            TargetStatLevel6 = config.Bind(sec, "TargetStatLevel6", "None",
                "Bonus secondary stat to guarantee at Level +6 upgrade (None = Vanilla RNG)");

            TargetStatLevel9 = config.Bind(sec, "TargetStatLevel9", "None",
                "Bonus secondary stat to guarantee at Level +9 upgrade (None = Vanilla RNG)");

            TargetStatLevel12 = config.Bind(sec, "TargetStatLevel12", "None",
                "Bonus secondary stat to guarantee at Level +12 upgrade (None = Vanilla RNG)");

            InstantNextRollOverride = config.Bind(sec, "InstantNextRollOverride", "None",
                "Direct fire: forces this stat on the very next upgrade milestone you trigger, then resets to None");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public static bool TryGetStat(string name, out Runes.Stat stat)
        {
            if (string.IsNullOrEmpty(name) || name.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                stat = (Runes.Stat)(-1);
                return false;
            }
            return NameToStat.TryGetValue(name, out stat);
        }

        public static bool IsRuneAlreadyHasStat(Rune rune, Runes.Stat target)
        {
            if (rune.PrimaryStat == target) return true;
            int count = rune.SecondaryStatsCount;
            for (int i = 0; i < count; i++)
            {
                if (rune.SecondaryStats[i] == target) return true;
            }
            return false;
        }

        private ConfigEntry<string> GetConfigForSlot(int slot)
        {
            return slot switch
            {
                3 => TargetStatLevel3,
                6 => TargetStatLevel6,
                9 => TargetStatLevel9,
                12 => TargetStatLevel12,
                99 => InstantNextRollOverride,
                _ => null
            };
        }

        private void StepStat(ConfigEntry<string> entry, int delta)
        {
            if (entry == null) return;
            string cur = entry.Value;
            int idx = Array.IndexOf(SortedStatNames, cur);
            if (idx < 0) idx = 0;
            int newIdx = (idx + delta + SortedStatNames.Length) % SortedStatNames.Length;
            entry.Value = SortedStatNames[newIdx];
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;

            curY += DrawToggle(x, curY, width, "Unrandomizer Active", Enabled, "Vanilla: OFF", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Duplicate Safe Fallback", FallbackToVanillaIfDuplicate, "Vanilla: ON", labelStyle, btnStyle);

            curY += DrawMilestoneDropdownRow(x, curY, width, "Level +3 Bonus", TargetStatLevel3, 3, labelStyle, btnStyle);
            curY += DrawMilestoneDropdownRow(x, curY, width, "Level +6 Bonus", TargetStatLevel6, 6, labelStyle, btnStyle);
            curY += DrawMilestoneDropdownRow(x, curY, width, "Level +9 Bonus", TargetStatLevel9, 9, labelStyle, btnStyle);
            curY += DrawMilestoneDropdownRow(x, curY, width, "Level +12 Bonus", TargetStatLevel12, 12, labelStyle, btnStyle);
            curY += DrawMilestoneDropdownRow(x, curY, width, "Next Roll Override", InstantNextRollOverride, 99, labelStyle, btnStyle);

            // Quick reset button
            if (GUI.Button(new Rect(x, curY, 180f, 24f), "↺ Reset All to None (Vanilla)", btnStyle))
            {
                TargetStatLevel3.Value = "None";
                TargetStatLevel6.Value = "None";
                TargetStatLevel9.Value = "None";
                TargetStatLevel12.Value = "None";
                InstantNextRollOverride.Value = "None";
                _activeDropdownSlot = -1;
            }
            curY += 28f;

            // Render active A-Z dropdown if open
            if (_activeDropdownSlot != -1)
            {
                curY += DrawAZDropdownPanel(x, curY, width, labelStyle, btnStyle);
            }

            return curY - y;
        }

        private float DrawMilestoneDropdownRow(float x, float y, float width, string label, ConfigEntry<string> entry, int slot, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            string val = entry?.Value ?? "None";
            string color = val == "None" ? "#888888" : "#55FFFF";

            // Label & current value
            GUI.Label(new Rect(x, y, width - 210f, 24f), $"{label}: <color={color}><b>{val}</b></color>", labelStyle);

            // [ < ] Prev A-Z
            if (GUI.Button(new Rect(x + width - 200f, y, 28f, 24f), "<", btnStyle))
            {
                StepStat(entry, -1);
            }

            // [ > ] Next A-Z
            if (GUI.Button(new Rect(x + width - 170f, y, 28f, 24f), ">", btnStyle))
            {
                StepStat(entry, 1);
            }

            // [ Dropdown A-Z ▼ ]
            bool isOpen = (_activeDropdownSlot == slot);
            string btnText = isOpen ? "▲ Close" : "▼ A - Z";
            if (GUI.Button(new Rect(x + width - 138f, y, 138f, 24f), btnText, btnStyle))
            {
                if (isOpen)
                {
                    _activeDropdownSlot = -1;
                }
                else
                {
                    _activeDropdownSlot = slot;
                    // Auto-select starting letter of current value
                    if (!string.IsNullOrEmpty(val) && val != "None")
                    {
                        _selectedLetter = char.ToUpperInvariant(val[0]);
                    }
                    else
                    {
                        _selectedLetter = 'A';
                    }
                }
            }

            return 28f;
        }

        private float DrawAZDropdownPanel(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float startY = y;
            var targetEntry = GetConfigForSlot(_activeDropdownSlot);
            string slotTitle = _activeDropdownSlot == 99 ? "Next Roll Override" : $"Level +{_activeDropdownSlot} Bonus";

            // Header bar
            GUI.Label(new Rect(x, curY(y), width, 22f), $"<b>Choose Stat for [{slotTitle}] (A - Z):</b>", labelStyle);
            y += 24f;

            // Letter Selection Bar (A - Z buttons)
            float letterBtnW = 28f;
            float letterBtnH = 24f;
            float curX = x;

            // [None] option button first
            if (GUI.Button(new Rect(curX, y, 52f, letterBtnH), "None", btnStyle))
            {
                if (targetEntry != null) targetEntry.Value = "None";
                _activeDropdownSlot = -1;
                return (y + letterBtnH + 6f) - startY;
            }
            curX += 56f;

            // Alphabet buttons
            for (int i = 0; i < AvailableLetters.Length; i++)
            {
                char c = AvailableLetters[i];
                bool isSelected = (_selectedLetter == c);

                if (curX + letterBtnW > x + width - 60f)
                {
                    // Wrap to next line if needed
                    curX = x;
                    y += letterBtnH + 2f;
                }

                string letterLabel = isSelected ? $"<b><color=#FFDD44>{c}</color></b>" : c.ToString();
                if (GUI.Button(new Rect(curX, y, letterBtnW, letterBtnH), letterLabel, btnStyle))
                {
                    _selectedLetter = c;
                }
                curX += letterBtnW + 2f;
            }

            // Close button
            if (GUI.Button(new Rect(x + width - 55f, y, 55f, letterBtnH), "✕ Close", btnStyle))
            {
                _activeDropdownSlot = -1;
                return (y + letterBtnH + 6f) - startY;
            }
            y += letterBtnH + 8f;

            // Stats under selected letter
            if (StatsByLetter.TryGetValue(_selectedLetter, out var stats))
            {
                GUI.Label(new Rect(x, y, width, 20f), $"<color=#FFDD44>Stats starting with '{_selectedLetter}' ({stats.Length}):</color>", labelStyle);
                y += 22f;

                // 2-column button grid
                float colW = (width - 10f) * 0.5f;
                float itemH = 24f;

                for (int i = 0; i < stats.Length; i++)
                {
                    string statName = stats[i];
                    int col = i % 2;
                    float itemX = x + col * (colW + 10f);
                    float itemY = y + (i / 2) * (itemH + 2f);

                    bool isCurrent = targetEntry != null && targetEntry.Value.Equals(statName, StringComparison.OrdinalIgnoreCase);
                    string display = isCurrent ? $"<b><color=#55FF55>✓ {statName}</color></b>" : statName;

                    if (GUI.Button(new Rect(itemX, itemY, colW, itemH), display, btnStyle))
                    {
                        if (targetEntry != null) targetEntry.Value = statName;
                        _activeDropdownSlot = -1; // close dropdown upon selection!
                    }
                }

                int rows = (stats.Length + 1) / 2;
                y += rows * (itemH + 2f) + 6f;
            }

            return y - startY;

            float curY(float val) => val;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(Runes), "GetRandomSecondaryStat", new Type[] { typeof(Rune), typeof(int) })]
            [HarmonyPrefix]
            public static bool Prefix_GetRandomSecondaryStat(Runes __instance, Rune rune, int seed, ref Runes.Stat __result)
            {
                if (Instance == null || !Instance.IsEnabled)
                    return true;

                try
                {
                    int level = rune.Level;
                    string targetName = null;

                    // 1. Direct Next Roll Override check
                    if (Instance.InstantNextRollOverride != null &&
                        !string.IsNullOrEmpty(Instance.InstantNextRollOverride.Value) &&
                        !Instance.InstantNextRollOverride.Value.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        targetName = Instance.InstantNextRollOverride.Value;
                        Instance.InstantNextRollOverride.Value = "None"; // Single-use consumption
                    }
                    else
                    {
                        // 2. Milestone plan
                        targetName = level switch
                        {
                            3 => Instance.TargetStatLevel3?.Value,
                            6 => Instance.TargetStatLevel6?.Value,
                            9 => Instance.TargetStatLevel9?.Value,
                            12 => Instance.TargetStatLevel12?.Value,
                            _ => null
                        };
                    }

                    if (string.IsNullOrEmpty(targetName) || targetName.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (TryGetStat(targetName, out Runes.Stat desiredStat))
                    {
                        // Duplicate affix safety check
                        if (IsRuneAlreadyHasStat(rune, desiredStat))
                        {
                            if (Instance.FallbackToVanillaIfDuplicate != null && Instance.FallbackToVanillaIfDuplicate.Value)
                            {
                                DimraethModPackPlugin.Log?.LogWarning(
                                    $"[ModPack.UpgradeBonusStatIsNotRandom] Rune at level {level} already possesses '{desiredStat}'. Falling back to vanilla RNG to preserve affix integrity.");
                                return true;
                            }
                        }

                        __result = desiredStat;
                        DimraethModPackPlugin.Log?.LogInfo(
                            $"[ModPack.UpgradeBonusStatIsNotRandom] Intercepted Level {level} bonus stat -> Guaranteed '{desiredStat}'!");
                        return false;
                    }
                }
                catch { }

                return true;
            }
        }
    }
}
