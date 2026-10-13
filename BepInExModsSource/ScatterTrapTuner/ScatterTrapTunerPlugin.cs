using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;

namespace ScatterTrapTuner
{
    /// <summary>
    /// BepInEx 6 (IL2CPP) plugin that reworks the Shadow skill-tree "Scatter Trap" upgrade (the scatter
    /// node of Hunter's Trap). Vanilla behaviour: on arrival the trap fans five sub-traps (HuntersTrapSubPrefab)
    /// around the landing spot within an 8-unit radius, so they land all over the place and arm after a
    /// 1-second delay. This mod makes it behave like the Barrage of Arrows Tuner:
    ///
    ///   * [Targeting] No more random scatter: the five sub-traps cluster on the NEAREST enemy to the
    ///     landing spot, using a small configurable scatter radius (Targeting.ScatterRadius), so the traps
    ///     land on the enemy instead of nowhere. That auto-targeted enemy is also the pull/drag centre.
    ///   * [Activation] The sub-traps arm instantly - HuntersTrapSubPrefab.SubTrapActivationDelay is zeroed
    ///     right before each sub-trap reads it, removing the vanilla 1-second arming delay.
    ///   * [Drag] Black-Hole suction IDENTICAL to PursuingBlizzardTuner: the surrounding enemies are dragged
    ///     onto the cluster centre (the auto-targeted nearest enemy) using the game's own
    ///     BaseSpellLibrary.DashTargetToPosition with the same modifier semantics - on activate and then on
    ///     every tick while the sub-traps live (a 2-per-second yank at the default 0.5s interval), exactly
    ///     like Pursuing Blizzard. The config keys mirror PursuingBlizzardTuner's [BlackHoleSuction] section.
    ///
    /// Gating: ComputeScatterPoints is a private static method that ONLY the scatter coroutine
    /// (HuntersTrapPrefab.ScatterSetup) calls, so this mod only ever affects the Scatter Trap cast. The
    /// HuntersTrapPrefab instance is captured in a Prefix on HandleArrival (the arrival hook that decides
    /// scatter vs normal), then consumed by ComputeScatterPoints the same frame.
    ///
    /// Co-op: DashTargetToPosition is the game's own networked knockback coroutine, and the enemy lookup is
    /// the game's own GetAllEnemiesInRangeOfPosition, so networking behaves like the vanilla upgrades. Run the
    /// SAME config on host and client so both peers agree.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ScatterTrapTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.scattertraptuner";
        public const string NAME = "Scatter Trap Tuner";
        public const string VERSION = "1.1.0";

        internal static new ManualLogSource Log;
        internal static ScatterTrapTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;

        // [Targeting] cluster the sub-traps on the nearest enemy instead of scattering randomly.
        public static ConfigEntry<bool> ClusterOnNearestTarget;
        public static ConfigEntry<float> ScatterRadius;
        public static ConfigEntry<float> DetectionRange;

        // [Activation] arm the sub-traps immediately (no vanilla 1s delay).
        public static ConfigEntry<bool> InstantActivation;

        // [BlackHoleSuction] pull/drag, kept key-for-key in step with PursuingBlizzardTuner's suction.
        public static ConfigEntry<bool> SuctionEnabled;
        public static ConfigEntry<bool> SuctionOnActivate;
        public static ConfigEntry<bool> SuctionOnTick;
        public static ConfigEntry<float> SuctionRadiusMultiplier;
        public static ConfigEntry<float> SuctionDashTime;
        public static ConfigEntry<float> SuctionDistanceMultiplier;
        public static ConfigEntry<float> SuctionTickInterval;

        public static ConfigEntry<bool> DiagnosticLogging;

        // Field offset verified against this build's decompiled metadata
        // (HuntersTrapPrefab._scatterTrap, field offset 0x342).
        internal static int ScatterTrapOffset = 0x342;

        // The HuntersTrapPrefab whose arrival is currently being handled, captured in a HandleArrival Prefix
        // and consumed by ComputeScatterPoints in the same frame. Stored as a raw pointer so we do not root a
        // managed wrapper past the cast.
        internal static IntPtr CapturedScatterTrapPtr = IntPtr.Zero;
        internal static float CapturedScatterTrapTime = float.NegativeInfinity;

        // [Drag] Suction centre = the auto-targeted nearest enemy (captured when the scatter points are
        // computed). The lingering sub-traps re-pull toward this centre every tick, like Pursuing Blizzard.
        internal static Vector2 SuctionCenter;
        internal static bool SuctionCenterValid;
        internal static float LastSuctionTime = float.NegativeInfinity;

        // Native class pointer of HuntersTrapSubPrefab, used for an allocation-free type gate in the
        // per-frame SkillShot.Update tick hook.
        internal static IntPtr SubTrapClassPtr = IntPtr.Zero;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the Scatter Trap behaves exactly like vanilla. (Default: true)");

            ClusterOnNearestTarget = Config.Bind("Targeting", "ClusterOnNearestTarget", true,
                "If true the five scatter sub-traps land on the nearest enemy (with a small scatter) instead of " +
                "fanning out randomly around the landing spot. That enemy is also the pull/drag centre. (Default: true)");

            ScatterRadius = Config.Bind("Targeting", "ScatterRadius", 1.5f,
                "Radius (world units) of the scatter around the nearest enemy. Keep it small so every sub-trap " +
                "still lands on the target; 0 = all five exactly on the enemy. (Default: 1.5)");

            DetectionRange = Config.Bind("Targeting", "DetectionRange", 15.0f,
                "Radius (world units) around the landing spot in which the nearest enemy is searched. The traps " +
                "cluster on the closest enemy found within this range. This is also the base grab radius that the " +
                "suction RadiusMultiplier scales. (Default: 15.0)");

            InstantActivation = Config.Bind("Activation", "InstantActivation", true,
                "If true the scatter sub-traps arm immediately when they spawn (removes the vanilla 1-second " +
                "arming delay). (Default: true)");

            // [2026-10-13] Key-for-key copy of PursuingBlizzardTuner's [BlackHoleSuction] section, so both mods
            // can be configured identically. The only additions are the base grab radius (= Targeting.DetectionRange)
            // and TickInterval (the trap has no intrinsic tick, so the cadence is explicit; 0.5s = a 2/s yank
            // like Pursuing Blizzard's default tick).
            SuctionEnabled = Config.Bind("BlackHoleSuction", "Enabled", true,
                "If true the Scatter Trap drags surrounding enemies onto the cluster centre (the auto-targeted " +
                "nearest enemy) using the game's own BaseSpellLibrary.DashTargetToPosition (the exact Black Hole / " +
                "Pursuing Blizzard primitive). (Default: true)");

            SuctionOnActivate = Config.Bind("BlackHoleSuction", "SuctionOnActivate", true,
                "Pull all enemies in range onto the cluster centre immediately when the scatter trap lands. (Default: true)");

            SuctionOnTick = Config.Bind("BlackHoleSuction", "SuctionOnTick", true,
                "Repeat the pull every TickInterval seconds while the sub-traps are alive. At the default 0.5s this " +
                "is the same 2-per-second suction as Pursuing Blizzard. (Default: true)");

            SuctionRadiusMultiplier = Config.Bind("BlackHoleSuction", "RadiusMultiplier", 1.0f,
                "Multiplier on the base grab radius (Targeting.DetectionRange) for how far enemies are grabbed. " +
                "1.0 = same as the detection range; >1 reaches beyond it. (Default: 1.0)");

            SuctionDashTime = Config.Bind("BlackHoleSuction", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank (Black Hole uses 0.3). (Default: 0.2)");

            SuctionDistanceMultiplier = Config.Bind("BlackHoleSuction", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly " +
                "onto the cluster centre; >1 pulls it past the centre (clamped by walls). (Default: 1.0)");

            SuctionTickInterval = Config.Bind("BlackHoleSuction", "TickInterval", 0.5f,
                "Seconds between repeated suction pulls (SuctionOnTick). 0.5 matches Pursuing Blizzard's default " +
                "2-per-second tick. (Default: 0.5)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogValues", false,
                "Log target selection and drag decisions to the BepInEx console. (Default: false)");

            // Cache the sub-trap class pointer for the tick hook's allocation-free type gate.
            try
            {
                SubTrapClassPtr = Il2CppClassPointerStore<HuntersTrapSubPrefab>.NativeClassPtr;
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to resolve HuntersTrapSubPrefab class pointer: {ex.Message}");
            }

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_HuntersTrapPrefab_HandleArrival));
            PatchOrLog(harmony, typeof(Patch_HuntersTrapPrefab_ComputeScatterPoints));
            PatchOrLog(harmony, typeof(Patch_HuntersTrapSubPrefab_OnStart_InstantActivation));
            PatchOrLog(harmony, typeof(Patch_SkillShot_Update_SuctionTick));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"ClusterOnNearestTarget: {ClusterOnNearestTarget.Value}");
            Log.LogInfo($"ScatterRadius: {ScatterRadius.Value}");
            Log.LogInfo($"DetectionRange: {DetectionRange.Value}");
            Log.LogInfo($"InstantActivation: {InstantActivation.Value}");
            Log.LogInfo($"BlackHoleSuction: {SuctionEnabled.Value} (activate={SuctionOnActivate.Value}, tick={SuctionOnTick.Value}, radiusX{SuctionRadiusMultiplier.Value}, distX{SuctionDistanceMultiplier.Value}, dashTime={SuctionDashTime.Value}s, tickInterval={SuctionTickInterval.Value}s)");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo("=================================================");
        }

        private void PatchOrLog(Harmony harmony, Type patchType)
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

        /// <summary>
        /// True when the HuntersTrapPrefab instance resolved the "Scatter Trap" upgrade
        /// (_scatterTrap, offset 0x342), so we only touch scatter casts.
        /// </summary>
        internal static unsafe bool IsScatterTrap(HuntersTrapPrefab trap)
        {
            if (trap == null || trap.Pointer == IntPtr.Zero) return false;
            return *(byte*)((byte*)trap.Pointer + ScatterTrapOffset) != 0;
        }

        /// <summary>
        /// Black Hole suction, IDENTICAL to PursuingBlizzardTuner.ApplySuction: drag every live enemy inside
        /// <paramref name="baseRadius"/> * RadiusMultiplier onto <paramref name="center"/> using the game's own
        /// BaseSpellLibrary.DashTargetToPosition - the exact primitive BlackholePrefab.AreaOfEffectStrike uses.
        /// The pull reuses the game's knockback coroutine, which disables the NavMeshAgent during the lerp and
        /// restores it afterward, raycasts walls, and honours ObjectsCommon.Immune /
        /// MonsterBehaviourLibrary.DisplaceExempt / authority. The only difference from Pursuing Blizzard is the
        /// centre: here it is the auto-targeted nearest enemy (its own transform is close to ~zero distance and
        /// is naturally skipped), not the spell's own position.
        /// </summary>
        internal static void ApplySuction(BaseSpellLibrary spell, Vector2 center, float baseRadius)
        {
            if (!Enabled.Value || !SuctionEnabled.Value) return;
            if (spell == null || spell.Pointer == IntPtr.Zero) return;

            try
            {
                float radius = baseRadius * SuctionRadiusMultiplier.Value;
                if (radius <= 0.1f) return;

                var enemies = spell.GetAllEnemiesInRangeOfPosition(center, radius);
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

                    // Same shape as BlackholePrefab.AreaOfEffectStrike / PursuingBlizzardTuner.ApplySuction:
                    //   DashTargetToPosition(target, center, time, distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot)
                    spell.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[ScatterTrapSuction] dragged {pulled} enemies toward cluster centre ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>HuntersTrapPrefab.HandleArrival</c>. This is the arrival hook that chooses between the
    /// normal (DelayedActivation) and the scatter (ScatterSetup) path. We capture the instance only when the
    /// scatter flag is set, so the later ComputeScatterPoints call knows which trap instance to ask for the
    /// nearby enemies. A non-scatter arrival clears the capture.
    ///
    /// MUST be a Prefix: HandleArrival starts the ScatterSetup coroutine synchronously, and ScatterSetup
    /// calls ComputeScatterPoints (ISIL 164) BEFORE its first yield (WaitForSeconds, ISIL 419). A Postfix
    /// would therefore run after ComputeScatterPoints had already executed and captured too late.
    /// </summary>
    [HarmonyPatch(typeof(HuntersTrapPrefab), "HandleArrival")]
    public static class Patch_HuntersTrapPrefab_HandleArrival
    {
        public static void Prefix(HuntersTrapPrefab __instance)
        {
            try
            {
                if (!ScatterTrapTunerPlugin.Enabled.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                if (ScatterTrapTunerPlugin.IsScatterTrap(__instance))
                {
                    ScatterTrapTunerPlugin.CapturedScatterTrapPtr = __instance.Pointer;
                    ScatterTrapTunerPlugin.CapturedScatterTrapTime = Time.time;

                    if (ScatterTrapTunerPlugin.DiagnosticLogging.Value)
                    {
                        ScatterTrapTunerPlugin.Log.LogInfo("[HandleArrival] scatter trap captured");
                    }
                }
                else
                {
                    ScatterTrapTunerPlugin.CapturedScatterTrapPtr = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                ScatterTrapTunerPlugin.Log?.LogError($"[Patch_HuntersTrapPrefab_HandleArrival] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on the private static <c>HuntersTrapPrefab.ComputeScatterPoints(center, radius, count)</c>.
    /// Vanilla computes <paramref name="count"/> points in an 8-unit disc around <paramref name="center"/>
    /// (the landing spot); we instead place them in a tight disc around the NEAREST enemy so the sub-traps
    /// land on the target and uses that enemy as the pull/drag centre. Only called by the scatter coroutine,
    /// so no other spell is affected. Returns the computed list and skips vanilla, or falls through to vanilla
    /// when no enemy is found / disabled.
    /// </summary>
    [HarmonyPatch(typeof(HuntersTrapPrefab), "ComputeScatterPoints")]
    public static class Patch_HuntersTrapPrefab_ComputeScatterPoints
    {
        public static bool Prefix(Vector3 center, float radius, int count, ref Il2CppGenerics.List<Vector3> __result)
        {
            try
            {
                if (!ScatterTrapTunerPlugin.Enabled.Value) return true;
                if (ScatterTrapTunerPlugin.CapturedScatterTrapPtr == IntPtr.Zero) return true;

                // Guard against a stale capture (the capture is normally consumed the same frame).
                float age = Time.time - ScatterTrapTunerPlugin.CapturedScatterTrapTime;
                if (age > 3f)
                {
                    ScatterTrapTunerPlugin.CapturedScatterTrapPtr = IntPtr.Zero;
                    return true;
                }

                var trap = new HuntersTrapPrefab(ScatterTrapTunerPlugin.CapturedScatterTrapPtr);
                Vector2 centerXY = new Vector2(center.x, center.y);

                // Find the nearest enemy to the landing spot within DetectionRange.
                float search = ScatterTrapTunerPlugin.DetectionRange.Value;
                if (search <= 0.1f) search = 15f;

                var enemies = trap.GetAllEnemiesInRangeOfPosition(centerXY, search);

                Vector2 anchor = centerXY;
                bool found = false;

                if (enemies != null)
                {
                    float best = float.MaxValue;
                    int n = enemies.Count;
                    for (int i = 0; i < n; i++)
                    {
                        ObjectsCommon enemy = enemies[i];
                        if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                        if (enemy.IsDead()) continue;

                        Vector3 ep = enemy.transform.position;
                        Vector2 epos = new Vector2(ep.x, ep.y);
                        float d = (epos - centerXY).sqrMagnitude;
                        if (d < best)
                        {
                            best = d;
                            anchor = epos;
                            found = true;
                        }
                    }
                }

                ScatterTrapTunerPlugin.CapturedScatterTrapPtr = IntPtr.Zero;

                if (!found)
                {
                    // Nothing to target: leave the vanilla scatter alone.
                    ScatterTrapTunerPlugin.SuctionCenterValid = false;
                    if (ScatterTrapTunerPlugin.DiagnosticLogging.Value)
                    {
                        ScatterTrapTunerPlugin.Log.LogInfo("[ComputeScatterPoints] no enemy in range, using vanilla scatter");
                    }
                    return true;
                }

                // The auto-targeted nearest enemy becomes the pull/drag centre.
                ScatterTrapTunerPlugin.SuctionCenter = anchor;
                ScatterTrapTunerPlugin.SuctionCenterValid = true;
                ScatterTrapTunerPlugin.LastSuctionTime = Time.time;

                // Pull surrounding enemies onto the cluster centre on activate (Pursuing Blizzard parity).
                if (ScatterTrapTunerPlugin.SuctionOnActivate.Value)
                {
                    ScatterTrapTunerPlugin.ApplySuction(trap, anchor, search);
                }

                if (!ScatterTrapTunerPlugin.ClusterOnNearestTarget.Value) return true;
                if (count <= 0) return true;

                float scatter = ScatterTrapTunerPlugin.ScatterRadius.Value;
                if (scatter < 0f) scatter = 0f;

                var points = new Il2CppGenerics.List<Vector3>();
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * scatter;
                    points.Add(new Vector3(anchor.x + offset.x, anchor.y + offset.y, center.z));
                }

                __result = points;

                if (ScatterTrapTunerPlugin.DiagnosticLogging.Value)
                {
                    ScatterTrapTunerPlugin.Log.LogInfo(
                        $"[ComputeScatterPoints] landing {centerXY}, centre ({anchor.x:F1}, {anchor.y:F1}), " +
                        $"search {search}, scatter {scatter}, points {count}");
                }

                return false;
            }
            catch (Exception ex)
            {
                ScatterTrapTunerPlugin.Log?.LogError($"[Patch_HuntersTrapPrefab_ComputeScatterPoints] {ex}");
                return true; // fall back to vanilla scatter
            }
        }
    }

    /// <summary>
    /// Prefix on <c>HuntersTrapSubPrefab.OnStart</c>. The sub-trap starts its <c>SubTrapActivation</c>
    /// coroutine with <c>HuntersTrapSubPrefab.SubTrapActivationDelay</c> (1.0s in vanilla). Zeroing the static
    /// right before it is read makes the trap arm instantly. The parent's field is zeroed too so it holds
    /// regardless of which class's static this build routes the read through.
    /// </summary>
    [HarmonyPatch(typeof(HuntersTrapSubPrefab), "OnStart")]
    public static class Patch_HuntersTrapSubPrefab_OnStart_InstantActivation
    {
        public static void Prefix()
        {
            try
            {
                if (!ScatterTrapTunerPlugin.Enabled.Value) return;
                if (!ScatterTrapTunerPlugin.InstantActivation.Value) return;

                HuntersTrapSubPrefab.SubTrapActivationDelay = 0f;
                HuntersTrapPrefab.SubTrapActivationDelay = 0f;

                if (ScatterTrapTunerPlugin.DiagnosticLogging.Value)
                {
                    ScatterTrapTunerPlugin.Log.LogInfo("[InstantActivation] SubTrapActivationDelay set to 0");
                }
            }
            catch (Exception ex)
            {
                ScatterTrapTunerPlugin.Log?.LogError($"[Patch_HuntersTrapSubPrefab_OnStart_InstantActivation] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>SkillShot.Update</c> (the frame update every scatter sub-trap inherits - it does not
    /// override Update). Repeats the Black-Hole suction toward the cluster centre every TickInterval seconds
    /// while the sub-traps are alive, so the pull is the same 2-per-second yank as Pursuing Blizzard's on-tick
    /// suction. Gated to actual <c>HuntersTrapSubPrefab</c> instances by native class pointer (allocation-free,
    /// so it is cheap to run on every skill-shot projectile) and throttled globally to one pull per interval.
    /// </summary>
    [HarmonyPatch(typeof(SkillShot), "Update")]
    public static class Patch_SkillShot_Update_SuctionTick
    {
        public static void Postfix(SkillShot __instance)
        {
            try
            {
                if (!ScatterTrapTunerPlugin.Enabled.Value) return;
                if (!ScatterTrapTunerPlugin.SuctionEnabled.Value) return;
                if (!ScatterTrapTunerPlugin.SuctionOnTick.Value) return;
                if (!ScatterTrapTunerPlugin.SuctionCenterValid) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;
                if (ScatterTrapTunerPlugin.SubTrapClassPtr == IntPtr.Zero) return;

                // Cheap type gate: only the scatter sub-traps re-pull.
                if (IL2CPP.il2cpp_object_get_class(__instance.Pointer) != ScatterTrapTunerPlugin.SubTrapClassPtr) return;

                float interval = ScatterTrapTunerPlugin.SuctionTickInterval.Value;
                if (interval < 0.05f) interval = 0.05f;
                if (Time.time - ScatterTrapTunerPlugin.LastSuctionTime < interval) return;
                ScatterTrapTunerPlugin.LastSuctionTime = Time.time;

                var sub = new HuntersTrapSubPrefab(__instance.Pointer);
                float baseRadius = ScatterTrapTunerPlugin.DetectionRange.Value;
                if (baseRadius <= 0.1f) baseRadius = 15f;
                ScatterTrapTunerPlugin.ApplySuction(sub, ScatterTrapTunerPlugin.SuctionCenter, baseRadius);
            }
            catch (Exception ex)
            {
                ScatterTrapTunerPlugin.Log?.LogError($"[Patch_SkillShot_Update_SuctionTick] {ex}");
            }
        }
    }
}