using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    /// <summary>
    /// Advances the world clock forward by N hours (or to a preset hour) through the game's own
    /// natural time path, so elapsed hours are real: minutes tick, hours/days/months/years roll over,
    /// weather rerolls on day changes and OnTimeChanged fires for every consumer - just applied instantly.
    /// [2026-10-07 09:05] Previously this was a clock-face jump that only overwrote GameTime.Hour and
    /// called ChangeGameTime; that never advanced the day/month/year counters nor rerolled weather, so the
    /// "spent" hours were not real. Old description kept below per repo rule.
    /// Jumps the world clock instantly to a preset hour or forward by N hours.
    /// Mirrors the working CheatMenu path: copy CurrentGameTime, set Hour, then ChangeGameTime.
    /// This is a jump, not a fast-forward.
    /// </summary>
    public class TimeSkipModule : ModModuleBase
    {
        public static TimeSkipModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Time Skip";
        public override string Description => "Instantly skip the world clock to a chosen time or forward by N hours (jump between day and night)";

        public ConfigEntry<int> SkipHours;

        private string _status = "";

        // [2026-10-07 09:05] Cached handle to TimeManager's private IncrementGameTime - the game's own
        // one-minute natural tick (see TimeUpdateCoroutine). Resolved once via reflection on first skip.
        private static System.Reflection.MethodInfo _incrementGameTime;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.TimeSkip";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Time Skip Module (Default: false, Vanilla: false)");

            SkipHours = config.Bind(sec, "SkipHours", 6,
                "Number of hours the Skip button advances (Default: 6)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            // No Harmony patches required; time is changed on-demand from the UI.
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;

            // Live clock readout. Also caches the current hour so the preset buttons below
            // can compute their own delta without touching game state outside try/catch.
            int currentHour = 0;
            try
            {
                TimeManager tm = TimeManager.Singleton;
                if (tm != null && tm.CurrentGameTime != null)
                {
                    currentHour = tm.CurrentGameTime.Hour;
                    GUI.Label(new Rect(x, curY, width, 20f),
                        $"Current time: {tm.CurrentGameTime.Hour:00}:{tm.CurrentGameTime.Minute:00}", labelStyle);
                }
                else
                {
                    GUI.Label(new Rect(x, curY, width, 20f), "<color=#FFAA55>World not loaded</color>", labelStyle);
                }
            }
            catch (Exception ex)
            {
                GUI.Label(new Rect(x, curY, width, 20f), "<color=#FFAA55>World not loaded</color>", labelStyle);
                DiagnosticsManager.Log("TimeSkip", $"Current time read failed: {ex.Message}");
            }
            curY += 24f;

            // Presets jump straight to a target hour: delta = (targetHour - currentHour + 24) % 24.
            if (GUI.Button(new Rect(x, curY, width, 24f), "Skip to Dawn (06:00)", btnStyle)) ApplySkip((6 - currentHour + 24) % 24);
            curY += 26f;
            if (GUI.Button(new Rect(x, curY, width, 24f), "Skip to Noon (12:00)", btnStyle)) ApplySkip((12 - currentHour + 24) % 24);
            curY += 26f;
            if (GUI.Button(new Rect(x, curY, width, 24f), "Skip to Dusk (18:00)", btnStyle)) ApplySkip((18 - currentHour + 24) % 24);
            curY += 26f;
            if (GUI.Button(new Rect(x, curY, width, 24f), "Skip to Midnight (00:00)", btnStyle)) ApplySkip((0 - currentHour + 24) % 24);
            curY += 26f;

            curY += DrawIntSpinner(x, curY, width, "Skip Hours", SkipHours, 1, 1, 24, "Vanilla: n/a", labelStyle, btnStyle);

            if (GUI.Button(new Rect(x, curY, width, 24f), $"Skip +{SkipHours.Value} h", btnStyle)) ApplySkip(SkipHours.Value);
            curY += 28f;

            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x, curY, width, 20f), _status, labelStyle);
                curY += 22f;
            }

            return curY - y;
        }

        // Single helper: advance the world clock forward by delta hours of NATURAL time.
        // [2026-10-07 09:05] Was: a clock-face jump. Old description kept below per repo rule:
        // // Single helper: jump the world clock forward by delta hours
        // // (wrap at midnight via (Hour + delta + 24) % 24, minutes reset to 0 - same as the CheatMenu).
        private void ApplySkip(int delta)
        {
            try
            {
                TimeManager tm = TimeManager.Singleton;
                if (tm == null || tm.CurrentGameTime == null)
                {
                    _status = "<color=#FFAA55>World not loaded yet.</color>";
                    return;
                }

                /* [2026-10-07 09:05] Obsolete: this clock-face jump only overwrote the hour (minutes reset to 0)
                   and called ChangeGameTime, which sets the networked time value but does NOT roll hours into
                   days/months/years nor reroll weather. Result: skipping from 20:00 +6h showed 02:00 of the
                   SAME day, so the "hours spent" never actually elapsed. Kept per repo rule.
                GameTime gt = new GameTime(tm.CurrentGameTime);
                gt.Hour = (gt.Hour + delta + 24) % 24;
                gt.Minute = 0;
                tm.ChangeGameTime(gt);

                _status = $"<color=#55FF55>Skipped to {gt.Hour:00}:{gt.Minute:00}</color>";
                DiagnosticsManager.Log("TimeSkip", $"Skipped world clock to {gt.Hour:00}:{gt.Minute:00} (delta {delta}h).");
                */

                // [2026-10-07 09:05] New: advance through the game's natural time path. IncrementGameTime
                // adds exactly one in-game minute per call and handles hour->day->month->year rollover,
                // weather rerolls on a day change, and publishing the new time (which fires OnTimeChanged
                // for all consumers). Looping it delta*60 times makes the elapsed hours/days real while
                // staying instant; it runs once per button press, not as a per-frame hook.
                if (_incrementGameTime == null)
                    _incrementGameTime = HarmonyLib.AccessTools.Method(typeof(TimeManager), "IncrementGameTime");

                if (_incrementGameTime == null)
                {
                    _status = "<color=#FF5555>Time skip failed: IncrementGameTime not found.</color>";
                    DiagnosticsManager.Log("TimeSkip", "IncrementGameTime method not found via reflection.");
                    return;
                }

                int steps = delta * 60;
                for (int i = 0; i < steps; i++)
                    _incrementGameTime.Invoke(tm, null);

                GameTime now = tm.CurrentGameTime;
                _status = $"<color=#55FF55>Skipped +{delta}h to {now.Hour:00}:{now.Minute:00} (Day {now.Day})</color>";
                DiagnosticsManager.Log("TimeSkip", $"Advanced natural time +{delta}h ({steps} min) to {now.Hour:00}:{now.Minute:00} Day {now.Day}.");
            }
            catch (Exception ex)
            {
                _status = "<color=#FF5555>Time skip failed (see log).</color>";
                DiagnosticsManager.Log("TimeSkip", $"Time skip failed: {ex.Message}");
            }
        }

        /* [2026-10-01 10:21] Obsolete: the previous draft had a separate SkipTo(targetHour) helper that
           duplicated the null-check/try-catch of ApplySkip. Per spec the module keeps exactly one helper
           (ApplySkip); presets now compute their own delta inline from the cached currentHour and call
           ApplySkip directly. Old code kept here per repo rule.
        private void SkipTo(int targetHour)
        {
            try
            {
                TimeManager tm = TimeManager.Singleton;
                if (tm == null || tm.CurrentGameTime == null)
                {
                    _status = "<color=#FFAA55>World not loaded yet.</color>";
                    return;
                }

                ApplySkip((targetHour - tm.CurrentGameTime.Hour + 24) % 24);
            }
            catch (Exception ex)
            {
                _status = "<color=#FF5555>Time skip failed (see log).</color>";
                DiagnosticsManager.Log("TimeSkip", $"Skip to hour {targetHour} failed: {ex.Message}");
            }
        }
        */
    }
}
