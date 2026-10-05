using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Stats
{
    public class CarryWeightModule : ModModuleBase
    {
        public static CarryWeightModule Instance { get; private set; }

        public override string Category => "Stats";
        public override string Name => "Carry Weight";
        public override string Description => "Increases maximum carry weight capacity for player inventory";

        public ConfigEntry<float> WeightMultiplier;
        public ConfigEntry<bool> UnlimitedCarryWeight;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Stats.CarryWeight";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Carry Weight Module (Default: false, Vanilla: false)");

            WeightMultiplier = config.Bind(sec, "WeightMultiplier", 10.0f,
                "Multiplier for inventory weight capacity (Default: 10.0, Vanilla: 1.0)");

            UnlimitedCarryWeight = config.Bind(sec, "UnlimitedCarryWeight", false,
                "Unlimited carry capacity / 1,000,000 (Default: false, Vanilla: false)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Unlimited Carry Weight", UnlimitedCarryWeight, "Vanilla: OFF", labelStyle, btnStyle);
            if (!UnlimitedCarryWeight.Value)
            {
                // [2026-09-29 08:51] Max limit 100.0 -> 9999 per user decision (limit survey 4/13, only EXP Multiplier + Carry Weight raised).
                // Old line kept commented out per repo rule:
                // curY += DrawFloatSpinner(x, curY, width, "Weight Multiplier", WeightMultiplier, 1.0f, 1.0f, 100.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
                curY += DrawFloatSpinner(x, curY, width, "Weight Multiplier", WeightMultiplier, 1.0f, 1.0f, 9999f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            }
            return curY - y;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateEncumbrance))]
            [HarmonyPostfix]
            public static void Postfix_CalculateEncumbrance(ref float __result, ObjectsCommon obj, Attributes attributes)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    // [2026-09-28 10:35] OBSOLETE - `obj.IsPlayer` dereferences a possibly
                    // null/stale pointer and hard-crashes with an uncatchable AV during the
                    // character-loading transition (same crash as CustomStatModule). Replaced
                    // with the pointer-identity test against the live local player.
                    // if (obj != null && (obj.IsPlayer || obj.TryCast<Player>() != null))
                    if (PlayerIdentity.IsLocalPlayer(obj))
                    {
                        if (Instance.UnlimitedCarryWeight != null && Instance.UnlimitedCarryWeight.Value)
                        {
                            __result = Formulas.UnlimitedCarryCapacity;
                        }
                        else
                        {
                            float mult = Instance.WeightMultiplier != null ? Instance.WeightMultiplier.Value : 10.0f;
                            if (mult > 1.0f)
                            {
                                __result *= mult;
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
