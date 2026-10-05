using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace PerfectParryMod
{
    [BepInPlugin("com.custom.perfectparrymod", "PerfectParryMod", "1.1.1")]
    public class PerfectParryModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> AlwaysPerfectParryWhileHolding;
        public static ConfigEntry<float> CustomParryWindowSeconds;
        public static ConfigEntry<bool> MultiHitParry;
        public static ConfigEntry<bool> NoStaminaDrainOnParry;

        public const float VanillaParryWindow = 0.08f;

        public override void Load()
        {
            Log = base.Log;

            ModEnabled = Config.Bind(
                "General",
                "ModEnabled",
                true,
                "Enable or disable the Perfect Parry Mod (Default: true)"
            );

            AlwaysPerfectParryWhileHolding = Config.Bind(
                "General",
                "AlwaysPerfectParryWhileHolding",
                false,
                "If true, ALWAYS triggers Perfect Parry / Retaliation / Vanish for the entire duration of holding block (Default: false)"
            );

            CustomParryWindowSeconds = Config.Bind(
                "General",
                "CustomParryWindowSeconds",
                3.0f,
                "Custom timing window in seconds when AlwaysPerfectParryWhileHolding is false. Vanilla is 0.08s (~80ms). e.g., set to 1.0, 2.0, or 3.0 (Default: 3.0)"
            );

            MultiHitParry = Config.Bind(
                "General",
                "MultiHitParry",
                true,
                "Allow repeated perfect parries / retaliations against multiple incoming hits during the same block hold (Default: true)"
            );

            NoStaminaDrainOnParry = Config.Bind(
                "General",
                "NoStaminaDrainOnParry",
                true,
                "Completely negate stamina loss when blocking/parrying within the timing window (Default: true)"
            );

            // Harmony patches for Minotaur Retaliation (Auto-casts Retaliation on block)
            Harmony.CreateAndPatchAll(typeof(Patch_RetaliationPrefab_HandleLocalIncomingHit));
            Harmony.CreateAndPatchAll(typeof(Patch_RetaliationPrefab_OnCasterDamageBlocked));
            Harmony.CreateAndPatchAll(typeof(Patch_RetaliationPrefab_ResolveBlockStamina));
            Harmony.CreateAndPatchAll(typeof(Patch_RetaliationPrefab_ServerValidatePerfectParry));

            // Harmony patches for Standard / Human Parry (Auto-casts Counter Attack on block)
            Harmony.CreateAndPatchAll(typeof(Patch_ParryPrefab_HandleLocalIncomingHit));
            Harmony.CreateAndPatchAll(typeof(Patch_ParryPrefab_OnCasterDamageBlocked));
            Harmony.CreateAndPatchAll(typeof(Patch_ParryPrefab_ResolveBlockStamina));
            Harmony.CreateAndPatchAll(typeof(Patch_ParryPrefab_ServerValidatePerfectParry));

            // Harmony patches for Rogue / Shadow Vanish (Auto-casts Vanish on block)
            Harmony.CreateAndPatchAll(typeof(Patch_VanishPrefab_HandleLocalIncomingHit));
            Harmony.CreateAndPatchAll(typeof(Patch_VanishPrefab_OnCasterDamageBlocked));
            Harmony.CreateAndPatchAll(typeof(Patch_VanishPrefab_ResolveBlockStamina));
            Harmony.CreateAndPatchAll(typeof(Patch_VanishPrefab_ServerValidatePerfectParry));

            Log.LogInfo("=================================================");
            Log.LogInfo("PerfectParryMod v1.1.1 (BepInEx 6) Initialized!");
            Log.LogInfo($"Mod Enabled: {ModEnabled.Value}");
            Log.LogInfo($"Always Perfect Parry While Holding: {AlwaysPerfectParryWhileHolding.Value}");
            Log.LogInfo($"Configured Window: {CustomParryWindowSeconds.Value}s (Vanilla: {VanillaParryWindow}s)");
            Log.LogInfo($"Multi-Hit Auto-Cast: {MultiHitParry.Value}");
            Log.LogInfo($"No Stamina Drain on Parry: {NoStaminaDrainOnParry.Value}");
            Log.LogInfo("Coverage: Minotaur (Retaliation), Human (Parry/Counter), Rogue (Vanish)");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");
        }

        public static bool ShouldTriggerParry(BaseSpell spell, float elapsedTime)
        {
            if (ModEnabled == null || !ModEnabled.Value)
                return false;

            // Ensure this only affects the player
            if (spell != null)
            {
                try
                {
                    if (spell._owner != null && !spell._owner.IsPlayer)
                        return false;
                }
                catch { }
            }

            if (AlwaysPerfectParryWhileHolding != null && AlwaysPerfectParryWhileHolding.Value)
                return true;

            float window = CustomParryWindowSeconds != null ? CustomParryWindowSeconds.Value : 3.0f;
            return elapsedTime <= window;
        }
    }

    // =========================================================================
    // Minotaur: RetaliationPrefab Patches (Auto-casts Retaliation on perfect block)
    // =========================================================================

    [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.HandleLocalIncomingHit))]
    public static class Patch_RetaliationPrefab_HandleLocalIncomingHit
    {
        public static void Prefix(RetaliationPrefab __instance, ObjectsCommon attacker, Spell viaSpell)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._staminaBroken = false;

                    if (PerfectParryModPlugin.MultiHitParry != null && PerfectParryModPlugin.MultiHitParry.Value)
                    {
                        __instance._perfectParryAchieved = false;
                        __instance._retaliationFired = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in RetaliationPrefab.HandleLocalIncomingHit Prefix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.OnCasterDamageBlocked))]
    public static class Patch_RetaliationPrefab_OnCasterDamageBlocked
    {
        public static void Prefix(RetaliationPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._staminaBroken = false;

                    if (PerfectParryModPlugin.MultiHitParry != null && PerfectParryModPlugin.MultiHitParry.Value)
                    {
                        __instance._perfectParryAchieved = false;
                        __instance._retaliationFired = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in RetaliationPrefab.OnCasterDamageBlocked Prefix: {ex}");
            }
        }

        public static void Postfix(RetaliationPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._perfectParryAchieved = true;

                    // Trigger Retaliation if vanilla window did not trigger it
                    if (!__instance._retaliationFired)
                    {
                        try { __instance.ApplyPerfectRetaliation(); } catch { }
                    }

                    try { __instance.RefundSameFrameBlockDrain(); } catch { }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in RetaliationPrefab.OnCasterDamageBlocked Postfix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.ResolveBlockStamina))]
    public static class Patch_RetaliationPrefab_ResolveBlockStamina
    {
        public static void Prefix(RetaliationPrefab __instance, ref float damage, ref float hitArrivalSpellTime)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, hitArrivalSpellTime))
                {
                    // Clamp hit arrival time to 0 to guarantee native code recognizes 0ms perfect parry
                    hitArrivalSpellTime = 0.0f;

                    if (PerfectParryModPlugin.NoStaminaDrainOnParry != null && PerfectParryModPlugin.NoStaminaDrainOnParry.Value)
                    {
                        damage = 0.0f;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in RetaliationPrefab.ResolveBlockStamina Prefix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.ServerValidatePerfectParry))]
    public static class Patch_RetaliationPrefab_ServerValidatePerfectParry
    {
        public static void Postfix(RetaliationPrefab __instance, ref bool __result)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __result = true;
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // Standard / Human: ParryPrefab Patches (Auto-casts Counter Attack on perfect block)
    // =========================================================================

    [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.HandleLocalIncomingHit))]
    public static class Patch_ParryPrefab_HandleLocalIncomingHit
    {
        public static void Prefix(ParryPrefab __instance, ObjectsCommon attacker, Spell viaSpell)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._staminaBroken = false;

                    if (PerfectParryModPlugin.MultiHitParry != null && PerfectParryModPlugin.MultiHitParry.Value)
                    {
                        __instance._perfectParryAchieved = false;
                        __instance._counterAttackFired = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in ParryPrefab.HandleLocalIncomingHit Prefix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.OnCasterDamageBlocked))]
    public static class Patch_ParryPrefab_OnCasterDamageBlocked
    {
        public static void Prefix(ParryPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._staminaBroken = false;

                    if (PerfectParryModPlugin.MultiHitParry != null && PerfectParryModPlugin.MultiHitParry.Value)
                    {
                        __instance._perfectParryAchieved = false;
                        __instance._counterAttackFired = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in ParryPrefab.OnCasterDamageBlocked Prefix: {ex}");
            }
        }

        public static void Postfix(ParryPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._perfectParryAchieved = true;

                    // Trigger Counter Attack if vanilla window did not trigger it
                    if (!__instance._counterAttackFired)
                    {
                        try { __instance.ApplyPerfectParry(); } catch { }
                    }

                    try { __instance.RefundSameFrameBlockDrain(); } catch { }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in ParryPrefab.OnCasterDamageBlocked Postfix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.ResolveBlockStamina))]
    public static class Patch_ParryPrefab_ResolveBlockStamina
    {
        public static void Prefix(ParryPrefab __instance, ref float damage, ref float hitArrivalSpellTime)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, hitArrivalSpellTime))
                {
                    hitArrivalSpellTime = 0.0f;

                    if (PerfectParryModPlugin.NoStaminaDrainOnParry != null && PerfectParryModPlugin.NoStaminaDrainOnParry.Value)
                    {
                        damage = 0.0f;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in ParryPrefab.ResolveBlockStamina Prefix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.ServerValidatePerfectParry))]
    public static class Patch_ParryPrefab_ServerValidatePerfectParry
    {
        public static void Postfix(ParryPrefab __instance, ref bool __result)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __result = true;
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // Rogue / Shadow: VanishPrefab Patches (Auto-casts Vanish on perfect block)
    // =========================================================================

    [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.HandleLocalIncomingHit))]
    public static class Patch_VanishPrefab_HandleLocalIncomingHit
    {
        public static void Prefix(VanishPrefab __instance, ObjectsCommon attacker, Spell viaSpell)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._staminaBroken = false;

                    if (PerfectParryModPlugin.MultiHitParry != null && PerfectParryModPlugin.MultiHitParry.Value)
                    {
                        __instance._perfectParryAchieved = false;
                        __instance._vanishFired = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in VanishPrefab.HandleLocalIncomingHit Prefix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.OnCasterDamageBlocked))]
    public static class Patch_VanishPrefab_OnCasterDamageBlocked
    {
        public static void Prefix(VanishPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._staminaBroken = false;

                    if (PerfectParryModPlugin.MultiHitParry != null && PerfectParryModPlugin.MultiHitParry.Value)
                    {
                        __instance._perfectParryAchieved = false;
                        __instance._vanishFired = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in VanishPrefab.OnCasterDamageBlocked Prefix: {ex}");
            }
        }

        public static void Postfix(VanishPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __instance._perfectParryAchieved = true;

                    // Trigger Vanish if vanilla window did not trigger it
                    if (!__instance._vanishFired)
                    {
                        try { __instance.ApplyPerfectVanish(source); } catch { }
                    }

                    try { __instance.RefundSameFrameBlockDrain(); } catch { }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in VanishPrefab.OnCasterDamageBlocked Postfix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.ResolveBlockStamina))]
    public static class Patch_VanishPrefab_ResolveBlockStamina
    {
        public static void Prefix(VanishPrefab __instance, ref float damage, ref float hitArrivalSpellTime)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, hitArrivalSpellTime))
                {
                    hitArrivalSpellTime = 0.0f;

                    if (PerfectParryModPlugin.NoStaminaDrainOnParry != null && PerfectParryModPlugin.NoStaminaDrainOnParry.Value)
                    {
                        damage = 0.0f;
                    }
                }
            }
            catch (Exception ex)
            {
                PerfectParryModPlugin.Log.LogError($"Error in VanishPrefab.ResolveBlockStamina Prefix: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.ServerValidatePerfectParry))]
    public static class Patch_VanishPrefab_ServerValidatePerfectParry
    {
        public static void Postfix(VanishPrefab __instance, ref bool __result)
        {
            try
            {
                if (PerfectParryModPlugin.ShouldTriggerParry(__instance, __instance._elapsedTime))
                {
                    __result = true;
                }
            }
            catch { }
        }
    }
}
