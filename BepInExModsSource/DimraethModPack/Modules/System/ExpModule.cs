using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    public class ExpModule : ModModuleBase
    {
        public static ExpModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "EXP Multiplier";
        public override string Description => "Multiplier for earned experience (combat, quests, rewards, and pets)";

        public ConfigEntry<int> ExpMultiplier;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.ExpMod";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable EXP Multiplier Module (Default: false, Vanilla: false)");

            ExpMultiplier = config.Bind(sec, "ExpMultiplier", 5,
                "Multiplier for earned EXP (Default: 5, Vanilla: 1)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            // [2026-09-29 08:51] Max limit 100 -> 9999 per user decision (limit survey 1/13, only EXP Multiplier + Carry Weight raised).
            // Old line kept commented out per repo rule:
            // curY += DrawIntSpinner(x, curY, width, "EXP Multiplier", ExpMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            curY += DrawIntSpinner(x, curY, width, "EXP Multiplier", ExpMultiplier, 1, 1, 9999, "Vanilla: 1x", labelStyle, btnStyle);
            return curY - y;
        }

        public static class Patches
        {
            [ThreadStatic]
            private static bool _isCombatXpInFlight = false;

            [HarmonyPatch(typeof(XP), nameof(XP.CalculateXPGained))]
            [HarmonyPostfix]
            public static void Postfix_CalculateXPGained(ref int __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    int mult = Instance.ExpMultiplier.Value;
                    if (mult > 1 && __result > 0)
                    {
                        __result *= mult;
                    }
                }
                catch { }
            }

            [HarmonyPatch(typeof(XP), nameof(XP.GrantXP))]
            public static class Patch_GrantXP
            {
                [HarmonyPrefix]
                public static void Prefix()
                {
                    _isCombatXpInFlight = true;
                }

                [HarmonyFinalizer]
                public static void Finalizer()
                {
                    _isCombatXpInFlight = false;
                }
            }

            [HarmonyPatch(typeof(XP), nameof(XP.GrantXPToPlayer))]
            [HarmonyPrefix]
            public static void Prefix_GrantXPToPlayer(Player player, ref int xp, string source, bool showPopup)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    if (_isCombatXpInFlight) return; // Prevent double-scaling if GrantXP internally routes to GrantXPToPlayer

                    int mult = Instance.ExpMultiplier.Value;
                    if (mult > 1 && xp > 0)
                    {
                        xp *= mult;
                    }
                }
                catch { }
            }

            [HarmonyPatch(typeof(XP), nameof(XP.AddXPToPet))]
            [HarmonyPrefix]
            public static void Prefix_AddXPToPet(Player player, ref int xp)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    int mult = Instance.ExpMultiplier.Value;
                    if (mult > 1 && xp > 0)
                    {
                        xp *= mult;
                    }
                }
                catch { }
            }
        }
    }
}
