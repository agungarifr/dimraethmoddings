using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
// [2026-10-04 00:40] Added: Il2CppStructArray<ParticleSystem.Particle> buffers are how the visible
// shard streak gets steered (see Patch_PlagueShardPrefab_Update.SteerParticleVfx).
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;

namespace PlagueShardsHoming
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that turns Plague Shards (Shadow skill) into homing
    /// projectiles with a visible turn arc (Chrono-Spike style) instead of a straight fan:
    ///
    ///   * The vanilla fan launch is untouched — OnStart still spreads the shards.
    ///   * While a shard is in flight, its own _flightDirection (0x35C, seeded with the fan
    ///     direction) is rotated toward the nearest live enemy at TurnRateDegPerSec
    ///     (180°/s default => a visible chase curve; 0 = instant snap).
    //     ^ [2026-10-04 13:30] Was: re-pick the nearest live enemy EVERY frame (per-frame
    //     retarget). Obsolete because the user asked for AetherHowlProjectilePrefab-style homing
    //     ("homing seperti aetherhowl"), whose UpdateHomingTarget chases ONE LOCKED target —
    //     resolved once and only re-resolved when the old one dies/goes hidden — instead of
    //     re-querying the nearest enemy each frame (which lets shards swap targets mid-flight and
    //     jitter between equidistant enemies). New behavior is the lock-on bullet below.
    ///   * While a shard is in flight, its own _flightDirection (0x35C, seeded with the fan
    ///     direction) is rotated toward a LOCKED target — the nearest live enemy at the moment
    ///     homing starts — at TurnRateDegPerSec (540°/s default => sharp yet smooth chase arc;
    ///     0 = instant snap). The lock is kept until that enemy dies or is destroyed, then the
    ///     next nearest one is locked (AetherHowl's ResolveHomingTarget semantics). See
    ///     Patch_PlagueShardPrefab_Update.ResolveLockedTarget.
    ///   * BaseSpellLibrary._targetPosition (0xF0) is rewritten to a far look-ahead point along
    ///     the new heading each frame, so SkillShot.MoveProjectileTowardsTarget keeps flying the
    ///     curve instead of "arriving" at the enemy and despawning before SweepShardFootprint
    ///     can deal damage (hits are applied by the sweep, NOT by reaching 0xF0).
    ///   * _launchPosition (0x364) is re-seeded to the shard's current position so the sweep
    ///     axis tracks the turning path and hits keep registering mid-turn.
    ///
    /// Homing ONLY: shard count, damage, speed and range are untouched.
    /// Co-op: run the same config on host and clients so every peer steers identically.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class PlagueShardsHomingPlugin : BasePlugin
    {
        public const string GUID = "com.custom.plagueshardshoming";
        public const string NAME = "Plague Shards Homing";
        // [2026-10-04 13:30] Was: "1.0.0". Bumped: targeting switched from per-frame nearest-enemy
        // retarget to AetherHowl-style target lock-on (see ResolveLockedTarget).
        //public const string VERSION = "1.0.0";
        // [2026-10-04 14:20] Was: "1.1.0". Bumped: shard-instant-death fix — _maximumTravelDistance
        // (0x2A8, the max-distance death trigger) now scales with LifetimeMultiplier and the aim
        // point is dt-robust, plus death-cause diagnostics (OnDestinationReached/OnMaxDistanceReached/
        // ApplyTargetHit). See Patch_PlagueShardPrefab_PlayProjectileVfx and the Update Prefix.
        //public const string VERSION = "1.1.0";
        // [2026-10-04 16:30] Was: "1.2.0". Bumped: fixed speed burst (175 u/s -> 18 u/s steady via
        // DriveSpeedProfile patch), tight turn radius, and removed crash-causing virtual method hooks.
        //public const string VERSION = "1.2.0";
        // [2026-10-04 17:30] Was: "1.3.0". Bumped: fixed visual dying mid-flight (startLifetime /
        // destroyDelay scaled to match 4 u/s flight time) and added early launch / 1/4 launch distance.
        //public const string VERSION = "1.3.0";
        public const string VERSION = "1.4.0";

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> TurnRateDegPerSec;
        public static ConfigEntry<float> DetectionRange;
        // [2026-10-04 16:30] NEW: user reported "speed terlalu cepat dan lifetime nya terlalu pendek".
        // In vanilla, DriveSpeedProfile computes speed based on an authored curve, which when range
        // is scaled spikes up to ~175 u/s! Setting a controlled steady speed (18 u/s) allows the shard
        // to execute its 540 deg/s turn cleanly with a tight ~1.9 unit radius.
        public static ConfigEntry<float> ProjectileSpeed;
        // [2026-10-04 09:15] NEW: user reported "lifetime projectilenya terlalu singkat" — stretches
        // how far/how long each shard flies (range, flight time, spell TTL and the streak visuals)
        // while keeping the vanilla speed. See Patch_PlagueShardPrefab_PlayProjectileVfx.
        public static ConfigEntry<float> LifetimeMultiplier;
        public static ConfigEntry<bool> RotateVisual;
        // [2026-10-03 23:20] Re-added from the live config (com.custom.plagueshardshoming.cfg), which
        // still contained this entry from the pre-wipe build — the recreated plugin must honor it or
        // the shard art may face the wrong way while turning.
        public static ConfigEntry<float> RotateVisualOffsetDeg;
        // [2026-10-04 17:30] NEW: user request — "jika itu di set by timer buat dia start mengejar lebih awal, jika itu jarak, buat dia start di jarak 1/4 dari vanilla dia start mengejar musuh".
        public static ConfigEntry<float> LaunchOffsetMultiplier;
        public static ConfigEntry<float> AnticipationMultiplier;
        public static ConfigEntry<bool> DiagnosticLogging;

        public override void Load()
        {
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the shards fly their vanilla straight fan. (Default: true)");

            // [2026-10-04 09:15] Was: default 180f (visible chase arc). The test flight showed the arc
            // too wide ("sudut belokkannya kurang tajam") and the user wants a smooth 180° reversal
            // ("bisa langsung putar balik 180 derajat"). 540°/s = ~2.1 u turn radius at the vanilla
            // ~20 u/s (180° in ~0.33 s): still a visible curve (~9°/frame at 60 fps), not an instant
            // snap. The old binding is kept below for reference.
            //TurnRateDegPerSec = Config.Bind("Homing", "TurnRateDegPerSec", 180f,
            //    "How fast a shard curves toward its target, in degrees per second. 0 = instant snap (auto-target look), 180 = visible chase arc. (Default: 180)");
            TurnRateDegPerSec = Config.Bind("Homing", "TurnRateDegPerSec", 540f,
                "How fast a shard curves toward its target, in degrees per second. 0 = instant snap (auto-target look), 540 = tight yet smooth chase arc that can reverse 180° in about a third of a second. (Default: 540)");

            // [2026-10-04 16:30] NEW: controllable steady homing speed. At 18 u/s and 540°/s turn rate,
            // turn radius is ~1.9 units, allowing tight pursuit curves.
            //ProjectileSpeed = Config.Bind("Homing", "ProjectileSpeed", 18f,
            //    "Flight speed of homing shards in units/second. (Default: 18; fixes the 175 u/s burst that broke turns)");
            // [2026-10-04 17:00] User request: "turunkan projectile speed nya jadi 1/5 vanilla".
            // Vanilla speed is 20 u/s; 1/5 is 4 u/s. At 4 u/s, turn radius is 0.42 units, turning on a dime!
            ProjectileSpeed = Config.Bind("Homing", "ProjectileSpeed", 4f,
                "Flight speed of homing shards in units/second. (Default: 4 = 1/5 vanilla 20 u/s; enables sharp turning on a dime)");

            // [2026-10-04 09:15] NEW: extends each shard's whole life (flight distance + time + TTL +
            // streak particles) by this factor. Speed stays vanilla because DriveSpeedProfile divides
            // the scaled range by the scaled flight time. 1 = vanilla (~8.8 u / ~0.44 s). (Default: 3)
            // [2026-10-04 16:30] Set default to 4f for generous homing range (~35-40 units).
            //LifetimeMultiplier = Config.Bind("Homing", "LifetimeMultiplier", 3f,
            //    "Multiplier for how far and how long each shard flies (range, flight time, spell TTL and the visible streak). Speed is unchanged. 1 = vanilla. (Default: 3)");
            LifetimeMultiplier = Config.Bind("Homing", "LifetimeMultiplier", 4f,
                "Multiplier for how far and how long each shard flies (range, flight time, spell TTL and the visible streak). 1 = vanilla (~8.8 u). (Default: 4)");

            // [2026-10-04 13:30] Was: "Radius around each shard in which enemies are considered as
            // homing targets." — true only for the per-frame retarget model. With AetherHowl-style
            // lock-on the radius only applies when ACQUIRING (or re-acquiring) the lock; once
            // locked, the chase follows that enemy anywhere (exactly like AetherHowl, which never
            // range-checks its _target mid-flight).
            //DetectionRange = Config.Bind("Homing", "DetectionRange", 30f,
            //    "Radius around each shard in which enemies are considered as homing targets. (Default: 30)");
            DetectionRange = Config.Bind("Homing", "DetectionRange", 30f,
                "Radius around each shard for acquiring a homing target lock. Once locked, the shard chases that enemy anywhere until it dies. (Default: 30)");

            RotateVisual = Config.Bind("Homing", "RotateVisual", true,
                "Rotate the shard (and its child VFX) to face its flight direction while turning. (Default: true)");

            RotateVisualOffsetDeg = Config.Bind("Homing", "RotateVisualOffsetDeg", 0f,
                "Extra Z rotation (degrees) added when RotateVisual is on, in case the shard art is not authored facing +X. (Default: 0)");

            // [2026-10-04 17:30] NEW: user request — "jika itu di set by timer buat dia start mengejar lebih awal, jika itu jarak, buat dia start di jarak 1/4 dari vanilla dia start mengejar musuh".
            LaunchOffsetMultiplier = Config.Bind("Homing", "LaunchOffsetMultiplier", 0.25f,
                "Multiplier for initial launch offset distance from caster. 1 = vanilla (~1.5u), 0.25 = 1/4 vanilla distance. (Default: 0.25)");

            AnticipationMultiplier = Config.Bind("Homing", "AnticipationMultiplier", 0.25f,
                "Multiplier for anticipation delay before shard launches and starts homing. 0.25 = early launch on frame 1; 1 = vanilla delay. (Default: 0.25)");

            // [2026-10-03 22:50] Was: default false. Temporarily default ON while homing is being
            // verified ("projectile masih lurus" bug). Flip back to false once confirmed working —
            // the old binding is kept below for reference.
            //DiagnosticLogging = Config.Bind("Diagnostics", "LogHoming", false, "...");
            DiagnosticLogging = Config.Bind("Diagnostics", "LogHoming", true,
                "Log each shard's homing state to the BepInEx console. (Default: true while debugging)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_Update));
            // [2026-10-04 16:30] NEW: enforce steady homing speed, bypass 175 u/s burst curve in DriveSpeedProfile.
            PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_DriveSpeedProfile));
            // [2026-10-04 09:15] NEW: lifetime scaling patches (see the classes below).
            PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_PlayProjectileVfx));
            PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_HandleBeforeSpellDestroyed));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_SpawnAndTrack));
            // [2026-10-04 14:20] NEW: the real lifetime-scaling gate (OnStart Postfix).
            PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_OnStart));

            // [2026-10-04 16:30] OBSOLETE / REMOVED: Virtual method hooks on IL2CPP derived classes caused
            // Fatal error. System.AccessViolationException in DynamicClass.DMD<PlagueShardPrefab::OnMaxDistanceReached>.
            // In IL2CPP, hooking virtual overrides dispatched through native vtable ([rax+598h]) corrupts
            // the native call trampoline. Removed to eliminate crashes.
            //PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_OnDestinationReached));
            //PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_OnMaxDistanceReached));
            //PatchOrLog(harmony, typeof(Patch_PlagueShardPrefab_ApplyTargetHit));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"TurnRateDegPerSec: {TurnRateDegPerSec.Value}");
            Log.LogInfo($"LifetimeMultiplier: {LifetimeMultiplier.Value}");
            Log.LogInfo($"DetectionRange: {DetectionRange.Value}");
            Log.LogInfo($"RotateVisual: {RotateVisual.Value}");
            Log.LogInfo($"RotateVisualOffsetDeg: {RotateVisualOffsetDeg.Value}");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo("=================================================");
        }

        private void PatchOrLog(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.PatchAll(patchType);
                // [2026-10-04 09:15] Was: "... applied to PlagueShardPrefab.Update." — the helper now
                // registers four different patch classes, so name only the class.
                //Log.LogInfo($"[Patch] {patchType.Name} applied to PlagueShardPrefab.Update.");
                Log.LogInfo($"[Patch] {patchType.Name} applied.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[PlagueShardsHoming] Failed to apply patch {patchType.Name}: {ex}");
            }
        }
    }

    /// <summary>
    /// Field offsets for this game build. Each offset is resolved at runtime through the IL2CPP
    /// field metadata where possible, with hard-coded fallbacks verified against the ISIL dump
    /// (modding/DecompilerTool/isil_out/IsilDump/Assembly-CSharp).
    /// </summary>
    internal static class GameFields
    {
        // Fallback offsets for this game build.
        private const int FallbackTargetPosition = 0xF0;           // BaseSpellLibrary._targetPosition
        private const int FallbackFlightDirection = 0x35C;         // PlagueShardPrefab._flightDirection (Vector2)
        private const int FallbackLaunchPosition = 0x364;          // PlagueShardPrefab._launchPosition (Vector2)
        private const int FallbackMaximumTravelDistance = 0x2A8;   // SkillShot._maximumTravelDistance
        private const int FallbackFlightElapsed = 0x370;           // PlagueShardPrefab._flightElapsed
        private const int FallbackLaunchProjectile = 0x2BB;        // SkillShot._launchProjectile
        // [2026-10-04 00:40] NEW: offsets for the visible-streak fix and its diagnostics. Verified
        // against the PlagueShardPrefab/SkillShot field list and ISIL: _projectileParticles is the
        // ParticleSystem[] stored by ConfigureShardParticles (`Move [r14+832], rax` — 832 decimal =
        // 0x340) and iterated by PlayProjectileVfx (`mov rsi,[rsi+340h]`); _useCurvedTrajectory is a
        // bool on SkillShot (PlagueShardPrefab.OnStart writes it at step 257, SkillShot.Update
        // dispatches the mover on it at step 118: 0 = MoveProjectileTowardsTarget, which is the only
        // mover that consumes our _targetPosition writes).
        private const int FallbackProjectileParticles = 0x340;    // PlagueShardPrefab._projectileParticles (ParticleSystem[])
        private const int FallbackUseCurvedTrajectory = 0x2BA;    // SkillShot._useCurvedTrajectory (bool)

        // [2026-10-04 14:20] NEW: _projectileSpeed (SkillShot, 0x2C4) — written by OnStart
        // (`Move [rdi+708], rax` = _shardSpeed, OnStart step 190) and rewritten every frame by
        // DriveSpeedProfile (`Move [rbx+708], xmm1`, step 072/158 — 708 decimal = 0x2C4). Needed
        // [2026-10-04 14:20] NEW: _projectileSpeed (SkillShot, 0x2C4) — written by OnStart
        // (`Move [rdi+708], rax` = _shardSpeed, OnStart step 190) and rewritten every frame by
        // DriveSpeedProfile (`Move [rbx+708], xmm1`, step 072/158 — 708 decimal = 0x2C4). Needed
        // for the dt-robust aim point: MoveProjectileTowardsTarget ARRIVES (and PlagueShard releases
        // the spell) when speed*Time.deltaTime >= distance-to-aim, so the aim must stay at least a
        // few travel-steps ahead even during frame-time spikes.
        private const int FallbackProjectileSpeed = 0x2C4;        // SkillShot._projectileSpeed
        // [2026-10-04 17:00] NEW: _shardSpeed (PlagueShardPrefab, 0x2F8) — authoring speed, default 20 u/s.
        private const int FallbackShardSpeed = 0x2F8;             // PlagueShardPrefab._shardSpeed
        // [2026-10-04 17:30] NEW: _launchOffset (PlagueShardPrefab, 0x2FC) — initial spawn offset from caster.
        private const int FallbackLaunchOffset = 0x2FC;            // PlagueShardPrefab._launchOffset

        // [2026-10-04 02:30] NEW: offsets for the lifetime scaling (user: "lifetime projectilenya
        // terlalu singkat"). Verified against the ilrecovery field lists + OnStart ISIL:
        // _rangeLimit is written at OnStart step 157 (`Move [rdi+876], xmm6` — 876 dec = 0x36C) as
        // the flight distance, _flightDuration at step 204 (`Move [rdi+884], xmm0` — 884 dec =
        // 0x374) as _rangeLimit / max(const, _shardSpeed), BaseSpellLibrary._maxTimeAlive (0x1C8) is
        // the spell TTL compared against _timealive (0x1CC) in BaseSpell.UpdateSpellAliveTime
        // (`Compare xmm0, [rbx+456]` after `_timealive += Time.deltaTime`), and _projectileVfx
        // (0x318) is the streak prefab handed to BaseSpellLibrary.SpawnAndTrack at OnStart step 343.
        private const int FallbackRangeLimit = 0x36C;             // PlagueShardPrefab._rangeLimit
        private const int FallbackFlightDuration = 0x374;         // PlagueShardPrefab._flightDuration
        private const int FallbackMaxTimeAlive = 0x1C8;           // BaseSpellLibrary._maxTimeAlive (spell TTL)
        private const int FallbackProjectileVfx = 0x318;          // PlagueShardPrefab._projectileVfx

        public static int TargetPositionOffset = FallbackTargetPosition;
        public static int FlightDirectionOffset = FallbackFlightDirection;
        public static int LaunchPositionOffset = FallbackLaunchPosition;
        public static int MaximumTravelDistanceOffset = FallbackMaximumTravelDistance;
        public static int FlightElapsedOffset = FallbackFlightElapsed;
        public static int LaunchProjectileOffset = FallbackLaunchProjectile;
        public static int ProjectileParticlesOffset = FallbackProjectileParticles;
        public static int UseCurvedTrajectoryOffset = FallbackUseCurvedTrajectory;
        // [2026-10-04 14:20] NEW: speed offset for the dt-robust aim point (see fallback comment).
        public static int ProjectileSpeedOffset = FallbackProjectileSpeed;
        // [2026-10-04 17:00] NEW: shard speed offset for scaling startSpeed.
        public static int ShardSpeedOffset = FallbackShardSpeed;
        // [2026-10-04 17:30] NEW: launch offset for scaling initial spawn distance.
        public static int LaunchOffsetOffset = FallbackLaunchOffset;
        // [2026-10-04 09:15] NEW: lifetime-scaling offsets (see the fallback comments above).
        public static int RangeLimitOffset = FallbackRangeLimit;
        public static int FlightDurationOffset = FallbackFlightDuration;
        public static int MaxTimeAliveOffset = FallbackMaxTimeAlive;
        public static int ProjectileVfxOffset = FallbackProjectileVfx;

        private static bool _resolved;

        public static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;

            Resolve(Il2CppClassPointerStore<BaseSpellLibrary>.NativeClassPtr, "_targetPosition",
                FallbackTargetPosition, off => TargetPositionOffset = off);
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_flightDirection",
                FallbackFlightDirection, off => FlightDirectionOffset = off);
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_launchPosition",
                FallbackLaunchPosition, off => LaunchPositionOffset = off);
            Resolve(Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_maximumTravelDistance",
                FallbackMaximumTravelDistance, off => MaximumTravelDistanceOffset = off);
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_flightElapsed",
                FallbackFlightElapsed, off => FlightElapsedOffset = off);
            Resolve(Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_launchProjectile",
                FallbackLaunchProjectile, off => LaunchProjectileOffset = off);
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_projectileParticles",
                FallbackProjectileParticles, off => ProjectileParticlesOffset = off);
            Resolve(Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_useCurvedTrajectory",
                FallbackUseCurvedTrajectory, off => UseCurvedTrajectoryOffset = off);
            // [2026-10-04 14:20] NEW: _projectileSpeed for the dt-robust aim point.
            Resolve(Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_projectileSpeed",
                FallbackProjectileSpeed, off => ProjectileSpeedOffset = off);
            // [2026-10-04 17:00] NEW: _shardSpeed resolution.
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_shardSpeed",
                FallbackShardSpeed, off => ShardSpeedOffset = off);
            // [2026-10-04 17:30] NEW: _launchOffset resolution.
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_launchOffset",
                FallbackLaunchOffset, off => LaunchOffsetOffset = off);
            // [2026-10-04 09:15] NEW: lifetime-scaling targets. _maxTimeAlive lives on
            // BaseSpellLibrary (the spell TTL); the other three are PlagueShardPrefab fields.
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_rangeLimit",
                FallbackRangeLimit, off => RangeLimitOffset = off);
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_flightDuration",
                FallbackFlightDuration, off => FlightDurationOffset = off);
            Resolve(Il2CppClassPointerStore<BaseSpellLibrary>.NativeClassPtr, "_maxTimeAlive",
                FallbackMaxTimeAlive, off => MaxTimeAliveOffset = off);
            Resolve(Il2CppClassPointerStore<PlagueShardPrefab>.NativeClassPtr, "_projectileVfx",
                FallbackProjectileVfx, off => ProjectileVfxOffset = off);
        }

        private static void Resolve(IntPtr classPtr, string fieldName, int fallback, Action<int> assign)
        {
            try
            {
                IntPtr field = IL2CPP.GetIl2CppField(classPtr, fieldName);
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0)
                    {
                        assign(off);
                        return;
                    }
                }
                PlagueShardsHomingPlugin.Log.LogWarning(
                    $"[GameFields] {fieldName} not found — using fallback 0x{fallback:X}.");
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log.LogWarning(
                    $"[GameFields] {fieldName} lookup failed ({ex.Message}) — using fallback 0x{fallback:X}.");
            }
            assign(fallback);
        }
    }

    /// <summary>
    /// Nearest-live-enemy lookup around a world position, via the game's own protected
    /// BaseSpellLibrary.GetAllEnemiesInRangeOfPosition(Vector2, float) (so team filtering stays
    /// vanilla) invoked through cached reflection.
    /// </summary>
    internal static class EnemyFinder
    {
        private static MethodInfo _getEnemiesOfPosition;
        private static bool _lookupFailed;

        /// <summary>Find the nearest live enemy around <paramref name="center"/>.
        /// <paramref name="seenCount"/> reports how many candidates the query returned (for diagnostics).</summary>
        public static ObjectsCommon FindNearest(BaseSpellLibrary caster, Vector2 center, float range, out int seenCount)
        {
            seenCount = 0;
            if (caster == null || range <= 0f) return null;

            if (_getEnemiesOfPosition == null && !_lookupFailed)
            {
                _getEnemiesOfPosition = AccessTools.Method(
                    typeof(BaseSpellLibrary), "GetAllEnemiesInRangeOfPosition");
                if (_getEnemiesOfPosition == null)
                {
                    _lookupFailed = true;
                    PlagueShardsHomingPlugin.Log?.LogError(
                        "[EnemyFinder] BaseSpellLibrary.GetAllEnemiesInRangeOfPosition not found — homing disabled.");
                    return null;
                }
            }
            if (_getEnemiesOfPosition == null) return null;

            var list = (Il2CppGenerics.List<ObjectsCommon>)_getEnemiesOfPosition.Invoke(
                caster, new object[] { center, range });
            if (list == null) return null;

            ObjectsCommon best = null;
            float bestSqr = float.MaxValue;

            int count = list.Count;
            seenCount = count;
            for (int i = 0; i < count; i++)
            {
                var enemy = list[i];
                if (enemy == null) continue;
                if (enemy.IsDead()) continue;

                Vector2 enemyPos = enemy.transform.position;
                float sqr = (enemyPos - center).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// Prefix on PlagueShardPrefab.Update: steer the shard toward the nearest enemy before the
    /// vanilla Update step moves it (MoveProjectileTowardsTarget) and sweeps for hits
    /// (SweepShardFootprint). The original Update is never skipped.
    /// </summary>
    [HarmonyPatch(typeof(PlagueShardPrefab), "Update")]
    internal static class Patch_PlagueShardPrefab_Update
    {
        // [2026-10-03 22:50] Proof-of-life + diag counters. The first invocation line is NOT gated
        // by the config: it tells us the patch fires at all (in an earlier test the log stayed
        // silent and nothing could be concluded).
        private static bool _announced;

        // [2026-10-04 00:40] NEW: scratch buffer reused across frames/shards for particle steering.
        // GetParticles/SetParticles take an Il2CppStructArray<ParticleSystem.Particle>; reusing one
        // array avoids per-frame allocations while the spell is in flight (grown on demand only).
        private static Il2CppStructArray<ParticleSystem.Particle> _particleBuffer;

        // [2026-10-04 00:40] NEW: one-shot flag for the VFX diagnostics (see LogVfxDiag).
        private static bool _vfxDiagLogged;
        private static int _diagCalls;

        // [2026-10-04 13:30] NEW: AetherHowl-style per-shard target lock (keyed by the shard's
        // IL2CPP instance pointer, same pattern as Patch_PlagueShardPrefab_PlayProjectileVfx._originals).
        // See ResolveLockedTarget for the semantics.
        private static readonly System.Collections.Generic.Dictionary<long, ObjectsCommon> _targetLocks =
            new System.Collections.Generic.Dictionary<long, ObjectsCommon>();

        /// <summary>[2026-10-04 13:30] NEW: drop the target lock of a shard about to be destroyed
        /// (called from Patch_PlagueShardPrefab_HandleBeforeSpellDestroyed) so a recycled instance
        /// pointer cannot inherit another shard's lock.</summary>
        public static void Cleanup(long key) => _targetLocks.Remove(key);

        /// <summary>
        /// [2026-10-04 13:30] NEW — AetherHowl-style lock-on target resolution
        /// (AetherHowlProjectilePrefab.ResolveHomingTarget semantics): keep chasing the SAME enemy
        /// frame after frame and resolve a new nearest one ONLY when the locked target is gone
        /// (destroyed or dead — the analog of AetherHowl's TargetIsHiddenFromTracking).        ///
        /// DetectionRange therefore only bounds lock ACQUISITION/re-acquisition; once locked, the
        /// chase follows the target anywhere (AetherHowl never range-checks its _target mid-flight).
        /// </summary>
        private static ObjectsCommon ResolveLockedTarget(
            PlagueShardPrefab shard, long shardKey, Vector2 position, out int seenCount, out bool newlyLocked)
        {
            seenCount = 0;
            newlyLocked = false;

            // [2026-10-04 13:30] Was: re-run EnemyFinder.FindNearest EVERY frame (per-frame
            // retarget). Obsolete — see the class-doc note: the user asked for AetherHowl-style
            // lock-on ("homing seperti aetherhowl"). Kept for reference:
            //ObjectsCommon target = EnemyFinder.FindNearest(
            //    shard, position, PlagueShardsHomingPlugin.DetectionRange.Value, out seenCount);

            if (_targetLocks.TryGetValue(shardKey, out ObjectsCommon locked)
                && locked != null && !locked.WasCollected && !locked.IsDead())
            {
                return locked;
            }

            ObjectsCommon target = EnemyFinder.FindNearest(
                shard, position, PlagueShardsHomingPlugin.DetectionRange.Value, out seenCount);
            if (target == null)
            {
                _targetLocks.Remove(shardKey);
                return null;
            }

            _targetLocks[shardKey] = target;
            newlyLocked = true;
            return target;
        }

        public static void Prefix(PlagueShardPrefab __instance)
        {
            try
            {
                if (!_announced)
                {
                    _announced = true;
                    PlagueShardsHomingPlugin.Log.LogInfo(
                        "[Patch_PlagueShardPrefab_Update] first invocation — patch is live.");
                }

                if (__instance == null) return;
                if (!PlagueShardsHomingPlugin.Enabled.Value) return;

                GameFields.EnsureResolved();

                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;

                    // [2026-10-03 09:00] Was: gate on `_flightElapsed (0x370) > 0`.
                    // Obsolete because DriveSpeedProfile only advances _flightElapsed when the
                    // static PlagueShardPrefab._speedProfile array (built by BuildSpeedProfile from
                    // the projectile VFX VelocityOverLifetime module) is non-null. When that array
                    // is null, DriveSpeedProfile returns immediately, _flightElapsed stays 0
                    // forever, and the gate blocked homing every frame ("projectile masih lurus").
                    // The canonical in-flight flag is SkillShot._launchProjectile (0x2BB): set to 1
                    // by SkillShot.LaunchProjectile at launch, flipped to 0 by
                    // MoveProjectileTowardsTarget on arrival. The wind-up phase is still skipped
                    // (the flag is 0 then).
                    //float flightElapsed = *(float*)((byte*)p + GameFields.FlightElapsedOffset);
                    //if (flightElapsed <= 0f) return;

                    byte launched = *(byte*)((byte*)p + GameFields.LaunchProjectileOffset);
                    float flightElapsed = *(float*)((byte*)p + GameFields.FlightElapsedOffset);
                    if (launched == 0)
                    {
                        // [2026-10-04 17:30] User request: "jika itu di set by timer buat dia start mengejar lebih awal".
                        // In vanilla, shard waits in LaunchAfterAnticipation coroutine (startDelay - hitboxLeadSeconds)
                        // before SkillShot.LaunchProjectile sets _launchProjectile=1 and plays particle systems.
                        // If AnticipationMultiplier <= 0.25f, bypass this delay and launch immediately so
                        // homing starts on frame 1 without delay!
                        // The old gate is preserved below for reference:
                        //LogDiag(__instance,
                        //    $"not in flight yet (_launchProjectile=0, elapsed {flightElapsed:F2}, dt {Time.deltaTime:F3}, f#{Time.frameCount})");
                        //return;
                        float anticMult = PlagueShardsHomingPlugin.AnticipationMultiplier.Value;
                        if (anticMult <= 0.25f)
                        {
                            __instance.LaunchProjectile();
                            IntPtr psArrayPtr = *(IntPtr*)((byte*)p + GameFields.ProjectileParticlesOffset);
                            if (psArrayPtr != IntPtr.Zero)
                            {
                                var systems = new Il2CppReferenceArray<ParticleSystem>(psArrayPtr);
                                for (int i = 0; i < systems.Length; i++)
                                {
                                    if (systems[i] != null && !systems[i].isPlaying)
                                        systems[i].Play();
                                }
                            }
                            *(byte*)((byte*)p + GameFields.LaunchProjectileOffset) = 1;
                            launched = 1;
                            LogDiag(__instance, $"Early launch triggered (anticipation x{anticMult:0.##}) on frame #{Time.frameCount}");
                        }
                        else
                        {
                            // [2026-10-04 14:20] Added dt + frame count: these lines are how we measure
                            // the real frame rate between Update calls (the last test showed each shard
                            // logging only ~2 lines total — dt here tells whether frames are seconds apart).
                            LogDiag(__instance,
                                $"not in flight yet (_launchProjectile=0, elapsed {flightElapsed:F2}, dt {Time.deltaTime:F3}, f#{Time.frameCount})");
                            return;
                        }
                    }

                    // [2026-10-04 00:40] NEW: one-shot VFX diag — closes the last runtime unknowns:
                    // (1) _useCurvedTrajectory must be 0, else the game moves the shard with
                    // MoveProjectileAlongCurve (throwTime spline) and never reads our _targetPosition
                    // writes — that would silently defeat the whole homing patch;
                    // (2) the particle simulation space, to confirm how the streak is rendered (both
                    // Local and World are handled by SteerParticleVfx — this is verification).
                    if (!_vfxDiagLogged && PlagueShardsHomingPlugin.DiagnosticLogging.Value)
                    {
                        _vfxDiagLogged = true;
                        LogVfxDiag(__instance, p);
                    }

                    Vector2 position = __instance.transform.position;

                    // [2026-10-04 13:30] Was: EnemyFinder.FindNearest(...) every frame — per-frame
                    // retarget. Obsolete because the user asked for AetherHowl-style homing
                    // ("homing seperti aetherhowl"): one LOCKED target chased until it dies, not a
                    // nearest-enemy re-pick each frame (which made shards swap targets mid-flight).
                    //ObjectsCommon target = EnemyFinder.FindNearest(
                    //    __instance, position, PlagueShardsHomingPlugin.DetectionRange.Value, out int seenCount);
                    long shardKey = p.ToInt64();
                    ObjectsCommon target = ResolveLockedTarget(
                        __instance, shardKey, position, out int seenCount, out bool newlyLocked);
                    if (target == null)
                    {
                        // no enemy in range -> pure vanilla fan flight
                        LogDiag(__instance,
                            $"in flight but NO enemy seen (query returned {seenCount}, range {PlagueShardsHomingPlugin.DetectionRange.Value})");
                        return;
                    }

                    Vector2 heading = *(Vector2*)((byte*)p + GameFields.FlightDirectionOffset);
                    if (heading.sqrMagnitude < 1e-6f)
                    {
                        LogDiag(__instance, "in flight but _flightDirection is zero — cannot steer yet");
                        return;
                    }
                    heading.Normalize();

                    Vector2 toTarget = ((Vector2)target.transform.position) - position;
                    Vector2 desired = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : heading;

                    float turnRate = PlagueShardsHomingPlugin.TurnRateDegPerSec.Value;
                    Vector2 newHeading = turnRate <= 0f
                        ? desired
                        : RotateTowards2D(heading, desired, turnRate * Mathf.Deg2Rad * Time.deltaTime);

                    // Keep the vanilla sweep axis in sync so SweepShardFootprint keeps hitting while
                    // turning: the sweep projects candidates onto (_flightDirection from
                    // _launchPosition) along the shard's travel axis (see PlagueShardPrefab ISIL,
                    // SweepShardFootprint ~line 2314: dot(candidate - _launchPosition, _flightDirection)),
                    // so both must describe the CURRENT curved path, not the original fan line.
                    *(Vector2*)((byte*)p + GameFields.FlightDirectionOffset) = newHeading;
                    *(Vector2*)((byte*)p + GameFields.LaunchPositionOffset) = position;

                    // Aim point far along the new heading: keeps MoveProjectileTowardsTarget flying
                    // instead of "arriving" at the enemy position and despawning (OnDestinationReached)
                    // before the sweep can damage it.
                    // [2026-10-04 14:20] Was: lookAhead = _maximumTravelDistance (0x2A8) only. Two
                    // problems, both found in the BepInEx log of the last test (each shard logged
                    // exactly ONE homing frame then died):
                    //   (1) PlagueShardPrefab.ApplyTargetHit/OnDestinationReached/OnMaxDistanceReached
                    //       all RELEASE the spell (shard dies), and MoveProjectileTowardsTarget
                    //       "arrives" the moment speed*Time.deltaTime >= distance-to-aim — a single
                    //       frame-time spike (dt >= 0.44 s at the vanilla 20 u/s, e.g. the spawn hitch
                    //       of 5 shards + VFX) teleported the shard onto the 8.8 u aim point and killed
                    //       it before it could ever turn;
                    //   (2) the same 8.8 u is the _maximumTravelDistance death trigger (not scaled by
                    //       the LifetimeMultiplier patch — that one only scaled _rangeLimit 0x36C), so
                    //       the shard died after 8.8 u of travel even with LifetimeMultiplier=6.
                    // New: aim at least 3 travel-steps (speed*dt) ahead, and with the now-scaled
                    // _maximumTravelDistance as the base — the AetherHowl pattern (its aim point sits
                    // at its own _maxTravelDistanceValue, far ahead of any single step).
                    float speed = *(float*)((byte*)p + GameFields.ProjectileSpeedOffset);
                    float dt = Time.deltaTime;
                    float lookAhead = Mathf.Max(
                        *(float*)((byte*)p + GameFields.MaximumTravelDistanceOffset),
                        speed * dt * 3f);
                    *(Vector2*)((byte*)p + GameFields.TargetPositionOffset) = position + newHeading * lookAhead;

                    // [2026-10-04 00:40] NEW: bend the VISIBLE streak along with the hitbox. Steering
                    // _flightDirection/_targetPosition alone only curves the invisible hitbox — the
                    // shard's visible body is the detached particle VFX (_projectileVfxInstance, spawned
                    // unparented via BaseSpellLibrary.SpawnAndTrack and never moved or rotated by the
                    // game), whose particles fly on baked world-space velocity along the fan direction.
                    // That is why the projectile still looked straight ("projectile masih lurus") while
                    // the homing logs showed the heading turning. Rotating every live particle's velocity
                    // by exactly this frame's turn delta — the same rotation the heading just received —
                    // makes the streak trace the same chase arc as the hitbox (Chrono-Spike style).
                    float turnDeltaRad = Vector2.SignedAngle(heading, newHeading) * Mathf.Deg2Rad;
                    SteerParticleVfx(__instance, turnDeltaRad);

                    if (PlagueShardsHomingPlugin.RotateVisual.Value)
                    {
                        float angle = Mathf.Atan2(newHeading.y, newHeading.x) * Mathf.Rad2Deg
                                      + PlagueShardsHomingPlugin.RotateVisualOffsetDeg.Value;
                        __instance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    }

                    // [2026-10-04 13:30] Was: always "HOMING -> ...". Extended: show whether this frame
                    // acquired a fresh lock or is chasing the existing one (lock-on model).
                    //LogDiag(__instance,
                    //    $"HOMING -> {target.name} (seen {seenCount}, heading {newHeading.x:F2},{newHeading.y:F2}, " +
                    //    $"elapsed {flightElapsed:F2}, lookAhead {lookAhead:F1})");
                    LogDiag(__instance,
                        $"{(newlyLocked ? "LOCKED+HOMING" : "HOMING")} -> {target.name} (seen {seenCount}, heading {newHeading.x:F2},{newHeading.y:F2}, " +
                        $"elapsed {flightElapsed:F2}, lookAhead {lookAhead:F1}, spd {speed:F1}, dt {dt:F3}, f#{Time.frameCount})");
                }
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log?.LogError($"[Patch_PlagueShardPrefab_Update] {ex}");
            }
        }

        /// <summary>Diagnostic line. [2026-10-03 22:50] Was: throttled to Time.frameCount % 120 — too
        /// coarse (one frame per 2 s), so a whole cast could pass unlogged and the log proved nothing.
        /// Now: first 200 calls always, then every 30 frames.</summary>
        private static void LogDiag(PlagueShardPrefab shard, string message)
        {
            if (!PlagueShardsHomingPlugin.DiagnosticLogging.Value) return;
            _diagCalls++;
            //if (Time.frameCount % 120 != 0) return;   // [2026-10-03 22:50] obsolete throttle, see above
            if (_diagCalls > 200 && Time.frameCount % 30 != 0) return;
            PlagueShardsHomingPlugin.Log.LogInfo($"[Update#{_diagCalls}] {shard.name}: {message}");
        }

        /// <summary>Rotate <paramref name="current"/> (unit) toward <paramref name="desired"/> (unit)
        /// by at most <paramref name="maxRadians"/> — the 2D analogue of Vector3.RotateTowards used
        /// by the game's own homing projectiles (AetherHowlProjectilePrefab).</summary>
        private static Vector2 RotateTowards2D(Vector2 current, Vector2 desired, float maxRadians)
        {
            float delta = Vector2.SignedAngle(current, desired) * Mathf.Deg2Rad;
            float step = Mathf.Clamp(delta, -maxRadians, maxRadians);
            float cos = Mathf.Cos(step);
            float sin = Mathf.Sin(step);
            return new Vector2(current.x * cos - current.y * sin, current.x * sin + current.y * cos);
        }

        /// <summary>[2026-10-04 00:40] NEW: one-shot diagnostics for the two facts this fix depends
        /// on — (1) _useCurvedTrajectory (0x2BA) must be 0, else the game moves the shard with
        /// MoveProjectileAlongCurve (throwTime spline) and never consumes our _targetPosition writes;
        /// (2) the projectile VFX's particle simulation space, to confirm how the streak is
        /// rendered. Both were open unknowns in the previous build (the homing logs proved the
        /// hitbox turns but the visuals stayed straight).</summary>
        private static unsafe void LogVfxDiag(PlagueShardPrefab shard, IntPtr p)
        {
            byte useCurved = *(byte*)((byte*)p + GameFields.UseCurvedTrajectoryOffset);
            IntPtr psArrayPtr = *(IntPtr*)((byte*)p + GameFields.ProjectileParticlesOffset);

            string simSpace = "n/a";
            int particleCount = -1;
            int systemCount = 0;
            if (psArrayPtr != IntPtr.Zero)
            {
                var systems = new Il2CppReferenceArray<ParticleSystem>(psArrayPtr);
                systemCount = systems.Length;
                if (systemCount > 0 && systems[0] != null)
                {
                    simSpace = systems[0].main.simulationSpace.ToString();
                    particleCount = systems[0].particleCount;
                }
            }

            PlagueShardsHomingPlugin.Log.LogInfo(
                $"[VFX] {shard.name}: _useCurvedTrajectory={(useCurved != 0)} " +
                $"(must be False for MoveProjectileTowardsTarget to consume our aim point) | " +
                $"particleSystems={systemCount} | simulationSpace={simSpace} | liveParticles={particleCount}");
        }

        /// <summary>
        /// [2026-10-04 00:40] NEW: rotate every live particle's velocity in the shard's projectile
        /// VFX by the same per-frame turn delta the steered hitbox just received, so the visible
        /// streak bends along the chase arc instead of flying straight.
        ///
        /// Why this exists: the shard's visible body is the detached particle instance        /// (_projectileVfxInstance, spawned unparented through BaseSpellLibrary.SpawnAndTrack and
        /// never moved or rotated by the game). Its particles keep their baked emission velocity
        /// along the fan direction, so steering only _flightDirection/_targetPosition curves the
        /// invisible hitbox while the streak keeps going straight — the "projectile masih lurus"
        /// symptom.
        ///
        /// Why velocity rotation is enough: the instance transform is never moved, so Local and        /// World simulation space are identical here (fixed frame), and Z-rotations commute —
        /// rotating the velocities in the particles' own simulation space rotates the rendered
        /// streak the same way. Particle positions are left alone on purpose: they follow from the
        /// rotated velocities on the next simulation step, which is what produces the visible curve
        /// (rotating positions would teleport the trail).
        ///
        /// The shard prefab only uses VelocityOverLifetimeModule.speedModifier (scales speed,        /// preserves direction — BuildSpeedProfile samples get_speedModifier), so the rotated
        /// velocities stick instead of being re-aimed straight by the module.
        /// </summary>
        private static unsafe void SteerParticleVfx(PlagueShardPrefab shard, float deltaRadians)
        {
            if (Mathf.Abs(deltaRadians) < 1e-5f) return;

            try
            {
                IntPtr psArrayPtr = *(IntPtr*)(IL2CPP.Il2CppObjectBaseToPtrNotNull(shard) + GameFields.ProjectileParticlesOffset);
                if (psArrayPtr == IntPtr.Zero) return;
                var systems = new Il2CppReferenceArray<ParticleSystem>(psArrayPtr);

                float cos = Mathf.Cos(deltaRadians);
                float sin = Mathf.Sin(deltaRadians);

                for (int s = 0; s < systems.Length; s++)
                {
                    var ps = systems[s];
                    if (ps == null) continue;

                    int count = ps.particleCount;
                    if (count <= 0) continue;

                    if (_particleBuffer == null || _particleBuffer.Length < count)
                        _particleBuffer = new Il2CppStructArray<ParticleSystem.Particle>(Mathf.Max(count, 32));

                    int n = ps.GetParticles(_particleBuffer);
                    int limit = Mathf.Min(n, _particleBuffer.Length);
                    float targetSpeed = PlagueShardsHomingPlugin.ProjectileSpeed.Value;
                    for (int i = 0; i < limit; i++)
                    {
                        // Struct elements: read-copy-modify-write (the indexer returns a copy).
                        var particle = _particleBuffer[i];
                        Vector3 v = particle.m_Velocity;
                        float vx = cos * v.x - sin * v.y;
                        float vy = sin * v.x + cos * v.y;
                        // [2026-10-04 17:00] Synchronize particle speed with projectile speed so visual matches hitbox
                        float curSpeed = Mathf.Sqrt(vx * vx + vy * vy);
                        if (curSpeed > 1e-4f && targetSpeed > 0f)
                        {
                            float factor = targetSpeed / curSpeed;
                            vx *= factor;
                            vy *= factor;
                        }
                        particle.m_Velocity = new Vector3(vx, vy, v.z);
                        // [2026-10-04 17:30] FIX "visual mati/hilang ditengah jalan":
                        // Keep live particle remainingLifetime healthy while in flight so the streak
                        // visual never expires before the hitbox arrives at slow speeds (4 u/s).
                        if (particle.remainingLifetime < 5f)
                        {
                            particle.remainingLifetime = 10f;
                        }
                        _particleBuffer[i] = particle;
                    }
                    ps.SetParticles(_particleBuffer, limit);
                }
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log?.LogWarning($"[SteerParticleVfx] {ex.Message}");
            }
        }
    }

    /// <summary>
    /// [2026-10-04 16:30] NEW — Fix "speed terlalu cepat":
    /// In vanilla PlagueShardPrefab.DriveSpeedProfile, speed is calculated from the authored VFX speed curve
    /// via (TravelFraction(new_u) - TravelFraction(old_u)) * _rangeLimit / dt.
    /// When _rangeLimit is extended for homing lifetime, this formula produces a massive initial burst
    /// of 160–175 units/sec! At 175 u/s, shards travel 5 units per frame, overshoot targets, have a huge
    /// turn radius (~19 units), and expire in under a second.
    /// This prefix replaces that calculation by advancing _flightElapsed normally and enforcing a steady,
    /// controlled speed (ProjectileSpeed, default 18 u/s). At 18 u/s, a 540 deg/s turn rate gives a tight
    /// ~1.9 unit radius, allowing the shard to smoothly curve and lock onto enemies.
    /// </summary>
    [HarmonyPatch(typeof(PlagueShardPrefab), "DriveSpeedProfile")]
    internal static class Patch_PlagueShardPrefab_DriveSpeedProfile
    {
        public static bool Prefix(PlagueShardPrefab __instance)
        {
            try
            {
                if (__instance == null || !PlagueShardsHomingPlugin.Enabled.Value) return true;

                GameFields.EnsureResolved();
                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return true;

                    byte launched = *(byte*)((byte*)p + GameFields.LaunchProjectileOffset);
                    if (launched == 0) return false;

                    // Advance flight elapsed
                    *(float*)((byte*)p + GameFields.FlightElapsedOffset) += Time.deltaTime;

                    // Override _projectileSpeed to steady homing speed
                    *(float*)((byte*)p + GameFields.ProjectileSpeedOffset) = PlagueShardsHomingPlugin.ProjectileSpeed.Value;
                }
                return false; // Skip the vanilla 175 u/s burst computation!
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log?.LogError($"[Patch_PlagueShardPrefab_DriveSpeedProfile] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// [2026-10-04 09:15] NEW — lifetime scaling (user: "lifetime projectilenya terlalu singkat").
    ///
    /// [2026-10-04 14:20] CORRECTED entry point: the original build gated this on
    /// PlagueShardPrefab.PlayProjectileVfx ("the emission gate"), but the whole-session BepInEx log
    /// of the last test shows the game NEVER calls PlayProjectileVfx for plague shards (0 log lines
    /// from this patch) — so none of the scaling ever ran and the shard kept dying at
    /// _maximumTravelDistance = 8.8 u. The real gate is PlagueShardPrefab.OnStart's Postfix
    /// (Patch_PlagueShardPrefab_OnStart below): by the time OnStart returns, all fields are written
    /// (_rangeLimit step 157, _flightDuration step 204, _maxTimeAlive step 381) and
    /// ConfigureShardParticles has set the particle lifetimes.
    ///
    /// PlayProjectileVfx is the emission gate: OnStart has already written _maximumTravelDistance
    /// (0x2A8), _rangeLimit (0x36C), _flightDuration (0x374) and the spell TTL
    /// (BaseSpellLibrary._maxTimeAlive, 0x1C8) by the time it runs, and ConfigureShardParticles has
    /// set every particle system's startLifetime. Scaling all of them by Homing.LifetimeMultiplier
    /// keeps the vanilla speed: OnStart derives _flightDuration = _rangeLimit / _shardSpeed and
    /// DriveSpeedProfile recomputes _projectileSpeed = ΔSpeedMultiplierAt(u) × _rangeLimit / dt with
    /// u = _flightElapsed / _flightDuration, so a common factor k on range and duration cancels.
    /// The streak is the detached particle instance, not the transform (see SteerParticleVfx), so
    /// startLifetime is scaled too or the visual would still die at the vanilla flight end.
    /// Idempotent: the first call caches what vanilla wrote and every call writes cache × k, so
    /// repeated Play calls and the second spawn path (SpawnProjectileVfx) cannot compound.
    /// </summary>
    [HarmonyPatch(typeof(PlagueShardPrefab), "PlayProjectileVfx")]
    internal static class Patch_PlagueShardPrefab_PlayProjectileVfx
    {
        private struct Originals
        {
            public float MaxTravel, Range, FlightDuration, MaxTimeAlive, ParticleLifetime;
        }

        private static readonly System.Collections.Generic.Dictionary<long, Originals> _originals =
            new System.Collections.Generic.Dictionary<long, Originals>();

        /// <summary>Drops the cached originals for a shard about to be destroyed (called from
        /// Patch_PlagueShardPrefab_HandleBeforeSpellDestroyed).</summary>
        public static void Cleanup(long key) => _originals.Remove(key);

        // [2026-10-04 14:20] Was: this Prefix was the only entry point of the scaling. Obsolete as
        // the PRIMARY gate — the game never calls PlayProjectileVfx for plague shards, so the
        // scaling silently never ran (see the class doc above). The logic is unchanged and now lives
        // in ScaleShard, driven primarily by Patch_PlagueShardPrefab_OnStart.Postfix; this Prefix is
        // kept as a secondary trigger in case PlayProjectileVfx ever does run (the originals cache
        // makes double-running idempotent).
        public static void Prefix(PlagueShardPrefab __instance) => ScaleShard(__instance);

        /// <summary>[2026-10-04 14:20] Extracted from Prefix so both PlayProjectileVfx (secondary)
        /// and OnStart Postfix (primary) can run the idempotent field/lifetime scaling.</summary>
        public static void ScaleShard(PlagueShardPrefab __instance)
        {
            try
            {
                if (__instance == null || !PlagueShardsHomingPlugin.Enabled.Value) return;

                float k = PlagueShardsHomingPlugin.LifetimeMultiplier.Value;
                if (k <= 0f || Mathf.Approximately(k, 1f)) return;

                GameFields.EnsureResolved();
                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;
                    long key = p.ToInt64();

                    IntPtr psArrayPtr = *(IntPtr*)((byte*)p + GameFields.ProjectileParticlesOffset);
                    var systems = psArrayPtr == IntPtr.Zero
                        ? null
                        : new Il2CppReferenceArray<ParticleSystem>(psArrayPtr);

                    if (!_originals.TryGetValue(key, out Originals orig))
                    {
                        orig = new Originals
                        {
                            MaxTravel = *(float*)((byte*)p + GameFields.MaximumTravelDistanceOffset),
                            Range = *(float*)((byte*)p + GameFields.RangeLimitOffset),
                            FlightDuration = *(float*)((byte*)p + GameFields.FlightDurationOffset),
                            MaxTimeAlive = *(float*)((byte*)p + GameFields.MaxTimeAliveOffset),
                        };
                        // ConfigureShardParticles writes the same lifetime on every system, so one
                        // sample is enough (first system that exists).
                        if (systems != null)
                        {
                            for (int i = 0; i < systems.Length; i++)
                            {
                                if (systems[i] == null) continue;
                                orig.ParticleLifetime = systems[i].main.startLifetime.constant;
                                break;
                            }
                        }
                        _originals[key] = orig;
                    }

                    float targetSpeed = PlagueShardsHomingPlugin.ProjectileSpeed.Value;
                    float speedSafe = Mathf.Max(0.5f, targetSpeed);

                    *(float*)((byte*)p + GameFields.ShardSpeedOffset) = targetSpeed;
                    *(float*)((byte*)p + GameFields.ProjectileSpeedOffset) = targetSpeed;

                    float scaledMaxTravel = orig.MaxTravel * k;
                    float scaledRange = orig.Range * k;

                    // [2026-10-04 17:30] FIX "visual mati/hilang ditengah jalan":
                    // Previously: FlightDuration and MaxTimeAlive were orig * k, and particleLifetime was
                    // orig.ParticleLifetime * k (~1.9s).
                    // Obsolete: At slow speed (4 u/s = 1/5 vanilla), flight duration is distance / speed = 40 / 4 = 10s!
                    // The particle died after 1.9s, leaving the invisible hitbox to fly for another 8s alone.
                    // Now: compute expected flight duration dynamically from scaledMaxTravel / targetSpeed + 5s buffer.
                    // The old code is kept below for reference:
                    //*(float*)((byte*)p + GameFields.FlightDurationOffset) = orig.FlightDuration * k;
                    //*(float*)((byte*)p + GameFields.MaxTimeAliveOffset) = orig.MaxTimeAlive * k;
                    float expectedFlightTime = (scaledMaxTravel / speedSafe) + 5f;

                    *(float*)((byte*)p + GameFields.MaximumTravelDistanceOffset) = scaledMaxTravel;
                    *(float*)((byte*)p + GameFields.RangeLimitOffset) = scaledRange;
                    *(float*)((byte*)p + GameFields.FlightDurationOffset) = expectedFlightTime;
                    *(float*)((byte*)p + GameFields.MaxTimeAliveOffset) = expectedFlightTime + 5f;

                    if (systems != null)
                    {
                        var speedCurve = new ParticleSystem.MinMaxCurve(targetSpeed);
                        var lifetimeCurve = new ParticleSystem.MinMaxCurve(expectedFlightTime);
                        for (int i = 0; i < systems.Length; i++)
                        {
                            var ps = systems[i];
                            if (ps == null) continue;
                            var main = ps.main;
                            main.startSpeed = speedCurve;
                            // [2026-10-04 17:30] Was:
                            //if (orig.ParticleLifetime > 0f)
                            //{
                            //    main.startLifetime = new ParticleSystem.MinMaxCurve(orig.ParticleLifetime * k);
                            //}
                            // Obsolete: orig.ParticleLifetime * k was only ~1.9s, which died mid-flight.
                            // New: set startLifetime to expectedFlightTime so visual lives through full flight!
                            main.startLifetime = lifetimeCurve;
                        }
                    }

                    if (PlagueShardsHomingPlugin.DiagnosticLogging.Value)
                        PlagueShardsHomingPlugin.Log.LogInfo(
                            $"[Lifetime] {__instance.name}: x{k:0.##} — speed {targetSpeed:0.##}u/s, range {orig.Range:0.##}->{scaledRange:0.##}, " +
                            $"duration {orig.FlightDuration:0.##}->{expectedFlightTime:0.##}s, " +
                            $"ttl {orig.MaxTimeAlive:0.##}->{expectedFlightTime + 5f:0.##}s, " +
                            $"particleLifetime {orig.ParticleLifetime:0.##}->{expectedFlightTime:0.##}s");
                }
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log.LogError($"[Patch_PlayProjectileVfx] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-04 14:20] NEW — the REAL lifetime-scaling gate. See the corrected note on
    /// Patch_PlagueShardPrefab_PlayProjectileVfx: the game never calls PlayProjectileVfx for plague
    /// shards, so the previous build's scaling never executed and the shard died at vanilla
    /// _maximumTravelDistance = 8.8 u ("lifetime terlalu singkat") plus died to premature aim-point
    /// arrival during frame-time spikes. OnStart writes every one of those fields before returning
    /// (_rangeLimit step 157, _flightDuration step 204, _maxTimeAlive step 381) and
    /// ConfigureShardParticles has configured the particle lifetimes during it, so scaling here is
    /// safe and complete. Delegates to the idempotent ScaleShard (originals cache).
    /// </summary>
    [HarmonyPatch(typeof(PlagueShardPrefab), "OnStart")]
    internal static class Patch_PlagueShardPrefab_OnStart
    {
        // [2026-10-04 17:30] NEW: user request — "jika itu jarak, buat dia start di jarak 1/4 dari vanilla dia start mengejar musuh".
        // In vanilla, _launchOffset (0x2FC) offsets the spawn position forward from the caster.
        // Scaling _launchOffset by LaunchOffsetMultiplier (default 0.25 = 1/4 vanilla) starts the shard
        // 1/4 the distance from caster.
        public static void Prefix(PlagueShardPrefab __instance)
        {
            try
            {
                if (__instance == null || !PlagueShardsHomingPlugin.Enabled.Value) return;
                GameFields.EnsureResolved();
                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;
                    float mult = PlagueShardsHomingPlugin.LaunchOffsetMultiplier.Value;
                    if (mult > 0f && mult < 1f)
                    {
                        *(float*)((byte*)p + GameFields.LaunchOffsetOffset) *= mult;
                    }
                }
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log?.LogWarning($"[Patch_PlagueShardPrefab_OnStart.Prefix] {ex.Message}");
            }
        }

        public static void Postfix(PlagueShardPrefab __instance) =>
            Patch_PlagueShardPrefab_PlayProjectileVfx.ScaleShard(__instance);
    }

    /*
    // [2026-10-04 16:30] OBSOLETE / REMOVED: Diagnostic patches on virtual methods caused
    // Fatal error. System.AccessViolationException in DynamicClass.DMD<PlagueShardPrefab::OnMaxDistanceReached>
    // In IL2CPP, hooking virtual overrides dispatched through native vtable ([rax+598h]) corrupts
    // the native call trampoline. Removed to eliminate crashes.
    internal static class ShardDeathDiag
    {
        private static readonly System.Collections.Generic.HashSet<long> _logged =
            new System.Collections.Generic.HashSet<long>();

        public static void Log(PlagueShardPrefab shard, string reason)
        {
            try
            {
                if (shard == null || !PlagueShardsHomingPlugin.DiagnosticLogging.Value) return;
                IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(shard);
                if (p == IntPtr.Zero) return;
                long key = p.ToInt64();
                if (!_logged.Add(key)) return;
                Vector2 pos = shard.transform.position;
                PlagueShardsHomingPlugin.Log.LogInfo(
                    $"[DEATH] {shard.name} #{key & 0xFFFF:X4}: {reason} | pos {pos.x:F1},{pos.y:F1} | dt {Time.deltaTime:F3} | f#{Time.frameCount}");
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log?.LogWarning($"[ShardDeathDiag] {ex.Message}");
            }
        }

        public static void Cleanup(long key) => _logged.Remove(key);
    }

    [HarmonyPatch(typeof(PlagueShardPrefab), "OnDestinationReached")]
    internal static class Patch_PlagueShardPrefab_OnDestinationReached
    {
        public static void Prefix(PlagueShardPrefab __instance) =>
            ShardDeathDiag.Log(__instance, "OnDestinationReached: arrived at aim point 0xF0 -> lingering shard + spell released");
    }

    [HarmonyPatch(typeof(PlagueShardPrefab), "OnMaxDistanceReached")]
    internal static class Patch_PlagueShardPrefab_OnMaxDistanceReached
    {
        public static void Prefix(PlagueShardPrefab __instance) =>
            ShardDeathDiag.Log(__instance, "OnMaxDistanceReached: travelled >= _maximumTravelDistance -> lingering shard + spell released");
    }

    [HarmonyPatch(typeof(PlagueShardPrefab), "ApplyTargetHit")]
    internal static class Patch_PlagueShardPrefab_ApplyTargetHit
    {
        public static void Prefix(PlagueShardPrefab __instance, ObjectsCommon target) =>
            ShardDeathDiag.Log(__instance, $"ApplyTargetHit({(target == null ? "null" : target.name)}): single-hit -> impact VFX + lingering shard + spell released");
    }
    */

    /// <summary>
    /// [2026-10-04 09:15] NEW — companion cleanup for the originals cache in
    /// Patch_PlagueShardPrefab_PlayProjectileVfx: drop the entry when the shard is destroyed so it
    /// cannot leak (nor be reused by a recycled instance id).
    /// </summary>
    [HarmonyPatch(typeof(PlagueShardPrefab), "HandleBeforeSpellDestroyed")]
    internal static class Patch_PlagueShardPrefab_HandleBeforeSpellDestroyed
    {
        public static void Prefix(PlagueShardPrefab __instance)
        {
            try
            {
                if (__instance == null) return;
                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p != IntPtr.Zero)
                    {
                        // [2026-10-04 16:30] ShardDeathDiag removed to prevent virtual method crashes.
                        //ShardDeathDiag.Log(__instance,
                        //    "HandleBeforeSpellDestroyed: destroyed WITHOUT any known death path (unexpected)");
                        //ShardDeathDiag.Cleanup(p.ToInt64());

                        Patch_PlagueShardPrefab_PlayProjectileVfx.Cleanup(p.ToInt64());
                        // [2026-10-04 13:30] NEW: also drop the AetherHowl-style target lock (see
                        // Patch_PlagueShardPrefab_Update.Cleanup) so a recycled instance pointer
                        // cannot inherit another shard's locked target.
                        Patch_PlagueShardPrefab_Update.Cleanup(p.ToInt64());
                    }
                }
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log.LogError($"[Patch_HandleBeforeSpellDestroyed] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-04 09:15] NEW — OnStart spawns the streak VFX through
    /// BaseSpellLibrary.SpawnAndTrack(prefab, position, rotation, destroyDelay), with
    /// destroyDelay = duration + _anticipationSeconds. That delay timer is armed BEFORE the
    /// PlayProjectileVfx scaling can touch _flightDuration, so without this the particle instance
    /// (again: the visible streak, not the transform) would be auto-destroyed at the vanilla flight
    /// end and the extended flight would look exactly as short as before. The delay is scaled only
    /// for our shard's projectile VFX (pointer match on PlagueShardPrefab._projectileVfx); impact
    /// VFX and lingering-shard zones keep their vanilla lifetimes. destroyDelay > 0 is required so
    /// the "tracked until the spell is destroyed" default (0) is left untouched.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "SpawnAndTrack",
        new Type[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(float) })]
    internal static class Patch_BaseSpellLibrary_SpawnAndTrack
    {
        public static void Prefix(BaseSpellLibrary __instance, GameObject prefab, ref float destroyDelay)
        {
            try
            {
                if (destroyDelay <= 0f || __instance == null || prefab == null) return;

                float k = PlagueShardsHomingPlugin.LifetimeMultiplier.Value;
                if (k <= 0f || Mathf.Approximately(k, 1f)) return;

                if (!IsSpellInstance<PlagueShardPrefab>(__instance)) return;

                GameFields.EnsureResolved();
                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;
                    IntPtr vfxPrefab = *(IntPtr*)((byte*)p + GameFields.ProjectileVfxOffset);
                    if (vfxPrefab == IntPtr.Zero) return;
                    if (IL2CPP.Il2CppObjectBaseToPtrNotNull(prefab) != vfxPrefab) return;
                }

                float before = destroyDelay;
                // [2026-10-04 17:30] Was:
                //destroyDelay = before * k;
                // Obsolete: When speed is reduced to 4 u/s (1/5 vanilla), flight takes 10-12s.
                // before * k was only ~7.9s, destroying the VFX GameObject before the shard could arrive!
                // New: ensure destroyDelay matches expected flight duration.
                float speedSafe = Mathf.Max(0.5f, PlagueShardsHomingPlugin.ProjectileSpeed.Value);
                float expectedFlightTime = (8.8f * k / speedSafe) + 5f;
                destroyDelay = Mathf.Max(before * k, expectedFlightTime);
                if (PlagueShardsHomingPlugin.DiagnosticLogging.Value)
                    PlagueShardsHomingPlugin.Log.LogInfo(
                        $"[Lifetime] {__instance.name}: streak VFX destroyDelay {before:0.##}->{destroyDelay:0.##}s (x{k:0.##})");
            }
            catch (Exception ex)
            {
                PlagueShardsHomingPlugin.Log.LogError($"[Patch_SpawnAndTrack] {ex}");
            }
        }

        /// <summary>
        /// Robust IL2CPP instance-type test (same approach as BarrageOfArrowsTuner): Harmony can hand
        /// the patch a wrapper typed as the declared patch type even when the native object is the
        /// derived class, so fall back to comparing the runtime IL2CPP class name.
        /// </summary>
        private static bool IsSpellInstance<T>(BaseSpellLibrary instance) where T : BaseSpellLibrary
        {
            if (instance == null) return false;
            if (instance is T) return true;
            return instance.GetIl2CppType().Name == typeof(T).Name;
        }
    }
}
