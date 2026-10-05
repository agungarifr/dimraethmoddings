using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace AlwaysRegenMod
{
    [BepInPlugin("com.custom.alwaysregenmod", "AlwaysRegenMod", "1.4.0")]
    public class AlwaysRegenModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static ConfigEntry<float> HealthRegenMultiplier;
        public static ConfigEntry<float> StaminaRegenMultiplier;
        public static ConfigEntry<bool> InfiniteSprint;
        public static ConfigEntry<bool> NoRegenLockout;
        public static ConfigEntry<bool> ContinuousRegen;

        public override void Load()
        {
            Log = base.Log;

            HealthRegenMultiplier = Config.Bind("General", "HealthRegenMultiplier", 99.0f, "Health Regen Multiplier (xVanilla, 1.0 = Vanilla) (Default: 99)");
            StaminaRegenMultiplier = Config.Bind("General", "StaminaRegenMultiplier", 99.0f, "Stamina Regen Multiplier (xVanilla, 1.0 = Vanilla) (Default: 99)");
            InfiniteSprint = Config.Bind("General", "InfiniteSprint", true, "Infinite Sprinting (0 Stamina Cost) (Default: true)");
            NoRegenLockout = Config.Bind("General", "NoRegenLockout", true, "Remove Delay Before Regen Starts After Damage/Casting (Default: true)");
            ContinuousRegen = Config.Bind("General", "ContinuousRegen", true, "Continuous 10Hz Regen Tick During Actions/Combat (Default: true)");

            Harmony.CreateAndPatchAll(typeof(Patch_CalculateHealthRegeneration));
            Harmony.CreateAndPatchAll(typeof(Patch_CalculateStaminaRegeneration));
            Harmony.CreateAndPatchAll(typeof(Patch_CalculateSprintingCostPerSecond));
            Harmony.CreateAndPatchAll(typeof(Patch_SpellLibrarySetRegenTimer));
            Harmony.CreateAndPatchAll(typeof(Patch_CommonsZeroPointOneSecond));

            Log.LogInfo("=================================================");
            Log.LogInfo("AlwaysRegenMod v1.4.0 (BepInEx 6) Initialized!");
            Log.LogInfo($"Health Regen Multiplier: {HealthRegenMultiplier.Value}x");
            Log.LogInfo($"Stamina Regen Multiplier: {StaminaRegenMultiplier.Value}x");
            Log.LogInfo($"Infinite Sprint: {InfiniteSprint.Value}");
            Log.LogInfo($"No Regen Lockout: {NoRegenLockout.Value}");
            Log.LogInfo($"Continuous 10Hz Regen: {ContinuousRegen.Value}");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");
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
                float mult = AlwaysRegenModPlugin.HealthRegenMultiplier != null ? AlwaysRegenModPlugin.HealthRegenMultiplier.Value : 99.0f;
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
                float mult = AlwaysRegenModPlugin.StaminaRegenMultiplier != null ? AlwaysRegenModPlugin.StaminaRegenMultiplier.Value : 99.0f;
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
                if (AlwaysRegenModPlugin.InfiniteSprint != null && AlwaysRegenModPlugin.InfiniteSprint.Value)
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
            if (AlwaysRegenModPlugin.NoRegenLockout != null && AlwaysRegenModPlugin.NoRegenLockout.Value)
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
                if (AlwaysRegenModPlugin.NoRegenLockout != null && AlwaysRegenModPlugin.NoRegenLockout.Value)
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
                if (AlwaysRegenModPlugin.ContinuousRegen == null || !AlwaysRegenModPlugin.ContinuousRegen.Value)
                    return;

                try
                {
                    float stamMult = AlwaysRegenModPlugin.StaminaRegenMultiplier != null ? AlwaysRegenModPlugin.StaminaRegenMultiplier.Value : 99.0f;
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

                    float hpMult = AlwaysRegenModPlugin.HealthRegenMultiplier != null ? AlwaysRegenModPlugin.HealthRegenMultiplier.Value : 99.0f;
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
