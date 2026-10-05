using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    /// <summary>
    /// Forces the local player's weather to any <see cref="Weathers"/> value.
    /// Uses the same override path the working in-game CheatMenu uses:
    /// WeatherManager.ChangeWeather + ChangeLocalWeatherOverride(bypassLerp: true).
    /// </summary>
    public class WeatherControlModule : ModModuleBase
    {
        public static WeatherControlModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Weather Controller";
        public override string Description => "Select and apply any weather type for the local player's kingdom";

        public ConfigEntry<Weathers> SelectedWeather;

        private static readonly Weathers[] AllWeathers = (Weathers[])Enum.GetValues(typeof(Weathers));

        private bool _dropdownOpen;
        private Weathers _pending;
        private string _status = "";

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.WeatherControl";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Weather Controller Module (Default: false, Vanilla: false)");

            SelectedWeather = config.Bind(sec, "SelectedWeather", Weathers.Sunny,
                "Weather applied by the Apply button. All Weathers values are selectable at runtime.");

            _pending = SelectedWeather.Value;
        }

        public override void ApplyPatches(Harmony harmony)
        {
            // No Harmony patches required: weather is applied on-demand from the Apply button.
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;

            GUI.Label(new Rect(x, curY, width, 20f), "Select a weather, then press Apply:", labelStyle);
            curY += 24f;

            // Dropdown header
            string arrow = _dropdownOpen ? "▲" : "▼";
            if (GUI.Button(new Rect(x, curY, width, 26f), $"Weather: {_pending}  {arrow}", btnStyle))
            {
                _dropdownOpen = !_dropdownOpen;
            }
            curY += 30f;

            // Expanded option list, drawn inline so it stays inside the manager's scroll group.
            if (_dropdownOpen)
            {
                for (int i = 0; i < AllWeathers.Length; i++)
                {
                    Weathers w = AllWeathers[i];
                    string label = (w == _pending) ? $"● {w}" : $"   {w}";
                    if (GUI.Button(new Rect(x, curY, width, 24f), label, btnStyle))
                    {
                        _pending = w;
                        _dropdownOpen = false;
                    }
                    curY += 26f;
                }
            }

            // Apply button + status
            if (GUI.Button(new Rect(x + width - 150f, curY, 150f, 26f), "Apply Weather", btnStyle))
            {
                ApplyWeather();
            }
            GUI.Label(new Rect(x, curY, width - 160f, 26f), _status, labelStyle);
            curY += 30f;

            return curY - y;
        }

        private void ApplyWeather()
        {
            try
            {
                var wm = WeatherManager.Singleton;
                if (wm == null)
                {
                    _status = "<color=#FFAA55>Weather manager not ready yet.</color>";
                    return;
                }

                Kingdom kingdom = wm.CurrentPlayerKingdom;

                // Same call pair used by the working CheatMenu weather buttons.
                try { wm.ChangeWeather(kingdom, _pending); } catch { }
                wm.ChangeLocalWeatherOverride(kingdom, _pending, true);

                SelectedWeather.Value = _pending;
                _status = $"<color=#55FF55>Applied: {_pending}</color>";
                DiagnosticsManager.Log("WeatherControl", $"Applied weather {_pending} for kingdom {kingdom}.");
            }
            catch (Exception ex)
            {
                _status = "<color=#FF5555>Apply failed (see log).</color>";
                DiagnosticsManager.Log("WeatherControl", $"Apply failed: {ex.Message}");
            }
        }
    }
}
