using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;

namespace FireballTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that tunes the vanilla Fireball starting skill:
    ///   * Max range              -> RangeMultiplier (default x3)
    ///   * Burning stacks         -> BurningMultiplier (default x3)
    ///   * Projectile speed       -> SpeedMultiplier (default x1.3)
    ///   * Cast time              -> InstantCast (default on, CastTime = near-zero => click-trigger cast, no wind-up)
    ///   * Explosive impact AoE   -> ExplosiveImpactAoeMultiplier (default x1.5 = +50% radius)
    ///   * Explosive impact pull  -> BlackHoleSuction (default on, uses DashTargetToPosition like ScatterTrapTuner)
    ///
    /// The mod only touches the Fireball spell. Every lever is behind a config entry so it can be
    /// reverted without rebuilding. Co-op: run the SAME config on host and client so both peers
    /// agree on the numbers (range/cast are both computed locally, burning is applied through the
    /// shared BaseSpellLibrary.AddStacksToTarget path; suction uses the game's own networked
    /// DashTargetToPosition primitive).
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class FireballTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.fireballtuner";
        public const string NAME = "Fireball Tuner";
        public const string VERSION = "1.1.0";

        // [2026-10-01] A cast time of exactly 0 breaks the Fireball launch. BaseSpellLibrary.Start copies
        // this component's CastTime (SpellLibrary 0x18C) into the prefab's BaseSpellLibrary._castTime
        // (0x1C0); BaseSpell.CastTimeCallbackCheck then early-returns when _castTime == 0, so it never
        // fires NetworkSpellManager.FireSpellCastCompleteServerRpc -> OnCastComplete -> SkillShot.LaunchProjectile
        // and no projectile is spawned. A tiny non-zero value keeps that launch path alive while remaining
        // effectively instant (the prefab's _aliveTime 0x1CC reaches it within one frame).
        internal const float InstantCastTime = 0.01f;

        internal static new ManualLogSource Log;
        internal static FireballTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> RangeMultiplier;
        public static ConfigEntry<float> BurningMultiplier;
        public static ConfigEntry<float> SpeedMultiplier;
        public static ConfigEntry<bool> InstantCast;
        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> UpdateBurningStacksInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

        // [2026-10-14] Explosive Impact tuning (FireballExplosiveImpactPrefab : AreaOfEffect)
        public static ConfigEntry<float> ExplosiveImpactAoeMultiplier;

        // [2026-10-14] Black Hole style suction on explosive impact (mirrors ScatterTrapTuner)
        public static ConfigEntry<bool> SuctionEnabled;
        public static ConfigEntry<bool> SuctionOnActivate;
        public static ConfigEntry<bool> SuctionOnStrike;
        public static ConfigEntry<float> SuctionRadiusMultiplier;
        public static ConfigEntry<float> SuctionDashTime;
        public static ConfigEntry<float> SuctionDistanceMultiplier;

        // Field offsets resolved at runtime via IL2CPP metadata (fallbacks from 10/7/2026 dump).
        // AreaOfEffect._aoeCollider (Collider2D) -> 0x288.
        internal static int AoeColliderOffset = 0x288;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false no Fireball value is modified (Default: true)");

            RangeMultiplier = Config.Bind("Fireball", "RangeMultiplier", 3f,
                "Multiplier for the Fireball's max travel distance and cast (targeting) radius. 1 = vanilla. (Default: 3)");

            BurningMultiplier = Config.Bind("Fireball", "BurningMultiplier", 3f,
                "Multiplier for the number of Burning stacks the Fireball applies. 1 = vanilla. (Default: 3)");

            // [2026-10-01] Was 2f (x2). Reduced to 1.3 because x2 made the projectile too fast.
            SpeedMultiplier = Config.Bind("Fireball", "SpeedMultiplier", 1.3f,
                "Multiplier for the Fireball projectile's travel speed. 1 = vanilla, 1.3 = 30% faster. (Default: 1.3)");

            InstantCast = Config.Bind("Fireball", "InstantCast", true,
                // [2026-10-01] Description updated: the cast time can no longer be exactly 0 (that suppressed the
                // launch callback); it is set to a near-zero value instead, which is still effectively instant.
                "If true the Fireball's cast time is reduced to a near-zero value so it fires immediately on click (no wind-up). (Default: true)");

            // [2026-10-14] Explosive Impact AoE scaling. FireballExplosiveImpactPrefab is an AreaOfEffect
            // whose trigger collider (AreaOfEffect._aoeCollider) IS the hit area. Scaling its geometry
            // widens the explosion radius in world space.
            ExplosiveImpactAoeMultiplier = Config.Bind("ExplosiveImpact", "AoeRadiusMultiplier", 1.5f,
                "Scale the Fireball explosive impact area of effect. 1.5 = +50% radius. Applied by scaling the " +
                "AoE trigger collider's own geometry (AreaOfEffect._aoeCollider), the same technique " +
                "EchoRiftTuner/TwisterTuner/BarrageOfArrowsTuner use. 1.0 disables. (Default: 1.5)");

            // [2026-10-14] Black Hole suction on explosive impact, mirrors ScatterTrapTuner's [BlackHoleSuction].
            // Uses the game's own BaseSpellLibrary.DashTargetToPosition - the exact primitive Black Hole,
            // Pursuing Blizzard, Barrage of Arrows, and Echo Rift use.
            SuctionEnabled = Config.Bind("BlackHoleSuction", "Enabled", true,
                "If true the Fireball explosive impact drags surrounding enemies toward the impact point using " +
                "the game's own BaseSpellLibrary.DashTargetToPosition (the exact Black Hole primitive). (Default: true)");

            SuctionOnActivate = Config.Bind("BlackHoleSuction", "SuctionOnActivate", true,
                "Pull all enemies in range toward the impact point the moment the explosion occurs. (Default: true)");

            SuctionOnStrike = Config.Bind("BlackHoleSuction", "SuctionOnStrike", true,
                "Also pull enemies toward the impact point when they are struck by the explosion (each " +
                "AreaOfEffectStrike), like TwisterTuner's on-entry pull. (Default: true)");

            SuctionRadiusMultiplier = Config.Bind("BlackHoleSuction", "RadiusMultiplier", 1.0f,
                "Multiplier on the explosive impact's AoE radius for how far enemies are grabbed. " +
                "1.0 = same as the explosion radius; >1 reaches beyond it. (Default: 1.0)");

            SuctionDashTime = Config.Bind("BlackHoleSuction", "DashTime", 0.2f,
                "Seconds for each enemy's drag lerp. Lower = faster/snappier yank (Black Hole uses 0.3). (Default: 0.2)");

            SuctionDistanceMultiplier = Config.Bind("BlackHoleSuction", "DistanceMultiplier", 1.0f,
                "Multiplier on the drag distance passed to DashTargetToPosition. 1.0 = pull the enemy exactly " +
                "onto the impact centre; >1 pulls it past the centre (clamped by walls). (Default: 1.0)");

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a line to the Fireball spell description (tooltip/skill tree/character creation) listing the tuned values: instant cast, range, burning, projectile speed, explosive impact AoE, suction. (Default: true)");

            UpdateBurningStacksInTooltip = Config.Bind("Tooltip", "UpdateBurningStacks", true,
                "Rewrite the 'Burning: N' line in the Fireball tooltip so the displayed stack count matches BurningMultiplier. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogFireballValues", false,
                "Log the Fireball's radius/cast-time, burning stack changes, explosive impact AoE scaling and suction events to the BepInEx console. (Default: false)");

            // Resolve field offsets from current build's IL2CPP metadata before installing patches.
            ResolveOffsets();

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_FireballPrefab_OnStart));
            PatchOrLog(harmony, typeof(Patch_FireballPrefab_OnStart_Speed));
            PatchOrLog(harmony, typeof(Patch_Spells_Cast));
            PatchOrLog(harmony, typeof(Patch_BaseSpell_CastTimeCallbackCheck));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToTarget));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedEffects));

            // [2026-10-14] Explosive Impact patches
            PatchOrLog(harmony, typeof(Patch_AreaOfEffect_Start_ExplosiveImpactAoe));
            PatchOrLog(harmony, typeof(Patch_FireballExplosiveImpactPrefab_OnStart_Suction));
            PatchOrLog(harmony, typeof(Patch_FireballExplosiveImpactPrefab_AreaOfEffectStrike_Suction));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"RangeMultiplier: {RangeMultiplier.Value}");
            Log.LogInfo($"BurningMultiplier: {BurningMultiplier.Value}");
            Log.LogInfo($"SpeedMultiplier: {SpeedMultiplier.Value}");
            Log.LogInfo($"InstantCast: {InstantCast.Value}");
            Log.LogInfo($"ExplosiveImpactAoeMultiplier: {ExplosiveImpactAoeMultiplier.Value}");
            Log.LogInfo($"BlackHoleSuction: {SuctionEnabled.Value} (activate={SuctionOnActivate.Value}, onStrike={SuctionOnStrike.Value}, radiusX{SuctionRadiusMultiplier.Value}, distX{SuctionDistanceMultiplier.Value}, dashTime={SuctionDashTime.Value}s)");
            Log.LogInfo($"ShowTunedStatsInTooltip: {ShowTunedStatsInTooltip.Value}");
            Log.LogInfo($"UpdateBurningStacksInTooltip: {UpdateBurningStacksInTooltip.Value}");
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
                Log.LogError($"[FireballTuner] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        /// <summary>
        /// Resolves field offsets from the current build's IL2CPP metadata. Fallback to verified
        /// offsets from the 10/7/2026 dump if resolution fails. Called once in Load() before
        /// patch registration.
        /// </summary>
        private static void ResolveOffsets()
        {
            try
            {
                var aoeClass = Il2CppClassPointerStore<AreaOfEffect>.NativeClassPtr;
                if (aoeClass != IntPtr.Zero)
                {
                    int aoeCollider = GetFieldOffset(aoeClass, "_aoeCollider", 0x288);
                    AoeColliderOffset = aoeCollider;
                    Log.LogInfo($"[ResolveOffsets] _aoeCollider offset: 0x{AoeColliderOffset:X}");
                }
                else
                {
                    Log.LogWarning("[ResolveOffsets] AreaOfEffect class pointer is null, using fallback offset 0x288");
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
        /// Robust IL2CPP instance-type test for patch call sites. Mirrors
        /// BarrageOfArrowsTunerPlugin.IsSpellInstance: Harmony can hand the patch a wrapper typed as the declared
        /// base type (AreaOfEffect/BaseSpellLibrary) even though the native object is the derived FireballExplosiveImpactPrefab,
        /// so a bare <c>is</c> check can silently miss. The fallback compares the runtime IL2CPP class name.
        /// </summary>
        internal static bool IsSpellInstance<T>(BaseSpellLibrary instance) where T : BaseSpellLibrary
        {
            if (instance == null) return false;
            if (instance is T) return true;
            return instance.GetIl2CppType().Name == typeof(T).Name;
        }

        /// <summary>
        /// Best-effort runtime check that the object is one of the fireball projectile prefabs
        /// (FireballPrefab, FireballSecondaryPrefab, FireballExplosiveImpactPrefab). All of them share
        /// the "Fireball" class-name prefix, so we don't need to hard-reference each interop type.
        /// </summary>
        internal static bool IsFireballPrefab(BaseSpellLibrary instance)
        {
            try
            {
                var type = instance.GetIl2CppType();
                if (type == null) return false;
                string name = type.Name;
                return !string.IsNullOrEmpty(name) && name.StartsWith("Fireball", StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exact match for the primary projectile prefab only. Used by the cast-timer patch because the
        /// secondary/impact prefabs (FireballSecondaryPrefab, FireballExplosiveImpactPrefab) inherit
        /// OnCastComplete -> SkillShot.LaunchProjectile; forcing their cast timer open could spawn a spurious
        /// projectile at spawn. Those prefabs still receive the near-zero cast time through the normal
        /// SpellLibrary.CastTime -> BaseSpellLibrary._castTime copy, so this narrowing is safe.
        /// </summary>
        internal static bool IsExactFireballPrefab(BaseSpellLibrary instance)
        {
            try
            {
                var type = instance.GetIl2CppType();
                return type != null && type.Name == "FireballPrefab";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Builds the extra tooltip line appended to the Fireball description so the in-game text reflects
        /// what this mod changes. Returns null when nothing is tuned or the feature is disabled.
        /// </summary>
        internal static string BuildTooltipSuffix()
        {
            if (!ShowTunedStatsInTooltip.Value) return null;

            var parts = new System.Collections.Generic.List<string>();
            if (InstantCast.Value) parts.Add("instant cast");
            if (RangeMultiplier.Value > 0f && RangeMultiplier.Value != 1f)
                parts.Add($"range x{FormatMultiplier(RangeMultiplier.Value)}");
            if (BurningMultiplier.Value > 0f && BurningMultiplier.Value != 1f)
                parts.Add($"burning x{FormatMultiplier(BurningMultiplier.Value)}");
            if (SpeedMultiplier.Value > 0f && SpeedMultiplier.Value != 1f)
                parts.Add($"projectile speed x{FormatMultiplier(SpeedMultiplier.Value)}");
            if (ExplosiveImpactAoeMultiplier.Value > 0f && ExplosiveImpactAoeMultiplier.Value != 1f)
                parts.Add($"explosion AoE x{FormatMultiplier(ExplosiveImpactAoeMultiplier.Value)}");
            if (SuctionEnabled.Value)
                parts.Add("explosion pulls enemies in");

            if (parts.Count == 0) return null;
            return "\n\n[Fireball Tuner] " + string.Join(", ", parts) + ".";
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Rewrites the numeric value of a tooltip effect line such as "Burning: 2" -> "Burning: 6".
        /// Only lines whose value (after the first ':') is a plain positive integer are touched, so the
        /// damage line "Fire Damage: (MF:2.2)" is left alone. Returns null when the line should not change.
        /// The label may be localized (e.g. Russian), which is why only the value is parsed.
        /// </summary>
        internal static string ScaleIntegerEffectLine(string line, float multiplier)
        {
            if (string.IsNullOrEmpty(line)) return null;
            if (multiplier <= 0f || multiplier == 1f) return null;

            int idx = line.IndexOf(':');
            if (idx <= 0) return null;

            string value = line.Substring(idx + 1).Trim();
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                return null;
            if (amount <= 0) return null;

            int scaled = (int)Math.Round(amount * (double)multiplier, MidpointRounding.AwayFromZero);
            if (scaled < 1) scaled = 1;

            return line.Substring(0, idx + 1) + " " + scaled.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// [2026-10-14] Widen the Fireball explosive impact area. FireballExplosiveImpactPrefab is an AreaOfEffect whose
        /// <c>AreaOfEffectStrike</c> drives <c>AreaOfEffect.QuickStrike</c>; QuickStrike enables <c>_aoeCollider</c> and its
        /// trigger-enter strikes hit everything inside. We scale that collider's own geometry, so the explosion hit
        /// area grows. Only FireballExplosiveImpactPrefab instances are touched, so every other AreaOfEffect spell is untouched.
        /// </summary>
        internal static unsafe void ApplyExplosiveImpactAoe(AreaOfEffect aoe)
        {
            try
            {
                if (aoe == null || aoe.Pointer == IntPtr.Zero) return;
                if (!IsSpellInstance<FireballExplosiveImpactPrefab>(aoe)) return;

                float mult = ExplosiveImpactAoeMultiplier.Value;
                if (mult <= 0f || Math.Abs(mult - 1f) < 0.0001f) return;

                IntPtr colPtr = *(IntPtr*)((byte*)aoe.Pointer + AoeColliderOffset);
                if (colPtr == IntPtr.Zero) return;

                var col = new Collider2D(colPtr);
                ScaleColliderGeometry(col, mult);

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[ExplosiveImpactAoE] impact collider ({col.GetType().Name}) scaled x{FormatMultiplier(mult)}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplyExplosiveImpactAoe] {ex}");
            }
        }

        /// <summary>
        /// [2026-10-14] Multiply a Collider2D's own shape by <paramref name="mult"/>. Mirrors
        /// EchoRiftTunerPlugin.ScaleColliderGeometry / TwisterTunerPlugin.ScaleColliderGeometry / BarrageOfArrowsTunerPlugin.ScaleColliderGeometry
        /// so the trigger area grows in world space regardless of whether this build's Collider2D follows its Transform.
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
        /// [2026-10-14] Drag every live enemy inside the configured radius onto the impact centre using the game's
        /// own <c>BaseSpellLibrary.DashTargetToPosition</c> - the exact primitive BlackholePrefab.AreaOfEffectStrike,
        /// ScatterTrapTuner, EchoRiftTuner, BarrageOfArrowsTuner and PursuingBlizzardTuner use. That call disables the
        /// NavMeshAgent during the lerp and restores it afterward, raycasts walls, and honours ObjectsCommon.Immune /
        /// MonsterBehaviourLibrary.DisplaceExempt / authority. The centre is the spell's own transform (the impact point).
        /// </summary>
        internal static void ApplySuction(FireballExplosiveImpactPrefab impact)
        {
            if (!Enabled.Value || !SuctionEnabled.Value) return;
            if (impact == null || impact.Pointer == IntPtr.Zero) return;

            try
            {
                var t = impact.transform;
                if (t == null) return;
                Vector3 p = t.position;
                Vector2 center = new Vector2(p.x, p.y);

                // Use the explosion's own AoE radius as the base grab radius, scaled by RadiusMultiplier.
                // We approximate the radius from the collider if available, otherwise fall back to a reasonable default.
                float baseRadius = 5f; // vanilla Fireball explosion radius ~5 units
                var col = impact.GetComponent<Collider2D>();
                if (col != null)
                {
                    var circle = col.TryCast<CircleCollider2D>();
                    if (circle != null) baseRadius = circle.radius;
                }

                float radius = baseRadius * SuctionRadiusMultiplier.Value;
                if (radius <= 0.1f) return;

                var enemies = impact.GetAllEnemiesInRangeOfPosition(center, radius);
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

                    // Same shape as BlackholePrefab.AreaOfEffectStrike / ScatterTrapTuner.ApplySuction:
                    //   DashTargetToPosition(target, center, time, distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot)
                    impact.DashTargetToPosition(enemy, center, dashTime, dist * distMult, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log?.LogInfo($"[ExplosiveImpactSuction] dragged {pulled} enemies toward impact ({center.x:F1}, {center.y:F1}), radius {radius:F1}, distX{distMult:F2}, time {dashTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on the private <c>SpellLibrary.ApplyDefinition</c>. This is where a spell component copies
    /// its <see cref="SpellDefinition"/> values onto the runtime fields (<c>CastRadius -> BaseCastRadius</c>
    /// and <c>CastTime -> CastTime</c> are copied unconditionally). We scale the radius and optionally zero
    /// the cast time for the Fireball spell only. Called once per spell component (from the component's
    /// setup/Awake path), so it is not double-applied.
    /// </summary>
    [HarmonyPatch(typeof(SpellLibrary), "ApplyDefinition")]
    public static class Patch_SpellLibrary_ApplyDefinition
    {
        public static void Postfix(SpellLibrary __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (__instance.Spell != Spell.Fireball) return;

                float rangeMult = FireballTunerPlugin.RangeMultiplier.Value;
                float baseRadius = __instance.BaseCastRadius;
                float newRadius = baseRadius;

                if (rangeMult > 0f && rangeMult != 1f && baseRadius > 0f)
                {
                    newRadius = baseRadius * rangeMult;
                    __instance.BaseCastRadius = newRadius;
                }

                float oldCastTime = __instance.CastTime;
                if (FireballTunerPlugin.InstantCast.Value)
                {
                    // [2026-10-01] Was: __instance.CastTime = 0f;  -- a 0 cast time makes the spawned prefab's
                    // _castTime 0 (copied in BaseSpellLibrary.Start) and BaseSpell.CastTimeCallbackCheck then
                    // early-returns without launching, so the projectile never fired. Use near-zero instead.
                    //__instance.CastTime = 0f;
                    __instance.CastTime = FireballTunerPlugin.InstantCastTime;
                }

                if (FireballTunerPlugin.DiagnosticLogging.Value)
                {
                    FireballTunerPlugin.Log.LogInfo(
                        $"[ApplyDefinition] Fireball radius {baseRadius} -> {newRadius}, " +
                        $"castTime {oldCastTime} -> {__instance.CastTime}, " +
                        $"spellRadius {__instance.BaseSpellRadius}, baseDamage {__instance.BaseDamage}");
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_SpellLibrary_ApplyDefinition] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>FireballPrefab.OnStart</c>. The vanilla Fireball hard-codes its projectile
    /// max travel distance (SkillShot._maximumTravelDistance = 13.0) inside OnStart rather than going
    /// through SetMaximumTravelDistance, so the only reliable hook is after OnStart. We scale the field
    /// in place; the offset is resolved from IL2CPP metadata at runtime with a build-time fallback.
    /// Only FireballPrefab sets this field (the secondary/explosive prefabs do not).
    /// </summary>
    [HarmonyPatch(typeof(FireballPrefab), "OnStart")]
    public static class Patch_FireballPrefab_OnStart
    {
        // Fallback offset (SkillShot._maximumTravelDistance) for this game build.
        private const int FallbackMaxTravelOffset = 0x2A8;
        private static int _maxTravelOffset = FallbackMaxTravelOffset;
        private static bool _offsetResolved;

        private static void EnsureOffset()
        {
            if (_offsetResolved) return;
            _offsetResolved = true;
            try
            {
                IntPtr field = IL2CPP.GetIl2CppField(
                    Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_maximumTravelDistance");
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0) _maxTravelOffset = off;
                }
            }
            catch
            {
                // Keep the fallback offset.
            }
        }

        public static void Postfix(FireballPrefab __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!FireballTunerPlugin.Enabled.Value) return;

                float mult = FireballTunerPlugin.RangeMultiplier.Value;
                if (mult <= 0f || mult == 1f) return;

                EnsureOffset();

                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;

                    float* maxTravel = (float*)((byte*)p + _maxTravelOffset);
                    float vanilla = *maxTravel;
                    if (vanilla > 0f)
                    {
                        *maxTravel = vanilla * mult;
                        if (FireballTunerPlugin.DiagnosticLogging.Value)
                        {
                            FireballTunerPlugin.Log.LogInfo(
                                $"[OnStart] Fireball max travel distance {vanilla} -> {*maxTravel} (offset 0x{_maxTravelOffset:X})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_FireballPrefab_OnStart] {ex}");
            }
        }
    }

    /// <summary>
    /// Second, independent postfix on <c>FireballPrefab.OnStart</c> that scales the projectile speed.
    /// The vanilla Fireball hard-codes <c>SkillShot._projectileSpeed = 30</c> inside OnStart (never through
    /// SetProjectileSpeed), so we multiply the field in place right after. The charged cast overwrites the
    /// speed later in <c>OnActivateRelease</c> and is intentionally left untouched. Offset resolved from
    /// IL2CPP metadata at runtime with a build-time fallback.
    /// </summary>
    [HarmonyPatch(typeof(FireballPrefab), "OnStart")]
    public static class Patch_FireballPrefab_OnStart_Speed
    {
        // Fallback offset (SkillShot._projectileSpeed) for this game build.
        private const int FallbackProjectileSpeedOffset = 0x2C4;
        private static int _projectileSpeedOffset = FallbackProjectileSpeedOffset;
        private static bool _offsetResolved;

        private static void EnsureOffset()
        {
            if (_offsetResolved) return;
            _offsetResolved = true;
            try
            {
                IntPtr field = IL2CPP.GetIl2CppField(
                    Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_projectileSpeed");
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0) _projectileSpeedOffset = off;
                }
            }
            catch
            {
                // Keep the fallback offset.
            }
        }

        public static void Postfix(FireballPrefab __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!FireballTunerPlugin.Enabled.Value) return;

                float mult = FireballTunerPlugin.SpeedMultiplier.Value;
                if (mult <= 0f || mult == 1f) return;

                EnsureOffset();

                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;

                    float* speed = (float*)((byte*)p + _projectileSpeedOffset);
                    float vanilla = *speed;
                    if (vanilla > 0f)
                    {
                        *speed = vanilla * mult;
                        if (FireballTunerPlugin.DiagnosticLogging.Value)
                        {
                            FireballTunerPlugin.Log.LogInfo(
                                $"[OnStart] Fireball projectile speed {vanilla} -> {*speed} (offset 0x{_projectileSpeedOffset:X})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_FireballPrefab_OnStart_Speed] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>Spells.Cast</c> (the spellbook entry point, e.g. the <c>Fireball : Spells</c> component).
    /// Casting routes through <c>Spells.DelayActivateCastSpell</c>, which yields
    /// <c>WaitForSeconds(SpellLibrary.CastTime)</c> before the projectile prefab is spawned. Reducing CastTime
    /// here — immediately before that coroutine reads it — removes the wind-up regardless of when (or whether)
    /// <c>ApplyDefinition</c> ran, since the prefab later copies the same component's CastTime into
    /// <c>BaseSpellLibrary._castTime</c>. The value must stay non-zero: a 0 makes CastTimeCallbackCheck
    /// early-return and skip the launch (see <see cref="FireballTunerPlugin.InstantCastTime"/>).
    /// </summary>
    [HarmonyPatch(typeof(Spells), "Cast")]
    public static class Patch_Spells_Cast
    {
        public static void Prefix(Spells __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (!FireballTunerPlugin.InstantCast.Value) return;
                if (__instance.Spell != Spell.Fireball) return;

                // [2026-10-01] Was: __instance.CastTime = 0f;  -- the spawned prefab copies this into _castTime
                // and a 0 value suppressed the launch callback (see FireballTunerPlugin.InstantCastTime).
                //__instance.CastTime = 0f;
                __instance.CastTime = FireballTunerPlugin.InstantCastTime;

                if (FireballTunerPlugin.DiagnosticLogging.Value)
                {
                    FireballTunerPlugin.Log.LogInfo("[Cast] Fireball CastTime forced to near-zero for instant cast.");
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_Spells_Cast] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>BaseSpell.CastTimeCallbackCheck</c> (called every frame from BaseSpell.Update). The method
    /// early-returns when <c>BaseSpellLibrary._castTime (0x1C0) == 0</c>; that early return is NOT an
    /// instant-complete branch — it skips the cast-complete RPC and the launch. Forcing _castTime to a tiny
    /// non-zero value on the Fireball prefab guarantees the launch branch runs with no residual wind-up, even
    /// if the component copy at Start did not pick up the reduced cast time.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpell), "CastTimeCallbackCheck")]
    public static class Patch_BaseSpell_CastTimeCallbackCheck
    {
        // Fallback offset (BaseSpellLibrary._castTime) for this game build.
        private const int FallbackCastTimeOffset = 0x1C0;
        private static int _castTimeOffset = FallbackCastTimeOffset;
        private static bool _offsetResolved;

        private static void EnsureOffset()
        {
            if (_offsetResolved) return;
            _offsetResolved = true;
            try
            {
                IntPtr field = IL2CPP.GetIl2CppField(
                    Il2CppClassPointerStore<BaseSpellLibrary>.NativeClassPtr, "_castTime");
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0) _castTimeOffset = off;
                }
            }
            catch
            {
                // Keep the fallback offset.
            }
        }

        public static void Prefix(BaseSpell __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (!FireballTunerPlugin.InstantCast.Value) return;
                // [2026-10-01] Tightened from IsFireballPrefab (StartsWith "Fireball") to the exact primary
                // prefab: impact/secondary prefabs inherit OnCastComplete -> LaunchProjectile and must not have
                // their cast timer forced open at spawn. They get the near-zero cast time via the component copy.
                //if (!FireballTunerPlugin.IsFireballPrefab(__instance)) return;
                if (!FireballTunerPlugin.IsExactFireballPrefab(__instance)) return;

                EnsureOffset();

                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;

                    float* castTime = (float*)((byte*)p + _castTimeOffset);
                    // [2026-10-01] Was: force to 0. CastTimeCallbackCheck returns immediately when _castTime == 0,
                    // which skipped the cast-complete RPC and the projectile launch entirely. Force a tiny non-zero
                    // value instead so the launch branch still runs (within one frame).
                    //if (*castTime != 0f)
                    if (*castTime != FireballTunerPlugin.InstantCastTime)
                    {
                        // [2026-10-01] Was: *castTime = 0f;
                        //*castTime = 0f;
                        *castTime = FireballTunerPlugin.InstantCastTime;
                        if (FireballTunerPlugin.DiagnosticLogging.Value)
                        {
                            FireballTunerPlugin.Log.LogInfo(
                                $"[CastTimeCallbackCheck] {__instance.GetIl2CppType().Name} _castTime forced to near-zero.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_BaseSpell_CastTimeCallbackCheck] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on the public <c>BaseSpellLibrary.AddStacksToTarget</c>. Every StackingEffect the Fireball
    /// applies (base Burning = 2, Igniting Fireball = +4, Flashheating conversion) flows through here.
    /// We scale only Burning stacks coming from a Fireball projectile prefab so no other fire spell is touched.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), nameof(BaseSpellLibrary.AddStacksToTarget))]
    public static class Patch_BaseSpellLibrary_AddStacksToTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, StackingEffect effect, ref int amount)
        {
            try
            {
                if (__instance == null) return;
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (effect != StackingEffect.Burning) return;
                if (amount <= 0) return;

                float mult = FireballTunerPlugin.BurningMultiplier.Value;
                if (mult <= 0f || mult == 1f) return;

                if (!FireballTunerPlugin.IsFireballPrefab(__instance)) return;

                int newAmount = (int)Math.Round(amount * (double)mult, MidpointRounding.AwayFromZero);
                if (newAmount < 1) newAmount = 1;

                if (FireballTunerPlugin.DiagnosticLogging.Value)
                {
                    FireballTunerPlugin.Log.LogInfo(
                        $"[AddStacksToTarget] {__instance.GetIl2CppType().Name} Burning {amount} -> {newAmount}");
                }

                amount = newAmount;
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_AddStacksToTarget] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>SpellTooltipDatabase.GetLocalizedDescription(Spell)</c>. The in-game description is an
    /// authored localization string (key <c>SPELL_FIREBALL_DESC</c>) that never changes when we tune the
    /// runtime values, so the tooltip would otherwise still advertise the vanilla stats. We append a short
    /// tuned-stats line for the Fireball spell only. This is the single text producer used by the spellbook,
    /// spell catalogue, skill-tree node tooltip, character creation and pet panel.
    /// </summary>
    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedDescription")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedDescription
    {
        public static void Postfix(Spell spell, ref string __result)
        {
            try
            {
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (spell != Spell.Fireball) return;

                string suffix = FireballTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;

                if (FireballTunerPlugin.DiagnosticLogging.Value)
                {
                    FireballTunerPlugin.Log.LogInfo($"[Tooltip] Fireball description -> {__result}");
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_SpellTooltipDatabase_GetLocalizedDescription] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>SpellTooltipDatabase.GetLocalizedEffects(Spell)</c>. Effect rows are authored strings
    /// (e.g. "Burning: 2") that do not follow the runtime values, so the Burning stack count shown in the
    /// Fireball tooltip would stay at the vanilla number. We rebuild the returned list for the Fireball spell
    /// only, scaling any plain-integer effect value by <c>BurningMultiplier</c> (the damage row
    /// ("...: (MF:2.2)") is not an integer and is left untouched). A new list is returned so the cached
    /// entry data is never mutated.
    /// </summary>
    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedEffects")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedEffects
    {
        public static void Postfix(Spell spell, ref Il2CppGenerics.List<string> __result)
        {
            try
            {
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (spell != Spell.Fireball) return;
                if (!FireballTunerPlugin.UpdateBurningStacksInTooltip.Value) return;
                if (__result == null) return;

                float mult = FireballTunerPlugin.BurningMultiplier.Value;
                if (mult <= 0f || mult == 1f) return;

                var updated = new Il2CppGenerics.List<string>();
                bool changed = false;

                for (int i = 0; i < __result.Count; i++)
                {
                    string line = __result[i];
                    string scaled = FireballTunerPlugin.ScaleIntegerEffectLine(line, mult);
                    if (scaled != null)
                    {
                        updated.Add(scaled);
                        changed = true;
                    }
                    else
                    {
                        updated.Add(line);
                    }
                }

                if (!changed) return;

                __result = updated;

                if (FireballTunerPlugin.DiagnosticLogging.Value)
                {
                    FireballTunerPlugin.Log.LogInfo("[Tooltip] Fireball effects:");
                    for (int i = 0; i < updated.Count; i++)
                    {
                        FireballTunerPlugin.Log.LogInfo($"    {updated[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_SpellTooltipDatabase_GetLocalizedEffects] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-14] Postfix on <c>AreaOfEffect.Start</c>. By the time this runs the game has resolved
    /// <c>AreaOfEffect._aoeCollider</c> (assigned in Start), so this is the reliable point to widen the
    /// Fireball explosive impact area. Only FireballExplosiveImpactPrefab instances are affected.
    /// </summary>
    [HarmonyPatch(typeof(AreaOfEffect), "Start")]
    public static class Patch_AreaOfEffect_Start_ExplosiveImpactAoe
    {
        public static void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (!FireballTunerPlugin.Enabled.Value) return;
                FireballTunerPlugin.ApplyExplosiveImpactAoe(__instance);
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_AreaOfEffect_Start_ExplosiveImpactAoe] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-14] Postfix on <c>FireballExplosiveImpactPrefab.OnStart</c>. Pulls surrounding enemies
    /// onto the impact point once when the explosion occurs, mirroring ScatterTrapTuner's on-activate suction
    /// and EchoRiftTuner's on-activate drag.
    /// </summary>
    [HarmonyPatch(typeof(FireballExplosiveImpactPrefab), "OnStart")]
    public static class Patch_FireballExplosiveImpactPrefab_OnStart_Suction
    {
        public static void Postfix(FireballExplosiveImpactPrefab __instance)
        {
            try
            {
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (!FireballTunerPlugin.SuctionEnabled.Value) return;
                if (!FireballTunerPlugin.SuctionOnActivate.Value) return;
                FireballTunerPlugin.ApplySuction(__instance);
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_FireballExplosiveImpactPrefab_OnStart_Suction] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-14] Postfix on <c>FireballExplosiveImpactPrefab.AreaOfEffectStrike</c>. Pulls the struck
    /// enemy toward the impact centre on each strike, mirroring TwisterTuner's on-entry suction and
    /// ScatterTrapTuner's per-tick pull. This creates a continuous "drag" feeling as enemies are hit.
    /// </summary>
    [HarmonyPatch(typeof(FireballExplosiveImpactPrefab), "AreaOfEffectStrike")]
    public static class Patch_FireballExplosiveImpactPrefab_AreaOfEffectStrike_Suction
    {
        public static void Postfix(FireballExplosiveImpactPrefab __instance, ObjectsCommon target)
        {
            try
            {
                if (!FireballTunerPlugin.Enabled.Value) return;
                if (!FireballTunerPlugin.SuctionEnabled.Value) return;
                if (!FireballTunerPlugin.SuctionOnStrike.Value) return;
                if (__instance == null || target == null) return;
                if (target.IsDead()) return;

                var t = __instance.transform;
                if (t == null) return;
                Vector3 cp = t.position;
                Vector2 center = new Vector2(cp.x, cp.y);

                // Use the explosion's own AoE radius as the base grab radius, scaled by RadiusMultiplier.
                float baseRadius = 5f; // vanilla Fireball explosion radius ~5 units
                var col = __instance.GetComponent<Collider2D>();
                if (col != null)
                {
                    var circle = col.TryCast<CircleCollider2D>();
                    if (circle != null) baseRadius = circle.radius;
                }

                float radius = baseRadius * FireballTunerPlugin.SuctionRadiusMultiplier.Value;
                Vector3 ep3 = target.transform.position;
                float dist = Vector2.Distance(new Vector2(ep3.x, ep3.y), center);
                if (dist <= 0.05f || dist > radius) return;

                float dashTime = FireballTunerPlugin.SuctionDashTime.Value;
                float distMult = FireballTunerPlugin.SuctionDistanceMultiplier.Value;

                // Same shape as BlackholePrefab.AreaOfEffectStrike / ScatterTrapTuner.ApplySuction:
                //   DashTargetToPosition(target, center, time, distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot)
                __instance.DashTargetToPosition(target, center, dashTime, dist * distMult, false, false, false, null);

                if (FireballTunerPlugin.DiagnosticLogging.Value)
                {
                    FireballTunerPlugin.Log.LogInfo($"[ExplosiveImpactSuction] on-strike dragged enemy toward impact ({center.x:F1}, {center.y:F1}), dist {dist:F1}");
                }
            }
            catch (Exception ex)
            {
                FireballTunerPlugin.Log?.LogError($"[Patch_FireballExplosiveImpactPrefab_AreaOfEffectStrike_Suction] {ex}");
            }
        }
    }
}
