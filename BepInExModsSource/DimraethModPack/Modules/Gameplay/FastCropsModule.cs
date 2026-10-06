using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    // [2026-10-06 11:15] Module 2 (user request): "fast crops grow" with a configurable modifier.
    // Sanctum farming plots (BuildItemType.FlaxPlot=27, AppleTreePlot=45; FarmingPlantType.Flax=4,
    // AppleTree=5) are FarmingClient components. Their growth budget is initialised in
    // FarmingClient.SetFarmingClient(buildHash, itemType, growthTime, maxGrowthTime, variant, plantType, newLoad):
    // the ints are written to GrowthTime (field +0xC0) and MaxGrowthTime (+0xC4), and ServerTickGrowth
    // then increments GrowthTime by 1 per second until it reaches MaxGrowthTime. Scaling both args at
    // init keeps the stage/progress ratio (0..1) intact but the crop ripens ~multiplier times faster.
    // Both callers are covered: BuildUtility (placing/planting) and PlayerBaseManager (load/sync).
    //
    // [2026-10-06 12:35] Fix (user report: "works on newly planted crops but not existing crops"):
    // the SetFarmingClient prefix only scales plots at plant/spawn time, so plots already in the world
    // when the module is enabled kept their unscaled MaxGrowthTime. OnUpdate() now detects the
    // false->true enable transition and rescales every already-loaded FarmingClient once. A shared
    // HashSet<IntPtr> (the same dedup pattern the Loot module uses) prevents double-scaling plots that
    // the prefix already handled during load.
    public class FastCropsModule : ModModuleBase
    {
        public static FastCropsModule Instance { get; private set; }

        // Pointers of plots whose growth has already been scaled (by the prefix or the rescale pass).
        private static readonly HashSet<IntPtr> _scaledPlots = new HashSet<IntPtr>();
        private static bool _wasEnabled;

        public override string Category => "Gameplay";
        public override string Name => "Fast Crops";
        public override string Description => "Speed up crop growth on farming plots (apple tree, flax, etc.) with a configurable multiplier";

        public ConfigEntry<int> GrowthSpeedMultiplier;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.FastCrops";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Fast Crops (Default: false, Vanilla: false)");

            GrowthSpeedMultiplier = config.Bind(sec, "GrowthSpeedMultiplier", 5,
                "Crop growth speed multiplier for farming plots (Default: 5, Vanilla: 1)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntSpinner(x, curY, width, "Growth Speed Multiplier", GrowthSpeedMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            return curY - y;
        }

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
                    // Dedup: skip plots already scaled at spawn/plant time (avoids double-scaling).
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
            // Only the two growth ints are touched; the remaining args (buildHash, itemType, variant,
            // plantType, newLoad) are left untouched and are not needed here.
            [HarmonyPrefix]
            public static void Prefix(FarmingClient __instance, ref int growthTime, ref int maxGrowthTime)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                int mult = Instance.GrowthSpeedMultiplier.Value;
                if (mult <= 1) return;
                try
                {
                    // Scale the target (and the elapsed progress to match) so an already-planted crop
                    // keeps its current stage ratio yet finishes sooner. Clamp the target to >= 1 so a
                    // plot still needs at least one growth tick.
                    if (maxGrowthTime > 0) maxGrowthTime = Math.Max(1, maxGrowthTime / mult);
                    if (growthTime > 0) growthTime = Math.Max(0, growthTime / mult);
                    if (__instance != null) _scaledPlots.Add(__instance.Pointer);
                }
                catch { }
            }
        }
    }
}
