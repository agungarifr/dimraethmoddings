using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;

namespace BarrageOfArrowsTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that reworks the "Barrage of Arrows" upgrade of the
    /// Hail of Arrows skill:
    ///   * Instead of dropping one arrow per enemy, all 6 arrows are clustered on the nearest target
    ///     with a small scatter so the hit area is a tight cluster (not 6 separate enemies).
    ///   * The barrage leaves a poison pool at the impact area.
    ///   * The barrage's enemy detection range is multiplied (Barrage.DetectionRangeMultiplier).
    ///   * [2026-10-07] The barrage drags surrounding enemies toward its centre using the game's own
    ///     BaseSpellLibrary.DashTargetToPosition - the exact Black Hole primitive PursuingBlizzardTuner uses.
    ///
    /// It also tunes the BASE (channeled) Hail of Arrows:
    ///   * [2026-10-07] The base spell's impact area is scaled (BaseHailOfArrows.AoeRadiusMultiplier, default 1.5).
    ///     The base spell spawns HailOfArrowsArrowPrefab arrows whose AreaOfEffect trigger collider is the real
    ///     hit area (the spell prefab's own AreaOfEffectStrike is a no-op), so that collider is scaled. Barrage
    ///     arrows are the same prefab but are excluded.
    ///
    /// The barrage behaviours are gated to the "Barrage of Arrows" upgrade only: the gate is armed when the
    /// HailOfArrowsPrefab reads a "Barrage of Arrows" upgrade level greater than zero. The base AoE scaling is the
    /// mirror image - it only applies when that gate is NOT armed.
    ///
    /// Co-op: the pool reuses the game's own "Poison Pool" upgrade path
    /// (HailOfArrowsArrowPrefab.OnStart -> BaseSpellLibrary.SpawnElementalPool), so networking behaves
    /// exactly like the vanilla upgrade. Run the SAME config on host and client so both peers agree.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BarrageOfArrowsTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.barrageofarrowstuner";
        public const string NAME = "Barrage of Arrows Tuner";
        public const string VERSION = "1.1.0";

        internal static new ManualLogSource Log;
        internal static BarrageOfArrowsTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ClusterOnNearestTarget;
        public static ConfigEntry<float> ScatterRadius;
        public static ConfigEntry<float> DetectionRangeMultiplier;
        public static ConfigEntry<bool> PoisonPoolEnabled;
        public static ConfigEntry<int> MaxPoolsPerCast;
        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

        // [2026-10-07] Base Hail of Arrows impact-AoE scaling (base/channeled spell only).
        public static ConfigEntry<float> BaseAoeRadiusMultiplier;

        // [2026-10-07] Barrage-of-Arrows enemy drag (mirrors PursuingBlizzardTuner's Black Hole suction).
        public static ConfigEntry<bool> DragEnabled;
        public static ConfigEntry<bool> DragOnActivate;
        public static ConfigEntry<bool> DragOnTick;
        public static ConfigEntry<float> DragTickInterval;
        public static ConfigEntry<float> DragRadius;
        public static ConfigEntry<float> DragDashTime;
        public static ConfigEntry<float> DragDistanceMultiplier;

        // [2026-10-07] Field offsets verified against this build's decompiled metadata.
        internal static int BarrageOfArrowsOffset = 0x2F4; // HailOfArrowsPrefab._barrageOfArrows (bool)
        internal static int AoeColliderOffset = 0x288;      // AreaOfEffect._aoeCollider (Collider2D)
        internal static float LastDragTime = float.NegativeInfinity;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false no Barrage of Arrows behaviour is modified. (Default: true)");

            ClusterOnNearestTarget = Config.Bind("Barrage", "ClusterOnNearestTarget", true,
                "If true the barrage drops all arrows on the nearest enemy with a small scatter instead of one arrow per enemy. (Default: true)");

            ScatterRadius = Config.Bind("Barrage", "ScatterRadius", 2.5f,
                "Radius (world units) of the scatter around the nearest target. Keep it below the arrow impact radius (~4) so every arrow still hits. (Default: 2.5)");

            // [2026-10-01 15:15] New: barrage detection range multiplier (task requirement). Vanilla _barrageRadius = 10.
            DetectionRangeMultiplier = Config.Bind("Barrage", "DetectionRangeMultiplier", 3.0f,
                "Multiplier applied to the barrage's enemy detection range (vanilla radius 10, so 3.0 = 30 units). Values <= 0 disable the multiplier. (Default: 3.0)");

            PoisonPoolEnabled = Config.Bind("PoisonPool", "Enabled", true,
                "If true the barrage leaves a poison pool at the impact area. (Default: true)");

            MaxPoolsPerCast = Config.Bind("PoisonPool", "MaxPoolsPerCast", 1,
                "How many poison pools a single barrage may leave. 1 = a single pool. Set 0 to disable pooling. (Default: 1)");

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a tuned-stats line to the 'Barrage of Arrows' upgrade text (spell tooltip modifier row and codex entry) so the in-game description matches what this mod changes. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogBarrageValues", false,
                "Log barrage point selection and poison pool decisions to the BepInEx console. (Default: false)");

            // [2026-10-07] Base Hail of Arrows impact-AoE scaling. The base (channeled) spell drops
            // HailOfArrowsArrowPrefab arrows; each arrow's AreaOfEffect trigger collider is the actual hit area
            // (the spell prefab's own AreaOfEffectStrike is a no-op). Scaling that collider widens the base AoE.
            BaseAoeRadiusMultiplier = Config.Bind("BaseHailOfArrows", "AoeRadiusMultiplier", 1.5f,
                "Scale the BASE (channeled) Hail of Arrows impact area. 1.5 = +50%. Applied by scaling each base " +
                "arrow's AreaOfEffect trigger collider geometry. The Barrage of Arrows upgrade is deliberately " +
                "excluded (its arrows are untouched). 1.0 disables. (Default: 1.5)");

            // [2026-10-07] Barrage enemy drag. Mirrors PursuingBlizzardTuner's Black Hole suction: uses the game's
            // own BaseSpellLibrary.DashTargetToPosition (the primitive Black Hole uses) so the pull disables the
            // NavMeshAgent during the lerp, raycasts walls, and honours Immune/DisplaceExempt/authority.
            DragEnabled = Config.Bind("BlackHoleDrag", "Enabled", true,
                "If true the Barrage of Arrows upgrade drags surrounding enemies toward the barrage centre using the " +
                "game's own BaseSpellLibrary.DashTargetToPosition (the exact Black Hole primitive). Barrage upgrade " +
                "only; the base Hail of Arrows is not dragged. (Default: true)");

            DragOnActivate = Config.Bind("BlackHoleDrag", "DragOnActivate", true,
                "Pull all enemies in range into the barrage centre immediately when the barrage starts. (Default: true)");

            DragOnTick = Config.Bind("BlackHoleDrag", "DragOnTick", true,
                "Repeat the pull every TickInterval seconds while the barrage is alive. (Default: true)");

            DragTickInterval = Config.Bind("BlackHoleDrag", "TickInterval", 0.15f,
                "Seconds between repeated drags (matches the barrage's 0.15s arrow cadence). Lower = stronger, " +
                "more continuous suction. (Default: 0.15)");

            DragRadius = Config.Bind("BlackHoleDrag", "Radius", 10.0f,
                "Radius (world units) around the barrage centre in which enemies are dragged. Vanilla barrage " +
                "detection radius is 10. (Default: 10.0)");

            DragDashTime = Config.Bind("BlackHoleDrag", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank (Black Hole uses 0.3). (Default: 0.2)");

            DragDistanceMultiplier = Config.Bind("BlackHoleDrag", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly onto " +
                "the barrage centre; >1 pulls it past the centre (clamped by walls). (Default: 1.0)");

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_HailOfArrowsPrefab_ComputeBarragePoints));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetSpellUpgradeLevel));
            // [2026-10-01 15:15] New: 3x barrage enemy detection range (task requirement).
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetAllEnemiesInRange));
            // [2026-10-01 15:15] New: make the in-game "Barrage of Arrows" description reflect the modded behaviour.
            PatchOrLog(harmony, typeof(Patch_LocalizationManager_Get_BarrageText));
            // [2026-10-07] Base (channeled) Hail of Arrows impact-AoE scaling.
            PatchOrLog(harmony, typeof(Patch_AreaOfEffect_Start_BaseArrowAoe));
            // [2026-10-07] Barrage-of-Arrows enemy drag (activate + per tick), like PursuingBlizzardTuner.
            PatchOrLog(harmony, typeof(Patch_HailOfArrowsPrefab_OnStart_Drag));
            PatchOrLog(harmony, typeof(Patch_HailOfArrowsPrefab_Update_Drag));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"ClusterOnNearestTarget: {ClusterOnNearestTarget.Value}");
            Log.LogInfo($"ScatterRadius: {ScatterRadius.Value}");
            Log.LogInfo($"DetectionRangeMultiplier: {DetectionRangeMultiplier.Value}");
            Log.LogInfo($"PoisonPoolEnabled: {PoisonPoolEnabled.Value}");
            Log.LogInfo($"MaxPoolsPerCast: {MaxPoolsPerCast.Value}");
            Log.LogInfo($"ShowTunedStatsInTooltip: {ShowTunedStatsInTooltip.Value}");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo($"BaseHailOfArrows.AoeRadiusMultiplier: {BaseAoeRadiusMultiplier.Value}");
            Log.LogInfo($"BlackHoleDrag: {DragEnabled.Value} (activate={DragOnActivate.Value}, tick={DragOnTick.Value}, interval={DragTickInterval.Value}s, radius={DragRadius.Value}, distX={DragDistanceMultiplier.Value}, dashTime={DragDashTime.Value}s)");
            Log.LogInfo("=================================================");
        }

        private void PatchOrLog(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.PatchAll(patchType);
            }
            catch (Exception ex)
            {
                Log.LogError($"[BarrageOfArrowsTuner] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        /// <summary>
        /// Robust IL2CPP instance-type test for patch call sites.
        /// </summary>
        /// <remarks>
        /// [2026-10-01 15:15] Replaces the bare <c>__instance is HailOfArrowsPrefab</c> /
        /// <c>__instance is HailOfArrowsArrowPrefab</c> checks. Those can evaluate to false when Harmony
        /// hands the patch a wrapper typed as the declared patch type (BaseSpellLibrary) instead of the
        /// most-derived interop wrapper, even though the native object IS the derived class. That failure
        /// mode silently disabled both gate arming and the poison pool force (clustering kept working
        /// because it patches a static method with no __instance involved). The `is` check is kept first
        /// so subclasses of T still match when the wrapper is most-derived; the fallback compares the
        /// runtime IL2CPP class name (GetIl2CppType() reads the native object's class) with typeof(T).
        /// </remarks>
        internal static bool IsSpellInstance<T>(BaseSpellLibrary instance) where T : BaseSpellLibrary
        {
            if (instance == null) return false;
            if (instance is T) return true;
            return instance.GetIl2CppType().Name == typeof(T).Name;
        }

        /// <summary>
        /// Builds the extra line appended to the "Barrage of Arrows" upgrade text so the in-game description
        /// reflects the modded behaviour (cluster on nearest, poison pool, extended detection range).
        /// Returns null when the feature is disabled or nothing is tuned.
        /// </summary>
        internal static string BuildTooltipSuffix()
        {
            if (!ShowTunedStatsInTooltip.Value) return null;

            var parts = new System.Collections.Generic.List<string>();
            if (ClusterOnNearestTarget.Value) parts.Add("all arrows cluster on the nearest enemy");
            if (PoisonPoolEnabled.Value && MaxPoolsPerCast.Value > 0) parts.Add("leaves a poison pool");
            if (DetectionRangeMultiplier.Value > 0f && DetectionRangeMultiplier.Value != 1f)
                parts.Add($"detection range x{FormatMultiplier(DetectionRangeMultiplier.Value)}");
            // [2026-10-07] The barrage now drags enemies inward (see BlackHoleDrag).
            if (DragEnabled.Value) parts.Add("drags nearby enemies inward");

            if (parts.Count == 0) return null;
            return "\n\n[Barrage Tuner] " + string.Join(", ", parts) + ".";
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// [2026-10-07] True when the HailOfArrowsPrefab instance resolved the "Barrage of Arrows" upgrade
        /// (_barrageOfArrows, offset 0x2F4), so the drag only touches the barrage cast, never the base spell.
        /// </summary>
        internal static unsafe bool IsBarrage(HailOfArrowsPrefab hp)
        {
            if (hp == null || hp.Pointer == IntPtr.Zero) return false;
            return *(byte*)((byte*)hp.Pointer + BarrageOfArrowsOffset) != 0;
        }

        /// <summary>
        /// [2026-10-07] Widen the BASE Hail of Arrows impact area. The base (channeled) spell spawns
        /// HailOfArrowsArrowPrefab arrows whose AreaOfEffect trigger collider (AreaOfEffect._aoeCollider, resolved
        /// in AreaOfEffect.Start) is the real hit area - the spell prefab's own AreaOfEffectStrike is a no-op - so
        /// we scale that collider's geometry. Barrage arrows are the same prefab, so they are excluded via the
        /// BarrageGate (armed only during a barrage cast).
        /// </summary>
        internal static unsafe void ApplyBaseArrowAoe(AreaOfEffect aoe)
        {
            try
            {
                if (aoe == null || aoe.Pointer == IntPtr.Zero) return;
                // Only the Hail of Arrows arrow prefab carries this spell's impact collider.
                if (!IsSpellInstance<HailOfArrowsArrowPrefab>(aoe)) return;
                // The gate is armed only during a barrage cast, so an armed gate means "barrage arrow": leave it alone.
                if (BarrageGate.IsArmed()) return;

                float mult = BaseAoeRadiusMultiplier.Value;
                if (mult <= 0f || Math.Abs(mult - 1f) < 0.0001f) return;

                IntPtr colPtr = *(IntPtr*)((byte*)aoe.Pointer + AoeColliderOffset);
                if (colPtr == IntPtr.Zero) return;

                var col = new Collider2D(colPtr);
                ScaleColliderGeometry(col, mult);

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[BaseAoE] HailOfArrowsArrowPrefab impact collider ({col.GetType().Name}) scaled x{FormatMultiplier(mult)}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplyBaseArrowAoe] {ex}");
            }
        }

        /// <summary>
        /// [2026-10-07] Multiply a Collider2D's own shape by <paramref name="mult"/>. Mirrors
        /// TwisterTunerPlugin.ScaleColliderGeometry so the trigger area grows in world space regardless of whether
        /// this build's Collider2D follows its Transform.
        /// </summary>
        internal static void ScaleColliderGeometry(Collider2D col, float mult)
        {
            var circle = col.TryCast<CircleCollider2D>();
            if (circle != null) { circle.radius *= mult; circle.offset *= mult; return; }

            var box = col.TryCast<BoxCollider2D>();
            if (box != null) { box.size *= mult; box.offset *= mult; box.edgeRadius *= mult; return; }

            var capsule = col.TryCast<CapsuleCollider2D>();
            if (capsule != null) { capsule.size *= mult; capsule.offset *= mult; return; }

            var poly = col.TryCast<PolygonCollider2D>();
            if (poly != null)
            {
                var pts = poly.points;
                for (int i = 0; i < pts.Length; i++) pts[i] *= mult;
                poly.points = pts;
                return;
            }

            var edge = col.TryCast<EdgeCollider2D>();
            if (edge != null)
            {
                var pts = edge.points;
                for (int i = 0; i < pts.Length; i++) pts[i] *= mult;
                edge.points = pts;
                return;
            }

            // Unknown 2D collider type: fall back to scaling its own transform.
            var t = col.transform;
            var s = t.localScale;
            t.localScale = new Vector3(s.x * mult, s.y * mult, s.z);
        }

        /// <summary>
        /// [2026-10-07] Drag every live enemy inside the barrage's radius toward its centre using the game's own
        /// BaseSpellLibrary.DashTargetToPosition - the exact primitive BlackholePrefab.AreaOfEffectStrike (and
        /// PursuingBlizzardTuner) use. The pull reuses the game's knockback coroutine, which disables the
        /// NavMeshAgent during the lerp and restores it afterward, raycasts walls, and honours
        /// ObjectsCommon.Immune / MonsterBehaviourLibrary.DisplaceExempt / authority.
        /// </summary>
        internal static void ApplySuction(HailOfArrowsPrefab hp)
        {
            if (!Enabled.Value || !DragEnabled.Value) return;
            if (hp == null || hp.Pointer == IntPtr.Zero) return;

            try
            {
                var t = hp.transform;
                if (t == null) return;
                Vector3 p = t.position;
                Vector2 center = new Vector2(p.x, p.y);

                float radius = DragRadius.Value;
                if (radius <= 0.1f) return;

                var enemies = hp.GetAllEnemiesInRangeOfPosition(center, radius);
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

                    Vector3 ep = enemy.transform.position;
                    float dist = Vector2.Distance(new Vector2(ep.x, ep.y), center);
                    if (dist <= 0.05f) continue;

                    // DashTargetToPosition(target, center, time, distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot)
                    hp.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[BarrageDrag] dragged {pulled} enemies toward barrage ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }
    }

    /// <summary>
    /// Tracks whether a "Barrage of Arrows" cast is currently resolving. Armed from
    /// <see cref="Patch_BaseSpellLibrary_GetSpellUpgradeLevel"/> when a HailOfArrowsPrefab reads the
    /// "Barrage of Arrows" upgrade and, since [2026-10-01 15:15], also from
    /// <see cref="Patch_HailOfArrowsPrefab_ComputeBarragePoints"/> right before the arrows spawn,
    /// then consumed once per poison pool by the arrow prefabs.
    /// A short time window keeps a stale arm from leaking into an unrelated (base) Hail of Arrows cast.
    /// </summary>
    internal static class BarrageGate
    {
        private const float ActiveWindowSeconds = 3f;

        private static float _armedAt = float.NegativeInfinity;
        private static int _poolsRemaining;

        public static void Arm(int maxPools)
        {
            _armedAt = Time.time;
            _poolsRemaining = maxPools;
        }

        public static bool IsArmed()
        {
            return Time.time - _armedAt <= ActiveWindowSeconds;
        }

        public static bool TryConsumePool()
        {
            if (_poolsRemaining <= 0) return false;
            _poolsRemaining--;
            return true;
        }
    }

    /// <summary>
    /// Prefix on the private static <c>HailOfArrowsPrefab.ComputeBarragePoints</c>. Vanilla picks one
    /// landing point per enemy (plus random points when there are fewer than 6 enemies), which makes the
    /// barrage hit 6 separate enemies. We instead return <c>count</c> points scattered around the nearest
    /// enemy so every arrow lands on the same target. This method is only ever called from the barrage
    /// routine, so the change is inherently limited to the "Barrage of Arrows" upgrade.
    /// </summary>
    [HarmonyPatch(typeof(HailOfArrowsPrefab), "ComputeBarragePoints")]
    public static class Patch_HailOfArrowsPrefab_ComputeBarragePoints
    {
        public static bool Prefix(
            Vector2 center,
            float radius,
            Il2CppGenerics.List<ObjectsCommon> enemies,
            int count,
            ref Il2CppGenerics.List<Vector2> __result)
        {
            try
            {
                // [2026-10-01 15:15] New: arm the poison-pool gate here as well. ComputeBarragePoints is
                // barrage-only and runs immediately before the arrows spawn, so this arm is immune to any
                // timing/typematching problem in the GetSpellUpgradeLevel-based arming below (that path is
                // kept as a second arm site). Runs before the clustering early-returns on purpose.
                BarrageGate.Arm(BarrageOfArrowsTunerPlugin.MaxPoolsPerCast.Value);
                if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                {
                    BarrageOfArrowsTunerPlugin.Log.LogInfo(
                        $"[ComputeBarragePoints] barrage gate armed (max pools {BarrageOfArrowsTunerPlugin.MaxPoolsPerCast.Value})");
                }

                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return true;
                if (!BarrageOfArrowsTunerPlugin.ClusterOnNearestTarget.Value) return true;
                if (count <= 0) return true;

                Vector2 anchor = center;
                bool foundTarget = false;

                if (enemies != null)
                {
                    float best = float.MaxValue;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var enemy = enemies[i];
                        if (enemy == null) continue;

                        Vector2 p = enemy.transform.position;
                        float d = (p - center).sqrMagnitude;
                        if (d < best)
                        {
                            best = d;
                            anchor = p;
                            foundTarget = true;
                        }
                    }
                }

                float scatter = BarrageOfArrowsTunerPlugin.ScatterRadius.Value;
                if (scatter < 0f) scatter = 0f;

                var points = new Il2CppGenerics.List<Vector2>();
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * scatter;
                    points.Add(anchor + offset);
                }

                __result = points;

                if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                {
                    BarrageOfArrowsTunerPlugin.Log.LogInfo(
                        $"[ComputeBarragePoints] center {center}, target {(foundTarget ? anchor.ToString() : "none, using center")}, " +
                        $"scatter {scatter}, points {count}");
                }

                return false;
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_HailOfArrowsPrefab_ComputeBarragePoints] {ex}");
                return true; // fall back to vanilla point selection
            }
        }
    }

    /// <summary>
    /// Postfix on the protected <c>BaseSpellLibrary.GetSpellUpgradeLevel</c>. Serves two purposes:
    ///   1. Arm the barrage gate when a HailOfArrowsPrefab asks for the "Barrage of Arrows" upgrade.
    ///   2. While the gate is armed, make the barrage arrows report a "Poison Pool" upgrade so the
    ///      existing arrow code spawns a poison pool through the vanilla SpawnElementalPool path.
    /// Only HailOfArrowsPrefab / HailOfArrowsArrowPrefab instances are touched, so base Hail of Arrows
    /// and every other spell are unaffected.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetSpellUpgradeLevel")]
    public static class Patch_BaseSpellLibrary_GetSpellUpgradeLevel
    {
        public static void Postfix(BaseSpellLibrary __instance, string spellUpgradeName, ref int __result)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                if (__instance == null || spellUpgradeName == null) return;

                if (spellUpgradeName == "Barrage of Arrows")
                {
                    // [2026-10-01 15:15] Was: if (__result > 0 && __instance is HailOfArrowsPrefab).
                    // The bare `is` check can fail on base-typed interop wrappers (see IsSpellInstance),
                    // which left the gate permanently unarmed and is the suspected root cause of the
                    // missing poison pool. IsSpellInstance keeps the `is` check and adds a runtime
                    // IL2CPP class-name comparison.
                    // if (__result > 0 && __instance is HailOfArrowsPrefab)
                    if (__result > 0 && BarrageOfArrowsTunerPlugin.IsSpellInstance<HailOfArrowsPrefab>(__instance))
                    {
                        BarrageGate.Arm(BarrageOfArrowsTunerPlugin.MaxPoolsPerCast.Value);

                        if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                        {
                            BarrageOfArrowsTunerPlugin.Log.LogInfo(
                                $"[GetSpellUpgradeLevel] Barrage of Arrows read on {__instance.GetIl2CppType().Name} (result {__result}), gate armed");
                        }
                    }
                    return;
                }

                if (spellUpgradeName != "Poison Pool") return;

                // [2026-10-01 15:15] New diagnostics: log every "Poison Pool" query (and which runtime type
                // asked) so one test run with Diagnostics.LogBarrageValues=true shows whether the arrow ever
                // reaches this interception.
                bool isArrow = BarrageOfArrowsTunerPlugin.IsSpellInstance<HailOfArrowsArrowPrefab>(__instance);
                if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                {
                    BarrageOfArrowsTunerPlugin.Log.LogInfo(
                        $"[GetSpellUpgradeLevel] Poison Pool queried on {__instance.GetIl2CppType().Name} " +
                        $"(arrow match: {isArrow}, gate armed: {BarrageGate.IsArmed()}, vanilla result {__result})");
                }

                if (!BarrageOfArrowsTunerPlugin.PoisonPoolEnabled.Value) return;
                if (BarrageOfArrowsTunerPlugin.MaxPoolsPerCast.Value <= 0) return;
                // [2026-10-01 15:15] Was: if (!(__instance is HailOfArrowsArrowPrefab)) return;
                // Same base-typed-wrapper problem as above; see IsSpellInstance remarks. The vanilla
                // OnStart computes _poisonPool from this call's result and overwrites it immediately, so
                // forcing __result here is still the only way to make the vanilla pool path run.
                // if (!(__instance is HailOfArrowsArrowPrefab)) return;
                if (!isArrow) return;
                if (!BarrageGate.IsArmed()) return;

                if (BarrageGate.TryConsumePool())
                {
                    __result = Math.Max(__result, 1);

                    if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                    {
                        BarrageOfArrowsTunerPlugin.Log.LogInfo(
                            $"[GetSpellUpgradeLevel] Poison Pool forced on {__instance.GetIl2CppType().Name} (result {__result}), one pool consumed");
                    }
                }
                else
                {
                    // Cap extra pools so a single barrage leaves exactly MaxPoolsPerCast.
                    __result = 0;
                }
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_GetSpellUpgradeLevel] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on the protected <c>BaseSpellLibrary.GetAllEnemiesInRange</c>, the enemy lookup used by the
    /// barrage routine (its only caller on HailOfArrowsPrefab). Multiplies <c>range</c> by
    /// Barrage.DetectionRangeMultiplier so the barrage sees enemies farther away. Only HailOfArrowsPrefab
    /// instances are touched and that call site is barrage-only, so base Hail of Arrows is unaffected.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetAllEnemiesInRange")]
    public static class Patch_BaseSpellLibrary_GetAllEnemiesInRange
    {
        public static void Prefix(BaseSpellLibrary __instance, ref float range)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                if (range <= 0f) return;
                if (!BarrageOfArrowsTunerPlugin.IsSpellInstance<HailOfArrowsPrefab>(__instance)) return;

                float multiplier = BarrageOfArrowsTunerPlugin.DetectionRangeMultiplier.Value;
                if (multiplier <= 0f) return; // <= 0 disables the multiplier

                float before = range;
                range *= multiplier;

                if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                {
                    BarrageOfArrowsTunerPlugin.Log.LogInfo(
                        $"[GetAllEnemiesInRange] detection range {before} -> {range} (x{multiplier})");
                }
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_GetAllEnemiesInRange] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>LocalizationManager.Get(category, key, defaultText)</c>. The "Barrage of Arrows"
    /// upgrade text is authored localization data that never changes when the runtime behaviour is tuned,
    /// so the tooltip/codex would still advertise the vanilla (one-arrow-per-enemy, no pool) behaviour.
    /// We append a short tuned-stats line for exactly two keys:
    ///   * <c>SPELL_HAILOFARROWS_MOD_0</c> - the upgrade's modifier row in the Hail of Arrows
    ///     spell tooltip (catalogue / spellbook).
    ///   * <c>CODEX_SPELLHAILOFARROWS_BARRAGEOFARROWS_DESC</c> - the upgrade's codex entry.
    /// Both keys are fixed ASCII constants, so this is language-independent; the base Hail of Arrows and
    /// every other spell/upgrade are untouched.
    /// </summary>
    [HarmonyPatch(typeof(LocalizationManager), "Get")]
    public static class Patch_LocalizationManager_Get_BarrageText
    {
        private const string BarrageModKey = "SPELL_HAILOFARROWS_MOD_0";
        private const string BarrageCodexDescKey = "CODEX_SPELLHAILOFARROWS_BARRAGEOFARROWS_DESC";

        public static void Postfix(string key, ref string __result)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                if (key == null) return;
                if (key != BarrageModKey && key != BarrageCodexDescKey) return;

                string suffix = BarrageOfArrowsTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;

                if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
                {
                    BarrageOfArrowsTunerPlugin.Log.LogInfo($"[Tooltip] {key} -> {__result}");
                }
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_LocalizationManager_Get_BarrageText] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-07] Postfix on <c>AreaOfEffect.Start</c>. By the time this runs the game has resolved
    /// <c>AreaOfEffect._aoeCollider</c> (assigned in Start), so this is the reliable point to widen the BASE Hail
    /// of Arrows arrow impact area. Only HailOfArrowsArrowPrefab instances are affected, and only when no barrage
    /// cast is in flight (BarrageGate), so the Barrage of Arrows upgrade keeps its vanilla impact area.
    /// </summary>
    [HarmonyPatch(typeof(AreaOfEffect), "Start")]
    public static class Patch_AreaOfEffect_Start_BaseArrowAoe
    {
        public static void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                BarrageOfArrowsTunerPlugin.ApplyBaseArrowAoe(__instance);
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_AreaOfEffect_Start_BaseArrowAoe] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-07] Postfix on <c>HailOfArrowsPrefab.OnStart</c>. When the spell resolved the "Barrage of Arrows"
    /// upgrade (<c>_barrageOfArrows</c>) it drags surrounding enemies toward the barrage centre once on activation,
    /// mirroring PursuingBlizzardTuner's Black Hole suction. Base Hail of Arrows casts are not dragged.
    /// </summary>
    [HarmonyPatch(typeof(HailOfArrowsPrefab), "OnStart")]
    public static class Patch_HailOfArrowsPrefab_OnStart_Drag
    {
        public static void Postfix(HailOfArrowsPrefab __instance)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                if (!BarrageOfArrowsTunerPlugin.DragEnabled.Value) return;
                if (!BarrageOfArrowsTunerPlugin.DragOnActivate.Value) return;
                if (!BarrageOfArrowsTunerPlugin.IsBarrage(__instance)) return;
                BarrageOfArrowsTunerPlugin.ApplySuction(__instance);
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_HailOfArrowsPrefab_OnStart_Drag] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-07] Postfix on <c>HailOfArrowsPrefab.Update</c>. Repeats the barrage drag every TickInterval
    /// seconds while a barrage is alive (the barrage prefab lingers ~5s), so enemies are repeatedly yanked onto
    /// the cluster centre - the same "on activate + on tick" shape as PursuingBlizzardTuner.
    /// </summary>
    [HarmonyPatch(typeof(HailOfArrowsPrefab), "Update")]
    public static class Patch_HailOfArrowsPrefab_Update_Drag
    {
        public static void Postfix(HailOfArrowsPrefab __instance)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                if (!BarrageOfArrowsTunerPlugin.DragEnabled.Value) return;
                if (!BarrageOfArrowsTunerPlugin.DragOnTick.Value) return;
                if (!BarrageOfArrowsTunerPlugin.IsBarrage(__instance)) return;

                float interval = BarrageOfArrowsTunerPlugin.DragTickInterval.Value;
                if (interval < 0.01f) interval = 0.01f;
                if (Time.time - BarrageOfArrowsTunerPlugin.LastDragTime < interval) return;
                BarrageOfArrowsTunerPlugin.LastDragTime = Time.time;

                BarrageOfArrowsTunerPlugin.ApplySuction(__instance);
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_HailOfArrowsPrefab_Update_Drag] {ex}");
            }
        }
    }
}
