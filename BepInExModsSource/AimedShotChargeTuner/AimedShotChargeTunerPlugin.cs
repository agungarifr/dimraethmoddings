using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;

namespace AimedShotChargeTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that speeds up the "Aimed Shot" spell's draw/charge.
    ///
    /// The charge is a single value: <c>SpellLibrary.CastTime</c> (field 0x18C) on the
    /// <c>AimedShot : Spells</c> component.
    ///   * <c>Spells.DelayActivateCastSpell</c> yields <c>WaitForSeconds(CastTime)</c> before the shot prefab spawns.
    ///   * <c>BaseSpellLibrary.Start</c> copies that same value into the spawned AimedShotPrefab's
    ///     <c>_castTime</c> (0x1C0); <c>AimedShotPrefab.OnStart</c> uses it as the arrow-draw duration and
    ///     <c>BaseSpell.CastTimeCallbackCheck</c> fires the release once <c>_timealive</c> (0x1CC) reaches it.
    /// Scaling that one value shortens the whole charge (wind-up + draw + release) proportionally.
    ///
    /// [IMPORTANT] The value must stay non-zero: <c>_castTime == 0</c> makes <c>CastTimeCallbackCheck</c>
    /// early-return without running the release callback, so the arrow never fires. ChargeTimeMultiplier is
    /// therefore clamped to a small floor (the same pitfall documented by FireballTuner).
    ///
    /// Co-op: the charge is computed locally, so run the SAME config on host and client.
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
        public static ConfigEntry<bool> DiagnosticLogging;

        // Unscaled (vanilla) CastTime per Aimed Shot component, captured before the first scaling so the
        // per-cast hook cannot compound the multiplier (0.5 * 0.5 * ...). Keyed by native object pointer.
        private static readonly Dictionary<IntPtr, float> _originalCastTime = new();

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the Aimed Shot charge time is left at vanilla. (Default: true)");

            ChargeTimeMultiplier = Config.Bind("AimedShot", "ChargeTimeMultiplier", 0.5f,
                "Multiplier applied to the Aimed Shot charge/draw time. 0.5 = half the charge time (fires twice as fast), 1 = vanilla. Clamped to a small non-zero floor because 0 stops the shot from ever releasing. (Default: 0.5)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogChargeValues", false,
                "Log the Aimed Shot charge-time changes to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_Spells_Cast));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"ChargeTimeMultiplier: {ChargeTimeMultiplier.Value}");
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
}
