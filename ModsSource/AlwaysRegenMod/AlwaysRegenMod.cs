using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(AlwaysRegenMod.AlwaysRegenMod), "AlwaysRegenMod", "1.3.0", "CustomMod")]
[assembly: MelonGame("Mudtek", "Dimraeth")]

namespace AlwaysRegenMod
{
    public class AlwaysRegenMod : MelonMod
    {
        public static MelonPreferences_Category PrefCategory;
        public static MelonPreferences_Entry<float> HealthRegenMultiplier;
        public static MelonPreferences_Entry<float> StaminaRegenMultiplier;
        public static MelonPreferences_Entry<bool> InfiniteSprint;
        public static MelonPreferences_Entry<bool> NoRegenLockout;
        public static MelonPreferences_Entry<bool> ContinuousRegen;

        public override void OnInitializeMelon()
        {
            PrefCategory = MelonPreferences.CreateCategory("AlwaysRegenMod", "Always Regen & Stamina Settings");
            HealthRegenMultiplier = PrefCategory.CreateEntry("HealthRegenMultiplier", 99.0f, "Health Regen Multiplier (xVanilla, 1.0 = Vanilla)");
            StaminaRegenMultiplier = PrefCategory.CreateEntry("StaminaRegenMultiplier", 99.0f, "Stamina Regen Multiplier (xVanilla, 1.0 = Vanilla)");
            InfiniteSprint = PrefCategory.CreateEntry("InfiniteSprint", true, "Infinite Sprinting (0 Stamina Cost)");
            NoRegenLockout = PrefCategory.CreateEntry("NoRegenLockout", true, "Remove Delay Before Regen Starts After Damage/Casting");
            ContinuousRegen = PrefCategory.CreateEntry("ContinuousRegen", true, "Continuous 10Hz Regen Tick During Actions/Combat");

            LoggerInstance.Msg("=================================================");
            LoggerInstance.Msg("AlwaysRegenMod v1.4.0 Initialized successfully!");
            LoggerInstance.Msg($"Health Regen Multiplier: {HealthRegenMultiplier.Value}x");
            LoggerInstance.Msg($"Stamina Regen Multiplier: {StaminaRegenMultiplier.Value}x");
            LoggerInstance.Msg($"Infinite Sprint: {InfiniteSprint.Value}");
            LoggerInstance.Msg($"No Regen Lockout: {NoRegenLockout.Value}");
            LoggerInstance.Msg($"Continuous 10Hz Regen: {ContinuousRegen.Value}");
            LoggerInstance.Msg("Zero-lag event-driven architecture active.");
            LoggerInstance.Msg("=================================================");
        }
    }

    // 1. Boost Health Regeneration formula for the player
    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateHealthRegeneration))]
    public static class Patch_CalculateHealthRegeneration
    {
        public static void Postfix(ref float __result, ObjectsCommon obj)
        {
            if (obj != null && obj.IsPlayer)
            {
                float mult = AlwaysRegenMod.HealthRegenMultiplier != null ? AlwaysRegenMod.HealthRegenMultiplier.Value : 99.0f;
                if (mult > 1.0f)
                {
                    float baseRate = __result > 0f ? __result : 5.0f;
                    __result = baseRate * mult;
                }
            }
        }
    }

    // 2. Boost Stamina Regeneration formula for the player
    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateStaminaRegeneration))]
    public static class Patch_CalculateStaminaRegeneration
    {
        public static void Postfix(ref float __result, ObjectsCommon obj)
        {
            if (obj != null && obj.IsPlayer)
            {
                float mult = AlwaysRegenMod.StaminaRegenMultiplier != null ? AlwaysRegenMod.StaminaRegenMultiplier.Value : 99.0f;
                if (mult > 1.0f)
                {
                    float baseRate = __result > 0f ? __result : 10.0f;
                    __result = baseRate * mult;
                }
            }
        }
    }

    // 3. Make sprinting cost 0 stamina so sprinting never drains stamina
    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateSprintingCostPerSecond))]
    public static class Patch_CalculateSprintingCostPerSecond
    {
        public static void Postfix(ref float __result, Player player)
        {
            if (player != null && player.IsPlayer)
            {
                if (AlwaysRegenMod.InfiniteSprint != null && AlwaysRegenMod.InfiniteSprint.Value)
                {
                    __result = 0f;
                }
            }
        }
    }

    // 4. Neutralize spell/damage suppression lockout timers
    [HarmonyPatch(typeof(BaseSpellLibrary), nameof(BaseSpellLibrary.SetRegenTimer))]
    public static class Patch_SpellLibrarySetRegenTimer
    {
        public static void Prefix(ref float time)
        {
            if (AlwaysRegenMod.NoRegenLockout != null && AlwaysRegenMod.NoRegenLockout.Value)
            {
                time = 0f;
            }
        }
    }

    // 5. Unconditional 10Hz Continuous Regeneration Tick
    // Runs on ObjectsCommon.CommonsZeroPointOneSecond (every 0.1 seconds)
    // Ensures that both HP and Stamina continuously regenerate at user-configured rate
    // even while sprinting, moving, attacking, casting, or taking damage.
    [HarmonyPatch(typeof(ObjectsCommon), nameof(ObjectsCommon.CommonsZeroPointOneSecond))]
    public static class Patch_CommonsZeroPointOneSecond
    {
        public static void Prefix(ObjectsCommon __instance)
        {
            if (__instance != null && __instance.IsPlayer)
            {
                if (AlwaysRegenMod.NoRegenLockout != null && AlwaysRegenMod.NoRegenLockout.Value)
                {
                    // Reset combat/damage recovery delay timer to 0 so natural ticks don't stall
                    __instance.RegenTimer = 0f;
                }
            }
        }

        public static void Postfix(ObjectsCommon __instance)
        {
            if (__instance != null && __instance.IsPlayer)
            {
                if (AlwaysRegenMod.ContinuousRegen == null || !AlwaysRegenMod.ContinuousRegen.Value)
                    return;

                try
                {
                    float stamMult = AlwaysRegenMod.StaminaRegenMultiplier != null ? AlwaysRegenMod.StaminaRegenMultiplier.Value : 99.0f;
                    if (stamMult > 1.0f)
                    {
                        var curStam = __instance.Stamina;
                        var maxStam = __instance.MaxStamina;
                        if (curStam != null && maxStam != null)
                        {
                            float cur = curStam.Value;
                            float max = maxStam.Value;
                            if (cur < max && max > 0f)
                            {
                                float tickAmount = 1.0f * stamMult; // ~99 per 0.1s at 99x
                                __instance.SetStamina(Mathf.Min(cur + tickAmount, max));
                            }
                        }
                    }

                    float hpMult = AlwaysRegenMod.HealthRegenMultiplier != null ? AlwaysRegenMod.HealthRegenMultiplier.Value : 99.0f;
                    if (hpMult > 1.0f)
                    {
                        var curHp = __instance.Health;
                        var maxHp = __instance.MaxHealth;
                        if (curHp != null && maxHp != null)
                        {
                            float curH = curHp.Value;
                            float maxH = maxHp.Value;
                            if (curH < maxH && maxH > 0f)
                            {
                                float tickAmount = 0.3f * hpMult; // ~30 HP per 0.1s at 99x
                                __instance.SetHealth(Mathf.Min(curH + tickAmount, maxH));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore transient uninitialized frames
                }
            }
        }
    }
}
