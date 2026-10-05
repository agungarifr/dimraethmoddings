using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    public class PerfectParryModule : ModModuleBase
    {
        public static PerfectParryModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Perfect Parry";
        public override string Description => "Extends perfect parry timing window, counter attack timing, and blocks without stamina drain";

        public const float VanillaParryWindow = 0.08f;

        public ConfigEntry<bool> AlwaysPerfectParryWhileHolding;
        public ConfigEntry<float> CustomParryWindowSeconds;
        public ConfigEntry<bool> MultiHitParry;
        public ConfigEntry<bool> NoStaminaDrainOnParry;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.PerfectParry";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Perfect Parry Module (Default: false, Vanilla: false)");

            AlwaysPerfectParryWhileHolding = config.Bind(sec, "AlwaysPerfectParryWhileHolding", false,
                "Always trigger Perfect Parry while holding block button (Default: false, Vanilla: false)");

            CustomParryWindowSeconds = config.Bind(sec, "CustomParryWindowSeconds", 2.0f,
                "Perfect parry timing window in seconds (Default: 2.0, Vanilla: 0.08)");

            MultiHitParry = config.Bind(sec, "MultiHitParry", true,
                "Allow chained parries against rapid multi-hit attacks (Default: true, Vanilla: false)");

            NoStaminaDrainOnParry = config.Bind(sec, "NoStaminaDrainOnParry", true,
                "Negate stamina drain on successful parries or blocks within window (Default: true, Vanilla: false)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            // Minotaur (Retaliation)
            harmony.PatchAll(typeof(Patch_RetaliationPrefab_HandleLocalIncomingHit));
            harmony.PatchAll(typeof(Patch_RetaliationPrefab_OnCasterDamageBlocked));
            harmony.PatchAll(typeof(Patch_RetaliationPrefab_ResolveBlockStamina));
            harmony.PatchAll(typeof(Patch_RetaliationPrefab_ServerValidatePerfectParry));

            // Standard / Human (Parry)
            harmony.PatchAll(typeof(Patch_ParryPrefab_HandleLocalIncomingHit));
            harmony.PatchAll(typeof(Patch_ParryPrefab_OnCasterDamageBlocked));
            harmony.PatchAll(typeof(Patch_ParryPrefab_ResolveBlockStamina));
            harmony.PatchAll(typeof(Patch_ParryPrefab_ServerValidatePerfectParry));

            // Rogue (Vanish)
            harmony.PatchAll(typeof(Patch_VanishPrefab_HandleLocalIncomingHit));
            harmony.PatchAll(typeof(Patch_VanishPrefab_OnCasterDamageBlocked));
            harmony.PatchAll(typeof(Patch_VanishPrefab_ResolveBlockStamina));
            harmony.PatchAll(typeof(Patch_VanishPrefab_ServerValidatePerfectParry));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Always Perfect On Hold", AlwaysPerfectParryWhileHolding, "Vanilla: OFF", labelStyle, btnStyle);
            if (!AlwaysPerfectParryWhileHolding.Value)
            {
                curY += DrawFloatSpinner(x, curY, width, "Parry Window", CustomParryWindowSeconds, 0.2f, 0.08f, 5.0f, $"Vanilla: {VanillaParryWindow:F2}s", labelStyle, btnStyle, "0.00s");
            }
            curY += DrawToggle(x, curY, width, "Multi-Hit Parry", MultiHitParry, "Vanilla: OFF", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "No Stamina Drain", NoStaminaDrainOnParry, "Vanilla: OFF", labelStyle, btnStyle);
            return curY - y;
        }

        public static bool ShouldTriggerParry(BaseSpell spell, float elapsedTime)
        {
            if (Instance == null || !Instance.IsEnabled) return false;
            if (spell != null)
            {
                try
                {
                    // [2026-09-28 10:35] OBSOLETE - `spell._owner.IsPlayer` is a native field
                    // read on a possibly null/stale pointer (uncatchable AV). Match the live
                    // local player by pointer instead.
                    // if (spell._owner != null && !spell._owner.IsPlayer) return false;
                    if (!PlayerIdentity.IsLocalPlayer(spell._owner)) return false;
                }
                catch { }
            }

            if (Instance.AlwaysPerfectParryWhileHolding != null && Instance.AlwaysPerfectParryWhileHolding.Value) return true;
            float window = Instance.CustomParryWindowSeconds != null ? Instance.CustomParryWindowSeconds.Value : 2.0f;
            return elapsedTime <= window;
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
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __instance._staminaBroken = false;

                        if (Instance?.MultiHitParry != null && Instance.MultiHitParry.Value)
                        {
                            __instance._perfectParryAchieved = false;
                            __instance._retaliationFired = false;
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.OnCasterDamageBlocked))]
        public static class Patch_RetaliationPrefab_OnCasterDamageBlocked
        {
            public static void Prefix(RetaliationPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __instance._staminaBroken = false;

                        if (Instance?.MultiHitParry != null && Instance.MultiHitParry.Value)
                        {
                            __instance._perfectParryAchieved = false;
                            __instance._retaliationFired = false;
                        }
                    }
                }
                catch { }
            }

            public static void Postfix(RetaliationPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
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
                catch { }
            }
        }

        [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.ResolveBlockStamina))]
        public static class Patch_RetaliationPrefab_ResolveBlockStamina
        {
            public static void Prefix(RetaliationPrefab __instance, ref float damage, ref float hitArrivalSpellTime)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, hitArrivalSpellTime))
                    {
                        // Clamp hit arrival time to 0 to guarantee native code recognizes 0ms perfect parry
                        hitArrivalSpellTime = 0.0f;

                        if (Instance?.NoStaminaDrainOnParry != null && Instance.NoStaminaDrainOnParry.Value)
                        {
                            damage = 0.0f;
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(RetaliationPrefab), nameof(RetaliationPrefab.ServerValidatePerfectParry))]
        public static class Patch_RetaliationPrefab_ServerValidatePerfectParry
        {
            public static void Postfix(RetaliationPrefab __instance, ref bool __result)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
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
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __instance._staminaBroken = false;

                        if (Instance?.MultiHitParry != null && Instance.MultiHitParry.Value)
                        {
                            __instance._perfectParryAchieved = false;
                            __instance._counterAttackFired = false;
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.OnCasterDamageBlocked))]
        public static class Patch_ParryPrefab_OnCasterDamageBlocked
        {
            public static void Prefix(ParryPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __instance._staminaBroken = false;

                        if (Instance?.MultiHitParry != null && Instance.MultiHitParry.Value)
                        {
                            __instance._perfectParryAchieved = false;
                            __instance._counterAttackFired = false;
                        }
                    }
                }
                catch { }
            }

            public static void Postfix(ParryPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
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
                catch { }
            }
        }

        [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.ResolveBlockStamina))]
        public static class Patch_ParryPrefab_ResolveBlockStamina
        {
            public static void Prefix(ParryPrefab __instance, ref float damage, ref float hitArrivalSpellTime)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, hitArrivalSpellTime))
                    {
                        hitArrivalSpellTime = 0.0f;

                        if (Instance?.NoStaminaDrainOnParry != null && Instance.NoStaminaDrainOnParry.Value)
                        {
                            damage = 0.0f;
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(ParryPrefab), nameof(ParryPrefab.ServerValidatePerfectParry))]
        public static class Patch_ParryPrefab_ServerValidatePerfectParry
        {
            public static void Postfix(ParryPrefab __instance, ref bool __result)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
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
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __instance._staminaBroken = false;

                        if (Instance?.MultiHitParry != null && Instance.MultiHitParry.Value)
                        {
                            __instance._perfectParryAchieved = false;
                            __instance._vanishFired = false;
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.OnCasterDamageBlocked))]
        public static class Patch_VanishPrefab_OnCasterDamageBlocked
        {
            public static void Prefix(VanishPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __instance._staminaBroken = false;

                        if (Instance?.MultiHitParry != null && Instance.MultiHitParry.Value)
                        {
                            __instance._perfectParryAchieved = false;
                            __instance._vanishFired = false;
                        }
                    }
                }
                catch { }
            }

            public static void Postfix(VanishPrefab __instance, ObjectsCommon source, DamageType damageType, float damage, float range)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
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
                catch { }
            }
        }

        [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.ResolveBlockStamina))]
        public static class Patch_VanishPrefab_ResolveBlockStamina
        {
            public static void Prefix(VanishPrefab __instance, ref float damage, ref float hitArrivalSpellTime)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, hitArrivalSpellTime))
                    {
                        hitArrivalSpellTime = 0.0f;

                        if (Instance?.NoStaminaDrainOnParry != null && Instance.NoStaminaDrainOnParry.Value)
                        {
                            damage = 0.0f;
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(VanishPrefab), nameof(VanishPrefab.ServerValidatePerfectParry))]
        public static class Patch_VanishPrefab_ServerValidatePerfectParry
        {
            public static void Postfix(VanishPrefab __instance, ref bool __result)
            {
                try
                {
                    if (ShouldTriggerParry(__instance, __instance._elapsedTime))
                    {
                        __result = true;
                    }
                }
                catch { }
            }
        }
    }
}
