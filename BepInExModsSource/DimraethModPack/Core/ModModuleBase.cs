using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Core
{
    public abstract class ModModuleBase
    {
        public abstract string Category { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }

        public ConfigEntry<bool> Enabled { get; protected set; }
        public bool IsEnabled => Enabled != null && Enabled.Value;

        public abstract void BindConfig(ConfigFile config);
        public abstract void ApplyPatches(Harmony harmony);
        public abstract float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle);

        // Helper UI methods for simple, clean drawing with explicit Rects
        protected float DrawToggle(float x, float y, float width, string label, ConfigEntry<bool> entry, string vanillaInfo, GUIStyle labelStyle, GUIStyle btnStyle, Action<bool> onChanged = null)
        {
            if (entry == null) return 0f;
            GUI.Label(new Rect(x, y, width - 100f, 24f), $"{label}: {(entry.Value ? "<color=#55FF55>ON</color>" : "<color=#FF6666>OFF</color>")} <color=#888888>({vanillaInfo})</color>", labelStyle);
            if (GUI.Button(new Rect(x + width - 90f, y, 85f, 24f), entry.Value ? "Set OFF" : "Set ON", btnStyle))
            {
                entry.Value = !entry.Value;
                onChanged?.Invoke(entry.Value);
            }
            return 28f;
        }

        protected float DrawFloatSpinner(float x, float y, float width, string label, ConfigEntry<float> entry, float step, float min, float max, string vanillaInfo, GUIStyle labelStyle, GUIStyle btnStyle, string format = "0.##", Action<float> onChanged = null)
        {
            if (entry == null) return 0f;
            /* [2026-09-29 01:22] Obsolete: old 2-button (-/+ single-step) layout below replaced by the
               4-button dual-precision layout (big step = 10x `step`, small step = `step`) per user request:
               "stepper +- 10 di tiap module". The old single-step buttons made reaching high values tedious
               (e.g. EXP Multiplier climbing by +1). Old code kept here per repo rule.
            GUI.Label(new Rect(x, y, width - 100f, 24f), $"{label}: <color=#FFDD44>{entry.Value.ToString(format)}</color> <color=#888888>({vanillaInfo})</color>", labelStyle);
            float prev = entry.Value;
            if (GUI.Button(new Rect(x + width - 90f, y, 40f, 24f), "-", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value - step, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 45f, y, 40f, 24f), "+", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value + step, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            */
            float bigStep = step * 10f;
            GUI.Label(new Rect(x, y, width - 175f, 24f), $"{label}: <color=#FFDD44>{entry.Value.ToString(format)}</color> <color=#888888>({vanillaInfo})</color>", labelStyle);
            float prev = entry.Value;
            if (GUI.Button(new Rect(x + width - 170f, y, 40f, 24f), $"-{bigStep.ToString("0.##")}", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value - bigStep, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 126f, y, 36f, 24f), $"-{step.ToString("0.##")}", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value - step, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 86f, y, 36f, 24f), $"+{step.ToString("0.##")}", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value + step, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 46f, y, 40f, 24f), $"+{bigStep.ToString("0.##")}", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value + bigStep, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            return 28f;
        }

        protected float DrawIntSpinner(float x, float y, float width, string label, ConfigEntry<int> entry, int step, int min, int max, string vanillaInfo, GUIStyle labelStyle, GUIStyle btnStyle, Action<int> onChanged = null)
        {
            if (entry == null) return 0f;
            /* [2026-09-29 01:22] Obsolete: old 2-button (-/+ single-step) layout below replaced by the
               4-button dual-precision layout (big step = 10x `step`, small step = `step`) per user request:
               "stepper +- 10 di tiap module". With step=1 this yields literal -10/-1/+1/+10 buttons (same
               style as the existing DrawIntDualSpinner). Old code kept here per repo rule.
            GUI.Label(new Rect(x, y, width - 100f, 24f), $"{label}: <color=#FFDD44>{entry.Value}</color> <color=#888888>({vanillaInfo})</color>", labelStyle);
            int prev = entry.Value;
            if (GUI.Button(new Rect(x + width - 90f, y, 40f, 24f), "-", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value - step, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 45f, y, 40f, 24f), "+", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value + step, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            */
            int bigStep = step * 10;
            GUI.Label(new Rect(x, y, width - 175f, 24f), $"{label}: <color=#FFDD44>{entry.Value}</color> <color=#888888>({vanillaInfo})</color>", labelStyle);
            int prev = entry.Value;
            if (GUI.Button(new Rect(x + width - 170f, y, 40f, 24f), $"-{bigStep}", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value - bigStep, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 126f, y, 36f, 24f), $"-{step}", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value - step, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 86f, y, 36f, 24f), $"+{step}", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value + step, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 46f, y, 40f, 24f), $"+{bigStep}", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value + bigStep, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            return 28f;
        }

        protected float DrawIntDualSpinner(float x, float y, float width, string label, ConfigEntry<int> entry, int min, int max, string vanillaInfo, GUIStyle labelStyle, GUIStyle btnStyle, Action<int> onChanged = null)
        {
            if (entry == null) return 0f;
            GUI.Label(new Rect(x, y, width - 175f, 24f), $"{label}: <color=#FFDD44>{entry.Value}</color> <color=#888888>({vanillaInfo})</color>", labelStyle);
            int prev = entry.Value;

            if (GUI.Button(new Rect(x + width - 170f, y, 40f, 24f), "-10", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value - 10, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 126f, y, 36f, 24f), "-1", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value - 1, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 86f, y, 36f, 24f), "+1", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value + 1, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 46f, y, 40f, 24f), "+10", btnStyle))
            {
                entry.Value = Math.Clamp(entry.Value + 10, min, max);
                if (entry.Value != prev) onChanged?.Invoke(entry.Value);
            }
            return 28f;
        }

        protected float DrawFloatDualSpinner(float x, float y, float width, string label, ConfigEntry<float> entry, float min, float max, string vanillaInfo, GUIStyle labelStyle, GUIStyle btnStyle, string format = "0.##", Action<float> onChanged = null)
        {
            if (entry == null) return 0f;
            GUI.Label(new Rect(x, y, width - 175f, 24f), $"{label}: <color=#FFDD44>{entry.Value.ToString(format)}</color> <color=#888888>({vanillaInfo})</color>", labelStyle);
            float prev = entry.Value;

            if (GUI.Button(new Rect(x + width - 170f, y, 40f, 24f), "-10", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value - 10f, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 126f, y, 36f, 24f), "-1", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value - 1f, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 86f, y, 36f, 24f), "+1", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value + 1f, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 46f, y, 40f, 24f), "+10", btnStyle))
            {
                entry.Value = (float)Math.Round(Math.Clamp(entry.Value + 10f, min, max), 2);
                if (Math.Abs(entry.Value - prev) > 0.0001f) onChanged?.Invoke(entry.Value);
            }
            return 28f;
        }

        protected float DrawEnumPicker<T>(float x, float y, float width, string label, ConfigEntry<T> entry, T[] options, GUIStyle labelStyle, GUIStyle btnStyle, Action<T> onChanged = null) where T : Enum
        {
            if (entry == null || options == null || options.Length == 0) return 0f;
            int idx = Array.IndexOf(options, entry.Value);
            if (idx < 0) idx = 0;

            GUI.Label(new Rect(x, y, width - 175f, 24f), $"{label}: <color=#FFDD44>[{entry.Value}]</color>", labelStyle);

            if (GUI.Button(new Rect(x + width - 170f, y, 36f, 24f), "◄", btnStyle))
            {
                idx = (idx - 1 + options.Length) % options.Length;
                entry.Value = options[idx];
                onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 130f, y, 86f, 24f), entry.Value.ToString(), btnStyle))
            {
                idx = (idx + 1) % options.Length;
                entry.Value = options[idx];
                onChanged?.Invoke(entry.Value);
            }
            if (GUI.Button(new Rect(x + width - 40f, y, 36f, 24f), "►", btnStyle))
            {
                idx = (idx + 1) % options.Length;
                entry.Value = options[idx];
                onChanged?.Invoke(entry.Value);
            }
            return 28f;
        }
    }
}
