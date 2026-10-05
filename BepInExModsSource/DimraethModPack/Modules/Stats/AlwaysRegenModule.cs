using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Stats
{
    public class AlwaysRegenModule : ModModuleBase
    {
        public static AlwaysRegenModule Instance { get; private set; }

        public override string Category => "Stats";
        public override string Name => "Always Regen";
        public override string Description => "Continuous passive regeneration for HP, Stamina, and infinite sprinting";

        public ConfigEntry<float> HealthRegenMultiplier;
        public ConfigEntry<float> StaminaRegenMultiplier;
        public ConfigEntry<bool> InfiniteSprint;
        public ConfigEntry<bool> NoRegenLockout;
        public ConfigEntry<bool> ContinuousRegen;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Stats.AlwaysRegen";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Always Regen Module (Default: false, Vanilla: false)");

            HealthRegenMultiplier = config.Bind(sec, "HealthRegenMultiplier", 10.0f,
                "Multiplier for health regeneration rate (Default: 10.0, Vanilla: 1.0)");

            StaminaRegenMultiplier = config.Bind(sec, "StaminaRegenMultiplier", 10.0f,
                "Multiplier for stamina regeneration rate (Default: 10.0, Vanilla: 1.0)");

            InfiniteSprint = config.Bind(sec, "InfiniteSprint", true,
                "Sprint without consuming any stamina (Default: true, Vanilla: false)");

            NoRegenLockout = config.Bind(sec, "NoRegenLockout", true,
                "Remove regeneration lockout delay after taking hits or casting spells (Default: true, Vanilla: false)");

            ContinuousRegen = config.Bind(sec, "ContinuousRegen", true,
                "Active 10Hz continuous tick regeneration during combat and actions (Default: true, Vanilla: false)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawFloatSpinner(x, curY, width, "Health Regen Mult", HealthRegenMultiplier, 1.0f, 1.0f, 100.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Stamina Regen Mult", StaminaRegenMultiplier, 1.0f, 1.0f, 100.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawToggle(x, curY, width, "Infinite Sprint", InfiniteSprint, "Vanilla: OFF", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "No Regen Lockout", NoRegenLockout, "Vanilla: OFF", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Continuous 10Hz Regen", ContinuousRegen, "Vanilla: OFF", labelStyle, btnStyle);
            return curY - y;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateHealthRegeneration))]
            [HarmonyPostfix]
            public static void Postfix_CalculateHealthRegeneration(ref float __result, ObjectsCommon obj)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                // [2026-09-28 10:35] OBSOLETE - `obj.IsPlayer` dereferences a possibly null/stale
                // pointer (uncatchable AV, same crash as CustomStatModule). Use pointer identity.
                // if (obj != null && obj.IsPlayer)
                if (PlayerIdentity.IsLocalPlayer(obj))
                {
                    float mult = Instance.HealthRegenMultiplier.Value;
                    if (mult > 1.0f)
                    {
                        float baseRate = __result > 0f ? __result : 5.0f;
                        __result = baseRate * mult;
                    }
                }
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateStaminaRegeneration))]
            [HarmonyPostfix]
            public static void Postfix_CalculateStaminaRegeneration(ref float __result, ObjectsCommon obj)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                // [2026-09-28 10:35] OBSOLETE - see Postfix_CalculateHealthRegeneration above.
                // if (obj != null && obj.IsPlayer)
                if (PlayerIdentity.IsLocalPlayer(obj))
                {
                    float mult = Instance.StaminaRegenMultiplier.Value;
                    if (mult > 1.0f)
                    {
                        float baseRate = __result > 0f ? __result : 10.0f;
                        __result = baseRate * mult;
                    }
                }
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateSprintingCostPerSecond))]
            [HarmonyPostfix]
            public static void Postfix_CalculateSprintingCostPerSecond(ref float __result, Player player)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                // [2026-09-28 10:35] OBSOLETE - `player.IsPlayer` dereferences a possibly
                // null/stale pointer. The parameter is already typed Player, so the real test
                // is "is it the live local player" - done without touching the object.
                // if (player != null && player.IsPlayer && Instance.InfiniteSprint.Value)
                if (PlayerIdentity.IsLocalPlayer(player) && Instance.InfiniteSprint.Value)
                {
                    __result = 0f;
                }
            }

            [HarmonyPatch(typeof(BaseSpellLibrary), nameof(BaseSpellLibrary.SetRegenTimer))]
            [HarmonyPrefix]
            public static void Prefix_SetRegenTimer(ref float time)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (Instance.NoRegenLockout.Value)
                {
                    time = 0f;
                }
            }

            [HarmonyPatch(typeof(ObjectsCommon), nameof(ObjectsCommon.CommonsZeroPointOneSecond))]
            public static class Patch_CommonsZeroPointOneSecond
            {
                [HarmonyPrefix]
                public static void Prefix(ObjectsCommon __instance)
                {
                    if (Instance == null || !Instance.IsEnabled) return;
                    // [2026-09-28 10:35] OBSOLETE - `__instance.IsPlayer` may deref a stale
                    // pointer. IsLocalPlayer already validated the pointer, so the following
                    // __instance.IsOwner access is safe.
                    // if (__instance != null && __instance.IsPlayer && __instance.IsOwner && Instance.NoRegenLockout.Value)
                    if (PlayerIdentity.IsLocalPlayer(__instance) && __instance.IsOwner && Instance.NoRegenLockout.Value)
                    {
                        __instance.RegenTimer = 0f;
                    }
                }

                [HarmonyPostfix]
                public static void Postfix(ObjectsCommon __instance)
                {
                    if (Instance == null || !Instance.IsEnabled) return;
                    // [2026-09-28 10:35] OBSOLETE - see Prefix above.
                    // if (__instance != null && __instance.IsPlayer && __instance.IsOwner && Instance.ContinuousRegen.Value)
                    if (PlayerIdentity.IsLocalPlayer(__instance) && __instance.IsOwner && Instance.ContinuousRegen.Value)
                    {
                        try
                        {
                            float stamMult = Instance.StaminaRegenMultiplier.Value;
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
                                        float tickAmount = 1.0f * stamMult;
                                        __instance.SetStamina(Mathf.Min(cur + tickAmount, max));
                                    }
                                }
                            }

                            float hpMult = Instance.HealthRegenMultiplier.Value;
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
                                        float tickAmount = 0.3f * hpMult;
                                        __instance.SetHealth(Mathf.Min(curH + tickAmount, maxH));
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
