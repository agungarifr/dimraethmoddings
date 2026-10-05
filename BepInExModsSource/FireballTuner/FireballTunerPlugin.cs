using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;

namespace FireballTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that tunes the vanilla Fireball starting skill:
    ///   * Max range      -> RangeMultiplier (default x3)
    ///   * Burning stacks -> BurningMultiplier (default x3)
    ///   * Projectile speed -> SpeedMultiplier (default x2)
    ///   * Cast time      -> InstantCast (default on, CastTime = near-zero => click-trigger cast, no wind-up)
    ///
    /// The mod only touches the Fireball spell. Every lever is behind a config entry so it can be
    /// reverted without rebuilding. Co-op: run the SAME config on host and client so both peers
    /// agree on the numbers (range/cast are both computed locally, burning is applied through the
    /// shared BaseSpellLibrary.AddStacksToTarget path).
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class FireballTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.fireballtuner";
        public const string NAME = "Fireball Tuner";
        public const string VERSION = "1.0.0";

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

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a line to the Fireball spell description (tooltip/skill tree/character creation) listing the tuned values: instant cast, range, burning, projectile speed. (Default: true)");

            UpdateBurningStacksInTooltip = Config.Bind("Tooltip", "UpdateBurningStacks", true,
                "Rewrite the 'Burning: N' line in the Fireball tooltip so the displayed stack count matches BurningMultiplier. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogFireballValues", false,
                "Log the Fireball's radius/cast-time and burning stack changes to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_FireballPrefab_OnStart));
            PatchOrLog(harmony, typeof(Patch_FireballPrefab_OnStart_Speed));
            PatchOrLog(harmony, typeof(Patch_Spells_Cast));
            PatchOrLog(harmony, typeof(Patch_BaseSpell_CastTimeCallbackCheck));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToTarget));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedEffects));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"RangeMultiplier: {RangeMultiplier.Value}");
            Log.LogInfo($"BurningMultiplier: {BurningMultiplier.Value}");
            Log.LogInfo($"SpeedMultiplier: {SpeedMultiplier.Value}");
            Log.LogInfo($"InstantCast: {InstantCast.Value}");
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
}
