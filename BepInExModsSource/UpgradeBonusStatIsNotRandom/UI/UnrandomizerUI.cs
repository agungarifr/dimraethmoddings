using System;
using System.Collections.Generic;
using UnityEngine;
using UpgradeBonusStatIsNotRandom.Core;
using UpgradeBonusStatIsNotRandom.Patches;

namespace UpgradeBonusStatIsNotRandom.UI
{
    public static class UnrandomizerUI
    {
        private static bool _initialized = false;
        private static GUIStyle _sWindow;
        private static GUIStyle _sTitle;
        private static GUIStyle _sSub;
        private static GUIStyle _sHeader;
        private static GUIStyle _sLabel;
        private static GUIStyle _sValue;
        private static GUIStyle _sBtnPrimary;
        private static GUIStyle _sBtnAction;
        private static GUIStyle _sBtnDanger;
        private static GUIStyle _sTextField;
        private static GUIStyle _sScrollBox;

        private static Texture2D _texBg;
        private static Texture2D _texBtn;
        private static Texture2D _texBtnHover;
        private static Texture2D _texHeader;

        // Selection modal state
        private static bool _pickerOpen = false;
        private static int _pickingForSlot = -1; // 3, 6, 9, 12, or 99 (instant)
        private static string _searchQuery = "";
        private static string _lastSearchQuery = "";
        private static readonly List<string> _filteredStats = new(128);
        private static Vector2 _pickerScroll = Vector2.zero;

        // Notification / Status feedback
        private static string _statusMsg = "";
        private static float _statusTimer = 0f;

        private static void InitStyles()
        {
            if (_initialized) return;

            _texBg = MakeSolidTex(1, 1, new Color(0.10f, 0.11f, 0.14f, 0.96f));
            _texHeader = MakeSolidTex(1, 1, new Color(0.18f, 0.20f, 0.26f, 1f));
            _texBtn = MakeSolidTex(1, 1, new Color(0.22f, 0.25f, 0.32f, 1f));
            _texBtnHover = MakeSolidTex(1, 1, new Color(0.28f, 0.33f, 0.44f, 1f));

            _sWindow = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _texBg }
            };

            _sTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.78f, 0.35f) }
            };

            _sSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.70f, 0.72f, 0.78f) }
            };

            _sHeader = new GUIStyle(GUI.skin.box)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { background = _texHeader, textColor = Color.white }
            };

            _sLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _sValue = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.4f, 0.85f, 1.0f) }
            };

            _sBtnPrimary = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _texBtn, textColor = Color.white },
                hover = { background = _texBtnHover, textColor = Color.white }
            };

            _sBtnAction = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                normal = { background = _texBtn, textColor = new Color(0.85f, 0.9f, 1f) },
                hover = { background = _texBtnHover, textColor = Color.white }
            };

            _sBtnDanger = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _sTextField = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            _sScrollBox = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _texHeader }
            };

            RefreshFilter();
            _initialized = true;
        }

        private static Texture2D MakeSolidTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private static void RefreshFilter()
        {
            _filteredStats.Clear();
            string q = _searchQuery.Trim().ToLowerInvariant();
            for (int i = 0; i < StatCatalog.StatNames.Length; i++)
            {
                string s = StatCatalog.StatNames[i];
                if (string.IsNullOrEmpty(q) || s.ToLowerInvariant().Contains(q))
                {
                    _filteredStats.Add(s);
                }
            }
            _lastSearchQuery = _searchQuery;
        }

        public static void Draw()
        {
            InitStyles();

            int savedDepth = GUI.depth;
            GUI.depth = -2500;

            float w = 580f;
            float h = 600f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _sWindow);

            // Title & Subtitle
            GUI.Label(new Rect(x + 10f, y + 12f, w - 20f, 24f), "UPGRADE BONUS STAT IS NOT RANDOM", _sTitle);
            GUI.Label(new Rect(x + 10f, y + 36f, w - 20f, 18f), "Plan guaranteed milestone bonuses | Press F7 to Toggle", _sSub);

            // Master Toggle
            bool modActive = UpgradeBonusStatIsNotRandomPlugin.ModEnabled.Value;
            bool newModActive = GUI.Toggle(new Rect(x + 20f, y + 60f, 160f, 22f), modActive, " Mod Active");
            if (newModActive != modActive)
            {
                UpgradeBonusStatIsNotRandomPlugin.ModEnabled.Value = newModActive;
            }

            // Fallback toggle
            bool fallback = UpgradeBonusStatIsNotRandomPlugin.FallbackToVanillaIfDuplicate.Value;
            bool newFallback = GUI.Toggle(new Rect(x + 220f, y + 60f, 320f, 22f), fallback, " Fallback to vanilla if stat already exists");
            if (newFallback != fallback)
            {
                UpgradeBonusStatIsNotRandomPlugin.FallbackToVanillaIfDuplicate.Value = newFallback;
            }

            // Milestone Plan Section
            GUI.Box(new Rect(x + 20f, y + 90f, w - 40f, 26f), "  PLANNED MILESTONE BONUS STATS (AT FORGE)", _sHeader);

            float rowY = y + 125f;
            DrawMilestoneRow(x + 20f, ref rowY, 3, "Level +3 Bonus:", UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel3.Value);
            DrawMilestoneRow(x + 20f, ref rowY, 6, "Level +6 Bonus:", UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel6.Value);
            DrawMilestoneRow(x + 20f, ref rowY, 9, "Level +9 Bonus:", UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel9.Value);
            DrawMilestoneRow(x + 20f, ref rowY, 12, "Level +12 Bonus:", UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel12.Value);

            // Direct / Instant Fire Override Section
            rowY += 10f;
            GUI.Box(new Rect(x + 20f, rowY, w - 40f, 26f), "  DIRECT FIRE / NEXT ROLL OVERRIDE", _sHeader);
            rowY += 35f;

            GUI.Label(new Rect(x + 20f, rowY, 170f, 24f), "Next Upgrade Roll:", _sLabel);
            GUI.Label(new Rect(x + 190f, rowY, 240f, 24f), UpgradeRunePatch.InstantNextRollOverride, _sValue);
            if (GUI.Button(new Rect(x + 440f, rowY, 100f, 24f), "Select...", _sBtnAction))
            {
                _pickingForSlot = 99;
                _pickerOpen = true;
                _searchQuery = "";
                RefreshFilter();
            }
            rowY += 28f;
            GUI.Label(new Rect(x + 20f, rowY, w - 40f, 18f), "(Directly applies chosen stat to the very next upgrade milestone, then resets)", _sSub);

            // Action Buttons at Bottom
            float btnY = y + h - 85f;
            if (GUI.Button(new Rect(x + 20f, btnY, 160f, 32f), "💾 Save Plan to Disk", _sBtnPrimary))
            {
                UpgradeBonusStatIsNotRandomPlugin.Instance?.Config?.Save();
                _statusMsg = "<color=#55FF55>Plan successfully saved to BepInEx config!</color>";
                _statusTimer = Time.unscaledTime + 3f;
            }

            if (GUI.Button(new Rect(x + 190f, btnY, 170f, 32f), "↺ Reset Plan to Vanilla", _sBtnDanger))
            {
                UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel3.Value = "None";
                UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel6.Value = "None";
                UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel9.Value = "None";
                UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel12.Value = "None";
                UpgradeRunePatch.InstantNextRollOverride = "None";
                UpgradeBonusStatIsNotRandomPlugin.Instance?.Config?.Save();
                _statusMsg = "<color=#FFFF55>Plan reset to vanilla RNG!</color>";
                _statusTimer = Time.unscaledTime + 3f;
            }

            if (GUI.Button(new Rect(x + 370f, btnY, 170f, 32f), "✕ Close (F7)", _sBtnPrimary))
            {
                UnrandomizerBehaviour.IsOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Status message
            if (Time.unscaledTime < _statusTimer && !string.IsNullOrEmpty(_statusMsg))
            {
                GUI.Label(new Rect(x + 20f, btnY + 40f, w - 40f, 22f), _statusMsg, _sSub);
            }

            // Stat Picker Popup
            if (_pickerOpen)
            {
                DrawStatPicker(x + 40f, y + 60f, w - 80f, h - 120f);
            }

            GUI.depth = savedDepth;
        }

        private static void DrawMilestoneRow(float x, ref float y, int slot, string label, string currentValue)
        {
            GUI.Label(new Rect(x, y, 150f, 24f), label, _sLabel);
            GUI.Label(new Rect(x + 160f, y, 250f, 24f), currentValue, _sValue);

            if (GUI.Button(new Rect(x + 420f, y, 100f, 24f), "Change...", _sBtnAction))
            {
                _pickingForSlot = slot;
                _pickerOpen = true;
                _searchQuery = "";
                RefreshFilter();
            }
            y += 32f;
        }

        private static void DrawStatPicker(float px, float py, float pw, float ph)
        {
            GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, _sWindow);
            GUI.Box(new Rect(px, py, pw, 28f), $"  Select Stat for {(_pickingForSlot == 99 ? "Next Roll" : $"Level +{_pickingForSlot}")}", _sHeader);

            // Search box
            GUI.Label(new Rect(px + 10f, py + 36f, 60f, 22f), "Search:", _sLabel);
            _searchQuery = GUI.TextField(new Rect(px + 75f, py + 36f, pw - 170f, 22f), _searchQuery, _sTextField);
            if (!_searchQuery.Equals(_lastSearchQuery, StringComparison.Ordinal))
            {
                RefreshFilter();
            }

            if (GUI.Button(new Rect(px + pw - 85f, py + 36f, 75f, 22f), "Clear", _sBtnAction))
            {
                _searchQuery = "";
                RefreshFilter();
            }

            // Scrollable list
            float listY = py + 66f;
            float listH = ph - 110f;
            GUI.Box(new Rect(px + 10f, listY, pw - 20f, listH), GUIContent.none, _sScrollBox);

            float contentHeight = _filteredStats.Count * 26f;
            _pickerScroll = GUI.BeginScrollView(
                new Rect(px + 12f, listY + 2f, pw - 24f, listH - 4f),
                _pickerScroll,
                new Rect(0, 0, pw - 45f, contentHeight));

            for (int i = 0; i < _filteredStats.Count; i++)
            {
                string sName = _filteredStats[i];
                float itemY = i * 26f;

                if (GUI.Button(new Rect(0, itemY, pw - 50f, 24f), sName, _sBtnAction))
                {
                    ApplyPickedStat(sName);
                    _pickerOpen = false;
                }
            }

            GUI.EndScrollView();

            // Cancel button
            if (GUI.Button(new Rect(px + (pw - 120f) * 0.5f, py + ph - 36f, 120f, 26f), "Cancel", _sBtnDanger))
            {
                _pickerOpen = false;
            }
        }

        private static void ApplyPickedStat(string statName)
        {
            string clean = statName.StartsWith("None", StringComparison.OrdinalIgnoreCase) ? "None" : statName;
            switch (_pickingForSlot)
            {
                case 3:
                    UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel3.Value = clean;
                    break;
                case 6:
                    UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel6.Value = clean;
                    break;
                case 9:
                    UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel9.Value = clean;
                    break;
                case 12:
                    UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel12.Value = clean;
                    break;
                case 99:
                    UpgradeRunePatch.InstantNextRollOverride = clean;
                    break;
            }
            _statusMsg = $"<color=#55FF55>Selected '{clean}' for {(_pickingForSlot == 99 ? "Next Roll" : $"Level +{_pickingForSlot}")}!</color>";
            _statusTimer = Time.unscaledTime + 2.5f;
        }
    }
}
