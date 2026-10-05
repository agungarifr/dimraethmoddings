using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DimraethSanctumChests
{
    // Self-contained UI child attached to the friend-made map quick-actions panel.
    // It builds a capped, scrollable list under the existing buttons and rebuilds its
    // rows every time the map panel becomes visible, so renaming/placing chests is
    // reflected the next time the map is opened.
    public sealed class ChestButtonPanel : MonoBehaviour
    {
        public ChestButtonPanel(IntPtr ptr) : base(ptr) { }

        private RectTransform _content;
        private TextMeshProUGUI _fontDonor;
        private readonly List<GameObject> _rows = new List<GameObject>();

        private void OnEnable()
        {
            try { Rebuild(); }
            catch (Exception ex) { Plugin.Log?.LogError($"Sanctum chest panel error: {ex}"); }
        }

        private void Rebuild()
        {
            EnsureBuilt();
            ClearRows();

            var chests = ChestDirectory.GetSanctumChests();
            if (chests.Count == 0)
            {
                AddRow(null, "No sanctum chests", false);
                return;
            }

            // [2026-10-02] Every listed chest is clickable, even when its GameObject is
            // despawned because the player is outside the Sanctum; clicking asks the host
            // to respawn the base and then opens it (see ChestDirectory.Open).
            foreach (var chest in chests)
                AddRow(chest, chest.Label, true);
        }

        private void EnsureBuilt()
        {
            if ((bool)_content) return;

            var root = transform;
            try { _fontDonor = root.GetComponentInChildren<TextMeshProUGUI>(true); } catch { }

            // Outer panel, placed just below the friend's own buttons/status line.
            var panel = new GameObject("SanctumChests");
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.SetParent(root, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -272f);
            panelRect.sizeDelta = new Vector2(190f, Mathf.Max(90f, Plugin.PanelHeight));

            var panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.07f, 0.09f, 0.12f, 0.85f);

            // Header.
            var headerGo = new GameObject("Header");
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.SetParent(panelRect, false);
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(6f, -2f);
            headerRect.sizeDelta = new Vector2(-12f, 18f);

            var header = headerGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(header);
            header.text = "Sanctum Chests";
            header.fontSize = 13f;
            header.color = new Color(1f, 0.82f, 0.4f, 1f);
            header.alignment = TextAlignmentOptions.Left;
            header.raycastTarget = false;

            // Scroll view (viewport + content).
            var scrollGo = new GameObject("Scroll");
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.SetParent(panelRect, false);
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(4f, 4f);
            scrollRect.offsetMax = new Vector2(-14f, -22f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = new GameObject("Viewport");
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.SetParent(scrollRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewportRect;

            var content = new GameObject("Content");
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2f;
            layout.padding = new RectOffset(2, 2, 2, 2);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;

            _content = contentRect;

            // A visible drag handle is important because the map screen also uses the
            // scroll wheel, which can steal wheel events from the list.
            var trackGo = new GameObject("Scrollbar");
            var trackRect = trackGo.AddComponent<RectTransform>();
            trackRect.SetParent(panelRect, false);
            trackRect.anchorMin = new Vector2(1f, 0f);
            trackRect.anchorMax = new Vector2(1f, 1f);
            trackRect.pivot = new Vector2(1f, 0.5f);
            trackRect.offsetMin = new Vector2(-12f, 4f);
            trackRect.offsetMax = new Vector2(-4f, -22f);

            var trackImg = trackGo.AddComponent<Image>();
            trackImg.color = new Color(0f, 0f, 0f, 0.45f);

            var scrollbar = trackGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slideGo = new GameObject("Sliding Area");
            var slideRect = slideGo.AddComponent<RectTransform>();
            slideRect.SetParent(trackRect, false);
            slideRect.anchorMin = Vector2.zero;
            slideRect.anchorMax = Vector2.one;
            slideRect.offsetMin = slideRect.offsetMax = Vector2.zero;

            var handleGo = new GameObject("Handle");
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.SetParent(slideRect, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;

            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.65f, 0.7f, 0.8f, 0.9f);

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImg;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        private void AddRow(ChestDirectory.ChestInfo chest, string text, bool interactable)
        {
            var row = new GameObject("ChestRow");
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.SetParent(_content, false);

            var element = row.AddComponent<LayoutElement>();
            element.preferredHeight = 28f;
            element.minHeight = 28f;
            element.flexibleHeight = 0f;

            var bg = row.AddComponent<Image>();
            bg.color = interactable
                ? new Color(0.105f, 0.145f, 0.19f, 0.95f)
                : new Color(0.12f, 0.12f, 0.12f, 0.5f);

            if (interactable && chest != null)
            {
                var button = row.AddComponent<Button>();
                button.targetGraphic = bg;

                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.3f, 1.25f, 1.15f, 1f);
                colors.pressedColor = new Color(0.72f, 0.78f, 0.88f, 1f);
                button.colors = colors;

                var captured = chest;
                button.onClick.AddListener((Action)(() =>
                {
                    try { ChestDirectory.Open(captured); }
                    catch (Exception ex) { Plugin.Log?.LogError($"Sanctum chest open failed: {ex}"); }
                }));
            }

            var labelGo = new GameObject("Label");
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.SetParent(rowRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 1f);
            labelRect.offsetMax = new Vector2(-6f, -1f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(label);
            label.text = text;
            label.fontSize = 13f;
            label.color = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;

            _rows.Add(row);
        }

        private void ApplyFont(TextMeshProUGUI label)
        {
            try
            {
                if ((bool)_fontDonor && (bool)_fontDonor.font)
                {
                    label.font = _fontDonor.font;
                    return;
                }
            }
            catch { }

            try
            {
                if (TMP_Settings.defaultFontAsset != null)
                    label.font = TMP_Settings.defaultFontAsset;
            }
            catch { }
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                try { if ((bool)_rows[i]) UnityEngine.Object.Destroy(_rows[i]); }
                catch { }
            }
            _rows.Clear();
        }
    }
}
