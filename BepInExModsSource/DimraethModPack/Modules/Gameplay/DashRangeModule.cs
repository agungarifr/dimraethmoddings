using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    /// <summary>
    /// [2026-10-16] Multiplies the player's dash distance (default 2x vanilla), per user request:
    /// "buat mod baru untuk di integrasi ke dimraeth mod pack, ini modif dash range jadi 2x jarak vanilla".
    ///
    /// The dedicated dash is the <c>PlayerDashPrefab</c> spell triggered by the Dash input action. Its
    /// <c>OnStart</c> is the ONLY caller of <c>ObjectsCommon.DashInDirection</c> (dash while holding a
    /// movement direction) and <c>ObjectsCommon.DashInFacingDirection</c> (dash without a direction), so
    /// scaling the <c>distance</c> argument of those two methods changes exactly the player's dash and
    /// nothing else:
    ///   * monster lunges / charge attacks reach their dash through
    ///     <c>BaseSpellLibrary.DashCasterForwardToPosition</c> -> the separate BaseSpellLibrary dash helper,
    ///     NOT these two methods;
    ///   * spell pulls/drags (<c>DashTargetToPosition</c> etc.) are a different primitive entirely.
    /// Duration is left at vanilla (0.3s), so doubling the distance also doubles the dash speed - the dash
    /// covers 2x the ground in the same time.
    /// </summary>
    public class DashRangeModule : ModModuleBase
    {
        public static DashRangeModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Dash Range";
        public override string Description => "Multiply the player's dash distance (default 2x vanilla)";

        public ConfigEntry<float> DistanceMultiplier;
        public ConfigEntry<bool> DiagnosticLogging;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            const string sec = "Gameplay.DashRange";

            Enabled = config.Bind(sec, "Enabled", true,
                "Master switch. When false the dash keeps its vanilla distance. (Default: true)");

            DistanceMultiplier = config.Bind(sec, "DistanceMultiplier", 2.0f,
                "Multiplier applied to the player's dash distance. 2.0 = 2x vanilla reach (the requested " +
                "value). The dash duration is unchanged, so the dash also moves 2x faster. (Default: 2.0)");

            DiagnosticLogging = config.Bind(sec, "DiagnosticLogging", false,
                "Log each player dash (vanilla distance -> scaled distance) to the BepInEx console. (Default: false)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patch_DashInFacingDirection));
            harmony.PatchAll(typeof(Patch_DashInDirection));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawFloatSpinner(x, curY, width, "Distance Multiplier", DistanceMultiplier,
                0.1f, 0.1f, 20f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            return curY - y;
        }

        /// <summary>Shared scaling applied by both dash prefixes.</summary>
        internal static void ScaleDash(ObjectsCommon instance, ref float distance)
        {
            if (Instance == null || !Instance.IsEnabled) return;

            try
            {
                if (instance == null || instance.Pointer == IntPtr.Zero) return;

                // Only the player's dedicated dash. Monster dashes never reach these two methods.
                var player = instance.TryCast<Player>();
                if (player == null) return;

                float mult = Instance.DistanceMultiplier.Value;
                if (mult <= 0f || Math.Abs(mult - 1f) < 0.0001f) return;
                if (distance <= 0f) return;

                float vanilla = distance;
                distance = vanilla * mult;

                if (Instance.DiagnosticLogging.Value)
                {
                    DimraethModPackPlugin.Log?.LogInfo(
                        $"[DashRange] player dash distance {vanilla:0.###} -> {distance:0.###} (x{mult:0.##})");
                }
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogError($"[DashRange] {ex}");
            }
        }

        /// <summary>
        /// Prefix on <c>ObjectsCommon.DashInFacingDirection(float duration, float distance, ...)</c>.
        /// PlayerDashPrefab.OnStart calls this when the player dashes WITHOUT a movement direction held.
        /// </summary>
        [HarmonyPatch(typeof(ObjectsCommon), "DashInFacingDirection")]
        public static class Patch_DashInFacingDirection
        {
            public static void Prefix(ObjectsCommon __instance, ref float distance)
                => ScaleDash(__instance, ref distance);
        }

        /// <summary>
        /// Prefix on <c>ObjectsCommon.DashInDirection(Vector2 dashDirection, float duration, float distance, ...)</c>.
        /// PlayerDashPrefab.OnStart calls this when the player dashes WHILE holding a movement direction.
        /// </summary>
        [HarmonyPatch(typeof(ObjectsCommon), "DashInDirection")]
        public static class Patch_DashInDirection
        {
            public static void Prefix(ObjectsCommon __instance, ref float distance)
                => ScaleDash(__instance, ref distance);
        }
    }
}
