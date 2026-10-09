using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace EchoRiftTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that tunes the Echo Rift spell:
    ///   * [2026-10-09] Doubles the "temporal burst" area of effect. Echo Rift is an <see cref="AreaOfEffect"/>:
    ///     <c>FirePulse</c> calls <c>AreaOfEffect.QuickStrike</c>, which enables the AoE trigger collider
    ///     (<c>AreaOfEffect._aoeCollider</c>, offset 0x288) and lets its OnTriggerEnter2D strikes hit every enemy
    ///     inside. That collider IS the burst hit area, so we scale its geometry by
    ///     <c>AreaOfEffect.RadiusMultiplier</c> (default 2.0 = twice the radius).
    ///   * [2026-10-09] Adds a Black-Hole-style pull: every enemy inside <c>BlackHoleDrag.Radius</c> is dragged
    ///     toward the rift centre using the game's own <c>BaseSpellLibrary.DashTargetToPosition</c> - the exact
    ///     primitive Black Hole / Barrage of Arrows / Pursuing Blizzard use. The pull fires on cast and on every
    ///     burst pulse. The default radius, dash time and distance multiplier match the Barrage of Arrows Tuner.
    ///
    /// Co-op: the drag reuses the game's own knockback primitive (which honours authority / Immune / DisplaceExempt),
    /// so run the SAME config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class EchoRiftTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.echorifttuner";
        public const string NAME = "Echo Rift Tuner";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static EchoRiftTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;

        // [2026-10-09] Temporal burst AoE scaling (base Echo Rift only - EchoRiftPrefab instances).
        public static ConfigEntry<float> AoeRadiusMultiplier;

        // [2026-10-09] Echo Rift enemy drag (mirrors BarrageOfArrowsTuner's Black Hole drag).
        public static ConfigEntry<bool> DragEnabled;
        public static ConfigEntry<bool> DragOnActivate;
        public static ConfigEntry<bool> DragOnPulse;
        public static ConfigEntry<float> DragRadius;
        public static ConfigEntry<float> DragDashTime;
        public static ConfigEntry<float> DragDistanceMultiplier;

        public static ConfigEntry<bool> DiagnosticLogging;

        // [2026-10-09] Field offsets verified against this build's decompiled metadata.
        internal static int AoeColliderOffset = 0x288; // AreaOfEffect._aoeCollider (Collider2D)

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false Echo Rift is left at vanilla values. (Default: true)");

            AoeRadiusMultiplier = Config.Bind("AreaOfEffect", "RadiusMultiplier", 2.0f,
                "Scale the Echo Rift temporal-burst area of effect. 2.0 = twice the vanilla radius (doubles the " +
                "hit area). Applied by scaling the burst trigger collider's own geometry (AreaOfEffect._aoeCollider), " +
                "the same technique BarrageOfArrowsTuner/TwisterTuner use, so it widens the trigger in world space " +
                "regardless of whether the collider follows its transform. 1.0 disables. (Default: 2.0)");

            DragEnabled = Config.Bind("BlackHoleDrag", "Enabled", true,
                "If true Echo Rift drags surrounding enemies toward its centre using the game's own " +
                "BaseSpellLibrary.DashTargetToPosition (the exact Black Hole primitive Barrage of Arrows uses). " +
                "(Default: true)");

            DragOnActivate = Config.Bind("BlackHoleDrag", "DragOnActivate", true,
                "Pull all enemies in range toward the rift the moment it is cast. (Default: true)");

            DragOnPulse = Config.Bind("BlackHoleDrag", "DragOnPulse", true,
                "Pull all enemies in range toward the rift on every burst pulse (each EchoRiftPrefab.FirePulse), so " +
                "enemies are repeatedly yanked onto the rift for the next pulse. (Default: true)");

            DragRadius = Config.Bind("BlackHoleDrag", "Radius", 10.0f,
                "Radius (world units) around the rift centre in which enemies are dragged. Matches the Barrage of " +
                "Arrows drag radius (default 10.0). (Default: 10.0)");

            DragDashTime = Config.Bind("BlackHoleDrag", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank. Matches the Barrage of Arrows " +
                "drag dash time (default 0.2). (Default: 0.2)");

            DragDistanceMultiplier = Config.Bind("BlackHoleDrag", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly onto " +
                "the rift centre; >1 pulls it past the centre (clamped by walls). Matches the Barrage of Arrows drag " +
                "distance multiplier (default 1.0). (Default: 1.0)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log Echo Rift AoE scaling and drag events to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_AreaOfEffect_Start_Aoe));
            PatchOrLog(harmony, typeof(Patch_EchoRiftPrefab_OnStart_Drag));
            PatchOrLog(harmony, typeof(Patch_EchoRiftPrefab_FirePulse_Drag));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"AreaOfEffect.RadiusMultiplier: {AoeRadiusMultiplier.Value}");
            Log.LogInfo($"BlackHoleDrag: {DragEnabled.Value} (activate={DragOnActivate.Value}, pulse={DragOnPulse.Value}, radius={DragRadius.Value}, distX={DragDistanceMultiplier.Value}, dashTime={DragDashTime.Value}s)");
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
                Log.LogError($"[EchoRiftTuner] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        /// <summary>
        /// Robust IL2CPP instance-type test for patch call sites. Mirrors
        /// BarrageOfArrowsTunerPlugin.IsSpellInstance: Harmony can hand the patch a wrapper typed as the declared
        /// base type (AreaOfEffect/BaseSpellLibrary) even though the native object is the derived EchoRiftPrefab,
        /// so a bare <c>is</c> check can silently miss. The fallback compares the runtime IL2CPP class name.
        /// </summary>
        internal static bool IsSpellInstance<T>(BaseSpellLibrary instance) where T : BaseSpellLibrary
        {
            if (instance == null) return false;
            if (instance is T) return true;
            return instance.GetIl2CppType().Name == typeof(T).Name;
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// [2026-10-09] Widen the Echo Rift temporal-burst area. EchoRiftPrefab is an AreaOfEffect whose
        /// <c>FirePulse</c> drives <c>AreaOfEffect.QuickStrike</c>; QuickStrike enables <c>_aoeCollider</c> and its
        /// trigger-enter strikes hit everything inside. We scale that collider's own geometry, so the burst hit
        /// area grows. Only EchoRiftPrefab instances are touched, so every other AreaOfEffect spell is untouched.
        /// </summary>
        internal static unsafe void ApplyAoe(AreaOfEffect aoe)
        {
            try
            {
                if (aoe == null || aoe.Pointer == IntPtr.Zero) return;
                if (!IsSpellInstance<EchoRiftPrefab>(aoe)) return;

                float mult = AoeRadiusMultiplier.Value;
                if (mult <= 0f || Math.Abs(mult - 1f) < 0.0001f) return;

                IntPtr colPtr = *(IntPtr*)((byte*)aoe.Pointer + AoeColliderOffset);
                if (colPtr == IntPtr.Zero) return;

                var col = new Collider2D(colPtr);
                ScaleColliderGeometry(col, mult);

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[EchoRiftAoE] burst collider ({col.GetType().Name}) scaled x{FormatMultiplier(mult)}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplyAoe] {ex}");
            }
        }

        /// <summary>
        /// [2026-10-09] Multiply a Collider2D's own shape by <paramref name="mult"/>. Mirrors
        /// BarrageOfArrowsTunerPlugin.ScaleColliderGeometry / TwisterTunerPlugin.ScaleColliderGeometry so the
        /// trigger area grows in world space regardless of whether this build's Collider2D follows its Transform.
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
        /// [2026-10-09] Drag every live enemy inside the configured radius onto the rift centre using the game's
        /// own <c>BaseSpellLibrary.DashTargetToPosition</c> - the exact primitive BlackholePrefab.AreaOfEffectStrike,
        /// BarrageOfArrowsTuner and PursuingBlizzardTuner use. That call disables the NavMeshAgent during the lerp
        /// and restores it afterwards, raycasts walls, and honours ObjectsCommon.Immune /
        /// MonsterBehaviourLibrary.DisplaceExempt / authority. The centre is the rift's own transform (Echo Rift is
        /// stationary), so there is no "centre enemy" to exclude.
        /// </summary>
        internal static void ApplySuction(EchoRiftPrefab rift)
        {
            if (!Enabled.Value || !DragEnabled.Value) return;
            if (rift == null || rift.Pointer == IntPtr.Zero) return;

            try
            {
                var t = rift.transform;
                if (t == null) return;
                Vector3 p = t.position;
                Vector2 center = new Vector2(p.x, p.y);

                float radius = DragRadius.Value;
                if (radius <= 0.1f) return;

                var enemies = rift.GetAllEnemiesInRangeOfPosition(center, radius);
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
                    rift.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[EchoRiftDrag] dragged {pulled} enemies toward rift ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-09] Postfix on <c>AreaOfEffect.Start</c>. The base Start resolves the spell's trigger collider
    /// (<c>_aoeCollider</c>) via GetComponentInChildren before calling BaseSpellLibrary.Start, so by the time the
    /// postfix runs the collider is populated. We scale it for EchoRiftPrefab instances only.
    /// </summary>
    [HarmonyPatch(typeof(AreaOfEffect), "Start")]
    public static class Patch_AreaOfEffect_Start_Aoe
    {
        public static void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (!EchoRiftTunerPlugin.Enabled.Value) return;
                EchoRiftTunerPlugin.ApplyAoe(__instance);
            }
            catch (Exception ex)
            {
                EchoRiftTunerPlugin.Log?.LogError($"[Patch_AreaOfEffect_Start_Aoe] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-09] Postfix on <c>EchoRiftPrefab.OnStart</c>. Pulls surrounding enemies onto the rift once when the
    /// spell is cast.
    /// </summary>
    [HarmonyPatch(typeof(EchoRiftPrefab), "OnStart")]
    public static class Patch_EchoRiftPrefab_OnStart_Drag
    {
        public static void Postfix(EchoRiftPrefab __instance)
        {
            try
            {
                if (!EchoRiftTunerPlugin.Enabled.Value) return;
                if (!EchoRiftTunerPlugin.DragEnabled.Value) return;
                if (!EchoRiftTunerPlugin.DragOnActivate.Value) return;
                EchoRiftTunerPlugin.ApplySuction(__instance);
            }
            catch (Exception ex)
            {
                EchoRiftTunerPlugin.Log?.LogError($"[Patch_EchoRiftPrefab_OnStart_Drag] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-09] Postfix on <c>EchoRiftPrefab.FirePulse</c>. Pulls surrounding enemies onto the rift on every
    /// burst pulse so the repeated pulls read as continuous suction and set up the next pulse.
    /// </summary>
    [HarmonyPatch(typeof(EchoRiftPrefab), "FirePulse")]
    public static class Patch_EchoRiftPrefab_FirePulse_Drag
    {
        public static void Postfix(EchoRiftPrefab __instance)
        {
            try
            {
                if (!EchoRiftTunerPlugin.Enabled.Value) return;
                if (!EchoRiftTunerPlugin.DragEnabled.Value) return;
                if (!EchoRiftTunerPlugin.DragOnPulse.Value) return;
                EchoRiftTunerPlugin.ApplySuction(__instance);
            }
            catch (Exception ex)
            {
                EchoRiftTunerPlugin.Log?.LogError($"[Patch_EchoRiftPrefab_FirePulse_Drag] {ex}");
            }
        }
    }
}
