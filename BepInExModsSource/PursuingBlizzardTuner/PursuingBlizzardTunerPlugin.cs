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
    /// [2026-10-06] Retuned: area 1.75x, 12 ticks @ 0.5s over a 6.0s lifetime, per-tick damage x0.90.
    // [2026-10-06] Previous tuning kept for reference (obsolete): +35% area (1.35x), 8 ticks, 3.85s, vanilla damage.
    //   * Area of Effect: Increased by +35% (scale 1.35x) instead of vanilla -20% penalty (scale 0.80x).
    //   * Tick Interval: Damage ticks every 0.5 seconds (half-second) instead of vanilla 1.0 second.
    //   * Total Ticks: Fixed to exactly 8 damage ticks.
    ///   * Area of Effect: Increased by +75% (scale 1.75x) instead of vanilla -20% penalty (scale 0.80x).
    ///   * Tick Interval: Damage ticks every 0.5 seconds (half-second) instead of vanilla 1.0 second.
    ///   * Total Ticks: Fixed to exactly 12 damage ticks over a 6.0 second lifetime.
    ///   * Damage Per Tick: Reduced by 10% (x0.90) versus vanilla per-tick damage.
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
        public const string VERSION = "1.2.0";

        internal static new ManualLogSource Log;
        internal static PursuingBlizzardTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> AreaMultiplier;
        public static ConfigEntry<float> TickInterval;
        public static ConfigEntry<int> TotalTicks;
        public static ConfigEntry<float> DamageMultiplier;

        // [2026-10-06] VFX tuning: scale World-space particle sizes to match the larger area, and extend the
        // persistent storm/spike systems so the visuals cover the full spell lifetime.
        public static ConfigEntry<bool> ScaleVfxParticles;
        public static ConfigEntry<bool> MatchVfxLifetime;
        public static ConfigEntry<bool> LogVfxTuning;

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
        internal static int DamageMultiplierOffset = 0x344; // BlizzardPrefab._damageMultiplier
        // [2026-10-06] Persistent VFX roots on BlizzardPrefab (children of the spell transform).
        internal static int StormVfxOffset = 0x2E8;        // _stormVFX
        internal static int SpikesVfxOffset = 0x2F0;       // _spikesVFX
        internal static int OBlizzardOffset = 0x300;       // _o_blizzard
        internal static int OSoftBlizzardOffset = 0x308;   // _o_softBlizzard

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false, Pursuing Blizzard uses vanilla values. (Default: true)");

            AreaMultiplier = Config.Bind("PursuingBlizzard", "AreaMultiplier", 1.75f,
                "Target area/scale multiplier for Pursuing Blizzard relative to base Blizzard (1.75 = +75% area instead of vanilla 0.80 = -20% area). (Default: 1.75)");

            TickInterval = Config.Bind("PursuingBlizzard", "TickInterval", 0.5f,
                "Seconds between damage pulses/ticks for Pursuing Blizzard. (Default: 0.5)");

            TotalTicks = Config.Bind("PursuingBlizzard", "TotalTicks", 12,
                "Total number of damage pulses/ticks for Pursuing Blizzard. (Default: 12)");

            DamageMultiplier = Config.Bind("PursuingBlizzard", "DamageMultiplier", 0.90f,
                "Multiplier applied to Pursuing Blizzard's per-tick damage. 1.0 = vanilla per-tick damage; " +
                "0.9 = 10% less damage per tick. (Default: 0.90)");

            // [2026-10-06] VFX tuning binds.
            ScaleVfxParticles = Config.Bind("Vfx", "ScaleVfxParticles", true,
                "If true, set the storm/spike particle systems to Hierarchy scaling so particle sizes follow the " +
                "enlarged spell transform (Local scaling ignores parent scale). (Default: true)");

            MatchVfxLifetime = Config.Bind("Vfx", "MatchVfxLifetime", true,
                "If true, enable looping on the persistent storm/spike VFX so they keep emitting for the full spell " +
                "lifetime (12 ticks @ 0.5s = 6.0s). (Default: true)");

            LogVfxTuning = Config.Bind("Vfx", "LogVfxTuning", true,
                "Log how many particle systems were resized/lifetime-extended. Temporary recon. (Default: true)");

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
            Log.LogInfo($"AreaMultiplier: {AreaMultiplier.Value} (x{AreaMultiplier.Value / 0.80f:F3} vs vanilla pursuing 0.80)");
            Log.LogInfo($"TickInterval: {TickInterval.Value}s (half-second ticks)");
            Log.LogInfo($"TotalTicks: {TotalTicks.Value} (lifetime {TotalTicks.Value * TickInterval.Value:F2}s)");
            Log.LogInfo($"DamageMultiplier: x{DamageMultiplier.Value} per tick");
            Log.LogInfo($"Vfx: scaleParticles={ScaleVfxParticles.Value}, matchLifetime={MatchVfxLifetime.Value}, log={LogVfxTuning.Value}");
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
                    DamageMultiplierOffset = GetFieldOffset(bpClass, "_damageMultiplier", 0x344);
                    StormVfxOffset = GetFieldOffset(bpClass, "_stormVFX", 0x2E8);
                    SpikesVfxOffset = GetFieldOffset(bpClass, "_spikesVFX", 0x2F0);
                    OBlizzardOffset = GetFieldOffset(bpClass, "_o_blizzard", 0x300);
                    OSoftBlizzardOffset = GetFieldOffset(bpClass, "_o_softBlizzard", 0x308);
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
                    Log.LogInfo($"[Offsets] _pursuingBlizzard=0x{PursuingBlizzardOffset:X}, _pulseCount=0x{PulseCountOffset:X}, _damageMultiplier=0x{DamageMultiplierOffset:X}, _maxTimeAlive=0x{MaxTimeAliveOffset:X}, _timealive=0x{TimeAliveOffset:X}, _oneSecondCounter=0x{OneSecondCounterOffset:X}, _stormVFX=0x{StormVfxOffset:X}, _spikesVFX=0x{SpikesVfxOffset:X}, _o_blizzard=0x{OBlizzardOffset:X}, _o_softBlizzard=0x{OSoftBlizzardOffset:X}");
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

        /// <summary>
        /// [2026-10-06] VFX tuning for the retuned Pursuing Blizzard. The IL2CPP interop exposes
        /// MainModule.duration as read-only and startSize as write-only, so we use the two settable levers:
        ///   * Scale: set the particle systems to ParticleSystemScalingMode.Hierarchy so particle sizes follow the
        ///     enlarged spell transform (Local scaling ignores parent scale, which is why the storm looked small).
        ///   * Lifetime: enable looping on the persistent storm/spike VFX roots so they keep emitting for the whole
        ///     spell lifetime. The per-tick blast bursts (_blastShards / _blastMist) are intentionally untouched.
        /// </summary>
        public static unsafe void ApplyVfxTuning(BlizzardPrefab bp, float areaRatio, float lifetime)
        {
            if (!Enabled.Value) return;
            if (bp == null || bp.Pointer == IntPtr.Zero) return;

            int scaled = 0;
            int looped = 0;

            try
            {
                // --- Scale: Hierarchy scaling makes particle size follow the (enlarged) spell transform. ---
                if (ScaleVfxParticles.Value)
                {
                    var all = bp.GetComponentsInChildren<ParticleSystem>(true);
                    if (all != null)
                    {
                        foreach (var ps in all)
                        {
                            if (ps == null) continue;
                            try
                            {
                                var main = ps.main;
                                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                                scaled++;
                            }
                            catch { /* one bad system should not abort the rest */ }
                        }
                    }
                }

                // --- Lifetime: persistent storm/spike VFX roots only. ---
                if (MatchVfxLifetime.Value)
                {
                    byte* ptr = (byte*)bp.Pointer;
                    int[] rootOffsets = { StormVfxOffset, SpikesVfxOffset, OBlizzardOffset, OSoftBlizzardOffset };
                    foreach (int off in rootOffsets)
                    {
                        IntPtr goPtr = *(IntPtr*)(ptr + off);
                        if (goPtr == IntPtr.Zero) continue;

                        var go = new GameObject(goPtr);
                        if (go == null) continue;

                        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
                        if (systems == null) continue;
                        foreach (var ps in systems)
                        {
                            if (ps == null) continue;
                            try
                            {
                                var main = ps.main;
                                if (!main.loop)
                                {
                                    main.loop = true;
                                    looped++;
                                }
                            }
                            catch { /* one bad system should not abort the rest */ }
                        }
                    }
                }

                if (LogVfxTuning.Value && (scaled > 0 || looped > 0))
                {
                    Log.LogInfo($"[BlizzardVfx] set {scaled} system(s) to Hierarchy scaling (area x{areaRatio:F2}); set {looped} persistent system(s) to loop for the {lifetime:F2}s lifetime");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplyVfxTuning] {ex}");
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
    /// [2026-10-06] Retuned: rescale the transform to AreaMultiplier (default 1.75x = +75%), set lifetime for
    /// TotalTicks @ TickInterval (default 12 @ 0.5s = 6.0s), scale per-tick damage by DamageMultiplier (default 0.90),
    /// and scale/extend the storm VFX to match.
    // [2026-10-06] Previous behaviour kept for reference (obsolete): transform 1.35x (+35%) and 8 ticks @ 0.5s (3.85s), vanilla damage.
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
                // We want target scale = AreaMultiplier (default 1.75 => +75%).
                // Ratio to convert from the vanilla 0.80x to the target: AreaMultiplier / 0.80.
                float scaleAdjustment = PursuingBlizzardTunerPlugin.AreaMultiplier.Value / 0.80f;
                Vector3 currentScale = __instance.transform.localScale;
                Vector3 newScale = currentScale * scaleAdjustment;
                __instance.transform.localScale = newScale;

                // Adjust _maxTimeAlive so the spell lifecycle supports exactly TotalTicks @ TickInterval.
                // OnStart seeds _oneSecondCounter = 1.0f, so Tick 1 fires on the first Update; then every
                // TickInterval. Last tick is at (TotalTicks - 1) * TickInterval, and TotalTicks * TickInterval
                // gives that span plus one interval of tail before despawn.
                // [2026-10-06] Replaced the old formula (kept for reference):
                //   float targetDuration = (TotalTicks - 1) * TickInterval + 0.35f;   // 8 ticks @ 0.5s => 3.85s
                float targetDuration = PursuingBlizzardTunerPlugin.TotalTicks.Value * PursuingBlizzardTunerPlugin.TickInterval.Value;
                float* pMaxTime = (float*)(ptr + PursuingBlizzardTunerPlugin.MaxTimeAliveOffset);
                *pMaxTime = targetDuration;

                // [2026-10-06] Per-tick damage tuning: multiply the vanilla _damageMultiplier by DamageMultiplier.
                if (PursuingBlizzardTunerPlugin.DamageMultiplier.Value != 1.0f)
                {
                    float* pDamage = (float*)(ptr + PursuingBlizzardTunerPlugin.DamageMultiplierOffset);
                    *pDamage *= PursuingBlizzardTunerPlugin.DamageMultiplier.Value;
                }

                // [2026-10-06] Scale + lifetime tuning for the storm/spike VFX.
                PursuingBlizzardTunerPlugin.ApplyVfxTuning(__instance, PursuingBlizzardTunerPlugin.AreaMultiplier.Value, targetDuration);

                // [2026-10-06] Black Hole style suction on cast, via the game's own DashTargetToPosition.
                if (PursuingBlizzardTunerPlugin.SuctionOnActivate.Value)
                {
                    PursuingBlizzardTunerPlugin.ApplySuction(__instance);
                }

                if (PursuingBlizzardTunerPlugin.DiagnosticLogging.Value)
                {
                    PursuingBlizzardTunerPlugin.Log.LogInfo(
                        $"[PursuingBlizzard] Scale adjusted from {currentScale.x:F3} to {newScale.x:F3} (x{scaleAdjustment:F3}). " +
                        $"MaxTimeAlive set to {targetDuration:F2}s for {PursuingBlizzardTunerPlugin.TotalTicks.Value} ticks @ {PursuingBlizzardTunerPlugin.TickInterval.Value:F2}s. " +
                        $"DamageMultiplier x{PursuingBlizzardTunerPlugin.DamageMultiplier.Value}.");
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
    ///   * When _pulseCount reaches TotalTicks, clamps timer to 0 so no further pulses fire before despawn.
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
                    // Exactly TotalTicks have fired. Clamp counter to 0 so no extra pulse can trigger while waiting to despawn.
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
