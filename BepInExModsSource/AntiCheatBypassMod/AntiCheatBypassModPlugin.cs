using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AntiCheatBypassMod
{
    /// <summary>
    /// [2026-09-29 21:36] New: standalone Anti-Cheat Bypass mod (user request).
    ///
    /// Goal: fully disable every Dimraeth anti-cheat mechanism documented in
    /// modding/docs/anticheat/ so the player can cheat freely - including spawning contraband
    /// gear and taking over-cap characters into multiplayer.
    ///
    /// Covered mechanisms (one line each, see the patch table in ApplyPatches):
    ///   02 GearLegality item destruction        -> Inspect returns Violation.None
    ///   04 Character identity (current build)    -> CharacterIdentityFromSkillTree.Reconcile skipped
    ///   05 CharacterPlausibility (hide + join)   -> FilterImplausible/IsImplausible/RejectsJoiningCharacter
    ///   03 Player XP/rune/attribute validators   -> always "clean"; kick/save-block/report disabled
    ///   05 Speedhack routine (direct ExitGame)   -> coroutine MoveNext stops at first step
    ///   07 Ban list                              -> IsBanned false on manager + data
    ///   09 Cheat telemetry                       -> OnCheatDetected / CheatAttempted suppressed
    ///   11 Obfuscated network values             -> integrity reporting + base-value repair disabled
    ///
    /// Patching is done by name via AccessTools (not "typeof") as a deliberate design choice: it
    /// keeps the DLL working across game updates that rename or add types, and a missing target
    /// logs a warning instead of aborting. Target names were verified against the current interop
    /// Assembly-CSharp.dll (same 2026-09-26 build as global-metadata.dat); the one mechanism that
    /// changed name is noted above.
    ///
    /// This is a client-side mod: it fully controls the instance running it (e.g. when you host).
    /// A host without the mod still applies its own checks to what you send.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class AntiCheatBypassModPlugin : BasePlugin
    {
        public const string PluginGuid = "com.custom.anticheatbypassmod";
        public const string PluginName = "AntiCheatBypassMod";
        // [2026-10-05] 1.0.0 -> 1.1.0: the 2026-10-05 hotfix added a second, Inspect-independent
        // rune-correction engine (ContrabandPreflight / Corrupt / RepairRuneCounts +
        // Player.PeriodicRuneIntegrityCheck / PeriodicAntiCheatCheck). Those are now bypassed too,
        // so edited gear is no longer silently reverted to vanilla.
        // [2026-10-05] 1.1.0 -> 1.2.0: also neutralize the anti-cheat bootstrap (Player.InitializeAntiCheat,
        // SubscribeAntiCheatValueChangedHooks, InitializeOwnerAntiCheatClientRpc) so the new engine
        // never arms its coroutines/value tripwires in the first place.
        public const string PluginVersion = "1.2.0";

        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;

            var harmony = new Harmony(PluginGuid);
            ApplyPatches(harmony);

            // Sticky top-left "anticheat bypassed" label. ClassInjector is required to add managed
            // MonoBehaviours to IL2CPP; DontDestroyOnLoad keeps it across scene changes.
            ClassInjector.RegisterTypeInIl2Cpp<BypassOverlayBehaviour>();
            var go = new GameObject("AntiCheatBypassOverlay");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<BypassOverlayBehaviour>();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded - anticheat bypassed.");
        }

        private static void ApplyPatches(Harmony harmony)
        {
            // ----- 02) Item destruction: all purge paths funnel through GearLegality.Inspect -----
            PatchByName(harmony, "GearLegality", "Inspect",
                nameof(AntiCheatBypassPatches.InspectPrefix));

            // ----- 02) 2026-10-05 hotfix: second correction path ("inflated numbers are corrected").
            // The new periodic integrity engine does NOT call Inspect; it gates on ContrabandPreflight
            // and mutates via Corrupt / RepairRuneCounts. Neutralize each so edited gear is neither
            // destroyed nor silently rewritten back to vanilla.
            PatchByName(harmony, "GearLegality", "ContrabandPreflight",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));
            PatchByName(harmony, "GearLegality", "Corrupt",
                nameof(AntiCheatBypassPatches.CorruptPrefix));
            PatchByName(harmony, "GearLegality", "RepairRuneCounts",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "GearLegality", "IsChargeContraband",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));

            // ----- 05) Character plausibility: stop hiding saves and stop the multiplayer join gate
            PatchByName(harmony, "CharacterPlausibility", "FilterImplausible",
                nameof(AntiCheatBypassPatches.FilterImplausiblePrefix));
            PatchByName(harmony, "CharacterPlausibility", "IsImplausible",
                nameof(AntiCheatBypassPatches.FalseReasonPrefix));
            PatchByName(harmony, "CharacterPlausibility", "RejectsJoiningCharacter",
                nameof(AntiCheatBypassPatches.FalseReasonPrefix));

            // ----- 04) Character identity reconciliation (docs/anticheat/04) --------------------
            // Pre-2026-09-26 this was CharacterIdentityIntegrity.FailsIdentityCheck/Reconcile
            // (returns IdentityVerdict). That type is gone from the current build; the successor
            // CharacterIdentityFromSkillTree.Reconcile recomputes/repairs a character's skill-point
            // allocation, so skipping it keeps cheated allocations intact.
            PatchByName(harmony, "CharacterIdentityFromSkillTree", "Reconcile",
                nameof(AntiCheatBypassPatches.ReconcilePrefix));

            // ----- 03) Player XP/rune/attribute validation: always report "clean"
            PatchByName(harmony, "Player", "ValidateXPState",
                nameof(AntiCheatBypassPatches.TrueResultPrefix));
            PatchByName(harmony, "Player", "ValidateXPInvariants",
                nameof(AntiCheatBypassPatches.TrueResultPrefix));
            PatchByName(harmony, "Player", "ValidateRuneState",
                nameof(AntiCheatBypassPatches.TrueResultPrefix));
            PatchByName(harmony, "Player", "ValidateAttributeXPConsistency",
                nameof(AntiCheatBypassPatches.SkipPrefix));

            // ----- 03) Enforcement: no kick, no save-block, no server self-report
            PatchByName(harmony, "Player", "KickForCheat",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "Player", "BlockSaveDueToCheatClientRpc",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "Player", "ReportLocalCheatServerRpc",
                nameof(AntiCheatBypassPatches.SkipPrefix));

            // ----- 03) Do not claw back over-granted (cheated) skill points
            PatchByName(harmony, "SkillTree", "TryRepairOverGrantedSkillPoints",
                nameof(AntiCheatBypassPatches.TryRepairPrefix));

            // ----- 07) Ban list: manager (custom connections) and data (Steam lobby joins)
            PatchByName(harmony, "BanListManager", "IsBanned",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));
            PatchByName(harmony, "BanListData", "IsBanned",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));

            // ----- 09) Cheat telemetry: the counter and the real-time event sender
            PatchByName(harmony, "GameAnalyticsManager", "OnCheatDetected",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "GameAnalyticsManager", "CheatAttempted",
                nameof(AntiCheatBypassPatches.SkipPrefix));

            // ----- 11) Obfuscated network values ---------------------------------------------
            // RaiseIntegrityViolation is the single source for the whole violation pipeline
            // (ClientDiagnostics tamper bit, GameAnalyticsManager.OnCheatDetected -> CheatAttempted
            // -> Supabase, FlagSessionAsCheating). Float routes through Int's raiser, so patching
            // the Int static covers both. RepairBaseValue is the rollback used only by the tamper
            // paths; no-op'ing it stops tampered base slots being reverted. HandleNetworkValueChanged
            // and get_Value are intentionally left intact so legitimate client sync still works -
            // mod-based value edits go through the trusted set_Value path and were never detected.
            PatchByName(harmony, "ObfuscatedNetworkInt", "RaiseIntegrityViolation",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "ObfuscatedNetworkInt", "RepairBaseValue",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "ObfuscatedNetworkFloat", "RepairBaseValue",
                nameof(AntiCheatBypassPatches.SkipPrefix));

            // ----- 05) Speedhack routine -------------------------------------------------------
            // This coroutine calls LoadManager.ExitGame directly, bypassing KickForCheat, so it
            // must be stopped on its own. MoveNext -> false ends the iterator on its first step
            // (the periodic XP/rune loops do not need this: their only effect is a KickForCheat,
            // which is already a no-op and whose validators now always report clean).
            PatchCoroutineMoveNext(harmony, "Player", "SpeedHackDetectionRoutine",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));

            // ----- 03) 2026-10-05 hotfix: periodic rune-integrity / anti-cheat coroutines --------
            // These are the new "detect impossible stats then correct/destroy" engines. Stopping
            // MoveNext on their first step kills the loop before it can touch edited runes or
            // kick for cheat. Nested iterators are named Player+<PeriodicRuneIntegrityCheck>d__215
            // and Player+<PeriodicAntiCheatCheck>d__204 in the 2026-10-05 interop.
            PatchCoroutineMoveNext(harmony, "Player", "PeriodicRuneIntegrityCheck",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));
            PatchCoroutineMoveNext(harmony, "Player", "PeriodicAntiCheatCheck",
                nameof(AntiCheatBypassPatches.FalseResultPrefix));

            // ----- 03) 2026-10-05 hotfix: anti-cheat bootstrap --------------------------------
            // Stop the new engine from arming at all: InitializeAntiCheat starts the periodic
            // coroutines and SubscribeAntiCheatValueChangedHooks wires the obfuscated-value
            // tripwires. Skipping both means the loops/tripwires never register, so there is
            // nothing left to correct edited runes or flag the session. The owner-side client RPC
            // is neutralized too (it is the "you may run the checks" handshake).
            PatchByName(harmony, "Player", "InitializeAntiCheat",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "Player", "SubscribeAntiCheatValueChangedHooks",
                nameof(AntiCheatBypassPatches.SkipPrefix));
            PatchByName(harmony, "Player", "InitializeOwnerAntiCheatClientRpc",
                nameof(AntiCheatBypassPatches.SkipPrefix));
        }

        /// <summary>
        /// Patch every declared method named <paramref name="methodName"/> on the named type.
        /// Missing type/method is logged and skipped (game updates may rename members).
        /// </summary>
        private static void PatchByName(Harmony harmony, string typeName, string methodName, string prefixName)
        {
            try
            {
                var type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    Log.LogWarning($"[AntiCheatBypass] Type '{typeName}' not found (game update?); " +
                                   $"'{methodName}' not bypassed.");
                    return;
                }

                var methods = AccessTools.GetDeclaredMethods(type)
                    .Where(m => m.Name == methodName)
                    .ToList();
                if (methods.Count == 0)
                {
                    Log.LogWarning($"[AntiCheatBypass] {typeName}.{methodName} not found; skipped.");
                    return;
                }

                var prefix = new HarmonyMethod(AccessTools.Method(typeof(AntiCheatBypassPatches), prefixName));
                foreach (var method in methods)
                {
                    harmony.Patch(method, prefix: prefix);
                    Log.LogInfo($"[AntiCheatBypass] Patched {typeName}.{methodName}.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[AntiCheatBypass] Failed to patch {typeName}.{methodName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Find a compiler-generated coroutine iterator nested in (or emitted alongside) the named
        /// type and stop its MoveNext immediately, killing the routine.
        /// </summary>
        private static void PatchCoroutineMoveNext(Harmony harmony, string containingTypeName,
            string iteratorNameFragment, string prefixName)
        {
            try
            {
                Type iterator = null;

                var containing = AccessTools.TypeByName(containingTypeName);
                if (containing != null)
                {
                    iterator = containing.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(t => t.Name.Contains(iteratorNameFragment, StringComparison.Ordinal));
                }

                // Some interop generators flatten compiler-generated types instead of nesting them.
                if (iterator == null)
                {
                    iterator = AccessTools.AllTypes()
                        .FirstOrDefault(t => t.Name.Contains(iteratorNameFragment, StringComparison.Ordinal));
                }

                if (iterator == null)
                {
                    Log.LogWarning($"[AntiCheatBypass] Iterator '{iteratorNameFragment}' not found; " +
                                   "speedhack routine not neutralized.");
                    return;
                }

                var moveNext = AccessTools.Method(iterator, "MoveNext");
                if (moveNext == null)
                {
                    Log.LogWarning($"[AntiCheatBypass] {iterator.Name}.MoveNext not found; skipped.");
                    return;
                }

                harmony.Patch(moveNext,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AntiCheatBypassPatches), prefixName)));
                Log.LogInfo($"[AntiCheatBypass] Patched {iterator.Name}.MoveNext (routine disabled).");
            }
            catch (Exception ex)
            {
                Log.LogError($"[AntiCheatBypass] Failed to patch iterator '{iteratorNameFragment}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sticky top-left overlay. Drawn every frame from OnGUI; IMGUI rich text colours the word
    /// "bypassed" bright green while the rest of the label stays default.
    /// </summary>
    public class BypassOverlayBehaviour : MonoBehaviour
    {
        // Required so Il2CppInterop can construct the injected type.
        public BypassOverlayBehaviour(IntPtr ptr) : base(ptr) { }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 14,
                };
            }

            GUI.Label(new Rect(12f, 10f, 420f, 26f),
                "anticheat <color=#00FF00>bypassed</color>", _style);
        }
    }
}
