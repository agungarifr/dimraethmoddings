using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace CarryWeightMod
{
    [BepInPlugin("com.custom.carryweightmod", "CarryWeightMod", "1.0.0")]
    public class CarryWeightModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static ConfigEntry<float> WeightMultiplier;
        public static ConfigEntry<bool> UnlimitedCarryWeight;

        public override void Load()
        {
            Log = base.Log;

            WeightMultiplier = Config.Bind("General", "WeightMultiplier", 99.0f, "Carry Weight Multiplier (xVanilla) (Default: 99)");
            UnlimitedCarryWeight = Config.Bind("General", "UnlimitedCarryWeight", false, "Set Carry Weight to 1,000,000 (Overrides Multiplier) (Default: false)");

            Harmony.CreateAndPatchAll(typeof(Patch_CalculateEncumbrance));

            Log.LogInfo("=================================================");
            Log.LogInfo("CarryWeightMod v1.0.0 (BepInEx 6) Initialized!");
            Log.LogInfo($"Carry Weight Multiplier: {WeightMultiplier.Value}x");
            Log.LogInfo($"Unlimited Carry Weight: {UnlimitedCarryWeight.Value}");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");
        }
    }

    /// <summary>
    /// Multiplies the player's carry weight limit (max encumbrance) by 99x.
    /// Formulas.CalculateEncumbrance is the single centralized method used by:
    /// - Backpack UI (BackpackStatusPresenter -> MaxWeight display & bar fill)
    /// - Overweight check (Formulas.ExceedsEncumbrance) for picking up items, harvesting, storage, shops, etc.
    /// - Remaining capacity calculation (Formulas.RemainingCarryCapacity)
    /// - Character Sheet (PlayerStats)
    /// - Attribute Upgrade screen (PlayerUpgradeUI)
    /// </summary>
    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateEncumbrance))]
    public static class Patch_CalculateEncumbrance
    {
        public static void Postfix(ref float __result, ObjectsCommon obj, Attributes attributes)
        {
            if (obj != null && (obj.IsPlayer || obj.TryCast<Player>() != null))
            {
                if (CarryWeightModPlugin.UnlimitedCarryWeight != null && CarryWeightModPlugin.UnlimitedCarryWeight.Value)
                {
                    __result = Formulas.UnlimitedCarryCapacity;
                }
                else
                {
                    float mult = CarryWeightModPlugin.WeightMultiplier != null ? CarryWeightModPlugin.WeightMultiplier.Value : 99.0f;
                    if (mult > 1.0f)
                    {
                        __result *= mult;
                    }
                }
            }
        }
    }
}
