using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    // [2026-10-06 13:30] REVAMP (user request): the old Fast Production was a Harmony postfix on
    // CraftingBench.Update() that added extra progress every frame. The user asked to lean on the game's
    // own system and use a BUTTON, like the revamped Instant Crops module, "so it doesnt burden the game".
    //
    // Implementation: a manual button ("Finish All Current Crafts Now"). When pressed it arms a short
    // drain that finishes every current/queued craft using the game's OWN completion path:
    //   - For each workbench/alchemy CraftingBench with an active job we write _currentProgress to
    //     Recipe.CraftingTime. Vanilla Update then sees progress >= CraftingTime, calls
    //     DepositIntoOutput(...) and resets progress.
    //   - The bench then decrements _currentQuantity by 1 (one batch per tick) and, when it reaches 0,
    //     pops the next queue entry. So we keep writing progress each frame until the bench is idle.
    //
    // Queue reality (verified in CraftingBench.txt): Queue is a NetworkList<BenchQueueItem> capped at
    // MaxQueueSize=4 ENTRIES, but each entry carries { RecipeUUID, int Quantity, float Progress }. So a
    // "water x99" job is ONE entry with Quantity=99 and the bench crafts it one batch per tick. Therefore
    // one click cannot be a single frame for large stacks; it drains the whole thing in ~1 tick per batch
    // (~1.5-2s for 99) with no further input, while keeping the game's real deposit (XP/credits/events).
    // Outside the drain window OnUpdate is a single bool check, so there is no standing per-frame cost.
    public class FastProductionModule : ModModuleBase
    {
        public static FastProductionModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Instant Production";
        public override string Description => "One button that finishes all current/queued workbench & alchemy crafts using the game's own deposit path";

        // [2026-10-06 13:30] Obsolete: the speed multiplier is no longer used - production is now a
        // one-shot finish button, not a per-frame rate modifier.
        // public ConfigEntry<int> SpeedMultiplier;

        private static string _lastResult = "";

        // Drain state: while draining, OnUpdate finishes one batch per frame until every target bench is
        // idle (or the safety deadline is hit).
        private static bool _draining;
        private static int _drainDeadlineFrame;

        // [2026-10-06 14:00] Temporary diagnostics for the "button doesn't work" report. Logs the network
        // role, the bench's authority/state, and a write read-back for the first few frames after a press
        // so we can tell whether the NetworkVariable write is authoritative (host) or silently ignored
        // (client / no write permission). Remove once the root cause is fixed.
        private static int _diagFrames;

        // Safety cap so a stuck bench (e.g. permanently output-blocked) can't keep the drain alive forever.
        private const int DrainSafetyFrames = 3600; // ~60s at 60fps; real queues finish far sooner

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.FastProduction";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Instant Production (Default: false, Vanilla: false)");

            /* [2026-10-06 13:30] Obsolete: speed multiplier removed with the revamp. The button finishes
               crafts through the game's deposit path, so a numeric speed multiplier is no longer
               meaningful. Kept for reference per repo rule.
            SpeedMultiplier = config.Bind(sec, "SpeedMultiplier", 5,
                "Crafting speed multiplier at workbench/alchemy stations (Default: 5, Vanilla: 1)");
            */
        }

        // [2026-10-06 13:30] No Harmony patches: the revamp writes the existing _currentProgress
        // NetworkVariable and lets the game's own Update do the deposit, so there is no standing hook.
        public override void ApplyPatches(Harmony harmony)
        {
            // intentionally empty — see the class comment for why.
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            GUI.Label(new Rect(x, curY, width, 24f),
                "Finishes every current/queued workbench & alchemy craft via the game's own deposit path. A large stack completes in a second or two.", labelStyle);
            curY += 28f;

            if (GUI.Button(new Rect(x, curY, Math.Min(320f, width), 30f), "Finish All Current Crafts Now", btnStyle))
            {
                if (!IsEnabled)
                {
                    _lastResult = "<color=#FF6666>Enable this module first.</color>";
                }
                else
                {
                    StartDrain();
                }
            }
            curY += 36f;

            if (!string.IsNullOrEmpty(_lastResult))
            {
                GUI.Label(new Rect(x, curY, width, 24f), _lastResult, labelStyle);
                curY += 28f;
            }
            return curY - y;
        }

        // Called every frame from ModPackManagerBehaviour.Update. Dormant (one bool check) unless a press
        // armed the drain; then it finishes one batch per frame until every target bench is idle.
        public static void OnUpdate()
        {
            if (!_draining) return;
            if (Time.frameCount > _drainDeadlineFrame)
            {
                _draining = false;
                _lastResult = "<color=#FFAA44>Stopped (safety cap) — some benches may be output-blocked.</color>";
                return;
            }

            int pending = FinishActiveCrafts();
            if (pending == 0)
            {
                _draining = false;
                _lastResult = "<color=#55FF55>All crafts finished.</color>";
            }
            else
            {
                _lastResult = $"<color=#55FF55>Finishing… {pending} bench(es) still busy.</color>";
            }
        }

        private static void StartDrain()
        {
            _draining = true;
            _drainDeadlineFrame = Time.frameCount + DrainSafetyFrames;
            _diagFrames = 12; // [2026-10-06 14:00] temporary diagnostics for first frames of a press
            _lastResult = "<color=#55FF55>Finishing…</color>";
            FinishActiveCrafts(); // act immediately on the press frame
        }

        // Writes each matching bench's progress to Recipe.CraftingTime; vanilla Update then deposits one
        // batch that tick and the bench advances. Returns how many target benches still have a job they can
        // actually deposit (output-blocked benches are excluded so they can't stall the drain).
        private static int FinishActiveCrafts()
        {
            try
            {
                var benches = UnityEngine.Object.FindObjectsOfType<CraftingBench>();
                if (benches == null) return 0;

                int pending = 0;
                int touched = 0;
                bool diagLogged = false;
                for (int i = 0; i < benches.Length; i++)
                {
                    var bench = benches[i];
                    if (bench == null) continue;
                    if (!IsFastStation(bench.Container)) continue;
                    if (!bench.HasActiveJob) continue;      // idle: nothing to do
                    if (bench.IsOutputBlocked) continue;    // output full: the game refuses the deposit

                    pending++; // a job we can still advance

                    Recipe recipe = bench.CurrentRecipe;
                    if (recipe == null) continue;
                    float target = recipe.CraftingTime;
                    if (target <= 0f) continue;

                    var progress = bench._currentProgress;
                    if (progress == null) continue;

                    // [2026-10-06 14:00] Temporary diagnostics (see _diagFrames). One-shot dump on the first
                    // frame of a drain, then a few frames of quantity/progress so we can see whether the
                    // craft is actually advancing. Remove once the root cause is fixed.
                    if (_diagFrames > 0)
                    {
                        LogDiag(bench, recipe, target, progress, ref diagLogged);
                        touched++;
                        continue;
                    }

                    if (progress.Value < target)
                    {
                        progress.Value = target;
                        touched++;
                    }
                }

                if (touched > 0)
                {
                    DimraethModPackPlugin.Log?.LogInfo($"[InstantProduction] progress->CraftingTime on {touched} bench(es)");
                }
                return pending;
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogError($"[InstantProduction] finish failed: {ex}");
                return 0;
            }
        }

        // [2026-10-06 14:00] Temporary diagnostic helper. Remove once the write-permission issue is fixed.
        private static void LogDiag(CraftingBench bench, Recipe recipe, float target, NetworkVariable<float> progress, ref bool logged)
        {
            try
            {
                var nm = NetworkManager.Singleton;
                string role = nm != null
                    ? $"IsServer={nm.IsServer} IsHost={nm.IsHost} IsClient={nm.IsClient}"
                    : "nm=null";

                if (!logged)
                {
                    logged = true;
                    float before = progress.Value;
                    progress.Value = target;
                    float after = progress.Value;
                    DimraethModPackPlugin.Log?.LogInfo(
                        $"[InstantProduction][diag] {role} | bench={bench.name} netId={bench.NetworkObjectId} isServer={bench.IsServer} isOwner={bench.IsOwner} qty={bench.CurrentQuantity} prog01={bench.Progress01} active={bench.HasActiveJob} crafting={bench.IsCrafting} blocked={bench.IsOutputBlocked} ct={recipe.CraftingTime} container={bench.Container}");
                    DimraethModPackPlugin.Log?.LogInfo(
                        $"[InstantProduction][diag] write progress {before} -> {after} (target {target})");
                }
                else
                {
                    DimraethModPackPlugin.Log?.LogInfo(
                        $"[InstantProduction][diag] f={_diagFrames} qty={bench.CurrentQuantity} prog={progress.Value} prog01={bench.Progress01} blocked={bench.IsOutputBlocked} active={bench.HasActiveJob}");
                }
                _diagFrames--;
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogError($"[InstantProduction][diag] failed: {ex}");
            }
        }


        // Workbench + Alchemy Table only (placed and player-built spellings).
        private static bool IsFastStation(StorageContainers container)
        {
            return container == StorageContainers.WorkBench
                || container == StorageContainers.AlchemyTable
                || container == StorageContainers.BuildWorkBench
                || container == StorageContainers.BuildAlchemyTable;
        }

        /* [2026-10-06 13:30] ===== OBSOLETE (pre-revamp per-frame speed multiplier), kept per repo rule. =====
           This postfix added Time.deltaTime*(mult-1) to _currentProgress after vanilla's add every frame
           (idle-gated by reading progress first). Replaced by the button above per user request.

        [HarmonyPatch(typeof(CraftingBench), "Update")]
        public static class Patches
        {
            private static bool _loggedFirstBench;

            private static void Postfix(CraftingBench __instance)
            {
                try
                {
                    if (Instance == null || !Instance.IsEnabled) return;
                    int mult = Instance.SpeedMultiplier.Value;
                    if (mult <= 1) return;
                    if (__instance == null) return;

                    var progress = __instance._currentProgress;
                    if (progress == null) return;
                    float current = progress.Value;
                    if (current <= 0f) return;

                    if (!_loggedFirstBench)
                    {
                        _loggedFirstBench = true;
                        DimraethModPackPlugin.Log?.LogInfo(
                            $"[FastProduction] first CraftingBench.Container = {__instance.Container} ({(int)__instance.Container})");
                    }

                    if (!IsFastStation(__instance.Container)) return;
                    if (__instance.IsOutputBlocked) return;
                    Recipe recipe = __instance.CurrentRecipe;
                    if (recipe == null) return;
                    if (recipe.CraftingTime <= 0) return;
                    if (current >= recipe.CraftingTime) return;

                    float next = current + Time.deltaTime * (mult - 1);
                    if (next > recipe.CraftingTime) next = recipe.CraftingTime;
                    progress.Value = next;
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogError($"[FastProduction] CraftingBench.Update postfix failed: {ex}");
                }
            }
        }
        */
    }
}
