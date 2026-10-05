using System;
using System.Collections.Generic;
using System.Globalization;
using EquipmentStatEditor.Core;
using UnityEngine;

namespace EquipmentStatEditor.UI
{
    /// <summary>
    /// In-game rune stat editor panel (hotkey, default F8).
    ///
    /// [2026-09-26 01:59] Ported from DimraethModPack/Modules/Stats/EquippedStatModifierModule.cs
    /// (its UI class) at the user's request: "fix the module's UI and move it to the standalone".
    /// That module was unregistered from the ModPack the same day (see DimraethModPackPlugin.cs);
    /// its source is preserved there. Adaptations of this port:
    ///   * All rune memory access goes through RuneMemory/EditorEngine (unboxed data-pointer
    ///     interop) instead of the module's own Memory layer - that layer read/wrote through a
    ///     boxed-header pointer and was the unreliable part (right-hand panel could go blank).
    ///   * UI errors are logged by the caller (EditorController.OnGUI) instead of being
    ///     swallowed by an empty catch, which is how the "no buttons" problem stayed hidden.
    ///   * ASCII button labels only - the module's unicode glyphs rendered as mojibake.
    ///   * GUI.BeginScrollView is still avoided: it is stripped from this Il2Cpp build
    ///     (see the module's own [2026-09-26 00:54] notes). List paging kept as the module
    ///     solved it.
    ///   * [2026-09-26 02:19] GUI.TextField is ALSO stripped here (GUIStateObjects.GetStateObject
    ///     -> "Method unstripping failed"); it aborted every frame mid-draw, which hid the
    ///     steppers and the Apply/Save/Revert buttons. Values and the picker search now use
    ///     focusable boxes with direct keyboard input (HandleKeyboard). The panel is also
    ///     stretchable now: laid out on a 1280x720 virtual canvas scaled via GUI.matrix.
    /// Scope: secondary stat add/remove/change + stat values + upgrade tiers of the selected
    /// equipped item. Everything else (Level, Rarity, Stars, Set, PrimaryStat, inventory runes)
    /// is still editable through the config file, which remains the backup workflow.
    /// </summary>
    public static class RuneEditorUI
    {
        public static bool IsOpen = false;

        private static int _selectedSlot = 2; // Default to Belt (Sash)
        private static int _lastLoadedSlot = -1;

        private static readonly List<Runes.Stat> _editStats = new();
        private static readonly List<string> _editValueStrings = new();
        private static readonly List<int> _editUpgrades = new();
        // [2026-09-27 12:37] Added: the loaded rune's star rating, so a newly-added or changed
        // affix can seed its natural roll (StatCapCatalog.TryGetNatural) instead of a flat 10.
        private static int _loadedStars;
        // [2026-09-26 02:46] Added: StatValues[0] is the PRIMARY stat's magnitude in the game
        // layout (values = secondaries + 1). Preserved across applies so editing secondaries
        // never silently changes the primary value.
        /* [2026-09-27 14:08] Obsolete: the primary value was kept as a float only. It is now
           editable (identity + value) and parsed from _primaryValueStr on apply, so this field
           is no longer read.
        private static float _primaryValue;
        */
        // [2026-09-27 14:08] Added: the primary stat is now editable (identity + value).
        // GearLegality.Inspect validates the primary *value* against TryStatValueCeiling(...,
        // isPrimary: true) - the same ceiling mechanism as secondaries, only level 12 instead of 5
        // - and never checks the primary stat's identity against the rune's set. Both are
        // therefore anti-cheat safe (a value above the primary ceiling is the only destroy trigger).
        private static Runes.Stat _primaryStat = Runes.Stat.Health;
        private static string _primaryValueStr = "0";
        // Sentinel focus row for the primary value box (real rows are >= 0, "none" is -1).
        private const int PrimaryFocusIndex = -2;

        private static int _pickerRow = -1;
        private static string _searchQuery = "";
        private static int _pickerPage = 0;
        // [2026-09-27 14:08] Added: the picker can now target the primary stat (see DrawPrimaryRow).
        private static bool _pickerIsPrimary;

        // [2026-09-26 11:14] Added: draggable picker window state (user request: "make the ui
        // draggable" - the stat list window must be movable so it stops covering the panel
        // behind it). _pickerPos is the window's top-left in the virtual-canvas space; it
        // defaults to the panel center on first open and then follows the title-bar drag.
        private static Vector2 _pickerPos;
        private static bool _pickerPosSet;
        private static bool _pickerDragging;
        private static Vector2 _pickerDragOffset;

        // [2026-09-26 02:19] Added: keyboard-focus state for the value boxes and the picker
        // search box (replacement for the stripped GUI.TextField - see HandleKeyboard).
        private static int _focusedValueRow = -1;
        private static bool _focusedSearch;

        private static string _statusMsg = "";
        private static float _statusTimer = 0f;

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
        private static GUIStyle _sValueField;
        private static GUIStyle _sValueFieldFocused;
        // [2026-09-27 12:23] Added: cap-aware styles - a red value box and a per-row cap label
        // when an affix value sits above the game's ceiling (StatCapCatalog).
        private static GUIStyle _sValueFieldWarn;
        private static GUIStyle _sCapLabel;
        private static GUIStyle _sCapWarn;
        /* [2026-09-26 02:19] Obsolete: _sTextField was only used by GUI.TextField, which is
           stripped from this Il2Cpp build (see HandleKeyboard / DrawRow). The value boxes
           are buttons now, styled by _sValueField / _sValueFieldFocused.
        private static GUIStyle _sTextField;
        */
        private static GUIStyle _sPickerBg;

        // ------------------------------------------------------------------
        // Rune access helpers (proven RuneMemory interop)
        // ------------------------------------------------------------------

        private static bool IsEquipped(Rune rune)
        {
            IntPtr data = RuneMemory.DataPtr(rune);
            return data != IntPtr.Zero && !RuneMemory.IsEmptyRune(data);
        }

        private static RuneSnapshot ReadRune(Rune rune)
        {
            IntPtr data = RuneMemory.DataPtr(rune);
            return data == IntPtr.Zero ? new RuneSnapshot() : RuneMemory.Read(data);
        }

        private static string SlotName(int index) =>
            index < Catalog.SlotNames.Length ? Catalog.SlotNames[index] : $"Slot {index}";

        // ------------------------------------------------------------------
        // Keyboard input (replacement for the stripped GUI.TextField)
        // ------------------------------------------------------------------

        /// <summary>
        /// [2026-09-26 02:19] Added: direct keyboard capture for the focused value box or
        /// picker search box. GUI.TextField crashes in this Il2Cpp build
        /// (GUIStateObjects.GetStateObject -> "Method unstripping failed"), so typed
        /// characters are read from the IMGUI KeyDown event instead.
        /// </summary>
        private static void HandleKeyboard()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            if (_focusedSearch)
            {
                if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    _focusedSearch = false;
                    e.Use();
                }
                else if (e.character == '\b')
                {
                    if (_searchQuery.Length > 0) _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);
                    _pickerPage = 0;
                    e.Use();
                }
                else if (!char.IsControl(e.character))
                {
                    _searchQuery += e.character;
                    _pickerPage = 0;
                    e.Use();
                }
                return;
            }

            // [2026-09-27 14:08] Changed: the focus target can now be the primary value box
            // (PrimaryFocusIndex) as well as a secondary row, so read/write through the helpers.
            /* [2026-09-27 14:08] Obsolete: direct index into _editValueStrings only.
            if (_focusedValueRow < 0 || _focusedValueRow >= _editValueStrings.Count) return;

            string cur = _editValueStrings[_focusedValueRow];
            if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                _focusedValueRow = -1;
                e.Use();
            }
            else if (e.character == '\b')
            {
                _editValueStrings[_focusedValueRow] = cur.Length > 1 ? cur.Substring(0, cur.Length - 1) : "0";
                e.Use();
            }
            else if (char.IsDigit(e.character) || e.character == '.' || e.character == '-')
            {
                // Replace the placeholder "0" on the first real digit / sign.
                _editValueStrings[_focusedValueRow] =
                    (cur == "0" && e.character != '.') ? e.character.ToString() : cur + e.character;
                e.Use();
            }
            */
            if (!TryGetFocusedValue(out string cur)) return;

            if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                _focusedValueRow = -1;
                e.Use();
            }
            else if (e.character == '\b')
            {
                SetFocusedValue(cur.Length > 1 ? cur.Substring(0, cur.Length - 1) : "0");
                e.Use();
            }
            else if (char.IsDigit(e.character) || e.character == '.' || e.character == '-')
            {
                // Replace the placeholder "0" on the first real digit / sign.
                SetFocusedValue((cur == "0" && e.character != '.') ? e.character.ToString() : cur + e.character);
                e.Use();
            }
        }

        // [2026-09-27 14:08] Added: value-focus helpers covering both a secondary row and the
        // primary box (PrimaryFocusIndex), shared by HandleKeyboard and the editors.
        private static bool TryGetFocusedValue(out string cur)
        {
            if (_focusedValueRow == PrimaryFocusIndex) { cur = _primaryValueStr; return true; }
            if (_focusedValueRow >= 0 && _focusedValueRow < _editValueStrings.Count)
            {
                cur = _editValueStrings[_focusedValueRow];
                return true;
            }
            cur = null;
            return false;
        }

        private static void SetFocusedValue(string v)
        {
            if (_focusedValueRow == PrimaryFocusIndex) _primaryValueStr = v;
            else if (_focusedValueRow >= 0 && _focusedValueRow < _editValueStrings.Count)
                _editValueStrings[_focusedValueRow] = v;
        }

        // ------------------------------------------------------------------
        // Open / close
        // ------------------------------------------------------------------

        public static void Toggle()
        {
            IsOpen = !IsOpen;
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _lastLoadedSlot = -1;
                _pickerRow = -1;
                // [2026-09-26 02:19] Added: reset keyboard focus with the panel.
                _focusedValueRow = -1;
                _focusedSearch = false;
                // [2026-09-26 11:14] Added: the picker drag latch must not survive a toggle.
                _pickerDragging = false;
                RuneMemory.EnsureOffsets();
                AutoSelectFirstEquipped();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private static void AutoSelectFirstEquipped()
        {
            try
            {
                var player = EditorEngine.GetLocalPlayer();
                if (player?.EquipmentWorn != null)
                {
                    for (int i = 0; i < player.EquipmentWorn.Count; i++)
                    {
                        if (IsEquipped(player.EquipmentWorn[i]))
                        {
                            if (!IsEquipped(player.EquipmentWorn[_selectedSlot]))
                            {
                                _selectedSlot = i;
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EquipmentStatEditorPlugin.Log.LogWarning($"[EquipmentStatEditor] UI auto-select failed: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Main draw
        // ------------------------------------------------------------------

        public static void Draw()
        {
            if (!IsOpen) return;
            InitStyles();
            HandleKeyboard();

            int savedDepth = GUI.depth;
            bool savedEnabled = GUI.enabled;
            // [2026-09-26 02:19] Added: stretchable scaling - the panel is laid out on a fixed
            // 1280x720 virtual canvas and GUI.matrix scales it to the real resolution, so no
            // control can be cut off at small resolutions and the UI grows on big screens.
            Matrix4x4 savedMatrix = GUI.matrix;

            try
            {
                GUI.depth = -3000;
                GUI.enabled = true;

                float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.5f, 2.5f);
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

                float w = 880f;
                float h = 660f;
                float x = (Screen.width / scale - w) * 0.5f;
                float y = (Screen.height / scale - h) * 0.5f;

                GUI.Label(new Rect(x, y, w, h), GUIContent.none, _sPanelBg);
                GUI.Label(new Rect(x, y, w, 4f), GUIContent.none, _sDivider);

                GUI.Label(new Rect(x + 20f, y + 10f, 450f, 24f), "EQUIPMENT STAT EDITOR", _sTitle);
                // [2026-09-29 10:29] Changed: subtitle width 520 -> 470 to make room for the new
                // Safe/Bypass mode button on the title bar row (see below).
                GUI.Label(new Rect(x + 20f, y + 34f, 470f, 18f),
                    "Edit secondary stat affixes & values on equipped gear, then apply. Config file remains the backup workflow.", _sSub);

                // [2026-09-29 10:29] Added: Safe/Bypass mode toggle. Safe (blue) = the vanilla
                // per-stat ceilings. Bypass (red) = 99x vanilla ceilings + the game's item
                // destruction disabled (GearLegality.Inspect -> Violation.None). The choice is
                // persisted to the config file (General.BypassMode).
                // [2026-09-30 02:47] Renamed to Vanilla/Modded caps: the button now only selects
                // the ceiling (General.UseModdedCaps). Item destruction is owned unconditionally
                // by the standalone AntiCheatBypassMod, so over-cap items survive regardless of
                // this toggle.
                bool moddedCaps = StatCapCatalog.ModdedCaps;
                if (GUI.Button(new Rect(x + w - 365f, y + 12f, 150f, 26f),
                        moddedCaps ? "Caps: Modded" : "Caps: Vanilla", moddedCaps ? _sBtnRed : _sBtnBlue))
                {
                    EquipmentStatEditorPlugin.SetCapMode(!moddedCaps, persist: true);
                    _statusMsg = StatCapCatalog.ModdedCaps
                        ? "<color=#FF8855>Modded caps ON - ceilings raised to 99x vanilla. Over-vanilla items survive thanks to the standalone AntiCheatBypassMod.</color>"
                        : "<color=#55FF55>Vanilla caps - the real per-stat ceilings. Item destruction is still disabled by the standalone AntiCheatBypassMod.</color>";
                    _statusTimer = 8f;
                }

                // [2026-09-27 15:15] Added: on-demand rune dump. Previously the config was rewritten
                // on every game autosave (RewriteDumpAfterSave=true), which churned the big .cfg
                // non-stop. Now the user writes it explicitly with this button.
                if (GUI.Button(new Rect(x + w - 205f, y + 12f, 150f, 26f), "Dump Runes to File", _sBtnAction))
                {
                    if (EditorEngine.DumpToFile(out string dumpPath))
                        _statusMsg = $"<color=#55FF55>Dumped runes to {dumpPath}</color>";
                    else
                        _statusMsg = "<color=#FF5555>Dump failed - see BepInEx log.</color>";
                    _statusTimer = 6f;
                }

                if (GUI.Button(new Rect(x + w - 45f, y + 12f, 30f, 26f), "X", _sCloseBtn))
                {
                    Toggle();
                    return;
                }

                var player = EditorEngine.GetLocalPlayer();
                if (player == null || player.EquipmentWorn == null)
                {
                    GUI.Label(new Rect(x + 20f, y + 80f, w - 40f, 40f),
                        "<color=#FF8888>Waiting for player to load into game world...</color>", _sLabelBold);
                    return;
                }

                SyncWorkingCopy(player);

                float leftX = x + 16f;
                float leftY = y + 60f;
                float leftW = 230f;
                float leftH = h - 100f;

                DrawSlots(player, leftX, leftY, leftW, leftH);

                GUI.Label(new Rect(leftX + leftW + 8f, leftY, 2f, leftH), GUIContent.none, _sDivider);

                float rightX = leftX + leftW + 18f;
                float rightY = leftY;
                float rightW = w - (rightX - x) - 16f;
                float rightH = leftH;

                DrawEditor(player, rightX, rightY, rightW, rightH);

                // [2026-09-26 02:46] Added: surface rollback notices from the apply-error handler
                // (the game can strip a rejected item seconds after the write).
                if (EditorEngine.UiNotice != null)
                {
                    _statusMsg = EditorEngine.UiNotice;
                    EditorEngine.UiNotice = null;
                    _statusTimer = 6f;
                }

                if (_statusTimer > 0f)
                {
                    _statusTimer -= Time.unscaledDeltaTime;
                    GUI.Label(new Rect(x + 20f, y + h - 28f, w - 40f, 20f), _statusMsg, _sLabel);
                }
                else
                {
                    GUI.Label(new Rect(x + 20f, y + h - 28f, w - 40f, 20f),
                        $"<color=#888888>Press {EquipmentStatEditorPlugin.EditorKey.Value} to toggle | " +
                        // [2026-09-29 10:29] Changed: "Max = safe cap" -> "Max = mode cap" - the
                        // Max button and the red warning now follow the Safe/Bypass mode ceiling.
                        // [2026-09-30 02:47] Changed: "SAFE/BYPASS" -> "VANILLA/MODDED".
                        "click a value box & type a number | Max = mode cap (VANILLA = real caps, MODDED = 99x) | over-cap values show red | Apply writes, Save Character persists</color>", _sSub);
                }

                // [2026-09-27 14:08] Changed: the picker also opens for the primary stat.
                if (_pickerRow != -1 || _pickerIsPrimary)
                    DrawPicker(x, y, w, h);
            }
            finally
            {
                GUI.matrix = savedMatrix;
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
            // [2026-09-27 12:37] Added: reset then capture the rune's stars for natural seeding.
            _loadedStars = 0;
            // [2026-09-26 02:19] Added: focus must not survive a slot switch.
            _focusedValueRow = -1;

            if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

            Rune rune = player.EquipmentWorn[_selectedSlot];
            if (!IsEquipped(rune)) return;

            var snap = ReadRune(rune);
            _loadedStars = snap.Stars; // [2026-09-27 12:37] capture stars for natural seeding
            // [2026-09-26 02:46] Fixed: value-slot mapping. The game stores StatValues = n + 1
            // with slot 0 for the PrimaryStat, so row i edits StatValues[i + 1]. The old code
            // read/wrote slot i (one off), which made every edited secondary display as 0 in
            // the equipment panel (user report 2026-09-26).
            // [2026-09-27 14:08] Changed: capture the primary stat identity + an editable string
            // (the standalone _primaryValue float is gone - see the field note).
            /* [2026-09-27 14:08] Obsolete: float-only capture (field removed).
            _primaryValue = snap.Values.Length == snap.Stats.Length + 1 ? snap.Values[0] : 0f;
            */
            float primaryVal = snap.Values.Length == snap.Stats.Length + 1 ? snap.Values[0] : 0f;
            _primaryStat = (Runes.Stat)snap.PrimaryStat;
            _primaryValueStr = primaryVal.ToString("0.##", CultureInfo.InvariantCulture);
            for (int i = 0; i < snap.Stats.Length; i++)
            {
                _editStats.Add((Runes.Stat)snap.Stats[i]);
                /* [2026-09-26 02:46] Obsolete: off-by-one mapping (see note above).
                float val = i < snap.Values.Length ? snap.Values[i] : 0f;
                */
                float val;
                if (snap.Values.Length == snap.Stats.Length + 1) val = snap.Values[i + 1];
                else if (i < snap.Values.Length) val = snap.Values[i]; // legacy layout from the old UI
                else val = 0f;
                _editValueStrings.Add(val.ToString("0.##", CultureInfo.InvariantCulture));
                int upg = i < snap.Upgrades.Length ? snap.Upgrades[i] : 0;
                _editUpgrades.Add(upg);
            }
        }

        // [2026-09-27 12:37] Added: natural value for a newly-added/changed affix at the loaded
        // rune's stars (StatCapCatalog.TryGetNatural = CalculateStatBaseValue). Falls back to "10"
        // for the few stats the game has no baseline for (its uncapped set).
        private static string NaturalSeed(Runes.Stat stat)
        {
            if (StatCapCatalog.TryGetNatural(stat, _loadedStars, out float v))
                return ((float)Math.Round(v, 2)).ToString(CultureInfo.InvariantCulture);
            return "10";
        }

        // ------------------------------------------------------------------
        // Left column: equipped slots
        // ------------------------------------------------------------------

        private static void DrawSlots(Player player, float x, float y, float width, float height)
        {
            GUI.Label(new Rect(x, y, width, 22f), "<b>EQUIPPED SLOTS</b>", _sLabelBold);
            y += 24f;

            int slotCount = player.EquipmentWorn.Count;

            for (int i = 0; i < slotCount; i++)
            {
                float itemY = y + (i * 34f);
                bool isCur = (_selectedSlot == i);

                Rune rune = player.EquipmentWorn[i];
                bool hasItem = IsEquipped(rune);

                string slotTitle = SlotName(i);
                string btnText;
                if (hasItem)
                {
                    var snap = ReadRune(rune);
                    string lvlText = snap.Level > 0 ? $" (+{snap.Level})" : "";
                    btnText = (isCur ? "* " : "  ") + slotTitle + lvlText;
                }
                else
                {
                    btnText = (isCur ? "* " : "  ") + slotTitle + " (Empty)";
                }
                GUIStyle style = isCur ? _sSlotBtnActive : (hasItem ? _sSlotBtn : _sSlotBtnEmpty);

                if (GUI.Button(new Rect(x, itemY, width - 20f, 30f), btnText, style))
                {
                    _selectedSlot = i;
                    _pickerRow = -1;
                    SyncWorkingCopy(player);
                }
            }
        }

        // ------------------------------------------------------------------
        // Right column: item info + affix rows + actions
        // ------------------------------------------------------------------

        private static void DrawEditor(Player player, float x, float y, float width, float height)
        {
            if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

            Rune rune = player.EquipmentWorn[_selectedSlot];
            if (!IsEquipped(rune))
            {
                GUI.Label(new Rect(x, y + 20f, width, 30f), $"<b>{SlotName(_selectedSlot)}</b> is currently empty.", _sLabelBold);
                GUI.Label(new Rect(x, y + 50f, width, 50f),
                    "Equip an item into this slot in-game to inspect and edit its secondary stats.\n(Select an equipped slot from the left list to view active items).", _sSub);
                return;
            }

            var snap = ReadRune(rune);
            string slotTitle = SlotName(_selectedSlot);

            GUI.Label(new Rect(x, y, width, 22f), $"<b>ITEM: {slotTitle.ToUpper()}</b>", _sTitle);
            y += 24f;

            string infoText = $"Level: <color=#FFDD44>+{snap.Level}</color>  |  " +
                              $"Stars: <color=#FFDD44>{(Runes.Stars)snap.Stars}</color>  |  " +
                              $"Rarity: <color=#FFAA55>{(Runes.Rarity)snap.Rarity}</color>  |  " +
                              $"Set: <color=#55FFFF>{(Runes.RuneSet)snap.Set}</color>";
            GUI.Label(new Rect(x, y, width, 20f), infoText, _sLabel);
            y += 20f;

            // [2026-09-26 02:46] Added: show the PrimaryStat value (StatValues slot 0).
            // [2026-09-27 14:08] Changed: the primary stat is now fully editable - pick a
            // different primary and/or set its value, bounded by the PRIMARY ceiling (level 12).
            /* [2026-09-26 02:46] Obsolete: read-only primary info (identity + value), replaced by
               the editable DrawPrimaryRow below.
            string primaryInfo = $"Primary Stat: <color=#55FF88><b>{(Runes.Stat)snap.PrimaryStat}</b></color> " +
                                 $"(value {(snap.Values.Length > 0 ? snap.Values[0] : 0f).ToString("0.##", CultureInfo.InvariantCulture)} - preserved on apply)";
            GUI.Label(new Rect(x, y, width, 20f), primaryInfo, _sLabel);
            y += 24f;
            */
            GUI.Label(new Rect(x, y + 2f, 260f, 24f), "<b>Primary Stat (core):</b>", _sLabelBold);
            y += 24f;
            DrawPrimaryRow(x, y, width - 20f);
            y += 46f;

            // [2026-09-26 02:46] Fixed: cap is MaxSecondaryStats (6), not FixedListCapacity (7).
            // StatValues has 7 slots and slot 0 belongs to PrimaryStat - 7 secondaries need 8
            // value slots, the overflow destroys the item in-game (user report 2026-09-26:
            // "if I added max 7 new stats, the item got disappeared").
            GUI.Label(new Rect(x, y + 2f, 220f, 24f),
                $"<b>Secondary Stats ({_editStats.Count} / {RuneMemory.MaxSecondaryStats}):</b>", _sLabelBold);

            if (_editStats.Count < RuneMemory.MaxSecondaryStats)
            {
                // [2026-09-26 02:19] Fix: button widened (140 -> 175) - "Add Secondary Stat"
                // was clipped to "Add Secondary Sta" at the old size.
                if (GUI.Button(new Rect(x + width - 185f, y, 175f, 24f), "+ Add Secondary Stat", _sBtnAction))
                {
                    Runes.Stat candidate = Runes.Stat.Health;
                    foreach (var s in Catalog.AllStats)
                    {
                        if (s != _primaryStat && !_editStats.Contains(s))
                        {
                            candidate = s;
                            break;
                        }
                    }
                    _editStats.Add(candidate);
                    // [2026-09-27 12:37] Changed: seed a new affix with its natural roll for this
                    // rune's stars (a flat "10" is below/above the real game value for most stats).
                    /* [2026-09-27 12:37] Obsolete: flat default value - no longer represents a
                       natural roll; replaced by NaturalSeed(candidate).
                    _editValueStrings.Add("10");
                    */
                    _editValueStrings.Add(NaturalSeed(candidate));
                    _editUpgrades.Add(0);
                }
            }
            y += 32f;

            for (int i = 0; i < _editStats.Count; i++)
            {
                float rowY = y + (i * 46f) + 4f;
                DrawRow(i, x, rowY, width - 20f);
            }

            // [2026-09-26 02:19] Fix: 306 -> 334 so the 7th affix row (ends at 310) can no
            // longer overlap the action buttons.
            y += 334f;

            float btnH = 34f;
            float btnW = (width - 20f) / 3f;

            if (GUI.Button(new Rect(x, y, btnW, btnH), "Apply to Item", _sBtnGreen))
            {
                Apply(player);
            }

            if (GUI.Button(new Rect(x + btnW + 10f, y, btnW, btnH), "Save Character", _sBtnBlue))
            {
                EditorEngine.SaveNow(player);
                _statusMsg = "<color=#55FF55>Native save triggered (Player.SaveGame).</color>";
                _statusTimer = 3.5f;
            }

            if (GUI.Button(new Rect(x + (btnW + 10f) * 2, y, btnW - 10f, btnH), "Revert", _sBtnAction))
            {
                _lastLoadedSlot = -1;
                SyncWorkingCopy(player);
                _statusMsg = "<color=#55FFFF>Reloaded stats from item in memory.</color>";
                _statusTimer = 2.5f;
            }
        }

        private static void DrawRow(int index, float x, float y, float width)
        {
            string statName = _editStats[index].ToString();
            string valStr = index < _editValueStrings.Count ? _editValueStrings[index] : "0";

            GUI.Label(new Rect(x, y + 4f, 26f, 26f), $"#{index + 1}", _sSub);

            float pickX = x + 30f;
            if (GUI.Button(new Rect(pickX, y + 2f, 175f, 28f), statName, _sBtnAction))
            {
                _pickerRow = index;
                // [2026-09-27 14:08] Added: a secondary row is not the primary target.
                _pickerIsPrimary = false;
                _searchQuery = "";
                _pickerPage = 0;
            }

            float stepX = pickX + 182f;
            if (GUI.Button(new Rect(stepX, y + 2f, 34f, 28f), "-10", _sBtnAction))
                AdjustVal(index, -10f);

            if (GUI.Button(new Rect(stepX + 36f, y + 2f, 30f, 28f), "-1", _sBtnAction))
                AdjustVal(index, -1f);

            float textX = stepX + 72f;
            /* [2026-09-26 02:19] Obsolete: GUI.TextField crashes in this Il2Cpp build
               (GUIStateObjects.GetStateObject -> "Method unstripping failed"). The exception
               aborted every frame right here, which hid the +1/+10/X steppers and the
               Apply/Save/Revert buttons below - the reported "steppers missing" bug.
               Replaced by a focusable value box: click it, then type digits / '.' / '-',
               Backspace deletes, Enter/Escape unfocuses (see HandleKeyboard).
            string newVal = GUI.TextField(new Rect(textX, y + 4f, 60f, 24f), valStr, _sTextField);
            if (newVal != valStr)
                _editValueStrings[index] = newVal;
            */
            // [2026-09-27 12:23] Added: this affix's secondary ceiling - drives the red warning
            // and the Max button. Secondary role: every row here is a secondary stat (the
            // PrimaryStat magnitude is preserved, not edited).
            // [2026-09-29 10:29] Changed: TryGetCap -> TryGetEffectiveCap so the ceiling follows
            // the cap mode (vanilla, or 99x vanilla with Modded caps).
            float curVal = 0f;
            float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out curVal);
            bool hasCap = StatCapCatalog.TryGetEffectiveCap(_editStats[index], false, out float cap);
            bool overCap = hasCap && curVal > cap + 0.0001f;

            bool focused = (_focusedValueRow == index);
            string shown = focused ? valStr + "|" : valStr;
            GUIStyle valStyle = overCap ? _sValueFieldWarn : (focused ? _sValueFieldFocused : _sValueField);
            if (GUI.Button(new Rect(textX, y + 2f, 96f, 28f), shown, valStyle))
            {
                _focusedValueRow = focused ? -1 : index;
            }

            // [2026-09-27 12:23] Added: cap hint under the value box (red when exceeded).
            string capText = hasCap ? $"max {cap.ToString("0.#", CultureInfo.InvariantCulture)}" : "uncapped";
            GUI.Label(new Rect(textX, y + 30f, 96f, 14f), capText, overCap ? _sCapWarn : _sCapLabel);

            float addX = textX + 100f;
            if (GUI.Button(new Rect(addX, y + 2f, 30f, 28f), "+1", _sBtnAction))
                AdjustVal(index, +1f);

            if (GUI.Button(new Rect(addX + 32f, y + 2f, 34f, 28f), "+10", _sBtnAction))
                AdjustVal(index, +10f);

            float delX = addX + 70f;
            // [2026-09-27 12:23] Added: "Max" sets the value straight to the safe cap (disabled
            // for the few stats the game leaves uncapped).
            // [2026-09-29 10:29] Changed: the cap is now the MODE cap (TryGetEffectiveCap) - the
            // vanilla ceiling in Safe mode, 99x vanilla in Bypass mode.
            // [2026-09-30 02:47] Updated wording: Safe/Bypass -> Vanilla/Modded caps.
            bool prevEnabled = GUI.enabled;
            GUI.enabled = hasCap;
            if (GUI.Button(new Rect(delX, y + 2f, 40f, 28f), "Max", _sBtnBlue))
            {
                _editValueStrings[index] = ((float)Math.Round(cap, 2)).ToString(CultureInfo.InvariantCulture);
                _focusedValueRow = -1;
            }
            GUI.enabled = prevEnabled;

            if (GUI.Button(new Rect(delX + 44f, y + 2f, 30f, 28f), "X", _sBtnRed))
            {
                _editStats.RemoveAt(index);
                _editValueStrings.RemoveAt(index);
                if (index < _editUpgrades.Count) _editUpgrades.RemoveAt(index);
                if (_pickerRow == index) _pickerRow = -1;
                // [2026-09-26 02:19] Added: keep the keyboard-focus index valid after a delete.
                if (_focusedValueRow == index) _focusedValueRow = -1;
                else if (_focusedValueRow > index) _focusedValueRow--;
            }
        }

        // [2026-09-27 14:08] Added: the editable primary-stat row. Mirrors DrawRow but uses the
        // PRIMARY ceiling (StatCapCatalog.TryGetCap(..., isPrimary: true)) and has no delete
        // button (the primary always exists). Clicking the stat button opens the picker in
        // primary mode (see DrawPicker / _pickerIsPrimary).
        // [2026-09-29 10:29] Changed: the ceiling is read via TryGetEffectiveCap (mode-aware:
        // vanilla in Safe mode, 99x vanilla in Bypass mode).
        // [2026-09-30 02:47] Updated wording: Safe/Bypass -> Vanilla/Modded caps.
        private static void DrawPrimaryRow(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y + 4f, 26f, 26f), "P", _sSub);

            float pickX = x + 30f;
            if (GUI.Button(new Rect(pickX, y + 2f, 175f, 28f), _primaryStat.ToString(), _sBtnAction))
            {
                _pickerRow = -1;
                _pickerIsPrimary = true;
                _searchQuery = "";
                _pickerPage = 0;
            }

            float stepX = pickX + 182f;
            if (GUI.Button(new Rect(stepX, y + 2f, 34f, 28f), "-10", _sBtnAction))
                AdjustPrimaryVal(-10f);

            if (GUI.Button(new Rect(stepX + 36f, y + 2f, 30f, 28f), "-1", _sBtnAction))
                AdjustPrimaryVal(-1f);

            float textX = stepX + 72f;
            float curVal = 0f;
            float.TryParse(_primaryValueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out curVal);
            // [2026-09-29 10:29] Changed: TryGetCap -> TryGetEffectiveCap (mode-aware ceiling).
            bool hasCap = StatCapCatalog.TryGetEffectiveCap(_primaryStat, true, out float cap);
            bool overCap = hasCap && curVal > cap + 0.0001f;

            bool focused = (_focusedValueRow == PrimaryFocusIndex);
            string shown = focused ? _primaryValueStr + "|" : _primaryValueStr;
            GUIStyle valStyle = overCap ? _sValueFieldWarn : (focused ? _sValueFieldFocused : _sValueField);
            if (GUI.Button(new Rect(textX, y + 2f, 96f, 28f), shown, valStyle))
                _focusedValueRow = focused ? -1 : PrimaryFocusIndex;

            string capText = hasCap ? $"max {cap.ToString("0.#", CultureInfo.InvariantCulture)}" : "uncapped";
            GUI.Label(new Rect(textX, y + 30f, 96f, 14f), capText, overCap ? _sCapWarn : _sCapLabel);

            float addX = textX + 100f;
            if (GUI.Button(new Rect(addX, y + 2f, 30f, 28f), "+1", _sBtnAction))
                AdjustPrimaryVal(+1f);

            if (GUI.Button(new Rect(addX + 32f, y + 2f, 34f, 28f), "+10", _sBtnAction))
                AdjustPrimaryVal(+10f);

            float delX = addX + 70f;
            bool prevEnabled = GUI.enabled;
            GUI.enabled = hasCap;
            if (GUI.Button(new Rect(delX, y + 2f, 40f, 28f), "Max", _sBtnBlue))
            {
                _primaryValueStr = ((float)Math.Round(cap, 2)).ToString(CultureInfo.InvariantCulture);
                _focusedValueRow = -1;
            }
            GUI.enabled = prevEnabled;
        }

        private static void AdjustPrimaryVal(float delta)
        {
            if (!float.TryParse(_primaryValueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float cur))
                cur = 0f;
            float updated = (float)Math.Round(cur + delta, 2);
            if (updated < 0f && delta < 0f) updated = 0f;
            _primaryValueStr = updated.ToString(CultureInfo.InvariantCulture);
        }

        private static void AdjustVal(int index, float delta)
        {
            if (index < 0 || index >= _editValueStrings.Count) return;
            string str = _editValueStrings[index];
            if (!float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float cur))
                cur = 0f;
            float updated = (float)Math.Round(cur + delta, 2);
            if (updated < 0f && delta < 0f) updated = 0f;
            _editValueStrings[index] = updated.ToString(CultureInfo.InvariantCulture);
        }

        private static void Apply(Player player)
        {
            if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

            var parsed = new List<float>(_editStats.Count);
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
                parsed.Add((float)Math.Round(f, 2));
            }

            // [2026-09-27 12:23] Added: block values above the game's ceiling - those are flagged
            // by GearLegality and destroy the item on save. The Max button writes exactly the cap,
            // so this only catches manual over-typing.
            for (int i = 0; i < _editStats.Count; i++)
            {
                // [2026-09-29 10:29] Changed: TryGetCap -> TryGetEffectiveCap - the Apply guard
                // now blocks above the MODE cap (99x vanilla while Bypass mode is ON). The error
                // tail is mode-aware: in Safe mode the game would destroy the item; in Bypass
                // mode the destruction is patched out but 99x vanilla stays the hard guardrail.
                // [2026-09-30 02:47] Changed: tail "bypass guardrail"/"game would destroy" ->
                // "modded cap"/"vanilla cap"; destruction is now disabled by the standalone mod.
                if (StatCapCatalog.TryGetEffectiveCap(_editStats[i], false, out float cap) && parsed[i] > cap + 0.0001f)
                {
                    _statusMsg = $"<color=#FF5555>Error: {_editStats[i]} = {parsed[i].ToString("0.##", CultureInfo.InvariantCulture)} is above its cap of {cap.ToString("0.#", CultureInfo.InvariantCulture)} - {(StatCapCatalog.ModdedCaps ? "the modded cap (99x vanilla)." : "the vanilla cap.")}</color>";
                    _statusTimer = 5f;
                    return;
                }
            }

            // [2026-09-27 14:08] Added: parse + bound-check the primary value against the PRIMARY
            // ceiling (level 12), and reject a secondary that duplicates the (possibly changed)
            // primary stat. GearLegality.Inspect checks both against TryStatValueCeiling.
            if (!float.TryParse(_primaryValueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float primaryVal) ||
                float.IsNaN(primaryVal) || float.IsInfinity(primaryVal))
            {
                _statusMsg = $"<color=#FF5555>Error: Invalid primary value '{_primaryValueStr}'.</color>";
                _statusTimer = 4f;
                return;
            }
            primaryVal = (float)Math.Round(primaryVal, 2);
            // [2026-09-29 10:29] Changed: TryGetCap -> TryGetEffectiveCap (mode-aware, same as the
            // secondary check above); error tail is mode-aware too.
            // [2026-09-30 02:47] Changed: tail "bypass guardrail"/"game would destroy" -> modded/vanilla cap.
            if (StatCapCatalog.TryGetEffectiveCap(_primaryStat, true, out float primaryCap) && primaryVal > primaryCap + 0.0001f)
            {
                _statusMsg = $"<color=#FF5555>Error: primary {_primaryStat} = {primaryVal.ToString("0.##", CultureInfo.InvariantCulture)} is above its cap of {primaryCap.ToString("0.#", CultureInfo.InvariantCulture)} - {(StatCapCatalog.ModdedCaps ? "the modded cap (99x vanilla)." : "the vanilla cap.")}</color>";
                _statusTimer = 5f;
                return;
            }
            if (_editStats.Contains(_primaryStat))
            {
                _statusMsg = $"<color=#FF5555>Error: the primary stat '{_primaryStat}' is also used as a secondary.</color>";
                _statusTimer = 4f;
                return;
            }

            var seen = new HashSet<Runes.Stat>();
            for (int i = 0; i < _editStats.Count; i++)
            {
                if (!seen.Add(_editStats[i]))
                {
                    _statusMsg = $"<color=#FF5555>Error: Duplicate secondary '{_editStats[i]}' not allowed.</color>";
                    _statusTimer = 4f;
                    return;
                }
            }

            // Upgrade tiers are edited nowhere in this panel yet - carry the loaded
            // values through (padded to the stat count) so ApplyEntry sees aligned lists.
            var upgrades = new List<int>(_editStats.Count);
            for (int i = 0; i < _editStats.Count; i++)
                upgrades.Add(i < _editUpgrades.Count ? _editUpgrades[i] : 0);

            // [2026-09-26 02:46] Fixed: StatValues needs the game layout - slot 0 holds the
            // PRIMARY stat magnitude (now editable), slots 1..n the edited secondary values.
            /* [2026-09-26 02:46] Obsolete: values aligned at slot 0 (see SyncWorkingCopy note).
            var entry = new RuneEntry
            {
                SecondaryStats = new List<Runes.Stat>(_editStats),
                StatValues = parsed,
                StatUpgrades = upgrades,
            };
            */
            // [2026-09-27 14:08] Changed: write the edited primary value and the primary stat
            // identity (previously preserved-only, so identity could not change from the panel).
            /* [2026-09-27 14:08] Obsolete: preserved primary value, identity never written.
            var statValues = new List<float>(_editStats.Count + 1) { _primaryValue };
            statValues.AddRange(parsed);
            var entry = new RuneEntry
            {
                SecondaryStats = new List<Runes.Stat>(_editStats),
                StatValues = statValues,
                StatUpgrades = upgrades,
            };
            */
            var statValues = new List<float>(_editStats.Count + 1) { primaryVal };
            statValues.AddRange(parsed);
            var entry = new RuneEntry
            {
                PrimaryStat = _primaryStat,
                SecondaryStats = new List<Runes.Stat>(_editStats),
                StatValues = statValues,
                StatUpgrades = upgrades,
            };

            if (EditorEngine.ApplyUiEdit(player, _selectedSlot, entry, out string err))
            {
                _statusMsg = $"<color=#55FF55>Successfully modified {SlotName(_selectedSlot)}! Stats updated in-game.</color>";
            }
            else
            {
                _statusMsg = $"<color=#FF5555>Apply failed: {err}</color>";
            }
            _statusTimer = 4f;
        }

        // ------------------------------------------------------------------
        // Stat picker modal (paginated - BeginScrollView is stripped from Il2Cpp)
        // ------------------------------------------------------------------

        private static void DrawPicker(float px, float py, float pw, float ph)
        {
            float modalW = 420f;
            float modalH = 460f;
            /* [2026-09-26 11:14] Obsolete: the picker was pinned to the center of the panel.
               User request (2026-09-26): "make the ui draggable instead" - the window must be
               movable so it stops covering the panel behind it. First open still centers (see
               _pickerPos below); after that the title bar drags it around and the position
               persists while the mod is loaded.
            float mx = px + (pw - modalW) * 0.5f;
            float my = py + (ph - modalH) * 0.5f;
            */
            if (!_pickerPosSet)
            {
                _pickerPos = new Vector2(px + (pw - modalW) * 0.5f, py + (ph - modalH) * 0.5f);
                _pickerPosSet = true;
            }

            // [2026-09-26 11:14] Added: title-bar dragging. Event.current.mousePosition is in
            // the same virtual-canvas space as the rects (GUI.Button hit-testing under the
            // GUI.matrix scaling already relies on exactly this). The strip stops 40px short
            // of the right edge so the X button keeps receiving its own click. Movement is
            // clamped to the panel so the window can never be dragged out of reach.
            Event dragEvt = Event.current;
            if (dragEvt != null)
            {
                Rect dragStrip = new Rect(_pickerPos.x, _pickerPos.y, modalW - 40f, 32f);
                if (dragEvt.type == EventType.MouseDown && dragEvt.button == 0 &&
                    dragStrip.Contains(dragEvt.mousePosition))
                {
                    _pickerDragging = true;
                    _pickerDragOffset = dragEvt.mousePosition - _pickerPos;
                    dragEvt.Use();
                }
                else if (dragEvt.type == EventType.MouseDrag && _pickerDragging)
                {
                    Vector2 pos = dragEvt.mousePosition - _pickerDragOffset;
                    pos.x = Mathf.Clamp(pos.x, px, px + pw - modalW);
                    pos.y = Mathf.Clamp(pos.y, py, py + ph - modalH);
                    _pickerPos = pos;
                    dragEvt.Use();
                }
                else if (dragEvt.type == EventType.MouseUp && _pickerDragging)
                {
                    _pickerDragging = false;
                    dragEvt.Use();
                }
            }

            float mx = _pickerPos.x;
            float my = _pickerPos.y;

            GUI.Label(new Rect(mx, my, modalW, modalH), GUIContent.none, _sPickerBg);
            GUI.Label(new Rect(mx, my, modalW, 3f), GUIContent.none, _sDivider);

            // [2026-09-27 14:08] Changed: the picker header reflects a primary-stat target.
            string pickerTitle = _pickerIsPrimary
                ? "<b>CHOOSE PRIMARY STAT</b>"
                : $"<b>CHOOSE STAT (Affix #{_pickerRow + 1})</b>";
            GUI.Label(new Rect(mx + 15f, my + 10f, 300f, 22f), pickerTitle, _sTitle);

            if (GUI.Button(new Rect(mx + modalW - 35f, my + 10f, 25f, 22f), "X", _sCloseBtn))
            {
                _pickerRow = -1;
                // [2026-09-27 14:08] Added: clear the primary target when the picker closes.
                _pickerIsPrimary = false;
                // [2026-09-26 02:19] Added: drop search focus when the picker closes.
                _focusedSearch = false;
                return;
            }

            GUI.Label(new Rect(mx + 15f, my + 38f, 60f, 24f), "Search:", _sLabel);
            /* [2026-09-26 02:19] Obsolete: GUI.TextField - stripped from this Il2Cpp build,
               it aborted the frame here and left the picker unusable (same crash as DrawRow).
               Replaced by a focusable search box fed from HandleKeyboard.
            _searchQuery = GUI.TextField(new Rect(mx + 80f, my + 38f, modalW - 100f, 24f), _searchQuery, _sTextField);
            */
            string searchShown = _focusedSearch
                ? (_searchQuery.Length > 0 ? _searchQuery + "|" : "type to filter|")
                : (_searchQuery.Length > 0 ? _searchQuery : "click & type to filter");
            if (GUI.Button(new Rect(mx + 80f, my + 38f, modalW - 100f, 24f), searchShown,
                    _focusedSearch ? _sValueFieldFocused : _sValueField))
            {
                _focusedSearch = true;
            }

            var matches = Catalog.Filter(_searchQuery);
            float listY = my + 70f;

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

                // [2026-09-27 14:08] Changed: highlight the current primary when picking for it.
                bool isCurrent = _pickerIsPrimary
                    ? (_primaryStat == s)
                    : (_pickerRow >= 0 && _pickerRow < _editStats.Count && _editStats[_pickerRow] == s);
                string label = isCurrent ? $"<b><color=#55FF55>> {sName}</color></b>" : sName;

                if (GUI.Button(new Rect(mx + 15f, itemY, modalW - 30f, 26f), label, isCurrent ? _sSlotBtnActive : _sBtnAction))
                {
                    if (_pickerIsPrimary)
                    {
                        // [2026-09-27 14:08] Added: choosing a new primary stat re-seeds its value
                        // to the natural roll (the old value belonged to the old stat).
                        _primaryStat = s;
                        _primaryValueStr = NaturalSeed(s);
                    }
                    else if (_pickerRow >= 0 && _pickerRow < _editStats.Count)
                    {
                        _editStats[_pickerRow] = s;
                        // [2026-09-27 12:37] Changed: switching a row's stat re-seeds its value to
                        // the new stat's natural roll (the old stat's value/upgrades no longer apply).
                        if (_pickerRow < _editValueStrings.Count)
                            _editValueStrings[_pickerRow] = NaturalSeed(s);
                        if (_pickerRow < _editUpgrades.Count)
                            _editUpgrades[_pickerRow] = 0;
                    }
                    _pickerRow = -1;
                    _pickerIsPrimary = false;
                    _pickerPage = 0;
                    // [2026-09-26 02:19] Added: drop search focus when a stat is picked.
                    _focusedSearch = false;
                    break;
                }
            }

            float pageY = my + modalH - 35f;
            if (GUI.Button(new Rect(mx + 15f, pageY, 100f, 24f), "Prev Page", _sBtnAction))
            {
                if (_pickerPage > 0) _pickerPage--;
            }
            GUI.Label(new Rect(mx + 160f, pageY, 100f, 24f), $"Page {_pickerPage + 1} / {Mathf.Max(1, totalPages)}", _sLabel);
            if (GUI.Button(new Rect(mx + modalW - 115f, pageY, 100f, 24f), "Next Page", _sBtnAction))
            {
                if (_pickerPage < totalPages - 1) _pickerPage++;
            }
        }

        // ------------------------------------------------------------------
        // Slot & stat catalogs
        // ------------------------------------------------------------------

        public static class Catalog
        {
            public static readonly string[] SlotNames =
            {
                "Slot 0: Rune",
                "Slot 1: Undercoat",
                "Slot 2: Belt (Sash)",
                "Slot 3: Charm",
                "Slot 4: Ring 1",
                "Slot 5: Ring 2",
                "Slot 6: Amulet 1",
                "Slot 7: Amulet 2",
                "Slot 8: Badge 1",
                "Slot 9: Badge 2",
                "Slot 10: Bracer 1",
                "Slot 11: Bracer 2"
            };

            public static readonly List<Runes.Stat> AllStats = new();

            static Catalog()
            {
                var values = (Runes.Stat[])Enum.GetValues(typeof(Runes.Stat));
                var sorted = new List<string>();
                var dict = new Dictionary<string, Runes.Stat>(StringComparer.OrdinalIgnoreCase);

                foreach (var v in values)
                {
                    if (v == Runes.Stat.None) continue;
                    string name = v.ToString();
                    sorted.Add(name);
                    dict[name] = v;
                }
                sorted.Sort(StringComparer.OrdinalIgnoreCase);

                foreach (var s in sorted)
                    AllStats.Add(dict[s]);
            }

            public static List<Runes.Stat> Filter(string query)
            {
                if (string.IsNullOrWhiteSpace(query)) return AllStats;
                string q = query.Trim();
                var list = new List<Runes.Stat>();
                foreach (var s in AllStats)
                {
                    if (s.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        list.Add(s);
                }
                return list;
            }
        }

        // ------------------------------------------------------------------
        // Styles (ported from the module; textures created once)
        // ------------------------------------------------------------------

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

            /* [2026-09-26 02:19] Obsolete: _sTextField styled the GUI.TextField calls, which are
               stripped from this Il2Cpp build (see HandleKeyboard). The focusable value/search
               boxes use _sValueField / _sValueFieldFocused below instead.
            _sTextField = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            */

            // [2026-09-26 02:19] Added: focusable value/search box styles (button-based,
            // replacement for the stripped GUI.TextField).
            _sValueField = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };

            _sValueFieldFocused = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };

            _sValueField.normal.background = MakeTex(1, 1, new Color(0.20f, 0.18f, 0.15f, 1f));
            _sValueField.hover.background = MakeTex(1, 1, new Color(0.28f, 0.25f, 0.20f, 1f));
            _sValueField.active.background = MakeTex(1, 1, new Color(0.28f, 0.25f, 0.20f, 1f));
            _sValueFieldFocused.normal.background = MakeTex(1, 1, new Color(0.95f, 0.88f, 0.35f, 1f));
            _sValueFieldFocused.hover.background = MakeTex(1, 1, new Color(0.95f, 0.88f, 0.35f, 1f));
            _sValueFieldFocused.active.background = MakeTex(1, 1, new Color(0.95f, 0.88f, 0.35f, 1f));

            // [2026-09-27 12:23] Added: red value box + red cap label for over-cap values.
            _sValueFieldWarn = new GUIStyle(_sValueField) { normal = { textColor = Color.white } };
            _sValueFieldWarn.normal.background = MakeTex(1, 1, new Color(0.62f, 0.16f, 0.16f, 1f));
            _sValueFieldWarn.hover.background = MakeTex(1, 1, new Color(0.72f, 0.20f, 0.20f, 1f));
            _sValueFieldWarn.active.background = MakeTex(1, 1, new Color(0.72f, 0.20f, 0.20f, 1f));

            _sCapLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.60f, 0.58f, 0.55f, 1f) }
            };
            _sCapWarn = new GUIStyle(_sCapLabel)
            {
                normal = { textColor = new Color(1f, 0.42f, 0.42f, 1f) }
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
