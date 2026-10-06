using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    // [2026-10-06 13:05] REVAMP (user request): the old Fast Crops design scaled crop growth time via a
    // Harmony prefix on FarmingClient.SetFarmingClient plus a per-frame OnUpdate rescale of already-loaded
    // plots. The user asked to instead "lean on [the] growth rate by [the] system" and provide a BUTTON
    // that triggers "insta ready to harvest", so the module does no per-frame work and installs no patches.
    // It now simply calls the game's own growth driver:
    //
    //     PlayerBaseManager.Singleton.AdvanceAllFarmGrowth(seconds, out matured, out grew)
    //
    // This is the exact call the vanilla cheat menu uses (CheatMenuDecompiled/CheatMenu.GameActions.cs
    // MatureCrops: AdvanceAllFarmGrowth(31536000, ...) = 365 days of growth in one shot). It walks the
    // server's FarmPlots and matures them through the normal growth/notification path, so it costs nothing
    // until the button is pressed and needs no IL hooks.
    public class FastCropsModule : ModModuleBase
    {
        public static FastCropsModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Instant Crops";
        public override string Description => "One button that instantly matures every farming plot (apple tree, flax, etc.) using the game's own growth system";

        // [2026-10-06 13:05] Obsolete: growth-speed multiplier config removed with the revamp. The module
        // is now a manual instant-mature button, not a continuous growth-rate modifier.
        // public ConfigEntry<int> GrowthSpeedMultiplier;

        // Result text shown under the button after the last press.
        private static string _lastResult = "";

        // [2026-10-06 13:05] 365 days in seconds — the same value the vanilla "mature crops" cheat uses.
        // Any value >= the longest crop MaxGrowthTime matures everything in a single AdvanceAllFarmGrowth.
        private const int InstantGrowSeconds = 31536000;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.FastCrops";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Instant Crops (Default: false, Vanilla: false)");

            /* [2026-10-06 13:05] Obsolete: growth-speed multiplier removed in the revamp. The button
               matures crops instantly through PlayerBaseManager.AdvanceAllFarmGrowth, so a numeric growth
               multiplier is no longer meaningful. Kept for reference per repo rule.
            GrowthSpeedMultiplier = config.Bind(sec, "GrowthSpeedMultiplier", 5,
                "Crop growth speed multiplier for farming plots (Default: 5, Vanilla: 1)");
            */
        }

        // [2026-10-06 13:05] No Harmony patches: the revamp taps an existing public game method, so there
        // is no per-spawn/per-frame hook and therefore no gameplay overhead.
        public override void ApplyPatches(Harmony harmony)
        {
            // intentionally empty — see the class comment for why.
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            GUI.Label(new Rect(x, curY, width, 24f),
                "Instantly matures every farming plot via the game's own growth system (no continuous overhead).", labelStyle);
            curY += 28f;

            if (GUI.Button(new Rect(x, curY, Math.Min(320f, width), 30f), "Make All Crops Ready To Harvest", btnStyle))
            {
                if (!IsEnabled)
                {
                    _lastResult = "<color=#FF6666>Enable this module first.</color>";
                }
                else
                {
                    AdvanceAll();
                }
            }
            curY += 36f;

            if (!string.IsNullOrEmpty(_lastResult))
            {
                GUI.Label(new Rect(x, curY, width, 24f), _lastResult, labelStyle);
                curY += 28f;
            }
            return curY - y;
        }

        // Matures every farming plot instantly by advancing the game's own growth by a huge number of
        // seconds. Runs only when the user presses the button.
        private static void AdvanceAll()
        {
            try
            {
                var mgr = PlayerBaseManager.Singleton;
                if (mgr == null)
                {
                    _lastResult = "<color=#FF6666>Player base not loaded yet.</color>";
                    return;
                }

                mgr.AdvanceAllFarmGrowth(InstantGrowSeconds, out int matured, out int grew);
                _lastResult = $"<color=#55FF55>Crops: {matured} matured, {grew} grew.</color>";
                DimraethModPackPlugin.Log?.LogInfo($"[InstantCrops] AdvanceAllFarmGrowth -> {matured} matured, {grew} grew.");
            }
            catch (Exception ex)
            {
                _lastResult = "<color=#FF6666>Failed — see log.</color>";
                DimraethModPackPlugin.Log?.LogError($"[InstantCrops] AdvanceAllFarmGrowth failed: {ex}");
            }
        }

        /* [2026-10-06 13:05] ===== OBSOLETE (pre-revamp growth-rate design), kept per repo rule. =====
           The old module scaled growth time via a Harmony prefix and a per-frame rescale pass. This is
           exactly what the user asked to replace with the manual instant-mature button above, to avoid
           burdening the game with continuous per-frame work.

        // Pointers of plots whose growth has already been scaled (by the prefix or the rescale pass).
        private static readonly HashSet<IntPtr> _scaledPlots = new HashSet<IntPtr>();
        private static bool _wasEnabled;

        // Called every frame from ModPackManagerBehaviour.Update (as DiagnosticsManager is).
        // Only acts on the false -> true enable transition.
        public static void OnUpdate()
        {
            var inst = Instance;
            if (inst == null) return;
            if (!inst.IsEnabled)
            {
                _wasEnabled = false;
                return;
            }
            if (_wasEnabled) return;
            _wasEnabled = true;
            RescaleExistingPlots();
        }

        private static void RescaleExistingPlots()
        {
            int mult = Instance?.GrowthSpeedMultiplier?.Value ?? 1;
            if (mult <= 1) return;
            try
            {
                var plots = UnityEngine.Object.FindObjectsOfType<FarmingClient>();
                if (plots == null) return;
                for (int i = 0; i < plots.Length; i++)
                {
                    var fc = plots[i];
                    if (fc == null) continue;
                    if (!_scaledPlots.Add(fc.Pointer)) continue;
                    if (fc.MaxGrowthTime > 0) fc.MaxGrowthTime = Math.Max(1, fc.MaxGrowthTime / mult);
                    if (fc.GrowthTime > 0) fc.GrowthTime = Math.Max(0, fc.GrowthTime / mult);
                }
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogWarning($"[FastCrops] rescale existing plots failed: {ex}");
            }
        }

        [HarmonyPatch(typeof(FarmingClient), nameof(FarmingClient.SetFarmingClient))]
        public static class Patches
        {
            [HarmonyPrefix]
            public static void Prefix(FarmingClient __instance, ref int growthTime, ref int maxGrowthTime)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                int mult = Instance.GrowthSpeedMultiplier.Value;
                if (mult <= 1) return;
                try
                {
                    if (maxGrowthTime > 0) maxGrowthTime = Math.Max(1, maxGrowthTime / mult);
                    if (growthTime > 0) growthTime = Math.Max(0, growthTime / mult);
                    if (__instance != null) _scaledPlots.Add(__instance.Pointer);
                }
                catch { }
            }
        }
        */
    }
}
