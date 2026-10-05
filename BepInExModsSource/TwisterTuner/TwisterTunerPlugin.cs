using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace TwisterTuner
{
    /// <summary>
    /// BepInEx 6 (IL2CPP) plugin that tunes the Minotaur <b>Twister</b> skill-tree upgrade of the
    /// <b>Vortex</b> spell (Spell.Vortex = 99).
    ///
    /// Twister is NOT a separate spell: it is the boolean <c>VortexPrefab._twister</c> (offset 0x300),
    /// set in <c>VortexPrefab.OnStart</c> from <c>BaseSpellLibrary.GetSpellUpgradeLevel("Twister")</c>.
    /// When true the vanilla prefab:
    ///   * skips the forward caster dash (DashCasterForwardToPosition),
    ///   * sets <c>_followOwner = Middle (1)</c> so the vortex follows the player ("move while spinning"),
    ///   * activates child(3) and SendMessage("Follow", caster.transform),
    ///   * sets duration = base + (Twister Duration Increase level) and plays the spin anim at speed 1.0,
    ///   * divides every tick's damage by the built-in <c>twisterDivisor</c> (== half of base Vortex damage),
    ///   * ticks damage once per second via <c>AreaOfEffectOn1Second</c>.
    ///
    /// This mod keeps all of that and exposes the requested levers:
    ///   * Duration        (default 6.0s; vanilla Twister base is ~4.0s)
    ///   * TicksMultiplier (default 2x -> a damage tick every 0.5s instead of 1.0s)
    ///   * TargetDamageFraction (default 0.5 -> half of vanilla Vortex damage; vanilla Twister already built this in)
    ///   * ForceTwister    (grants the Twister behaviour even without the skill node)
    ///   * NormalMoveSpeed (keeps the follow/no-dash stance intact)
    ///   * optional Black-Hole-style suction that drags nearby enemies into the vortex center.
    ///
    /// All logic is read from the shipped disassembly (modding/DecompilerTool/isil_out/...),
    /// never from the signature-only dumps.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class TwisterTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.twistertuner";
        public const string NAME = "Twister Tuner";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static TwisterTunerPlugin Instance;

        // ---- General ----
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> ForceTwister;
        public static ConfigEntry<bool> DiagnosticLogging;

        // ---- Twister tuning ----
        public static ConfigEntry<float> Duration;
        public static ConfigEntry<bool> SetCastTimeEqualToDuration;
        public static ConfigEntry<float> TicksMultiplier;
        public static ConfigEntry<bool> EnsureNormalMoveSpeed;
        public static ConfigEntry<float> BuiltInTwisterDivisor;
        public static ConfigEntry<float> TargetDamageFraction;

        // ---- Area of effect ----
        public static ConfigEntry<float> AoeRadiusMultiplier;

        // ---- Movement ----
        public static ConfigEntry<float> MoveSpeedMultiplier;

        // ---- Black Hole style suction (DashTargetToPosition, like BlackholePrefab) ----
        public static ConfigEntry<bool> SuctionEnabled;
        public static ConfigEntry<float> SuctionRadius;
        public static ConfigEntry<float> SuctionDashTime;
        public static ConfigEntry<bool> SuctionOnEntry;
        public static ConfigEntry<bool> SuctionPerTick;

        // ---- Tooltip ----
        public static ConfigEntry<bool> UpdateTooltip;

        // ------------------------------------------------------------------
        // Field offsets (verified against this build's disassembly; resolved
        // from IL2CPP metadata at runtime where possible).
        // ------------------------------------------------------------------
        internal static int TwisterOffset = 0x300;          // VortexPrefab._twister (bool)
        internal static int MaterialOffset = 0x2E8;         // VortexPrefab._material
        internal static int StableVortexOffset = 0x301;     // VortexPrefab._stableVortex (bool)
        internal static int TwisterDurationIncreaseOffset = 0x310; // VortexPrefab._twisterDurationIncrease (int)
        internal static int MaxTimeAliveOffset = 0x1C8;     // BaseSpellLibrary._maxTimeAlive (float)
        internal static int CastTimeOffset = 0x1C0;         // BaseSpellLibrary._castTime (float)
        internal static int FollowOwnerOffset = 0x1DC;      // BaseSpellLibrary._followOwner (FollowCasterType)
        internal static int TargetTimesOffset = 0x290;      // AreaOfEffect._targetTimes (Dictionary<ObjectsCommon,float>)
        internal static int AoeColliderOffset = 0x288;      // AreaOfEffect._aoeCollider (Collider2D)

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the Twister spell is left at vanilla values. (Default: true)");

            ForceTwister = Config.Bind("General", "ForceTwister", true,
                "Report the 'Twister' skill upgrade as owned so the Twister behaviour (no dash, move while " +
                "spinning, follow, spin anim speed 1.0) is always active on the Vortex spell, even without the " +
                "skill node. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log Twister duration, tick scaling, damage scaling and suction events to the BepInEx console. " +
                "(Default: false)");

            Duration = Config.Bind("Twister", "Duration", 6.0f,
                "Total lifetime of the Twister in seconds. Vanilla Twister base is ~4.0s. (Default: 6.0)");

            SetCastTimeEqualToDuration = Config.Bind("Twister", "SetCastTimeEqualToDuration", true,
                "Also set BaseSpellLibrary._castTime (0x1C0) to Duration, matching what vanilla Twister does in " +
                "OnStart. (Default: true)");

            TicksMultiplier = Config.Bind("Twister", "TicksMultiplier", 2.0f,
                "Tick-rate multiplier. 2.0 = a damage tick every 0.5s instead of 1.0s (2x ticks). (Default: 2.0)");

            EnsureNormalMoveSpeed = Config.Bind("Twister", "EnsureNormalMoveSpeed", true,
                "Force FollowCasterType.Middle (1) and keep the no-dash stance so the player moves at normal " +
                "speed while the vortex spins. (Default: true)");

            BuiltInTwisterDivisor = Config.Bind("Twister", "BuiltInTwisterDivisor", 2.0f,
                "The game's built-in damage divisor applied to Twister ticks (the disassembly labels it " +
                "'twisterDivisor'). Vanilla Twister already deals 0.5x base Vortex damage, so this is 2.0. " +
                "Only change it if a future build changes that constant. (Default: 2.0)");

            TargetDamageFraction = Config.Bind("Twister", "TargetDamageFraction", 0.5f,
                "The final Twister tick damage as a fraction of vanilla Vortex damage. 0.5 (default) = half of " +
                "vanilla Vortex. The mod compensates for the built-in divisor so this is the true final fraction. " +
                "(Default: 0.5)");

            AoeRadiusMultiplier = Config.Bind("AreaOfEffect", "RadiusMultiplier", 1.3f,
                "Scale the Twister's area of effect. 1.3 = +30%. Applied by scaling the AoE trigger " +
                "collider's own geometry (radius/size/points) directly (AreaOfEffect._aoeCollider), because " +
                "scaling the spell's transform proved not to widen the trigger in-game. The visual transform is " +
                "scaled too, and the collider geometry is only multiplied again if the collider is not already " +
                "following that transform (measured at runtime). Also scales the suction Radius. (Default: 1.3)");

            MoveSpeedMultiplier = Config.Bind("Movement", "MoveSpeedMultiplier", 1.5f,
                "While a Twister cast by the player is active, multiply that player's movement speed by this. " +
                "1.5 = +50% of the current value. Applied as a postfix on Formulas.CalculatePlayerSpeed. " +
                "Set 1.0 to disable. (Default: 1.5)");

            SuctionEnabled = Config.Bind("BlackHoleSuction", "Enabled", true,
                "Give Twister a Black-Hole-style pull using the game's own DashTargetToPosition primitive - the " +
                "exact call BlackholePrefab.AreaOfEffectStrike uses (see docs/BlackHole_Dragging_Mechanism.md). " +
                "Enemies inside Radius are dashed to the vortex center. (Default: true)");

            SuctionRadius = Config.Bind("BlackHoleSuction", "Radius", 6.0f,
                "Radius (world units) around the vortex center in which enemies are pulled. (Default: 6.0)");

            SuctionDashTime = Config.Bind("BlackHoleSuction", "DashTime", 0.3f,
                "Dash duration in seconds passed to DashTargetToPosition (time to slide to the center). " +
                "Vanilla Black Hole uses 0.3f (constant 0x18465DCF0, resolved in " +
                "docs/BlackHole_Dragging_Mechanism.md §5.3). (Default: 0.3)");

            SuctionOnEntry = Config.Bind("BlackHoleSuction", "OnEntry", true,
                "Dash an enemy toward the center the moment it enters the vortex (matches Black Hole). " +
                "(Default: true)");

            SuctionPerTick = Config.Bind("BlackHoleSuction", "PerTick", true,
                "Also dash every enemy in radius toward the center on each damage tick. Repeated pulls read as " +
                "continuous suction; Black Hole itself only pulls on entry. (Default: true)");

            UpdateTooltip = Config.Bind("Tooltip", "UpdateDescription", true,
                "Append a short summary of the tuned Twister values to the Vortex spell description. (Default: true)");

            ResolveOffsets();

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetSpellUpgradeLevel));
            PatchOrLog(harmony, typeof(Patch_VortexPrefab_OnStart));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_GetBaseDamage));
            PatchOrLog(harmony, typeof(Patch_AreaOfEffect_UpdateTargetTimes));
            // [2026-10-04 16:05] Patch_AreaOfEffect_Update (per-frame NavMesh suction) is no longer registered:
            // the pull now uses the game's own DashTargetToPosition on entry + per tick. The class is kept
            // commented below for reference.
            //PatchOrLog(harmony, typeof(Patch_AreaOfEffect_Update));
            PatchOrLog(harmony, typeof(Patch_VortexPrefab_AreaOfEffectStrike));
            PatchOrLog(harmony, typeof(Patch_VortexPrefab_AreaOfEffectOn1Second));
            PatchOrLog(harmony, typeof(Patch_AreaOfEffect_Start));
            PatchOrLog(harmony, typeof(Patch_Formulas_CalculatePlayerSpeed));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}  ForceTwister: {ForceTwister.Value}");
            Log.LogInfo($"Duration: {Duration.Value}s  TicksMultiplier: {TicksMultiplier.Value}x");
            Log.LogInfo($"TargetDamageFraction: {TargetDamageFraction.Value} (built-in divisor {BuiltInTwisterDivisor.Value})");
            Log.LogInfo($"EnsureNormalMoveSpeed: {EnsureNormalMoveSpeed.Value}");
            Log.LogInfo($"AoeRadiusMultiplier: {AoeRadiusMultiplier.Value} (suction radius {SuctionRadius.Value} -> {SuctionRadius.Value * AoeRadiusMultiplier.Value})");
            Log.LogInfo($"MoveSpeedMultiplier: {MoveSpeedMultiplier.Value} (while a Twister is active)");
            Log.LogInfo($"BlackHoleSuction: {SuctionEnabled.Value} (r={SuctionRadius.Value}, dashTime={SuctionDashTime.Value}, onEntry={SuctionOnEntry.Value}, perTick={SuctionPerTick.Value})");
            Log.LogInfo("=================================================");
        }

        // ------------------------------------------------------------------
        // Offset resolution + helpers
        // ------------------------------------------------------------------

        private static void ResolveOffsets()
        {
            try
            {
                var vortexClass = Il2CppClassPointerStore<VortexPrefab>.NativeClassPtr;
                if (vortexClass != IntPtr.Zero)
                {
                    TwisterOffset = GetFieldOffset(vortexClass, "_twister", TwisterOffset);
                    MaterialOffset = GetFieldOffset(vortexClass, "_material", MaterialOffset);
                    StableVortexOffset = GetFieldOffset(vortexClass, "_stableVortex", StableVortexOffset);
                    TwisterDurationIncreaseOffset = GetFieldOffset(vortexClass, "_twisterDurationIncrease", TwisterDurationIncreaseOffset);
                }

                var aoeClass = Il2CppClassPointerStore<AreaOfEffect>.NativeClassPtr;
                if (aoeClass != IntPtr.Zero)
                {
                    TargetTimesOffset = GetFieldOffset(aoeClass, "_targetTimes", TargetTimesOffset);
                    AoeColliderOffset = GetFieldOffset(aoeClass, "_aoeCollider", AoeColliderOffset);
                }

                var bslClass = Il2CppClassPointerStore<BaseSpellLibrary>.NativeClassPtr;
                if (bslClass != IntPtr.Zero)
                {
                    MaxTimeAliveOffset = GetFieldOffset(bslClass, "_maxTimeAlive", MaxTimeAliveOffset);
                    CastTimeOffset = GetFieldOffset(bslClass, "_castTime", CastTimeOffset);
                    FollowOwnerOffset = GetFieldOffset(bslClass, "_followOwner", FollowOwnerOffset);
                }

                Log.LogInfo($"[Offsets] _twister=0x{TwisterOffset:X} _maxTimeAlive=0x{MaxTimeAliveOffset:X} " +
                            $"_castTime=0x{CastTimeOffset:X} _followOwner=0x{FollowOwnerOffset:X} " +
                            $"_targetTimes=0x{TargetTimesOffset:X} _aoeCollider=0x{AoeColliderOffset:X}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[ResolveOffsets] Using verified fallback offsets: {ex.Message}");
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

        /// <summary>True when the instance is a VortexPrefab whose <c>_twister</c> flag is set.</summary>
        internal static unsafe bool IsTwister(VortexPrefab vp)
        {
            if (vp == null) return false;
            IntPtr p = vp.Pointer;
            if (p == IntPtr.Zero) return false;
            return *(byte*)((byte*)p + TwisterOffset) != 0;
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

        // ------------------------------------------------------------------
        // Black Hole style suction
        // ------------------------------------------------------------------

        // [2026-10-04 16:05] OBSOLETE - replaced by the DashTargetToPosition-based ApplySuction below.
        // The original continuous NavMeshAgent.Move pull fought the AI pathfinder and ignored the game's own
        // dash rules (Immune / DisplaceExempt / Rooted / server authority). After the Black Hole analysis in
        // docs/BlackHole_Dragging_Mechanism.md we use the game's own primitive instead. Kept for reference.
        /*
        /// <summary>
        /// Continuous gravitational pull of nearby enemies toward the vortex center.
        /// Center = the VortexPrefab transform, which follows the caster (FollowCasterType.Middle) once the
        /// Twister path is active. Enemies whose NavMeshAgent is usable are moved with agent.Move so we do not
        /// fight the AI pathfinder; otherwise their transform is displaced directly.
        /// </summary>
        internal static void ApplySuction(VortexPrefab vp)
        {
            if (!Enabled.Value || !SuctionEnabled.Value) return;
            if (!IsTwister(vp)) return;

            try
            {
                if (vp.transform == null) return;
                Vector3 cp = vp.transform.position;
                Vector2 center = new Vector2(cp.x, cp.y);

                float radius = SuctionRadius.Value;
                if (radius <= 0.1f) return;

                var enemies = vp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                float dt = Time.deltaTime;
                float speed = SuctionSpeed.Value;
                float stop = SuctionStopDistance.Value;
                int moved = 0;

                int count = enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    ObjectsCommon enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    Vector3 ep3 = enemy.transform.position;
                    Vector2 ep = new Vector2(ep3.x, ep3.y);
                    float dist = Vector2.Distance(ep, center);
                    if (dist <= stop || dist > radius) continue;

                    float step = Math.Min(speed * dt, dist - stop);
                    if (step <= 0f) continue;

                    Vector2 dir = (center - ep) / dist;
                    Vector3 move = new Vector3(dir.x * step, dir.y * step, 0f);

                    var agent = enemy.Agent;
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.Move(move);
                    }
                    else
                    {
                        enemy.transform.position = ep3 + move;
                    }

                    moved++;
                }

                if (DiagnosticLogging.Value && moved > 0)
                {
                    Log.LogInfo($"[Suction] dragged {moved} enemies toward twister center ({center.x:F1}, {center.y:F1}).");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }
        */

        /// <summary>
        /// Drag every live enemy within SuctionRadius toward the vortex center using the game's own
        /// <c>BaseSpellLibrary.DashTargetToPosition</c> - the exact primitive
        /// <c>BlackholePrefab.AreaOfEffectStrike</c> uses (docs/BlackHole_Dragging_Mechanism.md §2.2/§3.1).
        /// <c>distance</c> is the enemy's current distance to the center, so each call slides it onto the
        /// center (Black Hole's whole "suction" trick). The game's DashTargetToPosition itself enforces
        /// <c>ObjectsCommon.Immune</c>, <c>MonsterBehaviourLibrary.DisplaceExempt</c>, Rooted effects and
        /// server authority, so we do not re-implement those rules.
        /// Center = the VortexPrefab root transform, which <c>BaseSpell.FollowOwnerCheck</c> moves to the
        /// player's middle every frame while <c>_followOwner == FollowCasterType.Middle</c>.
        /// </summary>
        internal static void ApplySuction(VortexPrefab vp)
        {
            if (!Enabled.Value || !SuctionEnabled.Value) return;
            if (!IsTwister(vp)) return;

            try
            {
                if (vp.transform == null) return;
                Vector3 cp = vp.transform.position;
                Vector2 center = new Vector2(cp.x, cp.y);

                float radius = SuctionRadius.Value * AoeRadiusMultiplier.Value;
                if (radius <= 0.1f) return;

                var enemies = vp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                float dashTime = SuctionDashTime.Value;
                int pulled = 0;
                int count = enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    ObjectsCommon enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    Vector3 ep3 = enemy.transform.position;
                    float dist = Vector2.Distance(new Vector2(ep3.x, ep3.y), center);
                    if (dist <= 0.05f) continue;

                    // Same shape as BlackholePrefab.AreaOfEffectStrike:
                    //   DashTargetToPosition(target, center, time, distance = current distance, false, false, false, null)
                    vp.DashTargetToPosition(enemy, center, dashTime, dist, false, false, false, null);
                    pulled++;
                }

                if (DiagnosticLogging.Value && pulled > 0)
                {
                    Log.LogInfo($"[Suction] DashTargetToPosition pulled {pulled} enemies toward the twister center ({center.x:F1}, {center.y:F1}).");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuction] {ex}");
            }
        }

        // [2026-10-04 16:05] OBSOLETE - folded into the new DashTargetToPosition-based ApplySuction above.
        // This was the old optional per-tick DashTargetToPosition pulse; kept for reference.
        /*
        /// <summary>
        /// One DashTargetToPosition tug per enemy toward the vortex center (the exact primitive Black Hole
        /// uses in BlackholePrefab.AreaOfEffectStrike). Kept optional because it is server-authoritative and
        /// can no-op in some network contexts.
        /// </summary>
        internal static void ApplySuctionPulse(VortexPrefab vp)
        {
            if (!Enabled.Value || !SuctionEnabled.Value || !SuctionUseVanillaDash.Value) return;
            if (!IsTwister(vp)) return;

            try
            {
                if (vp.transform == null) return;
                Vector3 cp = vp.transform.position;
                Vector2 center = new Vector2(cp.x, cp.y);

                float radius = SuctionRadius.Value;
                var enemies = vp.GetAllEnemiesInRangeOfPosition(center, radius);
                if (enemies == null) return;

                int count = enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    ObjectsCommon enemy = enemies[i];
                    if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
                    if (enemy.IsDead()) continue;

                    Vector3 ep3 = enemy.transform.position;
                    float dist = Vector2.Distance(new Vector2(ep3.x, ep3.y), center);
                    if (dist <= 0.05f) continue;

                    // Same shape as BlackholePrefab.AreaOfEffectStrike:
                    //   DashTargetToPosition(target, spellPosition, time, distance, useMiddleOfSprite, ...)
                    vp.DashTargetToPosition(enemy, center, 0f, dist, false, false, false, null);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ApplySuctionPulse] {ex}");
            }
        }
        */

        /// <summary>Short summary appended to the Vortex tooltip describing the tuned values.</summary>
        internal static string BuildTooltipSuffix()
        {
            var parts = new System.Collections.Generic.List<string>();
            parts.Add($"duration {Duration.Value.ToString("0.##", CultureInfo.InvariantCulture)}s");
            if (TicksMultiplier.Value != 1f)
                parts.Add($"ticks x{TicksMultiplier.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
            if (TargetDamageFraction.Value != 1f)
                parts.Add($"damage x{TargetDamageFraction.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
            if (AoeRadiusMultiplier.Value != 1f)
                parts.Add($"area x{AoeRadiusMultiplier.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
            if (MoveSpeedMultiplier.Value != 1f)
                parts.Add($"move x{MoveSpeedMultiplier.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
            if (SuctionEnabled.Value)
                parts.Add("pulls enemies in");

            return "\n\n[Twister Tuner] " + string.Join(", ", parts) + ".";
        }

        // ------------------------------------------------------------------
        // Active-Twister tracking (drives the player movement-speed bonus)
        // ------------------------------------------------------------------

        /// <summary>Owner (player) pointer -> Time.time after which that Twister has ended.</summary>
        private static readonly System.Collections.Generic.Dictionary<IntPtr, float> ActiveTwisters = new();
        private static float _lastTwisterPurge;

        internal static void RegisterTwister(ObjectsCommon owner)
        {
            if (owner == null || owner.Pointer == IntPtr.Zero) return;
            float duration = Duration.Value > 0f ? Duration.Value : 6f;
            ActiveTwisters[owner.Pointer] = Time.time + duration + 0.25f;
            PurgeTwisters();
        }

        internal static bool IsPlayerTwisterActive(Player player)
        {
            if (player == null || player.Pointer == IntPtr.Zero) return false;
            if (!ActiveTwisters.TryGetValue(player.Pointer, out float expiry)) return false;
            if (Time.time >= expiry)
            {
                ActiveTwisters.Remove(player.Pointer);
                return false;
            }
            return true;
        }

        private static void PurgeTwisters()
        {
            if (Time.time - _lastTwisterPurge < 2f && ActiveTwisters.Count < 64) return;
            _lastTwisterPurge = Time.time;
            var dead = new System.Collections.Generic.List<IntPtr>();
            foreach (var kv in ActiveTwisters)
            {
                if (Time.time >= kv.Value) dead.Add(kv.Key);
            }
            for (int i = 0; i < dead.Count; i++) ActiveTwisters.Remove(dead[i]);
        }

        // ------------------------------------------------------------------
        // AoE collider geometry scaling
        // ------------------------------------------------------------------

        /// <summary>
        /// Widen a <see cref="Collider2D"/>'s own shape by <paramref name="mult"/>. Unlike scaling the
        /// Transform, this is guaranteed to change the collider's world bounds (the trigger area) on its own,
        /// independent of whether this build's Collider2D follows the transform scale.
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

        private static float ColliderExtent(Collider2D col)
        {
            if (col == null) return 0f;
            var b = col.bounds;
            return Mathf.Max(b.extents.x, b.extents.y);
        }

        /// <summary>
        /// Apply the AoE size multiplier to a Twister's trigger collider. Scales the spell transform (visuals)
        /// and then, only if the collider's world bounds did not follow that transform, multiplies the collider
        /// geometry too, so the functional area ends up exactly <c>AoeRadiusMultiplier</c> regardless of whether
        /// this build's Collider2D scales with its Transform.
        /// </summary>
        internal static void ApplyAoeScaling(AreaOfEffect aoe)
        {
            if (aoe == null || aoe.Pointer == IntPtr.Zero) return;
            VortexPrefab vp = aoe.TryCast<VortexPrefab>();
            if (vp == null || !IsTwister(vp)) return;

            float mult = AoeRadiusMultiplier.Value;
            if (mult <= 0f || Math.Abs(mult - 1f) < 0.0001f) return;

            Collider2D col = aoe.GetComponent<Collider2D>();
            if (col == null) col = aoe.GetComponentInChildren<Collider2D>();
            if (col == null)
            {
                Log?.LogWarning("[AoE] No Collider2D found on Twister; cannot widen the trigger area.");
                return;
            }

            float e0 = ColliderExtent(col);

            // If the collider has usable bounds, scale the transform and check whether the collider followed.
            // Otherwise (e.g. disabled/zero-size at Start) leave the transform alone and scale the shape only,
            // which is always exactly x mult in world space.
            if (e0 > 0.0001f)
            {
                var t = aoe.transform;
                var s = t.localScale;
                t.localScale = new Vector3(s.x * mult, s.y * mult, s.z);
                float e1 = ColliderExtent(col);

                bool followed = e1 > e0 * (1f + (mult - 1f) * 0.5f);
                if (!followed) ScaleColliderGeometry(col, mult);

                Log?.LogInfo($"[AoE] {col.GetType().Name} extent {e0:F3} -> {e1:F3} " +
                             $"(transformFollowed={followed}, geometry {(followed ? "unchanged" : "x" + mult.ToString("0.###", CultureInfo.InvariantCulture))})");
            }
            else
            {
                ScaleColliderGeometry(col, mult);
                Log?.LogInfo($"[AoE] {col.GetType().Name} had no measurable bounds; scaled geometry x{mult.ToString("0.###", CultureInfo.InvariantCulture)}.");
            }
        }
    }

    /// <summary>
    /// Prefix on BaseSpellLibrary.GetSpellUpgradeLevel(string). VortexPrefab.OnStart queries this for
    /// "Twister" to decide <c>_twister</c>. Forcing the level to >= 1 makes vanilla take the Twister branch
    /// (no dash, follow caster, spin anim 1.0) even when the skill node is not owned, so all downstream
    /// tuning applies to a consistent base.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetSpellUpgradeLevel")]
    public static class Patch_BaseSpellLibrary_GetSpellUpgradeLevel
    {
        public static void Prefix(BaseSpellLibrary __instance, string spellUpgradeName, ref int __result)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                if (!TwisterTunerPlugin.ForceTwister.Value) return;
                if (spellUpgradeName != "Twister") return;

                if (__result < 1)
                {
                    __result = 1;
                    if (TwisterTunerPlugin.DiagnosticLogging.Value)
                    {
                        TwisterTunerPlugin.Log.LogInfo("[GetSpellUpgradeLevel] 'Twister' upgrade level forced to 1.");
                    }
                }
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_GetSpellUpgradeLevel] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on VortexPrefab.OnStart. Vanilla has already decided the Twister branch and set
    /// _maxTimeAlive = base + duration-increase. We override the lifetime to the configured Duration
    /// (and, matching vanilla, _castTime too) and re-assert the follow/no-dash stance.
    /// </summary>
    [HarmonyPatch(typeof(VortexPrefab), "OnStart")]
    public static class Patch_VortexPrefab_OnStart
    {
        public static unsafe void Postfix(VortexPrefab __instance)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                byte* ptr = (byte*)__instance.Pointer;

                // If the ForceTwister prefix did not apply, still force the flag so our tuning is meaningful.
                if (TwisterTunerPlugin.ForceTwister.Value && *(byte*)(ptr + TwisterTunerPlugin.TwisterOffset) == 0)
                {
                    *(byte*)(ptr + TwisterTunerPlugin.TwisterOffset) = 1;
                }

                if (!TwisterTunerPlugin.IsTwister(__instance)) return;

                float duration = TwisterTunerPlugin.Duration.Value;

                float* pMaxTime = (float*)(ptr + TwisterTunerPlugin.MaxTimeAliveOffset);
                float oldMax = *pMaxTime;
                if (duration > 0f)
                {
                    *pMaxTime = duration;
                    if (TwisterTunerPlugin.SetCastTimeEqualToDuration.Value)
                    {
                        *(float*)(ptr + TwisterTunerPlugin.CastTimeOffset) = duration;
                    }
                }

                if (TwisterTunerPlugin.EnsureNormalMoveSpeed.Value)
                {
                    // FollowCasterType.Middle = 1 (the value vanilla Twister writes at OnStart).
                    *(int*)(ptr + TwisterTunerPlugin.FollowOwnerOffset) = 1;
                }

                // [2026-10-05 12:00] OBSOLETE - the AoE is no longer widened by scaling the spell transform here.
                // In-game that did not widen the trigger area, so widening now happens in
                // Patch_AreaOfEffect_Start -> ApplyAoeScaling, which scales the AoE collider's own geometry
                // (radius/size/points) and only relies on the transform when the collider actually follows it.
                /*
                float aoeMult = TwisterTunerPlugin.AoeRadiusMultiplier.Value;
                Vector3 baseScale = __instance.transform.localScale;
                if (aoeMult > 0f && Math.Abs(aoeMult - 1f) > 0.0001f)
                {
                    __instance.transform.localScale = new Vector3(
                        baseScale.x * aoeMult, baseScale.y * aoeMult, baseScale.z);
                }
                */

                // Track the caster so Patch_Formulas_CalculatePlayerSpeed can boost their movement speed
                // while this Twister is alive.
                TwisterTunerPlugin.RegisterTwister(__instance.GetOwner());

                if (TwisterTunerPlugin.DiagnosticLogging.Value)
                {
                    TwisterTunerPlugin.Log.LogInfo(
                        $"[OnStart] Twister lifetime {oldMax:F2}s -> {*pMaxTime:F2}s " +
                        $"(followOwner={*(int*)(ptr + TwisterTunerPlugin.FollowOwnerOffset)})");
                }
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_VortexPrefab_OnStart] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on BaseSpellLibrary.GetBaseDamage(float). ApplyVortexDamage calls this to obtain the tick's
    /// starting damage, then multiplies by the damage-increase and divides by the built-in twisterDivisor
    /// (2.0). To make the final tick damage equal <c>TargetDamageFraction</c> of vanilla Vortex damage we
    /// scale the base by <c>BuiltInTwisterDivisor * TargetDamageFraction</c> (default 2.0 * 0.5 = 1.0, i.e.
    /// unchanged: vanilla Twister is already half of base Vortex). Only Twister Vortex instances are touched.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), "GetBaseDamage")]
    public static class Patch_BaseSpellLibrary_GetBaseDamage
    {
        public static void Postfix(BaseSpellLibrary __instance, ref float __result)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                if (__instance == null) return;

                VortexPrefab vp = __instance.TryCast<VortexPrefab>();
                if (vp == null) return;
                if (!TwisterTunerPlugin.IsTwister(vp)) return;

                float factor = TwisterTunerPlugin.BuiltInTwisterDivisor.Value * TwisterTunerPlugin.TargetDamageFraction.Value;
                if (factor <= 0f || Math.Abs(factor - 1f) < 0.0001f) return;

                float old = __result;
                __result = old * factor;

                if (TwisterTunerPlugin.DiagnosticLogging.Value)
                {
                    TwisterTunerPlugin.Log.LogInfo($"[GetBaseDamage] Twister base damage {old:F2} -> {__result:F2} (x{factor:F3})");
                }
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_GetBaseDamage] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on AreaOfEffect.UpdateTargetTimes. Vanilla accumulates Time.deltaTime per target in
    /// <c>_targetTimes</c> and fires AreaOfEffectOn1Second when the integer second rolls over. We add
    /// (TicksMultiplier - 1) * deltaTime to every entry, so the accumulator advances at TicksMultiplier
    /// speed and vanilla's own dispatch produces a tick every 1.0 / TicksMultiplier seconds.
    /// Runs only for Twister VortexPrefabs.
    /// </summary>
    [HarmonyPatch(typeof(AreaOfEffect), "UpdateTargetTimes")]
    public static class Patch_AreaOfEffect_UpdateTargetTimes
    {
        public static unsafe void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                float mult = TwisterTunerPlugin.TicksMultiplier.Value;
                if (mult <= 1f) return;
                if (__instance == null || __instance.Pointer == IntPtr.Zero) return;

                VortexPrefab vp = __instance.TryCast<VortexPrefab>();
                if (vp == null) return;
                if (!TwisterTunerPlugin.IsTwister(vp)) return;

                IntPtr p = vp.Pointer;
                IntPtr dictPtr = *(IntPtr*)((byte*)p + TwisterTunerPlugin.TargetTimesOffset);
                if (dictPtr == IntPtr.Zero) return;

                var dict = new Il2CppSystem.Collections.Generic.Dictionary<ObjectsCommon, float>(dictPtr);

                // Collect keys first: setting a value on an existing key is not a structural change, but
                // collecting keeps us safe against any interop enumerator quirks.
                var keys = new System.Collections.Generic.List<ObjectsCommon>(dict.Count);
                var en = dict.GetEnumerator();
                while (en.MoveNext())
                {
                    var key = en.Current.Key;
                    if (key != null && key.Pointer != IntPtr.Zero) keys.Add(key);
                }

                float extra = Time.deltaTime * (mult - 1f);
                if (extra <= 0f) return;

                for (int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    float cur = dict[key];
                    dict[key] = cur + extra;
                }
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_AreaOfEffect_UpdateTargetTimes] {ex}");
            }
        }
    }

    // [2026-10-04 16:05] OBSOLETE - the per-frame NavMesh suction hook was replaced by the
    // DashTargetToPosition primitive applied on entry (Patch_VortexPrefab_AreaOfEffectStrike) and per tick
    // (Patch_VortexPrefab_AreaOfEffectOn1Second). Not registered in Load(). Kept for reference.
    /*
    /// <summary>
    /// Postfix on AreaOfEffect.Update. Drives the optional continuous Black-Hole-style suction for active
    /// Twister instances, once per frame.
    /// </summary>
    [HarmonyPatch(typeof(AreaOfEffect), "Update")]
    public static class Patch_AreaOfEffect_Update
    {
        public static void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value || !TwisterTunerPlugin.SuctionEnabled.Value) return;
                if (__instance == null) return;

                VortexPrefab vp = __instance.TryCast<VortexPrefab>();
                if (vp == null) return;

                TwisterTunerPlugin.ApplySuction(vp);
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_AreaOfEffect_Update] {ex}");
            }
        }
    }
    */

    /// <summary>
    /// Postfix on VortexPrefab.AreaOfEffectStrike. Vanilla calls this when an enemy ENTERS the vortex AoE
    /// (its override applies the on-entry damage). We additionally dash that entering enemy toward the
    /// center, mirroring BlackholePrefab.AreaOfEffectStrike (docs/BlackHole_Dragging_Mechanism.md §2).
    /// </summary>
    [HarmonyPatch(typeof(VortexPrefab), "AreaOfEffectStrike")]
    public static class Patch_VortexPrefab_AreaOfEffectStrike
    {
        public static void Postfix(VortexPrefab __instance, ObjectsCommon target)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value || !TwisterTunerPlugin.SuctionEnabled.Value) return;
                if (!TwisterTunerPlugin.SuctionOnEntry.Value) return;
                if (__instance == null || target == null) return;
                if (!TwisterTunerPlugin.IsTwister(__instance)) return;
                if (target.IsDead()) return;

                Vector3 cp = __instance.transform.position;
                Vector2 center = new Vector2(cp.x, cp.y);
                Vector3 ep3 = target.transform.position;
                float dist = Vector2.Distance(new Vector2(ep3.x, ep3.y), center);
                if (dist <= 0.05f) return;

                __instance.DashTargetToPosition(target, center, TwisterTunerPlugin.SuctionDashTime.Value,
                    dist, false, false, false, null);
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_VortexPrefab_AreaOfEffectStrike] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on VortexPrefab.AreaOfEffectOn1Second. Every damage tick, dash all enemies in radius toward
    /// the vortex center (repeated DashTargetToPosition calls read as continuous suction; Black Hole itself
    /// only pulls on entry, see docs/BlackHole_Dragging_Mechanism.md §6.3).
    /// </summary>
    [HarmonyPatch(typeof(VortexPrefab), "AreaOfEffectOn1Second")]
    public static class Patch_VortexPrefab_AreaOfEffectOn1Second
    {
        public static void Postfix(VortexPrefab __instance)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value || !TwisterTunerPlugin.SuctionEnabled.Value) return;
                if (!TwisterTunerPlugin.SuctionPerTick.Value) return;
                if (__instance == null) return;

                TwisterTunerPlugin.ApplySuction(__instance);
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_VortexPrefab_AreaOfEffectOn1Second] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on SpellTooltipDatabase.GetLocalizedDescription(Spell). Appends a short note to the Vortex
    /// description so the spellbook reflects the tuned Twister behaviour.
    /// </summary>
    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedDescription")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedDescription
    {
        public static void Postfix(Spell spell, ref string __result)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                if (!TwisterTunerPlugin.UpdateTooltip.Value) return;
                if (spell != Spell.Vortex) return;

                string suffix = TwisterTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_SpellTooltipDatabase_GetLocalizedDescription] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on AreaOfEffect.Start. By the time this runs the game has resolved <c>_aoeCollider</c>
    /// (AreaOfEffect._aoeCollider, assigned in Start), so this is the reliable point to widen a Twister's
    /// trigger area. Only Twister VortexPrefabs are affected.
    /// </summary>
    [HarmonyPatch(typeof(AreaOfEffect), "Start")]
    public static class Patch_AreaOfEffect_Start
    {
        public static void Postfix(AreaOfEffect __instance)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                TwisterTunerPlugin.ApplyAoeScaling(__instance);
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_AreaOfEffect_Start] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on Formulas.CalculatePlayerSpeed(Player, Attributes). While a Twister cast by that player is
    /// alive, multiplies the computed speed by MoveSpeedMultiplier (default 1.5 = +50%), giving the requested
    /// faster movement while spinning.
    /// </summary>
    [HarmonyPatch(typeof(Formulas), "CalculatePlayerSpeed")]
    public static class Patch_Formulas_CalculatePlayerSpeed
    {
        public static void Postfix(Player player, ref float __result)
        {
            try
            {
                if (!TwisterTunerPlugin.Enabled.Value) return;
                float mult = TwisterTunerPlugin.MoveSpeedMultiplier.Value;
                if (mult <= 0f || Math.Abs(mult - 1f) < 0.0001f) return;
                if (player == null) return;
                if (!TwisterTunerPlugin.IsPlayerTwisterActive(player)) return;

                __result *= mult;
            }
            catch (Exception ex)
            {
                TwisterTunerPlugin.Log?.LogError($"[Patch_Formulas_CalculatePlayerSpeed] {ex}");
            }
        }
    }
}
