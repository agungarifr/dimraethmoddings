using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace AimedShotChargeTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that tunes the vanilla "Aimed Shot" spell:
    ///   * Charge/draw time -> ChargeTimeMultiplier (default x0.5, i.e. half => fires twice as fast)
    ///   * Effective range  -> RangeMultiplier (default x1.5)
    ///   * Tooltip          -> ShowTunedStatsInTooltip (appends the tuned values to the spell text)
    ///
    /// Charge: the charge is a single value, <c>SpellLibrary.CastTime</c> (field 0x18C) on the
    /// <c>AimedShot : Spells</c> component.
    ///   * <c>Spells.DelayActivateCastSpell</c> yields <c>WaitForSeconds(CastTime)</c> before the shot prefab spawns.
    ///   * <c>BaseSpellLibrary.Start</c> copies that value into the spawned AimedShotPrefab's <c>_castTime</c>
    ///     (0x1C0); <c>AimedShotPrefab.OnStart</c> uses it as the arrow-draw duration and
    ///     <c>BaseSpell.CastTimeCallbackCheck</c> fires the release once <c>_timealive</c> (0x1CC) reaches it.
    /// Scaling that one value shortens the whole charge (wind-up + draw + release) proportionally.
    ///
    /// [IMPORTANT] The value must stay non-zero: <c>_castTime == 0</c> makes <c>CastTimeCallbackCheck</c>
    /// early-return without running the release callback, so the arrow never fires. ChargeTimeMultiplier is
    /// therefore clamped to a small floor (the same pitfall documented by FireballTuner).
    ///
    /// Range: the arrow's effective range is <c>SkillShot._maximumTravelDistance</c> (0x2A8), hard-set to 14.0
    /// inside <c>AimedShotArrowPrefab.OnStart</c>. We scale that field in place right after.
    ///
    /// Tooltip: patches <c>SpellTooltipDatabase.GetLocalizedDescription</c> and
    /// <c>SkillTreeNode.GetLocalizedDescription</c> — the two text producers the working Pyromancer
    /// (ice-to-fire) converter patches. The old <c>LocalizationManager.Get</c> hook (BarrageOfArrows)
    /// never showed up in-game.
    ///
    /// Co-op: charge and range are computed locally, so run the SAME config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class AimedShotChargeTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.aimedshotchargetuner";
        public const string NAME = "Aimed Shot Charge Tuner";
        public const string VERSION = "1.0.0";

        // [2026-10-07] Floor for the multiplier: 0 makes _castTime 0 and suppresses the release callback
        // (see class doc). 0.05 is near-instant while keeping the launch path alive.
        internal const float MinChargeMultiplier = 0.05f;

        internal static new ManualLogSource Log;
        internal static AimedShotChargeTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> ChargeTimeMultiplier;
        public static ConfigEntry<float> RangeMultiplier;
        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

        // Unscaled (vanilla) CastTime per Aimed Shot component, captured before the first scaling so the
        // per-cast hook cannot compound the multiplier (0.5 * 0.5 * ...). Keyed by native object pointer.
        private static readonly Dictionary<IntPtr, float> _originalCastTime = new();

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the Aimed Shot charge/range are left at vanilla. (Default: true)");

            ChargeTimeMultiplier = Config.Bind("AimedShot", "ChargeTimeMultiplier", 0.5f,
                "Multiplier applied to the Aimed Shot charge/draw time. 0.5 = half the charge time (fires twice as fast), 1 = vanilla. Clamped to a small non-zero floor because 0 stops the shot from ever releasing. (Default: 0.5)");

            RangeMultiplier = Config.Bind("AimedShot", "RangeMultiplier", 1.5f,
                "Multiplier applied to the Aimed Shot arrow's effective (max travel) range. 1 = vanilla, 1.5 = +50%. (Default: 1.5)");

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a line listing the tuned values (charge time / effective range) to the Aimed Shot spell description and skill-tree node text. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogChargeValues", false,
                "Log the Aimed Shot charge-time and range changes to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_Spells_Cast));
            PatchOrLog(harmony, typeof(Patch_AimedShotArrowPrefab_OnStart));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedDescription));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"ChargeTimeMultiplier: {ChargeTimeMultiplier.Value}");
            Log.LogInfo($"RangeMultiplier: {RangeMultiplier.Value}");
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
                Log.LogError($"[{NAME}] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        internal static bool IsActive => Instance != null && Enabled != null && Enabled.Value;

        internal static float EffectiveMultiplier
        {
            get
            {
                float m = ChargeTimeMultiplier != null ? ChargeTimeMultiplier.Value : 1f;
                if (float.IsNaN(m)) m = 1f;
                if (m < MinChargeMultiplier) m = MinChargeMultiplier;
                return m;
            }
        }

        /// <summary>
        /// Scales <paramref name="spell"/>'s CastTime from its cached vanilla value, so repeated calls
        /// (every Cast) never compound the multiplier. Records the vanilla value on first sight.
        /// Operates on <see cref="SpellLibrary"/> directly (CastTime is declared there) to avoid the
        /// IL2CPP interop wrapper-cast gotcha where Harmony may hand a wrapper typed as the patch type.
        /// </summary>
        internal static void ScaleChargeTime(SpellLibrary spell)
        {
            if (spell == null) return;

            IntPtr ptr = IL2CPP.Il2CppObjectBaseToPtr(spell);
            if (ptr == IntPtr.Zero) return;

            float mult = EffectiveMultiplier;
            if (mult == 1f) return;

            if (!_originalCastTime.TryGetValue(ptr, out float original))
            {
                original = spell.CastTime;
                if (original <= 0f) return; // Nothing meaningful to scale; leave untouched.
                _originalCastTime[ptr] = original;
            }

            float scaled = original * mult;
            if (spell.CastTime == scaled) return;

            spell.CastTime = scaled;
            if (DiagnosticLogging != null && DiagnosticLogging.Value)
                Log?.LogInfo($"[{NAME}] Aimed Shot charge time {original} -> {scaled} (x{mult}).");
        }

        /// <summary>
        /// Refreshes the cached vanilla CastTime from the value ApplyDefinition just copied, then scales it.
        /// ApplyDefinition re-copies the raw definition value, so this keeps the cache in sync if the
        /// definition ever changes and never compounds.
        /// </summary>
        internal static void RecordAndScale(SpellLibrary spell)
        {
            if (spell == null) return;
            IntPtr ptr = IL2CPP.Il2CppObjectBaseToPtr(spell);
            if (ptr == IntPtr.Zero) return;

            float raw = spell.CastTime;
            if (raw > 0f) _originalCastTime[ptr] = raw;

            ScaleChargeTime(spell);
        }

        /// <summary>
        /// Builds the extra line appended to the Aimed Shot spell text so the in-game description reflects
        /// the modded values. Returns null when the tooltip option is off or nothing is tuned.
        /// </summary>
        internal static string BuildTooltipSuffix()
        {
            if (ShowTunedStatsInTooltip == null || !ShowTunedStatsInTooltip.Value) return null;

            var parts = new List<string>();

            float cm = EffectiveMultiplier;
            if (cm != 1f) parts.Add($"charge time x{FormatMultiplier(cm)}");

            float rm = RangeMultiplier != null ? RangeMultiplier.Value : 1f;
            if (rm > 0f && rm != 1f) parts.Add($"effective range x{FormatMultiplier(rm)}");

            if (parts.Count == 0) return null;
            return "\n\n[Aimed Shot Tuner] " + string.Join(", ", parts) + ".";
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Postfix on the private <c>SpellLibrary.ApplyDefinition</c>, where a spell component copies its
    /// definition values onto the runtime fields (<c>CastTime -> CastTime</c> included). Runs once per
    /// component on the setup path. We scale the charge time for the Aimed Shot spell only.
    /// </summary>
    [HarmonyPatch(typeof(SpellLibrary), "ApplyDefinition")]
    public static class Patch_SpellLibrary_ApplyDefinition
    {
        public static void Postfix(SpellLibrary __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!AimedShotChargeTunerPlugin.IsActive) return;
                if (__instance.Spell != Spell.AimedShot) return;

                AimedShotChargeTunerPlugin.RecordAndScale(__instance);
            }
            catch (Exception ex)
            {
                AimedShotChargeTunerPlugin.Log?.LogError($"[Patch_SpellLibrary_ApplyDefinition] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>Spells.Cast</c> (the spellbook entry point, e.g. the <c>AimedShot : Spells</c>
    /// component). Casting routes through <c>Spells.DelayActivateCastSpell</c>, which yields
    /// <c>WaitForSeconds(SpellLibrary.CastTime)</c> before the shot prefab is spawned. Scaling CastTime here
    /// — immediately before that coroutine reads it — removes the wind-up regardless of when (or whether)
    /// ApplyDefinition ran, since the prefab later copies the same component's CastTime into its
    /// <c>BaseSpellLibrary._castTime</c>. The value stays non-zero (clamped), so the release callback still runs.
    /// </summary>
    [HarmonyPatch(typeof(Spells), "Cast")]
    public static class Patch_Spells_Cast
    {
        public static void Prefix(Spells __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!AimedShotChargeTunerPlugin.IsActive) return;
                if (__instance.Spell != Spell.AimedShot) return;

                AimedShotChargeTunerPlugin.ScaleChargeTime(__instance);
            }
            catch (Exception ex)
            {
                AimedShotChargeTunerPlugin.Log?.LogError($"[Patch_Spells_Cast] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>AimedShotArrowPrefab.OnStart</c>. The vanilla arrow hard-codes its effective range
    /// (<c>SkillShot._maximumTravelDistance = 14.0</c>) inside OnStart rather than going through
    /// SetMaximumTravelDistance, so the only reliable hook is after it. We scale the field in place; the
    /// offset is resolved from IL2CPP metadata at runtime with a build-time fallback.
    /// </summary>
    [HarmonyPatch(typeof(AimedShotArrowPrefab), "OnStart")]
    public static class Patch_AimedShotArrowPrefab_OnStart
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

        public static void Postfix(AimedShotArrowPrefab __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!AimedShotChargeTunerPlugin.IsActive) return;

                float mult = AimedShotChargeTunerPlugin.RangeMultiplier != null
                    ? AimedShotChargeTunerPlugin.RangeMultiplier.Value : 1f;
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
                        if (AimedShotChargeTunerPlugin.DiagnosticLogging != null && AimedShotChargeTunerPlugin.DiagnosticLogging.Value)
                        {
                            AimedShotChargeTunerPlugin.Log?.LogInfo(
                                $"[OnStart] Aimed Shot arrow max travel {vanilla} -> {*maxTravel} (offset 0x{_maxTravelOffset:X})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AimedShotChargeTunerPlugin.Log?.LogError($"[Patch_AimedShotArrowPrefab_OnStart] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>SpellTooltipDatabase.GetLocalizedDescription(Spell)</c>. This is the single text
    /// producer used by the spellbook, spell catalogue, character creation and pet panel. Mirrors the
    /// working Pyromancer (ice-to-fire) converter hook.
    /// </summary>
    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedDescription")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedDescription
    {
        public static void Postfix(Spell spell, ref string __result)
        {
            try
            {
                if (!AimedShotChargeTunerPlugin.IsActive) return;
                if (spell != Spell.AimedShot) return;

                string suffix = AimedShotChargeTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;
            }
            catch (Exception ex)
            {
                AimedShotChargeTunerPlugin.Log?.LogError($"[Patch_SpellTooltipDatabase_GetLocalizedDescription] {ex}");
            }
        }
    }

    /// <summary>
    /// Postfix on <c>SkillTreeNode.GetLocalizedDescription()</c> — the skill-tree node tooltip. The
    /// Pyromancer converter patches this too; the old <c>LocalizationManager.Get</c> hook did not show
    /// up here, which is why the previous "Barrage" style tooltip method never worked in the tree.
    /// Only nodes that unlock/relate to Aimed Shot are touched.
    /// </summary>
    [HarmonyPatch(typeof(SkillTreeNode), "GetLocalizedDescription")]
    public static class Patch_SkillTreeNode_GetLocalizedDescription
    {
        public static void Postfix(SkillTreeNode __instance, ref string __result)
        {
            try
            {
                if (!AimedShotChargeTunerPlugin.IsActive) return;
                if (__instance == null) return;
                if (__instance.AssociatedSpell != Spell.AimedShot && __instance.UnlockSpell != Spell.AimedShot) return;

                string suffix = AimedShotChargeTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;
            }
            catch (Exception ex)
            {
                AimedShotChargeTunerPlugin.Log?.LogError($"[Patch_SkillTreeNode_GetLocalizedDescription] {ex}");
            }
        }
    }
}
