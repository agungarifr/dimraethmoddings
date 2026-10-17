using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace BroadheadArrowTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that fixes the "Broadhead Shot" upgrade of Aimed Shot.
    ///
    /// THE BUG
    /// -------
    /// Taking "Broadhead Shot" turns the arrow's multi-hit OFF. Inside
    /// <c>AimedShotArrowPrefab.OnStart</c> the game literally does:
    ///     ISIL 143  Compare [rbx+0x32C], 0   ; _broadheadShot == 0 ?
    ///     ISIL 146  sete al                   ; al = (_broadheadShot == 0)
    ///     ISIL 150  Move [rbx+0x2B4], rax     ; _multiTarget = (_broadheadShot == 0)
    ///     ISIL 151  Move [rbx+0x2B5], 1       ; _sweepPathForHits = true
    /// so with the upgrade the shot not only loses its pierce, it also reports the milestone
    /// "multiHitEnabled" = false. That contradicts the upgrade text (a multi-hit shot) and is exactly
    /// the reported symptom: only the FIRST enemy is hit and only that one is knocked back.
    ///
    /// WHY THAT IS THE ONLY GATE
    /// -------------------------
    /// The arrow collects every collider along its path with <c>Physics2D.CircleCastAll</c> in
    /// <c>SkillShot.SweepStepForHits</c> (and in <c>HandleTriggerEnter2D</c> for the non-sweep path), but
    /// then breaks out of the hit loop as soon as multi-hit is off:
    ///     ISIL 217  Compare [rbx+0x2B4], rax  ; _multiTarget
    ///     ISIL ...  JumpIfNotEqual {next hit}  ; multi-hit ON  -> keep hitting
    ///     ISIL ...  JumpIfEqual   {exit loop}  ; multi-hit OFF -> stop at first enemy
    /// The knockback itself lives in <c>AimedShotArrowPrefab.ApplyTargetHit</c>
    /// (<c>BaseSpellLibrary.DashTargetBackwardsToPosition</c>) and already runs once PER target, so once the
    /// loop stops early, only one enemy is both damaged and pushed.
    ///
    /// THE FIX
    /// -------
    /// A Postfix on <c>AimedShotArrowPrefab.OnStart</c> that, when the Broadhead Shot upgrade is present,
    /// restores <c>_multiTarget = true</c>. The arrow then keeps scanning its sweep for hits, so every enemy
    /// gathered along the shot is hit AND pushed back by the existing per-target knockback.
    ///
    /// Field offsets are resolved from IL2CPP metadata at runtime with build-time fallbacks
    /// (<c>SkillShot._multiTarget = 0x2B4</c>, <c>AimedShotArrowPrefab._broadheadShot = 0x32C</c>).
    /// Both fields hold a bool, so the patch refuses to write unless each reads back as 0/1.
    ///
    /// CO-OP: the arrow is spawned and simulated locally, so use the SAME config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BroadheadArrowTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.broadheadarrowtuner";
        public const string NAME = "Broadhead Arrow Tuner";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static BroadheadArrowTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> BroadheadMultihit;
        public static ConfigEntry<bool> OnlyWithBroadheadUpgrade;
        public static ConfigEntry<bool> DiagnosticLogging;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the arrow is left exactly as vanilla. (Default: true)");

            BroadheadMultihit = Config.Bind("Broadhead", "BroadheadMultihit", true,
                "Restore multi-hit on the Aimed Shot arrow when the 'Broadhead Shot' upgrade is taken. Vanilla " +
                "turns multi-hit off for that upgrade (AimedShotArrowPrefab.OnStart: _multiTarget = (_broadheadShot == 0)), " +
                "so the shot stops at the first enemy and only that one is damaged/knocked back. With this on the " +
                "arrow pierces its sweep and pushes every enemy it touches. (Default: true)");

            OnlyWithBroadheadUpgrade = Config.Bind("Broadhead", "OnlyWithBroadheadUpgrade", true,
                "When true (recommended) multi-hit is restored ONLY when the Broadhead Shot upgrade is present. " +
                "Set to false to force multi-hit on every Aimed Shot arrow unconditionally. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogValues", false,
                "Log each Broadhead arrow's _broadheadShot/_multiTarget values and the applied fix to the " +
                "BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_AimedShotArrowPrefab_OnStart));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"BroadheadMultihit: {BroadheadMultihit.Value}");
            Log.LogInfo($"OnlyWithBroadheadUpgrade: {OnlyWithBroadheadUpgrade.Value}");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo("Broadhead Shot: arrow keeps multi-hit -> pierces and pushes every enemy in its path.");
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
    }

    /// <summary>
    /// Postfix on <c>AimedShotArrowPrefab.OnStart</c>. OnStart finishes by computing
    /// <c>_multiTarget = (_broadheadShot == 0)</c>, so we run right after it and put multi-hit back when the
    /// Broadhead Shot upgrade is present, letting the existing per-hit knockback fire for every enemy.
    /// The offsets are resolved from IL2CPP metadata at runtime with build-time fallbacks, and each field is
    /// validated to read back as 0/1 before anything is written (so a stale offset can never corrupt memory).
    /// </summary>
    [HarmonyPatch(typeof(AimedShotArrowPrefab), "OnStart")]
    public static class Patch_AimedShotArrowPrefab_OnStart
    {
        // Build-time fallbacks for this game build (verified from the ISIL dump).
        private const int FallbackBroadheadOffset = 0x32C;   // AimedShotArrowPrefab._broadheadShot
        private const int FallbackMultiTargetOffset = 0x2B4; // SkillShot._multiTarget

        private static int _broadheadOffset = FallbackBroadheadOffset;
        private static int _multiTargetOffset = FallbackMultiTargetOffset;
        private static bool _offsetsResolved;

        private static void EnsureOffsets()
        {
            if (_offsetsResolved) return;
            _offsetsResolved = true;

            try
            {
                IntPtr field = IL2CPP.GetIl2CppField(
                    Il2CppClassPointerStore<AimedShotArrowPrefab>.NativeClassPtr, "_broadheadShot");
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0) _broadheadOffset = off;
                }
            }
            catch
            {
                // Keep the fallback offset.
            }

            try
            {
                IntPtr field = IL2CPP.GetIl2CppField(
                    Il2CppClassPointerStore<SkillShot>.NativeClassPtr, "_multiTarget");
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0) _multiTargetOffset = off;
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
                if (!BroadheadArrowTunerPlugin.IsActive) return;
                if (!BroadheadArrowTunerPlugin.BroadheadMultihit.Value) return;

                EnsureOffsets();

                // A non-zero byte offset that resolves to a bool-sized value; refusing anything else keeps a
                // stale/mismatched offset from silently scribbling over unrelated object memory.
                unsafe
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(__instance);
                    if (p == IntPtr.Zero) return;

                    byte* broadheadPtr = (byte*)p + _broadheadOffset;
                    byte* multiTargetPtr = (byte*)p + _multiTargetOffset;

                    byte broadhead = *broadheadPtr;
                    if (broadhead > 1) return; // not a bool -> offset is wrong for this build, bail out

                    byte multiTarget = *multiTargetPtr;
                    if (multiTarget > 1) return; // not a bool -> offset is wrong for this build, bail out

                    bool upgradeTaken = broadhead != 0;
                    if (!upgradeTaken && BroadheadArrowTunerPlugin.OnlyWithBroadheadUpgrade.Value) return;

                    if (multiTarget == 1)
                    {
                        if (BroadheadArrowTunerPlugin.DiagnosticLogging.Value)
                        {
                            BroadheadArrowTunerPlugin.Log?.LogInfo(
                                "[OnStart] Broadhead Shot: multi-hit already on " +
                                $"(broadhead={broadhead}, multiTarget={multiTarget}; offsets 0x{_broadheadOffset:X}/0x{_multiTargetOffset:X})");
                        }
                        return;
                    }

                    *multiTargetPtr = 1;

                    if (BroadheadArrowTunerPlugin.DiagnosticLogging.Value)
                    {
                        BroadheadArrowTunerPlugin.Log?.LogInfo(
                            "[OnStart] Broadhead Shot: multi-hit restored " +
                            $"(broadhead={broadhead}, multiTarget={multiTarget} -> 1; offsets 0x{_broadheadOffset:X}/0x{_multiTargetOffset:X}) " +
                            "-> arrow now pierces and pushes every enemy it hits.");
                    }
                }
            }
            catch (Exception ex)
            {
                BroadheadArrowTunerPlugin.Log?.LogError($"[Patch_AimedShotArrowPrefab_OnStart] {ex}");
            }
        }
    }
}
