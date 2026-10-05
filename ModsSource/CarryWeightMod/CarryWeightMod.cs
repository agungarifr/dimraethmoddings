using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(CarryWeightMod.CarryWeightMod), "CarryWeightMod", "1.0.0", "CustomMod")]
[assembly: MelonGame("Mudtek", "Dimraeth")]

namespace CarryWeightMod
{
    public class CarryWeightMod : MelonMod
    {
        public static MelonPreferences_Category PrefCategory;
        public static MelonPreferences_Entry<float> WeightMultiplier;
        public static MelonPreferences_Entry<bool> UnlimitedCarryWeight;

        public override void OnInitializeMelon()
        {
            PrefCategory = MelonPreferences.CreateCategory("CarryWeightMod", "Carry Weight Multiplier Settings");
            WeightMultiplier = PrefCategory.CreateEntry("WeightMultiplier", 99.0f, "Carry Weight Multiplier (xVanilla)");
            UnlimitedCarryWeight = PrefCategory.CreateEntry("UnlimitedCarryWeight", false, "Set Carry Weight to 1,000,000 (Overrides Multiplier)");

            LoggerInstance.Msg("=================================================");
            LoggerInstance.Msg("CarryWeightMod v1.0.0 Initialized successfully!");
            LoggerInstance.Msg($"Carry Weight Multiplier: {WeightMultiplier.Value}x");
            LoggerInstance.Msg($"Unlimited Carry Weight: {UnlimitedCarryWeight.Value}");
            LoggerInstance.Msg("Zero-lag event-driven architecture active.");
            LoggerInstance.Msg("=================================================");
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
                if (CarryWeightMod.UnlimitedCarryWeight != null && CarryWeightMod.UnlimitedCarryWeight.Value)
                {
                    __result = Formulas.UnlimitedCarryCapacity;
                }
                else
                {
                    float mult = CarryWeightMod.WeightMultiplier != null ? CarryWeightMod.WeightMultiplier.Value : 99.0f;
                    if (mult > 1.0f)
                    {
                        __result *= mult;
                    }
                }
            }
        }
    }
}
