using System;
using System.Globalization;
using System.Reflection;
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
    ///   * [2026-10-07] The barrage's arrow-drop area and its drop-indicator visual are enlarged
    ///     (Barrage.DropAreaMultiplier, default 2.0).
    ///   * [2026-10-07] Arrows drop in sequence on a configurable tick (Barrage.TickInterval, default 0.5s
    ///     instead of the vanilla 0.15s) and, if the current target dies mid-sequence, the remaining arrows
    ///     re-target the nearest still-living enemy and resume the drop (Barrage.RetargetOnDeath).
    ///
    /// Both behaviours are gated to the "Barrage of Arrows" upgrade only: the gate is armed when the
    /// HailOfArrowsPrefab reads a "Barrage of Arrows" upgrade level greater than zero. The base
    /// (channeled) Hail of Arrows is left untouched.
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
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static BarrageOfArrowsTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ClusterOnNearestTarget;
        public static ConfigEntry<float> ScatterRadius;
        // [2026-10-07] New: scale the barrage's arrow-drop area (the circle the arrows land in) and its
        // per-arrow drop indicator visual. The drop area is the `radius` fed to ComputeBarragePoints
        // (vanilla _barrageRadius = 10); the visual is the target-indicator child (offset 0x320).
        public static ConfigEntry<float> DropAreaMultiplier;
        // [2026-10-07] New (sequential barrage, "Pursuing Blizzard" style): instead of dropping all
        // arrows on the vanilla 0.15s cadence, spread them over TickInterval seconds per arrow and, if
        // the current target dies, re-point the remaining arrows at the nearest still-living enemy.
        public static ConfigEntry<float> TickInterval;
        public static ConfigEntry<bool> RetargetOnDeath;
        public static ConfigEntry<float> DetectionRangeMultiplier;
        public static ConfigEntry<bool> PoisonPoolEnabled;
        public static ConfigEntry<int> MaxPoolsPerCast;
        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

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

            // [2026-10-07] New: scale the barrage's arrow-drop area (the circle the arrows land in) and the
            // per-arrow drop-indicator visual. The drop area is the cluster scatter around the target; the
            // visual is the target-indicator template (field 0x320). 2.0 = double both. Values <= 0 or == 1
            // disable scaling.
            DropAreaMultiplier = Config.Bind("Barrage", "DropAreaMultiplier", 2.0f,
                "Multiplier for the barrage's arrow-drop area (cluster scatter) and its drop-indicator visual. " +
                "2.0 = double both. Values <= 0 or 1.0 disable scaling. (Default: 2.0)");

            // [2026-10-07] New: sequential barrage ("Pursuing Blizzard" style). Each arrow drops TickInterval
            // seconds apart instead of the vanilla 0.15s cadence, and if the current target dies the remaining
            // arrows are re-pointed at the nearest still-living enemy.
            TickInterval = Config.Bind("Barrage", "TickInterval", 0.5f,
                "Seconds between each arrow in the barrage sequence (vanilla cadence is 0.15). Set to 0 to keep " +
                "the vanilla cadence. (Default: 0.5)");

            RetargetOnDeath = Config.Bind("Barrage", "RetargetOnDeath", true,
                "If true, when the barrage's current target dies mid-sequence the remaining arrows re-target the " +
                "nearest still-living enemy and resume dropping. (Default: true)");

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

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_HailOfArrowsPrefab_ComputeBarragePoints));
            // [2026-10-07] New: sequential drops + re-target-on-death, via the barrage coroutine's MoveNext.
            PatchOrLog(harmony, typeof(Patch_HailOfArrowsPrefab_BarrageRoutine_MoveNext));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetSpellUpgradeLevel));
            // [2026-10-01 15:15] New: 3x barrage enemy detection range (task requirement).
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetAllEnemiesInRange));
            // [2026-10-01 15:15] New: make the in-game "Barrage of Arrows" description reflect the modded behaviour.
            PatchOrLog(harmony, typeof(Patch_LocalizationManager_Get_BarrageText));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"ClusterOnNearestTarget: {ClusterOnNearestTarget.Value}");
            Log.LogInfo($"ScatterRadius: {ScatterRadius.Value}");
            Log.LogInfo($"DropAreaMultiplier: {DropAreaMultiplier.Value}");
            Log.LogInfo($"TickInterval: {TickInterval.Value}s (vanilla 0.15)");
            Log.LogInfo($"RetargetOnDeath: {RetargetOnDeath.Value}");
            Log.LogInfo($"DetectionRangeMultiplier: {DetectionRangeMultiplier.Value}");
            Log.LogInfo($"PoisonPoolEnabled: {PoisonPoolEnabled.Value}");
            Log.LogInfo($"MaxPoolsPerCast: {MaxPoolsPerCast.Value}");
            Log.LogInfo($"ShowTunedStatsInTooltip: {ShowTunedStatsInTooltip.Value}");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
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
            if (DropAreaMultiplier.Value > 0f && DropAreaMultiplier.Value != 1f)
                parts.Add($"drop area x{FormatMultiplier(DropAreaMultiplier.Value)}");
            if (TickInterval.Value > 0f && TickInterval.Value != 0.15f)
                parts.Add($"{FormatMultiplier(TickInterval.Value)}s arrow cadence");
            if (RetargetOnDeath.Value) parts.Add("re-targets the nearest living enemy if the target dies");
            if (PoisonPoolEnabled.Value && MaxPoolsPerCast.Value > 0) parts.Add("leaves a poison pool");
            if (DetectionRangeMultiplier.Value > 0f && DetectionRangeMultiplier.Value != 1f)
                parts.Add($"detection range x{FormatMultiplier(DetectionRangeMultiplier.Value)}");

            if (parts.Count == 0) return null;
            return "\n\n[Barrage Tuner] " + string.Join(", ", parts) + ".";
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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
            ref float radius,
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

                // [2026-10-07] New: widen the barrage's arrow-drop circle (the `radius` fed to this method,
                // vanilla _barrageRadius = 10) so the whole landing area, not just the visual, is enlarged.
                float areaMult = BarrageOfArrowsTunerPlugin.DropAreaMultiplier.Value;
                bool scaleArea = areaMult > 0f && areaMult != 1f;
                if (scaleArea && radius > 0f) radius *= areaMult;

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

                // [2026-10-07] New: the clustered landing area is the scatter around the anchor, so scale the
                // scatter by DropAreaMultiplier too. This is what actually widens where the arrows land.
                float scatter = BarrageOfArrowsTunerPlugin.ScatterRadius.Value;
                if (scatter < 0f) scatter = 0f;
                if (scaleArea) scatter *= areaMult;

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
    /// [2026-10-07] New: sequential barrage with re-target-on-death ("Pursuing Blizzard" style).
    /// <para>
    /// Prefix (runs before each coroutine step): keeps the per-arrow drop-indicator template scaled by
    /// <c>Barrage.DropAreaMultiplier</c> (the template's localScale is copied onto every spawned indicator by
    /// the vanilla routine), and re-points the NEXT arrow at the nearest still-living enemy so the barrage
    /// "changes target" when its current victim dies and resumes dropping.
    /// </para>
    /// <para>
    /// Postfix: when the coroutine yields the between-arrow cadence (state 1), replaces the yielded
    /// <c>&lt;&gt;2__current</c> with <c>WaitForSeconds(Barrage.TickInterval)</c> so arrows drop on the
    /// configured 0.5s tick instead of the vanilla 0.15s. The final post-loop wait (state 2) is untouched.
    /// </para>
    /// The coroutine type is used only by the "Barrage of Arrows" cast, so the base (channeled) Hail of
    /// Arrows is unaffected.
    /// </summary>
    [HarmonyPatch(typeof(HailOfArrowsPrefab._BarrageRoutine_d__24), "MoveNext")]
    public static class Patch_HailOfArrowsPrefab_BarrageRoutine_MoveNext
    {
        private struct TemplateScale
        {
            public IntPtr TemplatePtr;
            public Vector3 BaseScale;
        }

        // Keyed by the spell instance pointer. Removed when the coroutine finishes so this cannot grow
        // without bound across a session. Storing the template pointer makes the base-scale capture robust
        // to native pointer reuse.
        private static readonly System.Collections.Generic.Dictionary<IntPtr, TemplateScale> _templateScales =
            new System.Collections.Generic.Dictionary<IntPtr, TemplateScale>();

        private static MethodInfo _getEnemiesOfPosition;

        public static void Prefix(HailOfArrowsPrefab._BarrageRoutine_d__24 __instance)
        {
            try
            {
                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                var spell = __instance.__4__this;
                if (spell == null || spell.Pointer == IntPtr.Zero) return;

                float areaMult = BarrageOfArrowsTunerPlugin.DropAreaMultiplier.Value;
                if (areaMult > 0f && areaMult != 1f)
                {
                    ScaleIndicatorTemplate(spell, areaMult);
                }

                if (BarrageOfArrowsTunerPlugin.RetargetOnDeath.Value)
                {
                    RetargetNextPoint(__instance, spell);
                }
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_HailOfArrowsPrefab_BarrageRoutine_MoveNext.Prefix] {ex}");
            }
        }

        public static void Postfix(HailOfArrowsPrefab._BarrageRoutine_d__24 __instance, ref bool __result)
        {
            try
            {
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                if (!__result)
                {
                    var done = __instance.__4__this;
                    if (done != null && done.Pointer != IntPtr.Zero) _templateScales.Remove(done.Pointer);
                    return;
                }

                if (!BarrageOfArrowsTunerPlugin.Enabled.Value) return;

                // State 1 is the between-arrow cadence yield; state 2 is the post-loop wait (leave it).
                if (__instance.__1__state != 1) return;

                float interval = BarrageOfArrowsTunerPlugin.TickInterval.Value;
                if (interval <= 0f) return; // <= 0 keeps the vanilla 0.15s cadence

                __instance.__2__current = new WaitForSeconds(interval);
            }
            catch (Exception ex)
            {
                BarrageOfArrowsTunerPlugin.Log?.LogError($"[Patch_HailOfArrowsPrefab_BarrageRoutine_MoveNext.Postfix] {ex}");
            }
        }

        /// <summary>
        /// Scales the barrage's per-arrow drop-indicator template (field 0x320) so every spawned indicator
        /// is enlarged. The template's localScale is captured once per spell so repeated Prefix calls are
        /// idempotent and native pointer reuse cannot compound the scale.
        /// </summary>
        private static void ScaleIndicatorTemplate(HailOfArrowsPrefab spell, float mult)
        {
            var template = spell._targetIndicatorTemplate;
            if (template == null || template.Pointer == IntPtr.Zero) return;

            IntPtr spellPtr = spell.Pointer;
            IntPtr templatePtr = template.Pointer;

            TemplateScale entry;
            if (!_templateScales.TryGetValue(spellPtr, out entry) || entry.TemplatePtr != templatePtr)
            {
                entry = new TemplateScale { TemplatePtr = templatePtr, BaseScale = template.transform.localScale };
                _templateScales[spellPtr] = entry;
            }

            Vector3 target = entry.BaseScale * mult;
            if (template.transform.localScale != target)
            {
                template.transform.localScale = target;
            }
        }

        /// <summary>
        /// Re-points the next un-spawned barrage point at the nearest still-living enemy around the spell.
        /// Uses the game's own protected <c>BaseSpellLibrary.GetAllEnemiesInRangeOfPosition</c> (team-filtered)
        /// through cached reflection, then scatters within the same (possibly scaled) drop area.
        /// </summary>
        private static void RetargetNextPoint(HailOfArrowsPrefab._BarrageRoutine_d__24 sm, HailOfArrowsPrefab spell)
        {
            var points = sm._points_5__3;
            if (points == null) return;

            int count = points.Count;
            if (count <= 0) return;

            // On a state-1 resume the coroutine increments <i> before it spawns, so the arrow about to drop
            // is index i + 1. On state 0 <points> is still null (handled above).
            int next = sm.__1__state == 1 ? sm._i_5__4 + 1 : sm._i_5__4;
            if (next < 0 || next >= count) return;

            Vector3 spellPos = spell.transform.position;
            Vector2 center = new Vector2(spellPos.x, spellPos.y);

            float range = 10f;
            float detMult = BarrageOfArrowsTunerPlugin.DetectionRangeMultiplier.Value;
            if (detMult > 0f) range *= detMult;

            var enemy = FindNearestLivingEnemy(spell, center, range);
            if (enemy == null) return; // no live target: keep the original landing point

            Vector3 enemyPos = enemy.transform.position;
            Vector2 anchor = new Vector2(enemyPos.x, enemyPos.y);

            float scatter = BarrageOfArrowsTunerPlugin.ScatterRadius.Value;
            if (scatter < 0f) scatter = 0f;
            float areaMult = BarrageOfArrowsTunerPlugin.DropAreaMultiplier.Value;
            if (areaMult > 0f && areaMult != 1f) scatter *= areaMult;

            points[next] = anchor + UnityEngine.Random.insideUnitCircle * scatter;

            if (BarrageOfArrowsTunerPlugin.DiagnosticLogging.Value)
            {
                BarrageOfArrowsTunerPlugin.Log.LogInfo(
                    $"[BarrageSequence] re-targeted arrow {next}/{count - 1} to ({anchor.x:F1}, {anchor.y:F1}) " +
                    $"(scatter {scatter:F1})");
            }
        }

        private static ObjectsCommon FindNearestLivingEnemy(BaseSpellLibrary spell, Vector2 center, float range)
        {
            if (range <= 0f) return null;

            if (_getEnemiesOfPosition == null)
            {
                _getEnemiesOfPosition = AccessTools.Method(
                    typeof(BaseSpellLibrary), "GetAllEnemiesInRangeOfPosition");
                if (_getEnemiesOfPosition == null)
                {
                    BarrageOfArrowsTunerPlugin.Log?.LogError(
                        "[BarrageSequence] BaseSpellLibrary.GetAllEnemiesInRangeOfPosition not found; re-target disabled.");
                    return null;
                }
            }

            var list = (Il2CppGenerics.List<ObjectsCommon>)_getEnemiesOfPosition.Invoke(
                spell, new object[] { center, range });
            if (list == null) return null;

            ObjectsCommon best = null;
            float bestSqr = float.MaxValue;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var enemy = list[i];
                if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                if (enemy.IsDead()) continue;

                Vector3 p = enemy.transform.position;
                float sqr = (new Vector2(p.x, p.y) - center).sqrMagnitude;
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
}
