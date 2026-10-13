using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
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
    ///     land on the enemy instead of nowhere.
    ///   * [Activation] The sub-traps arm instantly - HuntersTrapSubPrefab.SubTrapActivationDelay is zeroed
    ///     right before each sub-trap reads it, removing the vanilla 1-second arming delay.
    ///   * [Drag] On scatter setup the surrounding enemies are dragged onto the cluster centre (the nearest
    ///     enemy) using the game's own BaseSpellLibrary.DashTargetToPosition - the exact Black Hole primitive
    ///     BarrageOfArrowsTuner / PursuingBlizzardTuner use. The centre enemy itself is never dragged.
    ///
    /// Gating: ComputeScatterPoints is a private static method that ONLY the scatter coroutine
    /// (HuntersTrapPrefab.ScatterSetup) calls, so this mod only ever affects the Scatter Trap cast. The
    /// HuntersTrapPrefab instance is captured in HandleArrival (the arrival hook that decides scatter vs
    /// normal), then consumed by ComputeScatterPoints the same frame.
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
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static ScatterTrapTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;

        // [Targeting] cluster the sub-traps on the nearest enemy instead of scattering randomly.
        public static ConfigEntry<bool> ClusterOnNearestTarget;
        public static ConfigEntry<float> ScatterRadius;
        public static ConfigEntry<float> DetectionRange;

        // [Activation] arm the sub-traps immediately (no vanilla 1s delay).
        public static ConfigEntry<bool> InstantActivation;

        // [Drag] pull surrounding enemies onto the cluster centre (Barrage-of-Arrows style).
        public static ConfigEntry<bool> DragEnabled;
        public static ConfigEntry<float> DragRadius;
        public static ConfigEntry<float> DragDashTime;
        public static ConfigEntry<float> DragDistanceMultiplier;

        public static ConfigEntry<bool> DiagnosticLogging;

        // Field offset verified against this build's decompiled metadata
        // (HuntersTrapPrefab._scatterTrap, field offset 0x342).
        internal static int ScatterTrapOffset = 0x342;

        // The HuntersTrapPrefab whose arrival is currently being handled, captured in HandleArrival and
        // consumed by ComputeScatterPoints in the same frame. Stored as a raw pointer so we do not root a
        // managed wrapper past the cast.
        internal static IntPtr CapturedScatterTrapPtr = IntPtr.Zero;
        internal static float CapturedScatterTrapTime = float.NegativeInfinity;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the Scatter Trap behaves exactly like vanilla. (Default: true)");

            ClusterOnNearestTarget = Config.Bind("Targeting", "ClusterOnNearestTarget", true,
                "If true the five scatter sub-traps land on the nearest enemy (with a small scatter) instead of " +
                "fanning out randomly around the landing spot. (Default: true)");

            ScatterRadius = Config.Bind("Targeting", "ScatterRadius", 1.5f,
                "Radius (world units) of the scatter around the nearest enemy. Keep it small so every sub-trap " +
                "still lands on the target; 0 = all five exactly on the enemy. (Default: 1.5)");

            DetectionRange = Config.Bind("Targeting", "DetectionRange", 15.0f,
                "Radius (world units) around the landing spot in which the nearest enemy is searched. The traps " +
                "cluster on the closest enemy found within this range. (Default: 15.0)");

            InstantActivation = Config.Bind("Activation", "InstantActivation", true,
                "If true the scatter sub-traps arm immediately when they spawn (removes the vanilla 1-second " +
                "arming delay). (Default: true)");

            DragEnabled = Config.Bind("Drag", "Enabled", true,
                "If true the scatter leaves a pull/drag that yanks surrounding enemies onto the cluster centre " +
                "using the game's own BaseSpellLibrary.DashTargetToPosition (the exact Black Hole primitive " +
                "BarrageOfArrowsTuner / PursuingBlizzardTuner use). (Default: true)");

            DragRadius = Config.Bind("Drag", "Radius", 10.0f,
                "Radius (world units) around the cluster centre in which enemies are dragged. (Default: 10.0)");

            DragDashTime = Config.Bind("Drag", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank (Black Hole uses 0.3). (Default: 0.2)");

            DragDistanceMultiplier = Config.Bind("Drag", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly onto " +
                "the cluster centre; >1 pulls it past the centre (clamped by walls). (Default: 1.0)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogValues", false,
                "Log target selection and drag decisions to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_HuntersTrapPrefab_HandleArrival));
            PatchOrLog(harmony, typeof(Patch_HuntersTrapPrefab_ComputeScatterPoints));
            PatchOrLog(harmony, typeof(Patch_HuntersTrapSubPrefab_OnStart_InstantActivation));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"ClusterOnNearestTarget: {ClusterOnNearestTarget.Value}");
            Log.LogInfo($"ScatterRadius: {ScatterRadius.Value}");
            Log.LogInfo($"DetectionRange: {DetectionRange.Value}");
            Log.LogInfo($"InstantActivation: {InstantActivation.Value}");
            Log.LogInfo($"Drag: {DragEnabled.Value} (radius={DragRadius.Value}, dashTime={DragDashTime.Value}s, distX={DragDistanceMultiplier.Value})");
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
        /// Drag every live enemy inside Drag.Radius onto <paramref name="center"/> using the game's own
        /// BaseSpellLibrary.DashTargetToPosition - the same primitive BarrageOfArrowsTuner uses. The cluster
        /// centre enemy (<paramref name="anchorPtr"/>) is skipped so it stays put.
        /// </summary>
        internal static void ApplyDrag(HuntersTrapPrefab trap, Vector2 center, IntPtr anchorPtr)
        {
            if (!Enabled.Value || !DragEnabled.Value) return;
            if (trap == null || trap.Pointer == IntPtr.Zero) return;

            try
            {
                float radius = DragRadius.Value;
                if (radius <= 0.1f) return;

                var enemies = trap.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                float dashTime = DragDashTime.Value;
                float distMult = DragDistanceMultiplier.Value;
                int pulled = 0;
                int count = enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    ObjectsCommon enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    // The cluster centre enemy must NOT be dragged off its own position.
                    if (anchorPtr != IntPtr.Zero && enemy.Pointer == anchorPtr) continue;

                    Vector3 ep = enemy.transform.position;
                    Vector2 epos = new Vector2(ep.x, ep.y);
                    float dist = Vector2.Distance(epos, center);
                    if (dist <= 0.05f) continue;

                    // DashTargetToPosition(target, position, time, distance, useMiddleOfSprite,
                    //                       ignoreImmune, ignoreExempt, ignoreBodyRoot)
                    trap.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[ScatterTrapDrag] dragged {pulled} enemies toward cluster centre ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplyDrag] {ex}");
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
    /// land on the target. Only called by the scatter coroutine, so no other spell is affected. Returns the
    /// computed list and skips vanilla, or falls through to vanilla when no enemy is found / disabled.
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
                IntPtr anchorPtr = IntPtr.Zero;
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
                            anchorPtr = enemy.Pointer;
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    // Nothing to target: leave the vanilla scatter alone.
                    ScatterTrapTunerPlugin.CapturedScatterTrapPtr = IntPtr.Zero;
                    if (ScatterTrapTunerPlugin.DiagnosticLogging.Value)
                    {
                        ScatterTrapTunerPlugin.Log.LogInfo("[ComputeScatterPoints] no enemy in range, using vanilla scatter");
                    }
                    return true;
                }

                // Pull surrounding enemies onto the cluster centre (Barrage-of-Arrows style).
                ScatterTrapTunerPlugin.ApplyDrag(trap, anchor, anchorPtr);

                ScatterTrapTunerPlugin.CapturedScatterTrapPtr = IntPtr.Zero;

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
                        $"[ComputeScatterPoints] landing {centerXY}, target ({anchor.x:F1}, {anchor.y:F1}), " +
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
}
