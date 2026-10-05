using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace PursuingBlizzardTuner
{
    /// <summary>
    /// BepInEx 6 (IL2CPP) plugin that tunes the Pursuing Blizzard upgrade for the Blizzard spell:
    ///   * Area of Effect: Increased by +35% (scale 1.35x) instead of vanilla -20% penalty (scale 0.80x).
    ///   * Tick Interval: Damage ticks every 0.5 seconds (half-second) instead of vanilla 1.0 second.
    ///   * Total Ticks: Fixed to exactly 8 damage ticks.
    ///   * Black Hole Suction: Drags all enemies inside the blizzard toward its center using the game's own
    ///     <c>BaseSpellLibrary.DashTargetToPosition</c> (the exact primitive Black Hole uses) on cast and on every
    ///     tick - a 2-per-second yank. Mirrors the Contagion Tuner suction (docs/BlackHole_Dragging_Mechanism.md).
    ///
    /// If Pursuing Blizzard is not unlocked/active on the Blizzard instance, vanilla Blizzard is untouched.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class PursuingBlizzardTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.pursuingblizzardtuner";
        public const string NAME = "Pursuing Blizzard Tuner";
        public const string VERSION = "1.1.0";

        internal static new ManualLogSource Log;
        internal static PursuingBlizzardTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> AreaMultiplier;
        public static ConfigEntry<float> TickInterval;
        public static ConfigEntry<int> TotalTicks;
        public static ConfigEntry<bool> DiagnosticLogging;

        // Black Hole style suction configuration (mirrors ContagionTuner).
        public static ConfigEntry<bool> SuctionEnabled;
        public static ConfigEntry<bool> SuctionOnActivate;
        public static ConfigEntry<bool> SuctionOnTick;
        public static ConfigEntry<float> SuctionRadiusMultiplier;
        public static ConfigEntry<float> SuctionDashTime;
        public static ConfigEntry<float> SuctionDistanceMultiplier;

        // Field offsets with verified vanilla fallbacks
        internal static int PursuingBlizzardOffset = 0x338;
        internal static int PulseCountOffset = 0x368;
        internal static int MaxTimeAliveOffset = 0x1C8;
        internal static int TimeAliveOffset = 0x1CC;
        internal static int OneSecondCounterOffset = 0x1D0;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false, Pursuing Blizzard uses vanilla values. (Default: true)");

            AreaMultiplier = Config.Bind("PursuingBlizzard", "AreaMultiplier", 1.35f,
                "Target area/scale multiplier for Pursuing Blizzard relative to base Blizzard (1.35 = +35% area instead of vanilla 0.80 = -20% area). (Default: 1.35)");

            TickInterval = Config.Bind("PursuingBlizzard", "TickInterval", 0.5f,
                "Seconds between damage pulses/ticks for Pursuing Blizzard. (Default: 0.5)");

            TotalTicks = Config.Bind("PursuingBlizzard", "TotalTicks", 8,
                "Total number of damage pulses/ticks for Pursuing Blizzard. (Default: 8)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log Pursuing Blizzard scale, tick pulses, and lifetime events to the BepInEx console. (Default: false)");

            // [2026-10-06] Black Hole style suction, ported from ContagionTuner.ApplySuction. Uses the game's own
            // BaseSpellLibrary.DashTargetToPosition (the primitive Black Hole uses) so the pull disables the
            // NavMeshAgent during the lerp, raycasts walls, and honours Immune/DisplaceExempt/authority.
            SuctionEnabled = Config.Bind("BlackHoleSuction", "Enabled", true,
                "If true, Pursuing Blizzard drags surrounding enemies into the blizzard center using the game's own " +
                "BaseSpellLibrary.DashTargetToPosition (the exact Black Hole primitive). (Default: true)");

            SuctionOnActivate = Config.Bind("BlackHoleSuction", "SuctionOnActivate", true,
                "Pull all enemies in range into the blizzard immediately when it opens/casts. (Default: true)");

            SuctionOnTick = Config.Bind("BlackHoleSuction", "SuctionOnTick", true,
                "Pull all enemies in range into the blizzard on every damage tick. At the default 0.5s tick " +
                "interval this is a 2-per-second suction. (Default: true)");

            SuctionRadiusMultiplier = Config.Bind("BlackHoleSuction", "RadiusMultiplier", 1.0f,
                "Multiplier on the blizzard's effective radius for how far enemies are grabbed. " +
                "1.0 = same as the blizzard; >1 reaches beyond it. (Default: 1.0)");

            SuctionDashTime = Config.Bind("BlackHoleSuction", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank (Black Hole uses 0.3). (Default: 0.2)");

            SuctionDistanceMultiplier = Config.Bind("BlackHoleSuction", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly " +
                "onto the blizzard center; >1 pulls it past the center for a stronger yank (clamped by walls). (Default: 1.0)");

            ResolveOffsets();

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_BlizzardPrefab_ApplyScaleAndDamageModifiers));
            PatchOrLog(harmony, typeof(Patch_BlizzardPrefab_Update));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"AreaMultiplier: {AreaMultiplier.Value} (+35% AoE)");
            Log.LogInfo($"TickInterval: {TickInterval.Value}s (half-second ticks)");
            Log.LogInfo($"TotalTicks: {TotalTicks.Value}");
            Log.LogInfo($"BlackHoleSuction: {SuctionEnabled.Value} (activate={SuctionOnActivate.Value}, tick={SuctionOnTick.Value}, radiusX{SuctionRadiusMultiplier.Value}, distX{SuctionDistanceMultiplier.Value}, dashTime={SuctionDashTime.Value}s)");
            Log.LogInfo("=================================================");
        }

        private static void ResolveOffsets()
        {
            try
            {
                var bpClass = Il2CppClassPointerStore<BlizzardPrefab>.NativeClassPtr;
                if (bpClass != IntPtr.Zero)
                {
                    PursuingBlizzardOffset = GetFieldOffset(bpClass, "_pursuingBlizzard", 0x338);
                    PulseCountOffset = GetFieldOffset(bpClass, "_pulseCount", 0x368);
                }

                var bslClass = Il2CppClassPointerStore<BaseSpellLibrary>.NativeClassPtr;
                if (bslClass != IntPtr.Zero)
                {
                    MaxTimeAliveOffset = GetFieldOffset(bslClass, "_maxTimeAlive", 0x1C8);
                    TimeAliveOffset = GetFieldOffset(bslClass, "_timealive", 0x1CC);
                    OneSecondCounterOffset = GetFieldOffset(bslClass, "_oneSecondCounter", 0x1D0);
                }

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[Offsets] _pursuingBlizzard=0x{PursuingBlizzardOffset:X}, _pulseCount=0x{PulseCountOffset:X}, _maxTimeAlive=0x{MaxTimeAliveOffset:X}, _timealive=0x{TimeAliveOffset:X}, _oneSecondCounter=0x{OneSecondCounterOffset:X}");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[ResolveOffsets] Exception resolving offsets, using verified fallbacks: {ex.Message}");
            }
        }

        private static int GetFieldOffset(IntPtr classPtr, string fieldName, int fallbackOffset)
        {
            try
            {
                if (classPtr != IntPtr.Zero)
                {
                    var field = IL2CPP.il2cpp_class_get_field_from_name(classPtr, fieldName);
                    if (field != IntPtr.Zero)
                    {
                        int offset = (int)IL2CPP.il2cpp_field_get_offset(field);
                        if (offset > 0) return offset;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Failed to resolve field {fieldName}: {ex.Message}. Using fallback 0x{fallbackOffset:X}");
            }
            return fallbackOffset;
        }

        /// <summary>
        /// Black Hole style suction for Pursuing Blizzard: drag every live enemy inside the blizzard toward the
        /// blizzard center using the game's own <c>BaseSpellLibrary.DashTargetToPosition</c> - the exact primitive
        /// <c>BlackholePrefab.AreaOfEffectStrike</c> uses (docs/BlackHole_Dragging_Mechanism.md), also used by
        /// ContagionTuner. The pull reuses the game's knockback coroutine, which disables the NavMeshAgent during
        /// the lerp and restores it afterward, raycasts walls, and honours <c>ObjectsCommon.Immune</c> /
        /// <c>MonsterBehaviourLibrary.DisplaceExempt</c> / authority. <c>distance</c> is the enemy's current 2D
        /// distance to the blizzard (times DistanceMultiplier), so each call slides it onto the blizzard center.
        /// </summary>
        public static unsafe void ApplySuction(BlizzardPrefab bp)
        {
            if (!Enabled.Value || !SuctionEnabled.Value) return;
            if (bp == null || bp.Pointer == IntPtr.Zero) return;

            try
            {
                Transform t = bp.transform;
                if (t == null) return;
                Vector3 p = t.position;
                Vector2 center = new Vector2(p.x, p.y);

                float radius = bp.GetEffectiveRadius() * SuctionRadiusMultiplier.Value;
                if (radius <= 0.1f) return;

                var enemies = bp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                float dashTime = SuctionDashTime.Value;
                float distMult = SuctionDistanceMultiplier.Value;
                int pulled = 0;
                int count = enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    ObjectsCommon enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    Vector3 ep = enemy.transform.position;
                    float dist = Vector2.Distance(new Vector2(ep.x, ep.y), center);
                    if (dist <= 0.05f) continue;

                    // Same shape as BlackholePrefab.AreaOfEffectStrike / ContagionTuner.ApplySuction:
                    //   DashTargetToPosition(target, center, time, distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot)
                    bp.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[BlizzardSuction] dragged {pulled} enemies toward blizzard ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }

        private static void PatchOrLog(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
                Log.LogInfo($"Patched {patchType.Name} successfully.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch {patchType.Name}: {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on BlizzardPrefab.ApplyScaleAndDamageModifiers.
    /// Vanilla applies a -20% scale reduction (0.80x) when _pursuingBlizzard > 0.
    /// We rescale the transform to +35% (1.35x) and calculate the appropriate spell lifetime for 8 ticks @ 0.5s.
    /// </summary>
    [HarmonyPatch(typeof(BlizzardPrefab), "ApplyScaleAndDamageModifiers")]
    public static class Patch_BlizzardPrefab_ApplyScaleAndDamageModifiers
    {
        public static unsafe void Postfix(BlizzardPrefab __instance)
        {
            try
            {
                if (!PursuingBlizzardTunerPlugin.Enabled.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)__instance.Pointer;
                int pursuing = *(int*)(ptr + PursuingBlizzardTunerPlugin.PursuingBlizzardOffset);
                if (pursuing <= 0) return; // Vanilla Blizzard without Pursuing upgrade is untouched

                // Vanilla applied scale factor 0.80f (-20%).
                // We want target scale 1.35f (+35%).
                // Ratio to convert from 0.80x to 1.35x: 1.35 / 0.80 = 1.6875.
                float scaleAdjustment = PursuingBlizzardTunerPlugin.AreaMultiplier.Value / 0.80f;
                Vector3 currentScale = __instance.transform.localScale;
                Vector3 newScale = currentScale * scaleAdjustment;
                __instance.transform.localScale = newScale;

                // Adjust _maxTimeAlive so the spell lifecycle supports exactly TotalTicks @ TickInterval.
                // 8 ticks at 0.5s: Tick 1 at t=0.0s, Tick 8 at t=3.5s.
                // Allow a small buffer (0.35s) after Tick 8 for smooth audio/VFX dissipation before despawn (3.85s total).
                float targetDuration = (PursuingBlizzardTunerPlugin.TotalTicks.Value - 1) * PursuingBlizzardTunerPlugin.TickInterval.Value + 0.35f;
                float* pMaxTime = (float*)(ptr + PursuingBlizzardTunerPlugin.MaxTimeAliveOffset);
                *pMaxTime = targetDuration;

                // [2026-10-06] Black Hole style suction on cast, via the game's own DashTargetToPosition.
                if (PursuingBlizzardTunerPlugin.SuctionOnActivate.Value)
                {
                    PursuingBlizzardTunerPlugin.ApplySuction(__instance);
                }

                if (PursuingBlizzardTunerPlugin.DiagnosticLogging.Value)
                {
                    PursuingBlizzardTunerPlugin.Log.LogInfo(
                        $"[PursuingBlizzard] Scale adjusted from {currentScale.x:F3} to {newScale.x:F3} (x{scaleAdjustment:F3}). " +
                        $"MaxTimeAlive set to {targetDuration:F2}s for {PursuingBlizzardTunerPlugin.TotalTicks.Value} ticks @ {PursuingBlizzardTunerPlugin.TickInterval.Value:F2}s.");
                }
            }
            catch (Exception ex)
            {
                PursuingBlizzardTunerPlugin.Log?.LogError($"[Patch_BlizzardPrefab_ApplyScaleAndDamageModifiers] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on BlizzardPrefab.Update.
    /// Controls tick pacing and enforces the exact tick cap:
    ///   * When _oneSecondCounter reaches 0.5s, it sets it to 1.0s so BaseSpell.OneSecondUpdate triggers On1SecondUpdate().
    ///   * When _pulseCount reaches TotalTicks (8), clamps timer to 0 so no further pulses fire before despawn.
    /// </summary>
    [HarmonyPatch(typeof(BlizzardPrefab), "Update")]
    public static class Patch_BlizzardPrefab_Update
    {
        public static unsafe void Prefix(BlizzardPrefab __instance)
        {
            try
            {
                if (!PursuingBlizzardTunerPlugin.Enabled.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)__instance.Pointer;
                int pursuing = *(int*)(ptr + PursuingBlizzardTunerPlugin.PursuingBlizzardOffset);
                if (pursuing <= 0) return;

                int pulses = *(int*)(ptr + PursuingBlizzardTunerPlugin.PulseCountOffset);
                int maxTicks = PursuingBlizzardTunerPlugin.TotalTicks.Value;

                float* pCounter = (float*)(ptr + PursuingBlizzardTunerPlugin.OneSecondCounterOffset);

                if (pulses >= maxTicks)
                {
                    // Exactly 8 ticks have fired. Clamp counter to 0 so no 9th pulse can trigger while waiting to despawn.
                    if (*pCounter >= PursuingBlizzardTunerPlugin.TickInterval.Value)
                    {
                        *pCounter = 0f;
                    }
                    return;
                }

                // If timer has accumulated up to our configured interval (0.5s), boost to 1.0s.
                // BaseSpell.OneSecondUpdate() in base.Update() will see >= 1.0f, invoke On1SecondUpdate() natively,
                // and reset the counter back to 0.0f.
                if (*pCounter >= PursuingBlizzardTunerPlugin.TickInterval.Value)
                {
                    *pCounter = 1.0f;

                    // [2026-10-06] Black Hole style suction on each tick (2/sec at the default 0.5s interval).
                    if (PursuingBlizzardTunerPlugin.SuctionOnTick.Value)
                    {
                        PursuingBlizzardTunerPlugin.ApplySuction(__instance);
                    }

                    if (PursuingBlizzardTunerPlugin.DiagnosticLogging.Value)
                    {
                        PursuingBlizzardTunerPlugin.Log.LogInfo(
                            $"[PursuingBlizzard] Triggering tick pulse {pulses + 1}/{maxTicks} (interval {PursuingBlizzardTunerPlugin.TickInterval.Value:F2}s)");
                    }
                }
            }
            catch (Exception ex)
            {
                PursuingBlizzardTunerPlugin.Log?.LogError($"[Patch_BlizzardPrefab_Update] {ex}");
            }
        }
    }
}
