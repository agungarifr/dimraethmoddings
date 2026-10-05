using System;
using System.Collections.Generic;
using System.Globalization;
using EquippedStatModifier.Core;
using UnityEngine;

namespace EquippedStatModifier.UI
{
    public class EquippedStatModifierUI
    {
        public static bool IsOpen = false;

        private static int _selectedSlot = 2; // Default to Belt (Sash)
        private static int _lastLoadedSlot = -1;
        private static string _lastLoadedUuid = "";

        // Working copy for the currently selected item
        private static List<Runes.Stat> _editStats = new();
        private static List<string> _editValueStrings = new();
        private static List<int> _editUpgrades = new();

        // Stat picker modal state
        private static int _pickerTargetIndex = -1;
        private static string _searchQuery = "";
        private static int _pickerPage = 0;
        /* [2026-09-26 00:54:00] Obsolete: private static Vector2 _pickerScroll = Vector2.zero; */

        // Main UI scroll
        private static Vector2 _leftScroll = Vector2.zero;
        private static Vector2 _rightScroll = Vector2.zero;

        // Feedback message
        private static string _statusMsg = "";
        private static float _statusTimer = 0f;

        // Styles
        private static bool _stylesReady = false;
        private static readonly List<Texture2D> _textures = new();
        private static GUIStyle _sPanelBg;
        private static GUIStyle _sTitle;
        private static GUIStyle _sSub;
        private static GUIStyle _sSlotBtn;
        private static GUIStyle _sSlotBtnActive;
        private static GUIStyle _sSlotBtnEmpty;
        private static GUIStyle _sLabel;
        private static GUIStyle _sLabelBold;
        private static GUIStyle _sBtnAction;
        private static GUIStyle _sBtnGreen;
        private static GUIStyle _sBtnBlue;
        private static GUIStyle _sBtnRed;
        private static GUIStyle _sCloseBtn;
        private static GUIStyle _sDivider;
        private static GUIStyle _sTextField;
        private static GUIStyle _sPickerBg;

        public static void Toggle()
        {
            IsOpen = !IsOpen;
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _lastLoadedSlot = -1;
                _pickerTargetIndex = -1;

                RuneMemory.EnsureOffsets();
                DumpDiagnostics();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private static void DumpDiagnostics()
        {
            try
            {
                var player = RuneMemory.GetLocalPlayer();
                if (player == null)
                {
                    RuneMemory.LogWarn("[EquippedStatModifier] Local player not found.");
                    return;
                }

                string charName = player.Name != null ? player.Name.Value.ToString() : "Unknown";
                int count = player.EquipmentWorn?.Count ?? -1;
                RuneMemory.LogInfo($"[EquippedStatModifier] Player: '{charName}', EquipmentWorn count: {count}");

                if (player.EquipmentWorn != null)
                {
                    int foundFirstEquipped = -1;
                    for (int i = 0; i < player.EquipmentWorn.Count; i++)
                    {
                        var rune = player.EquipmentWorn[i];
                        bool hasItem = RuneMemory.IsRuneEquipped(rune);

                        if (hasItem)
                        {
                            var snap = RuneMemory.Read(rune);
                            string secStr = string.Join(", ", snap.SecondaryStats);
                            RuneMemory.LogInfo($"  Slot {i} [EQUIPPED]: UUID={snap.Uuid} SlotType={snap.SlotType} Level={snap.Level} Primary={snap.PrimaryStat} Secondaries=[{secStr}]");
                            if (foundFirstEquipped == -1) foundFirstEquipped = i;
                        }
                        else
                        {
                            RuneMemory.LogInfo($"  Slot {i} [Empty]");
                        }
                    }

                    // Auto-select first slot with an actual equipped item if current is empty
                    if (foundFirstEquipped != -1)
                    {
                        var curRune = player.EquipmentWorn[_selectedSlot];
                        if (!RuneMemory.IsRuneEquipped(curRune))
                        {
                            _selectedSlot = foundFirstEquipped;
                            RuneMemory.LogInfo($"[EquippedStatModifier] Auto-selected active slot {_selectedSlot}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RuneMemory.LogError($"[EquippedStatModifier] Diagnostics error: {ex}");
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;
            InitStyles();

            int savedDepth = GUI.depth;
            bool savedEnabled = GUI.enabled;

            try
            {
                GUI.depth = -3000;
                GUI.enabled = true;

                float w = 780f;
                float h = 640f;
                float x = (Screen.width - w) * 0.5f;
                float y = (Screen.height - h) * 0.5f;

                // Main panel background
                GUI.Label(new Rect(x, y, w, h), GUIContent.none, _sPanelBg);
                GUI.Label(new Rect(x, y, w, 4f), GUIContent.none, _sDivider);

                // Title & Subtitle
                GUI.Label(new Rect(x + 20f, y + 10f, 450f, 24f), "EQUIPPED STAT MODIFIER", _sTitle);
                GUI.Label(new Rect(x + 20f, y + 34f, 500f, 18f),
                    "Detects equipped gear in real-time. Change secondary stat affixes & values, then apply.", _sSub);

                // Close button [X]
                if (GUI.Button(new Rect(x + w - 45f, y + 12f, 30f, 26f), "X", _sCloseBtn))
                {
                    Toggle();
                    return;
                }

                var player = RuneMemory.GetLocalPlayer();
                if (player == null || player.EquipmentWorn == null)
                {
                    GUI.Label(new Rect(x + 20f, y + 80f, w - 40f, 40f),
                        "<color=#FF8888>Waiting for player to load into game world...</color>", _sLabelBold);
                    return;
                }

                // Check and synchronize working copy if slot changed
                SyncWorkingCopy(player);

                // Left Panel: Equipped Slots List (Width: 230)
                float leftX = x + 16f;
                float leftY = y + 60f;
                float leftW = 230f;
                float leftH = h - 100f;

                DrawSlotsList(player, leftX, leftY, leftW, leftH);

                // Vertical Divider
                GUI.Label(new Rect(leftX + leftW + 8f, leftY, 2f, leftH), GUIContent.none, _sDivider);

                // Right Panel: Selected Item Editor (Width: 490)
                float rightX = leftX + leftW + 18f;
                float rightY = leftY;
                float rightW = w - (rightX - x) - 16f;
                float rightH = leftH;

                DrawEditorPanel(player, rightX, rightY, rightW, rightH);

                // Bottom Status Bar
                if (_statusTimer > 0f)
                {
                    _statusTimer -= Time.unscaledDeltaTime;
                    GUI.Label(new Rect(x + 20f, y + h - 28f, w - 40f, 20f), _statusMsg, _sLabel);
                }
                else
                {
                    GUI.Label(new Rect(x + 20f, y + h - 28f, w - 40f, 20f),
                        "<color=#888888>Press F8 to toggle | Edits modify memory directly and recalculate bonuses</color>", _sSub);
                }

                // Render floating Stat Picker Modal if open
                if (_pickerTargetIndex != -1)
                {
                    DrawStatPickerModal(x, y, w, h);
                }
            }
            finally
            {
                GUI.depth = savedDepth;
                GUI.enabled = savedEnabled;
            }
        }

        private static void SyncWorkingCopy(Player player)
        {
            if (_selectedSlot == _lastLoadedSlot) return;

            _lastLoadedSlot = _selectedSlot;
            _editStats.Clear();
            _editValueStrings.Clear();
            _editUpgrades.Clear();

            if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

            Rune rune = player.EquipmentWorn[_selectedSlot];
            if (!RuneMemory.IsRuneEquipped(rune))
            {
                _lastLoadedUuid = "";
                return;
            }

            var snap = RuneMemory.Read(rune);
            _lastLoadedUuid = snap.Uuid;

            for (int i = 0; i < snap.SecondaryStats.Count; i++)
            {
                _editStats.Add(snap.SecondaryStats[i]);
                float val = i < snap.StatValues.Count ? snap.StatValues[i] : 0f;
                _editValueStrings.Add(val.ToString("0.##", CultureInfo.InvariantCulture));
                int upg = i < snap.StatUpgrades.Count ? snap.StatUpgrades[i] : 0;
                _editUpgrades.Add(upg);
            }
        }

        private static void ReloadCurrentSlot(Player player)
        {
            _lastLoadedSlot = -1;
            SyncWorkingCopy(player);
            _statusMsg = "<color=#55FFFF>Reloaded stats from item in memory.</color>";
            _statusTimer = 2.5f;
        }

        private static void DrawSlotsList(Player player, float x, float y, float width, float height)
        {
            GUI.Label(new Rect(x, y, width, 22f), "<b>EQUIPPED SLOTS</b>", _sLabelBold);
            y += 24f;
            height -= 24f;

            int slotCount = player.EquipmentWorn.Count;

            /* [2026-09-26 00:54:00] Obsolete: GUI.BeginScrollView is stripped from the game's Il2Cpp build, causing a NotSupportedException.
               We now render the slots directly using absolute coordinates since 12 slots fit well within the screen height.
            Rect viewRect = new Rect(x, y, width, height);
            float contentHeight = slotCount * 34f;
            Rect scrollContentRect = new Rect(0, 0, width - 16f, contentHeight);
            _leftScroll = GUI.BeginScrollView(viewRect, _leftScroll, scrollContentRect);
            */

            for (int i = 0; i < slotCount; i++)
            {
                float itemY = y + (i * 34f);
                bool isCur = (_selectedSlot == i);

                Rune rune = player.EquipmentWorn[i];
                bool hasItem = RuneMemory.IsRuneEquipped(rune);

                string slotTitle = i < StatCatalog.SlotDisplayNames.Length ? StatCatalog.SlotDisplayNames[i] : $"Slot {i}";
                string btnText;
                GUIStyle style;

                if (hasItem)
                {
                    var snap = RuneMemory.Read(rune);
                    string lvlText = snap.Level > 0 ? $" (+{snap.Level})" : "";
                    btnText = isCur ? $"▶ {slotTitle}{lvlText}" : $"● {slotTitle}{lvlText}";
                    style = isCur ? _sSlotBtnActive : _sSlotBtn;
                }
                else
                {
                    btnText = isCur ? $"▶ {slotTitle} (Empty)" : $"○ {slotTitle} (Empty)";
                    style = isCur ? _sSlotBtnActive : _sSlotBtnEmpty;
                }

                if (GUI.Button(new Rect(x, itemY, width - 20f, 30f), btnText, style))
                {
                    _selectedSlot = i;
                    _pickerTargetIndex = -1;
                    SyncWorkingCopy(player);
                }
            }

            /* [2026-09-26 00:54:00] Obsolete: Removed along with BeginScrollView
            GUI.EndScrollView();
            */
        }

        private static void DrawEditorPanel(Player player, float x, float y, float width, float height)
        {
            if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count)
            {
                GUI.Label(new Rect(x, y, width, 30f), "Invalid slot selected.", _sLabel);
                return;
            }

            Rune rune = player.EquipmentWorn[_selectedSlot];
            if (!RuneMemory.IsRuneEquipped(rune))
            {
                string slotName = _selectedSlot < StatCatalog.SlotDisplayNames.Length
                    ? StatCatalog.SlotDisplayNames[_selectedSlot]
                    : $"Slot {_selectedSlot}";

                GUI.Label(new Rect(x, y + 20f, width, 30f), $"<b>{slotName}</b> is currently empty.", _sLabelBold);
                GUI.Label(new Rect(x, y + 50f, width, 50f),
                    "Equip an item into this slot in-game to inspect and edit its secondary stats.\n(Select an equipped slot from the left list to view active items).", _sSub);
                return;
            }

            var snap = RuneMemory.Read(rune);

            string slotTitle = _selectedSlot < StatCatalog.SlotDisplayNames.Length
                ? StatCatalog.SlotDisplayNames[_selectedSlot]
                : $"Slot {_selectedSlot}";

            GUI.Label(new Rect(x, y, width, 22f), $"<b>ITEM: {slotTitle.ToUpper()}</b>", _sTitle);
            y += 24f;

            string infoText = $"Level: <color=#FFDD44>+{snap.Level}</color>  |  " +
                              $"Stars: <color=#FFDD44>{snap.Stars}</color>  |  " +
                              $"Rarity: <color=#FFAA55>{snap.Rarity}</color>  |  " +
                              $"Set: <color=#55FFFF>{snap.Set}</color>";
            GUI.Label(new Rect(x, y, width, 20f), infoText, _sLabel);
            y += 20f;

            string primaryInfo = $"Primary Stat: <color=#55FF88><b>{snap.PrimaryStat}</b></color> (Core item stat)";
            GUI.Label(new Rect(x, y, width, 20f), primaryInfo, _sLabel);
            y += 24f;

            // Secondary Stats Header & Add button
            float secHeaderY = y;
            GUI.Label(new Rect(x, secHeaderY + 2f, 220f, 24f),
                $"<b>Secondary Stats ({_editStats.Count} / {RuneMemory.FixedListCapacity}):</b>", _sLabelBold);

            if (_editStats.Count < RuneMemory.FixedListCapacity)
            {
                if (GUI.Button(new Rect(x + width - 150f, secHeaderY, 140f, 24f), "+ Add Secondary Stat", _sBtnAction))
                {
                    Runes.Stat candidate = Runes.Stat.Health;
                    foreach (var s in StatCatalog.AllStats)
                    {
                        if (s != snap.PrimaryStat && !_editStats.Contains(s))
                        {
                            candidate = s;
                            break;
                        }
                    }
                    _editStats.Add(candidate);
                    _editValueStrings.Add("10");
                    _editUpgrades.Add(0);
                }
            }
            y += 32f;

            // Secondary Stats List Area
            /* [2026-09-26 00:54:00] Obsolete: GUI.BeginScrollView is stripped from the game's Il2Cpp build.
            Rect viewRect = new Rect(x, y, width, 300f);
            float contentH = Mathf.Max(300f, _editStats.Count * 46f + 10f);
            Rect scrollContent = new Rect(0, 0, width - 16f, contentH);
            _rightScroll = GUI.BeginScrollView(viewRect, _rightScroll, scrollContent);
            */

            for (int i = 0; i < _editStats.Count; i++)
            {
                float rowY = y + (i * 46f) + 4f;
                DrawStatRow(i, x, rowY, width - 20f, snap.PrimaryStat);
            }

            /* [2026-09-26 00:54:00] Obsolete: Removed along with BeginScrollView
            GUI.EndScrollView();
            */
            y += 306f;

            // Action Buttons Bar
            float btnH = 34f;
            float btnW = (width - 20f) / 3f;

            if (GUI.Button(new Rect(x, y, btnW, btnH), "✓ Apply to Item", _sBtnGreen))
            {
                ApplyChanges(player);
            }

            if (GUI.Button(new Rect(x + btnW + 10f, y, btnW, btnH), "💾 Save Character", _sBtnBlue))
            {
                if (RuneMemory.TriggerSave(player, out string saveMsg))
                {
                    _statusMsg = $"<color=#55FF55>{saveMsg}</color>";
                }
                else
                {
                    _statusMsg = $"<color=#FF5555>{saveMsg}</color>";
                }
                _statusTimer = 3.5f;
            }

            if (GUI.Button(new Rect(x + (btnW + 10f) * 2, y, btnW - 10f, btnH), "↺ Revert", _sBtnAction))
            {
                ReloadCurrentSlot(player);
            }
        }

        private static void DrawStatRow(int index, float x, float y, float width, Runes.Stat primaryStat)
        {
            string statName = _editStats[index].ToString();
            string valStr = index < _editValueStrings.Count ? _editValueStrings[index] : "0";

            GUI.Label(new Rect(x, y + 4f, 26f, 26f), $"#{index + 1}", _sSub);

            float pickX = x + 30f;
            if (GUI.Button(new Rect(pickX, y + 2f, 175f, 28f), $"▼ {statName}", _sBtnAction))
            {
                _pickerTargetIndex = index;
                _searchQuery = "";
                /* [2026-09-26 00:54:00] Obsolete: _pickerScroll = Vector2.zero; */
                _pickerPage = 0;
            }

            float stepX = pickX + 182f;
            if (GUI.Button(new Rect(stepX, y + 2f, 32f, 28f), "-10", _sBtnAction))
            {
                AdjustValue(index, -10f);
            }

            if (GUI.Button(new Rect(stepX + 34f, y + 2f, 28f, 28f), "-1", _sBtnAction))
            {
                AdjustValue(index, -1f);
            }

            float textX = stepX + 64f;
            string newVal = GUI.TextField(new Rect(textX, y + 4f, 60f, 24f), valStr, _sTextField);
            if (newVal != valStr)
            {
                _editValueStrings[index] = newVal;
            }

            float addX = textX + 64f;
            if (GUI.Button(new Rect(addX, y + 2f, 28f, 28f), "+1", _sBtnAction))
            {
                AdjustValue(index, +1f);
            }

            if (GUI.Button(new Rect(addX + 30f, y + 2f, 34f, 28f), "+10", _sBtnAction))
            {
                AdjustValue(index, +10f);
            }

            float delX = addX + 68f;
            if (GUI.Button(new Rect(delX, y + 2f, 28f, 28f), "✕", _sBtnRed))
            {
                _editStats.RemoveAt(index);
                _editValueStrings.RemoveAt(index);
                if (index < _editUpgrades.Count) _editUpgrades.RemoveAt(index);
                if (_pickerTargetIndex == index) _pickerTargetIndex = -1;
            }
        }

        private static void AdjustValue(int index, float delta)
        {
            if (index < 0 || index >= _editValueStrings.Count) return;
            string str = _editValueStrings[index];
            if (!float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float cur))
            {
                cur = 0f;
            }
            float updated = (float)Math.Round(cur + delta, 2);
            if (updated < 0f && delta < 0f) updated = 0f;
            _editValueStrings[index] = updated.ToString(CultureInfo.InvariantCulture);
        }

        private static void ApplyChanges(Player player)
        {
            if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count)
            {
                _statusMsg = "<color=#FF5555>Error: No slot selected.</color>";
                _statusTimer = 3.5f;
                return;
            }

            var parsedValues = new List<float>(_editStats.Count);
            for (int i = 0; i < _editStats.Count; i++)
            {
                string s = i < _editValueStrings.Count ? _editValueStrings[i] : "0";
                if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
                    float.IsNaN(f) || float.IsInfinity(f))
                {
                    _statusMsg = $"<color=#FF5555>Error: Invalid number '{s}' on affix #{i + 1}.</color>";
                    _statusTimer = 4f;
                    return;
                }
                parsedValues.Add((float)Math.Round(f, 2));
            }

            var seen = new HashSet<Runes.Stat>();
            for (int i = 0; i < _editStats.Count; i++)
            {
                var stat = _editStats[i];
                if (!seen.Add(stat))
                {
                    _statusMsg = $"<color=#FF5555>Error: Duplicate secondary stat '{stat}' is not allowed.</color>";
                    _statusTimer = 4f;
                    return;
                }
            }

            bool success = RuneMemory.ApplySecondaryStats(
                player,
                _selectedSlot,
                _editStats,
                parsedValues,
                _editUpgrades,
                out string err);

            if (success)
            {
                string slotName = _selectedSlot < StatCatalog.SlotDisplayNames.Length
                    ? StatCatalog.SlotDisplayNames[_selectedSlot]
                    : $"Slot {_selectedSlot}";
                _statusMsg = $"<color=#55FF55>✓ Successfully modified {slotName}! Stats updated in-game.</color>";
            }
            else
            {
                _statusMsg = $"<color=#FF5555>Apply failed: {err}</color>";
            }
            _statusTimer = 4f;
        }

        private static void DrawStatPickerModal(float parentX, float parentY, float parentW, float parentH)
        {
            float modalW = 420f;
            float modalH = 460f;
            float modalX = parentX + (parentW - modalW) * 0.5f;
            float modalY = parentY + (parentH - modalH) * 0.5f;

            GUI.Label(new Rect(modalX, modalY, modalW, modalH), GUIContent.none, _sPickerBg);
            GUI.Label(new Rect(modalX, modalY, modalW, 3f), GUIContent.none, _sDivider);

            GUI.Label(new Rect(modalX + 15f, modalY + 10f, 300f, 22f),
                $"<b>CHOOSE STAT (Affix #{_pickerTargetIndex + 1})</b>", _sTitle);

            if (GUI.Button(new Rect(modalX + modalW - 35f, modalY + 10f, 25f, 22f), "✕", _sCloseBtn))
            {
                _pickerTargetIndex = -1;
                return;
            }

            GUI.Label(new Rect(modalX + 15f, modalY + 38f, 60f, 24f), "Search:", _sLabel);
            _searchQuery = GUI.TextField(new Rect(modalX + 80f, modalY + 38f, modalW - 100f, 24f), _searchQuery, _sTextField);

            var matches = StatCatalog.FilterStats(_searchQuery);
            float listY = modalY + 70f;
            float listH = modalH - 120f; // Reduced to make room for pagination buttons

            /* [2026-09-26 00:54:00] Obsolete: GUI.BeginScrollView causes NotSupportedException in this IL2CPP build.
               Replaced with simple pagination.
            Rect viewRect = new Rect(modalX + 15f, listY, modalW - 30f, listH);
            float contentH = Mathf.Max(listH, matches.Count * 28f + 10f);
            Rect scrollContent = new Rect(0, 0, modalW - 50f, contentH);
            _pickerScroll = GUI.BeginScrollView(viewRect, _pickerScroll, scrollContent);
            */

            int itemsPerPage = 11;
            int totalPages = Mathf.CeilToInt((float)matches.Count / itemsPerPage);
            if (_pickerPage >= totalPages && totalPages > 0) _pickerPage = totalPages - 1;
            if (_pickerPage < 0) _pickerPage = 0;

            int startIndex = _pickerPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, matches.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                var s = matches[i];
                string sName = s.ToString();
                float itemY = listY + ((i - startIndex) * 28f);

                bool isCurrent = (_pickerTargetIndex < _editStats.Count && _editStats[_pickerTargetIndex] == s);
                string btnLabel = isCurrent ? $"<b><color=#55FF55>✓ {sName}</color></b>" : sName;

                if (GUI.Button(new Rect(modalX + 15f, itemY, modalW - 30f, 26f), btnLabel, isCurrent ? _sSlotBtnActive : _sBtnAction))
                {
                    if (_pickerTargetIndex >= 0 && _pickerTargetIndex < _editStats.Count)
                    {
                        _editStats[_pickerTargetIndex] = s;
                    }
                    _pickerTargetIndex = -1;
                    _pickerPage = 0; // reset for next time
                    break;
                }
            }

            /* [2026-09-26 00:54:00] Obsolete: Removed along with BeginScrollView
            GUI.EndScrollView();
            */

            // Pagination Controls
            float pageY = modalY + modalH - 35f;
            if (GUI.Button(new Rect(modalX + 15f, pageY, 100f, 24f), "Prev Page", _sBtnAction))
            {
                if (_pickerPage > 0) _pickerPage--;
            }
            GUI.Label(new Rect(modalX + 160f, pageY, 100f, 24f), $"Page {_pickerPage + 1} / {Mathf.Max(1, totalPages)}", _sLabel);
            if (GUI.Button(new Rect(modalX + modalW - 115f, pageY, 100f, 24f), "Next Page", _sBtnAction))
            {
                if (_pickerPage < totalPages - 1) _pickerPage++;
            }
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.DontSave;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    t.SetPixel(x, y, col);
                }
            }
            t.Apply();
            _textures.Add(t);
            return t;
        }

        private static void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _sPanelBg = new GUIStyle(GUI.skin.label);
            _sDivider = new GUIStyle(GUI.skin.label);
            _sPickerBg = new GUIStyle(GUI.skin.label);

            _sTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.85f, 0.40f, 1f) }
            };

            _sSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = new Color(0.70f, 0.68f, 0.65f, 1f) }
            };

            _sLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                normal = { textColor = new Color(0.85f, 0.82f, 0.78f, 1f) }
            };

            _sLabelBold = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = { textColor = new Color(0.95f, 0.92f, 0.85f, 1f) }
            };

            _sSlotBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.90f, 0.90f, 0.90f, 1f) }
            };

            _sSlotBtnActive = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.black }
            };

            _sSlotBtnEmpty = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.55f, 0.52f, 0.48f, 1f) }
            };

            _sBtnAction = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sBtnGreen = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sBtnBlue = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sBtnRed = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sCloseBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sTextField = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };

            _sPanelBg.normal.background = MakeTex(1, 1, new Color(0.12f, 0.10f, 0.08f, 0.97f));
            _sPickerBg.normal.background = MakeTex(1, 1, new Color(0.15f, 0.13f, 0.11f, 0.99f));
            _sDivider.normal.background = MakeTex(1, 1, new Color(0.85f, 0.70f, 0.15f, 1f));

            _sSlotBtn.normal.background = MakeTex(1, 1, new Color(0.24f, 0.21f, 0.18f, 0.9f));
            _sSlotBtnActive.normal.background = MakeTex(1, 1, new Color(0.88f, 0.72f, 0.18f, 1f));
            _sSlotBtnEmpty.normal.background = MakeTex(1, 1, new Color(0.18f, 0.16f, 0.14f, 0.7f));

            _sBtnAction.normal.background = MakeTex(1, 1, new Color(0.28f, 0.25f, 0.22f, 1f));
            _sBtnGreen.normal.background = MakeTex(1, 1, new Color(0.18f, 0.58f, 0.25f, 1f));
            _sBtnBlue.normal.background = MakeTex(1, 1, new Color(0.18f, 0.42f, 0.68f, 1f));
            _sBtnRed.normal.background = MakeTex(1, 1, new Color(0.65f, 0.20f, 0.20f, 1f));
            _sCloseBtn.normal.background = MakeTex(1, 1, new Color(0.65f, 0.20f, 0.20f, 1f));
        }
    }
}
