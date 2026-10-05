using System;
using HarmonyLib;
using UpgradeBonusStatIsNotRandom.Core;

namespace UpgradeBonusStatIsNotRandom.Patches
{
    [HarmonyPatch(typeof(Runes), "GetRandomSecondaryStat", new Type[] { typeof(Rune), typeof(int) })]
    public static class UpgradeRunePatch
    {
        public static string InstantNextRollOverride = "None";

        [HarmonyPrefix]
        public static bool Prefix(Runes __instance, Rune rune, int seed, ref Runes.Stat __result)
        {
            if (!UpgradeBonusStatIsNotRandomPlugin.ModEnabled.Value)
                return true;

            int level = rune.Level;
            string targetName = null;

            // 1. Check if Instant Direct Override is active
            if (!string.IsNullOrEmpty(InstantNextRollOverride) && !InstantNextRollOverride.StartsWith("None", StringComparison.OrdinalIgnoreCase))
            {
                targetName = InstantNextRollOverride;
                // Once fired, reset the single-use instant override unless planned
                InstantNextRollOverride = "None";
            }
            else
            {
                // 2. Check milestone plan for levels 3, 6, 9, 12
                targetName = level switch
                {
                    3 => UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel3.Value,
                    6 => UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel6.Value,
                    9 => UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel9.Value,
                    12 => UpgradeBonusStatIsNotRandomPlugin.TargetStatLevel12.Value,
                    _ => null
                };
            }

            if (string.IsNullOrEmpty(targetName) || targetName.StartsWith("None", StringComparison.OrdinalIgnoreCase))
            {
                return true; // Vanilla seeded RNG
            }

            if (StatCatalog.TryGetStat(targetName, out Runes.Stat desiredStat))
            {
                // Safety check: Don't create duplicate affixes or clash with PrimaryStat
                if (StatCatalog.IsRuneAlreadyHasStat(rune, desiredStat))
                {
                    if (UpgradeBonusStatIsNotRandomPlugin.FallbackToVanillaIfDuplicate.Value)
                    {
                        UpgradeBonusStatIsNotRandomPlugin.Log?.LogWarning(
                            $"[UpgradeBonusStatIsNotRandom] Rune at level {level} already possesses '{desiredStat}'. Falling back to vanilla RNG to prevent invalid affix state.");
                        return true;
                    }
                }

                __result = desiredStat;
                UpgradeBonusStatIsNotRandomPlugin.Log?.LogInfo(
                    $"[UpgradeBonusStatIsNotRandom] Successfully set Level {level} bonus stat -> '{desiredStat}'!");
                return false; // Skip vanilla RNG execution
            }

            return true;
        }
    }
}
