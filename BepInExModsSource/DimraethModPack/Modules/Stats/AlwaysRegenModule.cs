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
            // [2026-10-07 10:01] FIX (Always Regen not working in combat). Verified against the
            // current game (10/7 decompile) and HarmonyX 0Harmony.dll: Harmony.PatchAll(Type) only
            // processes methods DECLARED on the given type (CreateClassProcessor ->
            // PatchTools.GetPatchMethods -> AccessTools.GetDeclaredMethods -> Type.GetMethods), it
            // does NOT walk nested classes. The Prefix/Postfix for
            // ObjectsCommon.CommonsZeroPointOneSecond live in the nested class below, so they were
            // silently never applied - NoRegenLockout and ContinuousRegen (the actual in-combat regen
            // path) did nothing, leaving only the out-of-combat multiplier postfixes working.
            // Apply the nested class explicitly, the same way LootModV2/PerfectParry already do.
            harmony.PatchAll(typeof(Patches.Patch_CommonsZeroPointOneSecond));
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
            // [2026-10-07] TEMP DIAGNOSTICS for the "No Regen Lockout doesn't work" report.
            // Remove once the firing/flag values are confirmed in BepInEx\LogOutput.log.
            private static int _diagSetTimer;
            private static int _diagZp1Prefix;
            private static int _diagZp1Postfix;

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
                if (_diagSetTimer < 5)
                {
                    _diagSetTimer++;
                    DimraethModPackPlugin.Log?.LogInfo(
                        $"[AlwaysRegen][diag] BaseSpellLibrary.SetRegenTimer incoming time={time} noLockout={Instance.NoRegenLockout.Value}");
                }
                if (Instance.NoRegenLockout.Value)
                {
                    // [2026-10-07 10:01] FIX: vanilla stamina regen requires RegenTimer > 0.2
                    // (StaminaRegenTick does RegenTimer += 0.1 then skips regen while <= 0.2;
                    // lockout seeds are negative - QA suppress sets -10). Passing 0 still left a
                    // ~0.3s lockout every time the game re-seeded the timer on an attack/cast.
                    // Use a value safely above the gate so regen is allowed immediately.
                    // time = 0f;
                    time = 1f;
                }
            }

            [HarmonyPatch(typeof(ObjectsCommon), nameof(ObjectsCommon.CommonsZeroPointOneSecond))]
            public static class Patch_CommonsZeroPointOneSecond
            {
                [HarmonyPrefix]
                public static void Prefix(ObjectsCommon __instance)
                {
                    if (Instance == null || !Instance.IsEnabled) return;
                    bool isLocal = PlayerIdentity.IsLocalPlayer(__instance);
                    if (_diagZp1Prefix < 8)
                    {
                        _diagZp1Prefix++;
                        DimraethModPackPlugin.Log?.LogInfo(
                            $"[AlwaysRegen][diag] zp1 PREFIX isLocal={isLocal} isOwner={__instance.IsOwner} " +
                            $"isServer={__instance.IsServer} isClient={__instance.IsClient} " +
                            $"noLockout={Instance.NoRegenLockout.Value} cont={Instance.ContinuousRegen.Value} regen={__instance.RegenTimer}");
                    }
                    // [2026-09-28 10:35] OBSOLETE - `__instance.IsPlayer` may deref a stale
                    // pointer. IsLocalPlayer already validated the pointer, so the following
                    // __instance.IsOwner access is safe.
                    // if (__instance != null && __instance.IsPlayer && __instance.IsOwner && Instance.NoRegenLockout.Value)
                    if (PlayerIdentity.IsLocalPlayer(__instance) && __instance.IsOwner && Instance.NoRegenLockout.Value)
                    {
                        // [2026-10-07 10:01] FIX: 0 still lost to the `RegenTimer > 0.2` gate -
                        // because this ran every 0.1s tick, RegenTimer never climbed past 0.1 and
                        // stamina regen never fired. Hold it above the gate instead.
                        // __instance.RegenTimer = 0f;
                        __instance.RegenTimer = 1f;
                    }
                }

                [HarmonyPostfix]
                public static void Postfix(ObjectsCommon __instance)
                {
                    if (Instance == null || !Instance.IsEnabled) return;
                    bool isLocal = PlayerIdentity.IsLocalPlayer(__instance);
                    if (_diagZp1Postfix < 8)
                    {
                        _diagZp1Postfix++;
                        try
                        {
                            DimraethModPackPlugin.Log?.LogInfo(
                                $"[AlwaysRegen][diag] zp1 POSTFIX isLocal={isLocal} isOwner={__instance.IsOwner} " +
                                $"cont={Instance.ContinuousRegen.Value} stam={__instance.Stamina?.Value}/{__instance.MaxStamina?.Value} " +
                                $"hp={__instance.Health?.Value}/{__instance.MaxHealth?.Value}");
                        }
                        catch { }
                    }
                    // [2026-09-28 10:35] OBSOLETE - see Prefix above.
                    // if (__instance != null && __instance.IsPlayer && __instance.IsOwner && Instance.ContinuousRegen.Value)
                    if (isLocal && __instance.IsOwner && Instance.ContinuousRegen.Value)
                    {
                        try
                        {
                            float stamMult = Instance.StaminaRegenMultiplier.Value;
                            // [2026-10-07 10:01] FIX: ContinuousRegen is the in-combat regen path
                            // and must run whenever the toggle is on. The old `> 1.0f` guard disabled
                            // it at the config minimum (mult = 1), so turning the module on did
                            // nothing during battle. mult is >= 1 by config, so gate on the toggle.
                            // if (stamMult > 1.0f)
                            if (stamMult >= 1.0f)
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
                            // [2026-10-07 10:01] FIX: see the stamina block above.
                            // if (hpMult > 1.0f)
                            if (hpMult >= 1.0f)
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
