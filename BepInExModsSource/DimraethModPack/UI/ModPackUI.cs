using System;
using System.Collections.Generic;
using DimraethModPack.Core;
using DimraethModPack.Modules.SystemMod;
using UnityEngine;

namespace DimraethModPack.UI
{
    public class ModPackUI
    {
        public bool Visible = false;
        private int _selectedCategory = 0; // 0=Stats, 1=Gameplay, 2=Loot, 3=System
        private readonly string[] _categories = { "Stats", "Gameplay", "Loot", "System" };
        private Vector2 _scrollPos = Vector2.zero;

        private readonly List<ModModuleBase> _modules;
        private readonly List<Texture2D> _textures = new();

        // Styles
        private bool _stylesReady = false;
        private GUIStyle _sPanelBg;
        private GUIStyle _sTitle;
        private GUIStyle _sSub;
        private GUIStyle _sTabActive;
        private GUIStyle _sTabInactive;
        private GUIStyle _sModTitle;
        private GUIStyle _sModDesc;
        private GUIStyle _sLabel;
        private GUIStyle _sBtnOn;
        private GUIStyle _sBtnOff;
        private GUIStyle _sBtnAction;
        private GUIStyle _sCloseBtn;
        private GUIStyle _sDivider;

        private string _statusMsg = "";
        private float _statusTimer = 0f;

        public ModPackUI(List<ModModuleBase> modules)
        {
            _modules = modules;
        }

        public void Toggle()
        {
            Visible = !Visible;
            if (Visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (_stylesReady) RebuildTextures();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void Draw()
        {
            if (!Visible) return;
            InitStyles();

            int savedDepth = GUI.depth;
            bool savedEnabled = GUI.enabled;
            try
            {
                GUI.depth = -2000;
                GUI.enabled = true;

                float w = 640f;
                float h = 730f;
                float x = (Screen.width - w) * 0.5f;
                float y = (Screen.height - h) * 0.5f;

                // Background panel
                GUI.Label(new Rect(x, y, w, h), GUIContent.none, _sPanelBg);

                // Top Accent bar
                GUI.Label(new Rect(x, y, w, 4f), GUIContent.none, _sDivider);

                // Title & Subtitle
                GUI.Label(new Rect(x + 20f, y + 10f, 350f, 24f), "DIMRAETH MODPACK MANAGER", _sTitle);
                string diagInfo = $"<color=#88FFAA>FPS: {DiagnosticsManager.CurrentFps:F0}</color> (1% Low: <color=#FFAA55>{DiagnosticsManager.OnePercentLowFps:F0}</color>) | " +
                                  $"Stutters: <color={(DiagnosticsManager.StutterSpikeCount > 0 ? "#FF5555" : "#55FF55")}>{DiagnosticsManager.StutterSpikeCount}</color> | " +
                                  $"RAM: <color=#FFEE66>{DiagnosticsManager.ManagedRamMb:F0} MB</color>";
                GUI.Label(new Rect(x + 20f, y + 34f, 480f, 18f), diagInfo, _sSub);
                GUI.Label(new Rect(x + 20f, y + 50f, 480f, 16f), "<color=#888888>`: Toggle Menu | Real-time: Changes apply immediately</color>", _sSub);

                // Reload CFG button (reads changes if edited externally from Notepad)
                if (GUI.Button(new Rect(x + w - 175f, y + 15f, 125f, 26f), "↻ Reload Disk CFG", _sBtnAction))
                {
                    try
                    {
                        DimraethModPackPlugin.Instance?.Config?.Reload();
                        _statusMsg = "<color=#55FF55>Config reloaded successfully from disk!</color>";
                        _statusTimer = 3.0f;
                    }
                    catch (Exception ex)
                    {
                        _statusMsg = $"<color=#FF5555>Failed to reload: {ex.Message}</color>";
                        _statusTimer = 3.0f;
                    }
                }

                // Close button [X]
                if (GUI.Button(new Rect(x + w - 45f, y + 15f, 30f, 26f), "X", _sCloseBtn))
                {
                    Toggle();
                    return;
                }

                // Category Tabs
                float tabY = y + 70f;
                float tabW = (w - 40f) / _categories.Length;
                for (int i = 0; i < _categories.Length; i++)
                {
                    bool isCur = (_selectedCategory == i);
                    Rect tabRect = new Rect(x + 20f + i * tabW, tabY, tabW - 4f, 32f);
                    if (GUI.Button(tabRect, _categories[i].ToUpper(), isCur ? _sTabActive : _sTabInactive))
                    {
                        _selectedCategory = i;
                        _scrollPos = Vector2.zero;
                    }
                }

                // Category List Container
                float listX = x + 20f;
                float listY = tabY + 42f;
                float listW = w - 40f;
                float listH = h - 145f;

                Rect viewRect = new Rect(listX, listY, listW, listH);

                // Filter modules by category
                string currentCatName = _categories[_selectedCategory];
                var catMods = _modules.FindAll(m => string.Equals(m.Category, currentCatName, StringComparison.OrdinalIgnoreCase));

                // Mouse wheel scrolling
                if (viewRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
                {
                    _scrollPos.y += Event.current.delta.y * 28f;
                    if (_scrollPos.y < 0f) _scrollPos.y = 0f;
                    Event.current.Use();
                }

                GUI.BeginGroup(viewRect);

                float curY = -_scrollPos.y;
                for (int i = 0; i < catMods.Count; i++)
                {
                    var mod = catMods[i];

                    // Header row: ON/OFF toggle + Mod Name
                    bool isModOn = mod.IsEnabled;
                    string toggleText = isModOn ? "● ON" : "○ OFF";
                    if (GUI.Button(new Rect(5f, curY + 4f, 65f, 26f), toggleText, isModOn ? _sBtnOn : _sBtnOff))
                    {
                        if (mod.Enabled != null)
                            mod.Enabled.Value = !mod.Enabled.Value;
                    }

                    GUI.Label(new Rect(80f, curY + 6f, listW - 90f, 24f), mod.Name, _sModTitle);
                    curY += 32f;

                    // Short description
                    GUI.Label(new Rect(80f, curY, listW - 90f, 20f), mod.Description, _sModDesc);
                    curY += 24f;

                    // If mod is enabled, render its settings controls
                    if (isModOn)
                    {
                        float consumed = mod.DrawSettings(80f, curY, listW - 90f, _sLabel, _sBtnAction);
                        curY += consumed;
                    }

                    curY += 10f;
                    // Divider line
                    GUI.Label(new Rect(5f, curY, listW - 10f, 1f), GUIContent.none, _sDivider);
                    curY += 12f;
                }

                // Clamp max scroll
                float totalContentHeight = curY + _scrollPos.y;
                float maxScroll = Mathf.Max(0f, totalContentHeight - listH + 20f);
                if (_scrollPos.y > maxScroll) _scrollPos.y = maxScroll;

                GUI.EndGroup();

                // Bottom bar info
                if (_statusTimer > 0f)
                {
                    _statusTimer -= Time.unscaledDeltaTime;
                    GUI.Label(new Rect(x + 20f, y + h - 24f, w - 40f, 18f), _statusMsg, _sLabel);
                }
                else
                {
                    GUI.Label(new Rect(x + 20f, y + h - 24f, w - 40f, 18f), $"Category: {currentCatName} ({catMods.Count} mods) | Settings apply in real-time", _sSub);
                }
            }
            finally
            {
                GUI.depth = savedDepth;
                GUI.enabled = savedEnabled;
            }
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.hideFlags = HideFlags.DontSave;
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            t.SetPixels(pixels);
            t.Apply();
            _textures.Add(t);
            return t;
        }

        private void RebuildTextures()
        {
            foreach (var t in _textures)
            {
                try { if (t != null) UnityEngine.Object.Destroy(t); } catch { }
            }
            _textures.Clear();

            _sPanelBg.normal.background = MakeTex(1, 1, new Color(0.12f, 0.10f, 0.08f, 0.96f));
            _sDivider.normal.background = MakeTex(1, 1, new Color(0.82f, 0.68f, 0.12f, 1f));

            _sTabActive.normal.background = MakeTex(1, 1, new Color(0.82f, 0.68f, 0.12f, 1f));
            _sTabInactive.normal.background = MakeTex(1, 1, new Color(0.22f, 0.18f, 0.14f, 0.9f));

            _sBtnOn.normal.background = MakeTex(1, 1, new Color(0.18f, 0.55f, 0.22f, 1f));
            _sBtnOff.normal.background = MakeTex(1, 1, new Color(0.35f, 0.18f, 0.18f, 1f));
            _sCloseBtn.normal.background = MakeTex(1, 1, new Color(0.65f, 0.20f, 0.20f, 1f));
            _sBtnAction.normal.background = MakeTex(1, 1, new Color(0.28f, 0.25f, 0.22f, 1f));
        }

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _sPanelBg = new GUIStyle(GUI.skin.label);
            _sDivider = new GUIStyle(GUI.skin.label);

            _sTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.80f, 0.35f, 1f) }
            };

            _sSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.70f, 0.68f, 0.65f, 1f) }
            };

            _sTabActive = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };

            _sTabInactive = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.85f, 0.82f, 0.78f, 1f) }
            };

            _sModTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.92f, 0.85f, 1f) }
            };

            _sModDesc = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.68f, 0.65f, 0.60f, 1f) }
            };

            _sLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                normal = { textColor = new Color(0.85f, 0.82f, 0.78f, 1f) }
            };

            _sBtnOn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sBtnOff = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
            };

            _sBtnAction = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _sCloseBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            RebuildTextures();
        }
    }
}
