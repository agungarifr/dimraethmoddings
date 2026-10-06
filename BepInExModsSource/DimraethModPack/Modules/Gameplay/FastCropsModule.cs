using System;
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
    public class FastCropsModule : ModModuleBase
    {
        public static FastCropsModule Instance { get; private set; }

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

        [HarmonyPatch(typeof(FarmingClient), nameof(FarmingClient.SetFarmingClient))]
        public static class Patches
        {
            // Only the two growth ints are touched; the remaining args (buildHash, itemType, variant,
            // plantType, newLoad) are left untouched and are not needed here.
            [HarmonyPrefix]
            public static void Prefix(ref int growthTime, ref int maxGrowthTime)
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
                }
                catch { }
            }
        }
    }
}
