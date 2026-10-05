using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace ContagionTuner
{
    /// <summary>
    /// BepInEx 6 (IL2CPP) plugin that tunes the Contagion spell:
    ///   * Area of Effect: Increased by +50% (scale and radius multiplier 1.50x).
    ///   * Ticks: Increased by 2x (damage and poison tick every 0.5s instead of 1.0s, delivering 12 ticks in 6s).
    ///   * Black Hole Suction: Drags all surrounding enemies into the infected target host using the game's
    ///     own <c>BaseSpellLibrary.DashTargetToPosition</c> (the exact primitive Black Hole uses) on cast and
    ///     on every tick - a 2-per-second, full-distance yank. The infected host itself is never pulled.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ContagionTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.contagiontuner";
        public const string NAME = "Contagion Tuner";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static ContagionTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> AreaMultiplier;
        public static ConfigEntry<float> TickCountMultiplier;
        public static ConfigEntry<float> TickInterval;
        public static ConfigEntry<float> LifetimeSafetyBuffer;
        public static ConfigEntry<bool> DiagnosticLogging;

        // Black Hole spellbook suction configuration
        public static ConfigEntry<bool> SuctionEnabled;
        public static ConfigEntry<bool> SuctionOnActivate;
        public static ConfigEntry<bool> SuctionOnTick;
        public static ConfigEntry<float> SuctionRadiusMultiplier;
        public static ConfigEntry<float> SuctionDashTime;
        public static ConfigEntry<float> SuctionDistanceMultiplier;

        // [2026-10-05 09:15] OBSOLETE - the old continuous NavMeshAgent.Move suction knobs.
        // Superseded by the DashTargetToPosition dash (SuctionDashTime / SuctionRadiusMultiplier /
        // SuctionDistanceMultiplier). NavMeshAgent.Move fought the monster AI and was disabled 2026-10-04.
        // public static ConfigEntry<bool> SuctionContinuous;
        // public static ConfigEntry<float> SuctionContinuousSpeed;
        // public static ConfigEntry<float> SuctionPulseDistance;

        // Field offsets with verified vanilla fallbacks
        internal static int RadiusOffset = 0x340;
        internal static int VfxOffset = 0x2E8;
        internal static int ZoneRemainingOffset = 0x368;
        internal static int MaxTimeAliveOffset = 0x1C8;
        internal static int CenterOffset = 0x338;
        internal static int EndedOffset = 0x354;
        internal static int InfectedTargetOffset = 0x330;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false, Contagion uses vanilla values. (Default: true)");

            AreaMultiplier = Config.Bind("Contagion", "AreaMultiplier", 1.50f,
                "Target area and radius multiplier for Contagion (1.50 = +50% area/radius). (Default: 1.50)");

            TickCountMultiplier = Config.Bind("Contagion", "TickCountMultiplier", 2.0f,
                "Multiplier for total ticks across the spell duration (2.0 = 2x ticks, e.g. 12 ticks instead of 6). (Default: 2.0)");

            TickInterval = Config.Bind("Contagion", "TickInterval", 0.5f,
                "Seconds between damage and poison ticks for Contagion (0.5 = tick every half-second). (Default: 0.5)");

            LifetimeSafetyBuffer = Config.Bind("Contagion", "LifetimeSafetyBuffer", 0.35f,
                "Safety buffer in seconds added to MaxTimeAlive to ensure the final tick and zone ending complete cleanly before despawn. (Default: 0.35)");

            // [2026-10-05 09:15] Re-enabled as a Black Hole drag using the game's own DashTargetToPosition
            // (the primitive TwisterTuner uses). The 2026-10-04 "glitchy" verdict applied to the old
            // NavMeshAgent.Move implementation, not to the game's own knockback dash.
            SuctionEnabled = Config.Bind("BlackHoleSuction", "Enabled", true,
                "If true, Contagion drags surrounding enemies into the infected host using the game's own " +
                "BaseSpellLibrary.DashTargetToPosition (the exact Black Hole primitive). (Default: true)");

            SuctionOnActivate = Config.Bind("BlackHoleSuction", "SuctionOnActivate", true,
                "Pull all nearby enemies into the infected host immediately when Contagion opens/casts. (Default: true)");

            SuctionOnTick = Config.Bind("BlackHoleSuction", "SuctionOnTick", true,
                "Pull all nearby enemies into the infected host on every damage/poison tick. At the default " +
                "0.5s tick interval this is a 2-per-second suction, stronger than Black Hole's 1-per-second. (Default: true)");

            SuctionRadiusMultiplier = Config.Bind("BlackHoleSuction", "RadiusMultiplier", 1.0f,
                "Multiplier on Contagion's (already +50%) zone radius for how far enemies are grabbed. " +
                "1.0 = same as the zone; >1 reaches beyond it. (Default: 1.0)");

            SuctionDashTime = Config.Bind("BlackHoleSuction", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank (Black Hole uses 0.3). (Default: 0.2)");

            SuctionDistanceMultiplier = Config.Bind("BlackHoleSuction", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly " +
                "onto the host; >1 pulls it past the host center for a stronger yank (clamped by walls). (Default: 1.0)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log Contagion radius, visual scale, and tick events to the BepInEx console. (Default: false)");

            ResolveOffsets();

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_ContagionPrefab_Activate));
            PatchOrLog(harmony, typeof(Patch_ContagionPrefab_ZoneRoutine));
            PatchOrLog(harmony, typeof(Patch_ContagionPrefab_ZoneRoutine_MoveNext));
            // [2026-10-04 15:33] Disabled Patch_ContagionPrefab_Update per user request.
            // PatchOrLog(harmony, typeof(Patch_ContagionPrefab_Update));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"AreaMultiplier: {AreaMultiplier.Value} (+50% AoE)");
            Log.LogInfo($"TickCountMultiplier: {TickCountMultiplier.Value} (2x ticks)");
            Log.LogInfo($"TickInterval: {TickInterval.Value}s (half-second ticks)");
            Log.LogInfo($"BlackHoleSuction: {SuctionEnabled.Value} (activate={SuctionOnActivate.Value}, tick={SuctionOnTick.Value}, radiusX{SuctionRadiusMultiplier.Value}, distX{SuctionDistanceMultiplier.Value}, dashTime={SuctionDashTime.Value}s)");
            Log.LogInfo("=================================================");
        }

        // [2026-10-04 14:50] Replaced preliminary test method with production PullEnemiesToCenter.
        // Old compile verification stub kept for reference:
        // private static void TestCompile(ContagionPrefab cp, ObjectsCommon target)
        // {
        //     cp.DashTargetToPosition(target, Vector2.zero, 0f, 0f, false, false, false, null);
        //     var list = cp.GetAllEnemiesInRangeOfPosition(Vector2.zero, 5f);
        //     if (list != null)
        //     {
        //         for (int i = 0; i < list.Count; i++)
        //         {
        //             var enemy = list[i];
        //             if (enemy != null && !enemy.IsDead())
        //             {
        //                 float dist = Vector2.Distance(enemy.transform.position, Vector2.zero);
        //                 cp.DashTargetToPosition(enemy, Vector2.zero, 0f, dist, false, false, false, null);
        //             }
        //         }
        //     }
        // }

        // [2026-10-04 15:10] Obsolete PullEnemiesToCenter commented out below.
        // Reason: BaseSpellLibrary.DashTargetToPosition fails silently in multiplayer/offline non-dash context
        // with "[DashTargetToPosition] Monster dash skipped - not server". Additionally, Contagion is cast
        // directly onto an infected enemy host (_infectedTarget at 0x330); the suction center must be this
        // infected host enemy, and surrounding enemies inside the AoE must be pulled towards this host (while
        // the host itself must not be pulled into itself). We now use continuous frame-by-frame NavMeshAgent/
        // transform displacement (ApplySuctionFrame) plus impulse bursts on activate and ticks (ApplySuctionPulse).
        /*
        public static unsafe void PullEnemiesToCenter(ContagionPrefab cp)
        {
            try
            {
                if (!Enabled.Value) return;
                if (!SuctionEnabled.Value) return;
                if (cp == null || cp.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)cp.Pointer;
                byte ended = *(byte*)(ptr + EndedOffset);
                if (ended != 0) return;

                Vector2 center = *(Vector2*)(ptr + CenterOffset);
                if (center == Vector2.zero && cp.transform != null)
                {
                    center = (Vector2)cp.transform.position;
                }

                float radius = *(float*)(ptr + RadiusOffset);
                if (radius <= 0f) radius = 4.5f;

                var enemies = cp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                int pulled = 0;
                int count = enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    Vector2 enemyPos = (Vector2)enemy.transform.position;
                    float dist = Vector2.Distance(enemyPos, center);
                    if (dist <= 0.05f) continue;

                    cp.DashTargetToPosition(
                        enemy,
                        center,
                        0f,
                        dist,
                        false,
                        false,
                        false,
                        null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo(
                        $"[ContagionTuner] Black Hole Suction: pulled {pulled} enemies towards center ({center.x:F1}, {center.y:F1}), radius {radius:F1}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[PullEnemiesToCenter] {ex}");
            }
        }
        */

        /// <summary>
        /// Reads the infected target monster attached to Contagion (_infectedTarget at 0x330).
        /// </summary>
        public static unsafe ObjectsCommon GetInfectedTarget(ContagionPrefab cp)
        {
            if (cp == null || cp.Pointer == IntPtr.Zero) return null;
            byte* ptr = (byte*)cp.Pointer;
            IntPtr targetPtr = *(IntPtr*)(ptr + InfectedTargetOffset);
            if (targetPtr == IntPtr.Zero) return null;
            return new ObjectsCommon(targetPtr);
        }

        /// <summary>
        /// Resolves the epicenter of Contagion. Since Contagion is cast on an infected enemy,
        /// the center is strictly that enemy's current position so surrounding enemies get sucked
        /// into the host. If host is missing/dead, falls back to _center or cp.transform.
        /// </summary>
        public static unsafe Vector2 GetCenter(ContagionPrefab cp, ObjectsCommon infectedTarget)
        {
            if (infectedTarget != null && infectedTarget.Pointer != IntPtr.Zero && !infectedTarget.IsDead() && infectedTarget.transform != null)
            {
                return (Vector2)infectedTarget.transform.position;
            }

            if (cp != null && cp.Pointer != IntPtr.Zero)
            {
                byte* ptr = (byte*)cp.Pointer;
                Vector2 c = *(Vector2*)(ptr + CenterOffset);
                if (c != Vector2.zero) return c;

                if (cp.transform != null)
                {
                    return (Vector2)cp.transform.position;
                }
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Black Hole style suction for Contagion: drag every live enemy inside the zone toward the infected
        /// host using the game's own <c>BaseSpellLibrary.DashTargetToPosition</c> - the exact primitive
        /// <c>BlackholePrefab.AreaOfEffectStrike</c> uses (docs/BlackHole_Dragging_Mechanism.md). Unlike the
        /// old NavMeshAgent.Move pull (which fought monster pathfinding), this reuses the game's knockback
        /// coroutine, which disables the NavMeshAgent during the lerp and restores it afterward, raycasts
        /// walls, and honours <c>ObjectsCommon.Immune</c> / <c>MonsterBehaviourLibrary.DisplaceExempt</c> /
        /// authority. The infected host itself is never pulled.
        /// <c>distance</c> is the enemy's current distance to the host (times DistanceMultiplier), so each
        /// call slides it onto (or, if &gt;1, past) the host - Black Hole's whole "suction" trick.
        /// </summary>
        public static unsafe void ApplySuction(ContagionPrefab cp)
        {
            if (!Enabled.Value || !SuctionEnabled.Value) return;
            if (cp == null || cp.Pointer == IntPtr.Zero) return;

            byte* ptr = (byte*)cp.Pointer;
            if (*(byte*)(ptr + EndedOffset) != 0) return;

            try
            {
                ObjectsCommon host = GetInfectedTarget(cp);
                Vector2 center = GetCenter(cp, host);
                if (center == Vector2.zero) return;

                float radius = *(float*)(ptr + RadiusOffset) * SuctionRadiusMultiplier.Value;
                if (radius <= 0.1f) return;

                var enemies = cp.GetAllEnemiesInRangeOfPosition(center, radius);
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

                    // Never drag the infected host into itself.
                    if (host != null && enemy.Pointer == host.Pointer) continue;

                    Vector3 ep3 = enemy.transform.position;
                    float dist = Vector2.Distance(new Vector2(ep3.x, ep3.y), center);
                    if (dist <= 0.05f) continue;

                    // Same shape as BlackholePrefab.AreaOfEffectStrike:
                    //   DashTargetToPosition(target, center, time, distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot)
                    cp.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[Suction] DashTargetToPosition dragged {pulled} enemies toward host ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }

        // [2026-10-04 15:33] Obsolete Black Hole suction implementation commented out below per user request.
        // Reason: The suction effect felt glitchy in Dimraeth due to NavMeshAgent pathfinding and monster physics
        // collider resistance. The mod now focuses exclusively on +50% Area of Effect and 2x Ticks.
        /*
        /// <summary>
        /// Frame-by-frame continuous suction (Black Hole gravitational pull).
        /// Drags surrounding enemies inside the AoE radius towards the infected enemy host.
        /// Does NOT pull the infected enemy into itself.
        /// </summary>
        public static unsafe void ApplySuctionFrame(ContagionPrefab cp)
        {
            try
            {
                if (!Enabled.Value || !SuctionEnabled.Value || !SuctionContinuous.Value) return;
                if (cp == null || cp.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)cp.Pointer;
                byte ended = *(byte*)(ptr + EndedOffset);
                if (ended != 0) return;

                var infectedTarget = GetInfectedTarget(cp);
                Vector2 center = GetCenter(cp, infectedTarget);
                if (center == Vector2.zero) return;

                float radius = *(float*)(ptr + RadiusOffset);
                if (radius <= 0f) radius = 4.5f;

                var enemies = cp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                float dt = Time.deltaTime;
                float speed = SuctionContinuousSpeed.Value;
                const float minStopDist = 0.6f;
                int count = enemies.Count;

                for (int i = 0; i < count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    if (infectedTarget != null && enemy.Pointer == infectedTarget.Pointer) continue;

                    Vector2 enemyPos = (Vector2)enemy.transform.position;
                    Vector2 diff = center - enemyPos;
                    float dist = diff.magnitude;

                    if (dist <= minStopDist || dist > radius) continue;

                    float pullFactor = Mathf.Clamp01((dist - minStopDist) / 1.0f);
                    float step = Mathf.Min(speed * Mathf.Max(0.25f, pullFactor) * dt, dist - minStopDist);
                    if (step <= 0f) continue;

                    Vector2 moveVec = (diff / dist) * step;
                    Vector3 moveDelta = new Vector3(moveVec.x, moveVec.y, 0f);

                    var agent = enemy.Agent;
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.Move(moveDelta);
                    }
                    else if (enemy.transform != null)
                    {
                        enemy.transform.position += moveDelta;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuctionFrame] {ex}");
            }
        }

        /// <summary>
        /// Impulse suction pulse (triggered on cast activation and on each damage/poison tick).
        /// Re-centers nearby surrounding enemies into the infected enemy with a smooth gravitational tug.
        /// </summary>
        public static unsafe void ApplySuctionPulse(ContagionPrefab cp)
        {
            try
            {
                if (!Enabled.Value || !SuctionEnabled.Value) return;
                if (cp == null || cp.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)cp.Pointer;
                byte ended = *(byte*)(ptr + EndedOffset);
                if (ended != 0) return;

                var infectedTarget = GetInfectedTarget(cp);
                Vector2 center = GetCenter(cp, infectedTarget);
                if (center == Vector2.zero) return;

                float radius = *(float*)(ptr + RadiusOffset);
                if (radius <= 0f) radius = 4.5f;

                var enemies = cp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                float maxPulse = SuctionPulseDistance.Value;
                const float minStopDist = 0.6f;
                int count = enemies.Count;
                int pulled = 0;

                for (int i = 0; i < count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    if (infectedTarget != null && enemy.Pointer == infectedTarget.Pointer) continue;

                    Vector2 enemyPos = (Vector2)enemy.transform.position;
                    Vector2 diff = center - enemyPos;
                    float dist = diff.magnitude;

                    if (dist <= minStopDist || dist > radius) continue;

                    float pulseDist = Mathf.Min(maxPulse, dist - minStopDist);
                    if (pulseDist <= 0.05f) continue;

                    Vector2 dir = diff / dist;

                    Vector3 moveDelta = new Vector3(dir.x * pulseDist, dir.y * pulseDist, 0f);
                    var agent = enemy.Agent;
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.Move(moveDelta);
                    }
                    else if (enemy.transform != null)
                    {
                        enemy.transform.position += moveDelta;
                    }

                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo(
                        $"[ContagionTuner] Suction Pulse: pulled {pulled} surrounding enemies towards host at ({center.x:F1}, {center.y:F1}), radius {radius:F1}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuctionPulse] {ex}");
            }
        }
        */
        private static void ResolveOffsets()
        {
            try
            {
                var cpClass = Il2CppClassPointerStore<ContagionPrefab>.NativeClassPtr;
                if (cpClass != IntPtr.Zero)
                {
                    RadiusOffset = GetFieldOffset(cpClass, "_radius", 0x340);
                    VfxOffset = GetFieldOffset(cpClass, "_vfx", 0x2E8);
                    ZoneRemainingOffset = GetFieldOffset(cpClass, "_zoneRemaining", 0x368);
                    CenterOffset = GetFieldOffset(cpClass, "_center", 0x338);
                    EndedOffset = GetFieldOffset(cpClass, "_ended", 0x354);
                    InfectedTargetOffset = GetFieldOffset(cpClass, "_infectedTarget", 0x330);
                }

                var bslClass = Il2CppClassPointerStore<BaseSpellLibrary>.NativeClassPtr;
                if (bslClass != IntPtr.Zero)
                {
                    MaxTimeAliveOffset = GetFieldOffset(bslClass, "_maxTimeAlive", 0x1C8);
                }

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[Offsets] _radius=0x{RadiusOffset:X}, _vfx=0x{VfxOffset:X}, _zoneRemaining=0x{ZoneRemainingOffset:X}, _center=0x{CenterOffset:X}, _ended=0x{EndedOffset:X}, _infectedTarget=0x{InfectedTargetOffset:X}, _maxTimeAlive=0x{MaxTimeAliveOffset:X}");
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
    /// Postfix on ContagionPrefab.Activate.
    /// In vanilla Activate:
    ///   - _radius is computed from baseRadius (3.0) and skill tree upgrades.
    ///   - _vfx.transform.localScale is scaled by (_radius / 3.0f).
    ///   - _maxTimeAlive is set to duration (+ optional linger).
    /// We scale _radius and _vfx by AreaMultiplier (+50%) and add LifetimeSafetyBuffer to _maxTimeAlive.
    /// </summary>
    [HarmonyPatch(typeof(ContagionPrefab), "Activate")]
    public static class Patch_ContagionPrefab_Activate
    {
        public static unsafe void Postfix(ContagionPrefab __instance)
        {
            try
            {
                if (!ContagionTunerPlugin.Enabled.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)__instance.Pointer;
                float* pRadius = (float*)(ptr + ContagionTunerPlugin.RadiusOffset);
                float oldRadius = *pRadius;
                float mult = ContagionTunerPlugin.AreaMultiplier.Value;

                *pRadius = oldRadius * mult;

                if (__instance._vfx != null)
                {
                    Vector3 curScale = __instance._vfx.transform.localScale;
                    __instance._vfx.transform.localScale = curScale * mult;
                }

                // Add a small safety buffer to _maxTimeAlive so the 12th tick always finishes cleanly before BaseSpell despawn
                float* pMaxTime = (float*)(ptr + ContagionTunerPlugin.MaxTimeAliveOffset);
                *pMaxTime += ContagionTunerPlugin.LifetimeSafetyBuffer.Value;

                // [2026-10-05 09:15] Re-enabled Black Hole drag on Activate, now via the game's own
                // DashTargetToPosition (ApplySuction) instead of the old glitchy NavMeshAgent.Move pulse.
                if (ContagionTunerPlugin.SuctionOnActivate.Value)
                {
                    ContagionTunerPlugin.ApplySuction(__instance);
                }

                if (ContagionTunerPlugin.DiagnosticLogging.Value)
                {
                    ContagionTunerPlugin.Log.LogInfo(
                        $"[ContagionTuner] Activate: radius {oldRadius:F2} -> {*pRadius:F2} (x{mult:F2}), MaxTimeAlive now {*pMaxTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                ContagionTunerPlugin.Log?.LogError($"[Patch_ContagionPrefab_Activate] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on ContagionPrefab.ZoneRoutine.
    /// In vanilla Activate:
    ///   StartCoroutine(ZoneRoutine(duration))
    /// Vanilla ZoneRoutine creates _ZoneRoutine_d__51 with duration.
    /// MoveNext subtracts 1.0f from _zoneRemaining each tick and checks _zoneRemaining &gt;= 1.0f.
    /// By multiplying duration by TickCountMultiplier (2.0x), _zoneRemaining starts with 2x ticks (e.g. 12 instead of 6).
    /// </summary>
    [HarmonyPatch(typeof(ContagionPrefab), "ZoneRoutine")]
    public static class Patch_ContagionPrefab_ZoneRoutine
    {
        public static void Prefix(ref float duration)
        {
            try
            {
                if (!ContagionTunerPlugin.Enabled.Value) return;

                float oldDuration = duration;
                duration *= ContagionTunerPlugin.TickCountMultiplier.Value;

                if (ContagionTunerPlugin.DiagnosticLogging.Value)
                {
                    ContagionTunerPlugin.Log.LogInfo(
                        $"[ContagionTuner] ZoneRoutine duration scaled: {oldDuration:F2} -> {duration:F2} (total expected ticks: {duration:F0})");
                }
            }
            catch (Exception ex)
            {
                ContagionTunerPlugin.Log?.LogError($"[Patch_ContagionPrefab_ZoneRoutine] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on ContagionPrefab._ZoneRoutine_d__51.MoveNext.
    /// In vanilla MoveNext:
    ///   Yields new WaitForSeconds(1.0f) between ticks.
    /// We replace &lt;&gt;2__current with WaitForSeconds(TickInterval.Value) (default 0.5s),
    /// causing ticks to trigger every 0.5s instead of 1.0s.
    /// </summary>
    [HarmonyPatch(typeof(ContagionPrefab._ZoneRoutine_d__51), "MoveNext")]
    public static class Patch_ContagionPrefab_ZoneRoutine_MoveNext
    {
        public static void Postfix(ContagionPrefab._ZoneRoutine_d__51 __instance, ref bool __result)
        {
            try
            {
                if (!ContagionTunerPlugin.Enabled.Value) return;
                if (!__result) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                float interval = ContagionTunerPlugin.TickInterval.Value;
                __instance.__2__current = new WaitForSeconds(interval);

                // [2026-10-05 09:15] Re-enabled Black Hole drag on each tick, via the game's own
                // DashTargetToPosition. At the 0.5s tick interval this is a 2-per-second suction.
                if (ContagionTunerPlugin.SuctionOnTick.Value)
                {
                    ContagionTunerPlugin.ApplySuction(__instance.__4__this);
                }

                if (ContagionTunerPlugin.DiagnosticLogging.Value)
                {
                    ContagionTunerPlugin.Log.LogInfo($"[ContagionTuner] Next tick scheduled in {interval:F2}s");
                }
            }
            catch (Exception ex)
            {
                ContagionTunerPlugin.Log?.LogError($"[Patch_ContagionPrefab_ZoneRoutine_MoveNext] {ex}");
            }
        }
    }

    // [2026-10-04 15:33] Disabled Patch_ContagionPrefab_Update per user request.
    // Reason: Black Hole suction effect removed as continuous NavMesh manipulation conflicted with monster AI pathfinding.
    /*
    /// <summary>
    /// Postfix on ContagionPrefab.Update.
    /// Runs continuous frame-by-frame gravitational suction, smoothly dragging
    /// surrounding enemies towards the infected host monster.
    /// </summary>
    [HarmonyPatch(typeof(ContagionPrefab), "Update")]
    public static class Patch_ContagionPrefab_Update
    {
        public static void Postfix(ContagionPrefab __instance)
        {
            try
            {
                if (!ContagionTunerPlugin.Enabled.Value) return;
                if (!ContagionTunerPlugin.SuctionEnabled.Value) return;
                if (!ContagionTunerPlugin.SuctionContinuous.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                ContagionTunerPlugin.ApplySuctionFrame(__instance);
            }
            catch (Exception ex)
            {
                ContagionTunerPlugin.Log?.LogError($"[Patch_ContagionPrefab_Update] {ex}");
            }
        }
    }
    */
}
