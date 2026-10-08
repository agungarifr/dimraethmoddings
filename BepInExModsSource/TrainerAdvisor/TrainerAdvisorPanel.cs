using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DimraethTrainerAdvisor
{
    // Display-only panel injected into the vanilla trainer window. It is parented under the
    // NPCTrainView's _panelRoot so it shows and hides with the window, reads the local player's
    // current level / XP / attributes, and prints the optimal physical, magic and hybrid spends.
    // It never buys, stages, or writes any point.
    public sealed class TrainerAdvisorPanel : MonoBehaviour
    {
        public TrainerAdvisorPanel(IntPtr ptr) : base(ptr) { }

        private NPCTrainView _view;
        private Player _player;
        private RectTransform _panelRect;
        private TextMeshProUGUI _text;
        private TextMeshProUGUI _fontDonor;
        private float _nextRefresh;
        private string _lastKey;

        public void Configure(NPCTrainView view, Player player)
        {
            _view = view;
            _player = player;
            EnsureBuilt();
            Refresh(force: true);
        }

        private void Update()
        {
            if (_text == null || _player == null) return;
            float now = Time.unscaledTime;
            if (now < _nextRefresh) return;
            _nextRefresh = now + 0.25f;
            Refresh(force: false);
        }

        private void EnsureBuilt()
        {
            if ((bool)_panelRect) return;

            Transform parent = null;
            try
            {
                if (_view != null && (bool)_view._panelRoot) parent = _view._panelRoot.transform;
                else if (_view != null) parent = _view.transform;
            }
            catch { }
            if (parent == null) parent = transform;

            try { _fontDonor = parent.GetComponentInChildren<TextMeshProUGUI>(true); } catch { }

            float width = Plugin.PanelWidth;
            try
            {
                var pr = parent as RectTransform;
                if ((bool)pr && pr.rect.width > 1f)
                {
                    // Never exceed ~40% of the trainer window if we dock inside it.
                    if (Plugin.AnchorInside) width = Mathf.Min(width, pr.rect.width * 0.4f);
                }
            }
            catch { }

            var panel = new GameObject("TrainerAdvisor");
            _panelRect = panel.AddComponent<RectTransform>();
            _panelRect.SetParent(parent, false);

            if (Plugin.AnchorInside)
            {
                _panelRect.anchorMin = new Vector2(1f, 0f);
                _panelRect.anchorMax = new Vector2(1f, 1f);
                _panelRect.pivot = new Vector2(1f, 0.5f);
                _panelRect.anchoredPosition = new Vector2(-8f, 0f);
                _panelRect.sizeDelta = new Vector2(width, -16f);
            }
            else
            {
                // Just outside the window's right edge, top-aligned, so it never covers the
                // vanilla trainer layout.
                _panelRect.anchorMin = new Vector2(1f, 1f);
                _panelRect.anchorMax = new Vector2(1f, 1f);
                _panelRect.pivot = new Vector2(0f, 1f);
                _panelRect.anchoredPosition = new Vector2(12f, 0f);
                _panelRect.sizeDelta = new Vector2(width, Plugin.PanelHeight);
            }

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.11f, 0.92f);
            bg.raycastTarget = false;

            // Keep any layout group on the trainer window from repositioning or sizing us.
            var layoutElement = panel.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var textGo = new GameObject("Text");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(_panelRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            _text = textGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(_text);
            _text.fontSize = 13f;
            _text.color = new Color(0.92f, 0.91f, 0.88f, 1f);
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.raycastTarget = false;
            _text.enableWordWrapping = true;
            _text.overflowMode = TextOverflowModes.Overflow;
        }

        private void ApplyFont(TextMeshProUGUI label)
        {
            try
            {
                if ((bool)_fontDonor && (bool)_fontDonor.font) { label.font = _fontDonor.font; return; }
            }
            catch { }
            try
            {
                if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            }
            catch { }
        }

        private void Refresh(bool force)
        {
            try
            {
                AdvisorState state = ReadState();
                if (state == null) return;
                string key = BuildKey(state);
                if (!force && key == _lastKey) return;
                _lastKey = key;
                _text.text = BuildText(state);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Trainer advisor refresh failed: {ex}");
            }
        }

        private AdvisorState ReadState()
        {
            if (_player == null) return null;

            var attrsVar = _player.Attributes;
            if (attrsVar == null) return null;
            var attrs = attrsVar.Value;
            if (attrs == null) return null;

            return new AdvisorState
            {
                Race = _player.Race.Value,
                Class = _player.Class.Value,
                Level = _player.Level.Value,
                AccumulatedXp = _player.AccumulatedXP.Value,
                AllTimeXp = _player.AllTimeXP.Value,
                Attributes = new[]
                {
                    attrs.Memory, attrs.Charisma, attrs.Adventure, attrs.Physique,
                    attrs.Intelligence, attrs.Agility, attrs.Strength, attrs.Energy,
                },
            };
        }

        private static string BuildKey(AdvisorState s)
        {
            var sb = new StringBuilder();
            sb.Append(s.Race).Append('|').Append(s.Class).Append('|')
              .Append(s.Level).Append('|').Append(s.AccumulatedXp).Append('|').Append(s.AllTimeXp);
            for (int i = 0; i < 8; i++) sb.Append('|').Append(s.Attributes[i]);
            return sb.ToString();
        }

        private static string BuildText(AdvisorState state)
        {
            var sb = new StringBuilder();

            sb.Append("<b><color=#F2C14E>Trainer Advisor</color></b>\n");
            sb.Append("<size=86%>").Append(RaceName(state.Race)).Append(" ").Append(ClassName(state.Class))
              .Append("  ·  Level ").Append(state.Level).Append("/").Append(AttributeOptimizer.LevelCap).Append("</size>\n");

            int toCap = AttributeOptimizer.RemainingXpToCap(state);
            int req = state.Level < AttributeOptimizer.LevelCap
                ? AttributeOptimizer.XpRequiredForLevel(state.Level) : 0;
            sb.Append("<size=86%>XP spent ").Append(state.AllTimeXp.ToString("N0"))
              .Append("   ·   to cap ").Append(toCap.ToString("N0"));
            if (req > 0) sb.Append("   ·   bar ").Append(state.AccumulatedXp).Append('/').Append(req);
            sb.Append("</size>\n");

            double nowPatk = AttributeOptimizer.PhysicalCombatPower(state.Attributes);
            double nowMatk = AttributeOptimizer.MagicCombatPower(state.Attributes);
            sb.Append("<size=86%>Now:  Patk <color=#FF8A5C>").Append(nowPatk.ToString("0.0"))
              .Append("</color>   Matk <color=#B07DFF>").Append(nowMatk.ToString("0.0")).Append("</color></size>\n\n");

            if (!AttributeOptimizer.SupportsBuild(state.Race, state.Class))
            {
                sb.Append("<size=86%><i>Optimizer unavailable for this race/class.</i></size>");
                return sb.ToString();
            }

            if (state.Level >= AttributeOptimizer.LevelCap)
            {
                sb.Append("<b><color=#6FCE8F>Level cap reached</color></b>\n");
                sb.Append("<size=86%>No further points can be bought.</size>\n\n");
            }
            else
            {
                AppendRecommendation(sb, state, DamageFocus.Physical, "#FF8A5C", "PHYSICAL");
                sb.Append('\n');
                AppendRecommendation(sb, state, DamageFocus.Magic, "#B07DFF", "MAGIC");
                sb.Append('\n');
                AppendRecommendation(sb, state, DamageFocus.Hybrid, "#6FCE8F", "HYBRID (max total)");
                sb.Append('\n');
            }

            AppendWarnings(sb, state);
            sb.Append("<size=78%><i>Display only — does not spend points.</i></size>");
            return sb.ToString();
        }

        private static void AppendRecommendation(StringBuilder sb, AdvisorState state, DamageFocus focus, string color, string title)
        {
            var rec = AttributeOptimizer.Recommend(state, focus);
            if (rec == null) return;

            sb.Append("<b><color=").Append(color).Append('>').Append(title).Append("</color></b>  ");
            if (rec.PointsBought <= 0)
            {
                sb.Append("<size=86%>(nothing left to buy)</size>");
                return;
            }

            sb.Append("<size=86%>");
            bool first = true;
            for (int i = 0; i < 8; i++)
            {
                if (rec.AddedPoints[i] <= 0) continue;
                if (!first) sb.Append(", ");
                sb.Append(AttributeOptimizer.AttributeName(i)).Append(" +").Append(rec.AddedPoints[i]);
                first = false;
            }
            sb.Append("</size>\n");

            sb.Append("<size=86%>  ");
            if (focus == DamageFocus.Hybrid)
            {
                sb.Append("Patk ").Append(AttributeOptimizer.PhysicalCombatPower(rec.FinalAttributes).ToString("0.0"))
                  .Append(" · Matk ").Append(AttributeOptimizer.MagicCombatPower(rec.FinalAttributes).ToString("0.0"))
                  .Append(" · total ").Append(rec.CombatPower.ToString("0.0"));
            }
            else
            {
                string label = focus == DamageFocus.Physical ? "Patk" : "Matk";
                string other = focus == DamageFocus.Physical ? "Matk" : "Patk";
                sb.Append(label).Append(" <b>").Append(rec.CombatPower.ToString("0.0")).Append("</b>  (")
                  .Append(other).Append(' ').Append(rec.SecondaryPower.ToString("0.0")).Append(')');
            }
            sb.Append("  → Lv ").Append(rec.EndLevel).Append("  ·  ").Append(rec.SpentXp.ToString("N0")).Append(" XP");
            sb.Append("</size>");
        }

        private static void AppendWarnings(StringBuilder sb, AdvisorState state)
        {
            int expected = AttributeOptimizer.ExpectedLifetimeXp(state);
            if (state.AllTimeXp != expected)
            {
                sb.Append("<color=#FF6B6B><size=82%>⚠ XP/level mismatch: spent ")
                  .Append(state.AllTimeXp.ToString("N0")).Append(" vs level implies ")
                  .Append(expected.ToString("N0")).Append("</size></color>\n");
            }

            // Points above the starting spread in attributes that never feed damage.
            int[] start = StartingAttributes(state.Race, state.Class);
            int wasted = 0;
            for (int i = 0; i < 8; i++)
            {
                if (i == 1 || i == 2 || i == 4 || i == 5 || i == 6) continue;
                int over = state.Attributes[i] - start[i];
                if (over > 0) wasted += over;
            }
            if (wasted > 0)
            {
                sb.Append("<color=#FFB454><size=82%>⚠ ").Append(wasted)
                  .Append(" point(s) spent on stats that give no combat power (Mem/Phy/Ene)</size></color>\n");
            }
        }

        private static int[] StartingAttributes(int race, int cls)
        {
            // Mirrors AttributeOptimizer's table; kept here only for the warning line.
            int[] start = new int[8];
            switch (race)
            {
                case 1: start = new[] { 3, 3, 3, 3, 3, 3, 3, 3 }; break;
                case 2: start = new[] { 3, 3, 3, 1, 3, 5, 3, 3 }; break;
                case 3: start = new[] { 2, 2, 3, 4, 1, 3, 5, 4 }; break;
                default: return new int[8];
            }
            int[] cb;
            switch (cls)
            {
                case 1: cb = new[] { 4, 4, 3, 2, 5, 2, 1, 3 }; break;
                case 4: cb = new[] { 2, 2, 3, 4, 1, 3, 5, 4 }; break;
                case 5: cb = new[] { 3, 3, 3, 1, 3, 5, 3, 3 }; break;
                default: return new int[8];
            }
            for (int i = 0; i < 8; i++) start[i] += cb[i];
            return start;
        }

        private static string RaceName(int race)
        {
            switch (race)
            {
                case 1: return "Human";
                case 2: return "Elf";
                case 3: return "Minotaur";
                default: return "Race " + race;
            }
        }

        private static string ClassName(int cls)
        {
            switch (cls)
            {
                case 1: return "Magician";
                case 2: return "Shaman";
                case 3: return "Bulwark";
                case 4: return "Brawler";
                case 5: return "Shadow";
                case 6: return "Builder";
                default: return "Class " + cls;
            }
        }
    }
}
