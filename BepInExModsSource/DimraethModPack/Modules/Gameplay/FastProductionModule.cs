using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    // [2026-10-06 13:30] REVAMP (user request): the old Fast Production was a Harmony postfix on
    // CraftingBench.Update() that added extra progress every frame. The user asked to lean on the game's
    // own system and use a BUTTON, like the revamped Instant Crops module, "so it doesnt burden the game".
    //
    // Implementation: a manual button ("Finish All Current Crafts Now"). When pressed it caches the
    // crafting-station CraftingBench instances ONCE and arms a short drain that finishes every
    // current/queued craft using the game's OWN completion path:
    //   - For each cached bench with an active job we write _currentProgress to Recipe.CraftingTime.
    //     Vanilla Update then sees progress >= CraftingTime, calls DepositIntoOutput(...) and resets progress.
    //   - The bench then decrements _currentQuantity by 1 (one batch per tick) and, when it reaches 0,
    //     pops the next queue entry. So we keep writing progress each frame until the bench is idle.
    //
    // Queue reality (verified in CraftingBench.txt): Queue is a NetworkList<BenchQueueItem> capped at
    // MaxQueueSize=4 ENTRIES, but each entry carries { RecipeUUID, int Quantity, float Progress }. So a
    // "water x99" job is ONE entry with Quantity=99 and the bench crafts it one batch per tick. Therefore
    // one click cannot be a single frame for large stacks; it drains the whole thing in ~1 tick per batch
    // (~1.5-2s for 99) with no further input, while keeping the game's real deposit (XP/credits/events).
    //
    // [2026-10-06 14:30] STUTTER FIX: the first drain implementation called
    // UnityEngine.Object.FindObjectsOfType<CraftingBench>() EVERY frame while draining, which is the
    // expensive call that stuttered the game. We now call it exactly ONCE per button press (StartDrain),
    // cache the array, and iterate the cache each drain frame. There is no per-frame scene search.
    //
    // Outside the drain window OnUpdate is a single bool check, so there is no standing per-frame cost.
    public class FastProductionModule : ModModuleBase
    {
        public static FastProductionModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Instant Production";
        public override string Description => "One button that finishes all current/queued workbench, alchemy, campfire & woodfire-stove crafts using the game's own deposit path";

        // [2026-10-06 13:30] Obsolete: the speed multiplier is no longer used - production is now a
        // one-shot finish button, not a per-frame rate modifier.
        // public ConfigEntry<int> SpeedMultiplier;

        private static string _lastResult = "";

        // Drain state: while draining, OnUpdate finishes one batch per frame until every target bench is
        // idle (or the safety deadline is hit).
        private static bool _draining;
        private static int _drainDeadlineFrame;
        private static int _lastTouchFrame;
        private static int _lastPending;

        // [2026-10-06 14:30] Benches are found ONCE on press and cached here instead of re-running
        // FindObjectsOfType every frame (that per-frame scene search was the source of the stutter).
        private static CraftingBench[] _targets;

        // Safety cap so a stuck bench (e.g. permanently output-blocked) can't keep the drain alive forever.
        private const int DrainSafetyFrames = 3600; // ~60s at 60fps; real queues finish far sooner
        // Stop once this many consecutive frames pass without advancing any bench.
        private const int DrainIdleFrames = 3;

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
                "Finishes every current/queued workbench, alchemy, campfire & woodfire-stove craft via the game's own deposit path. A large stack completes in a second or two.", labelStyle);
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
                StopDrain("<color=#FFAA44>Stopped (safety cap) — some benches may be output-blocked.</color>");
                return;
            }

            int touched = Tick();
            if (touched > 0)
            {
                _lastTouchFrame = Time.frameCount;
                _lastResult = $"<color=#55FF55>Finishing… {_lastPending} bench(es) still busy.</color>";
            }
            else if (Time.frameCount - _lastTouchFrame > DrainIdleFrames)
            {
                StopDrain("<color=#55FF55>All crafts finished.</color>");
            }
        }

        private static void StartDrain()
        {
            // [2026-10-06 14:30] Find the benches exactly once here (not every frame).
            _targets = UnityEngine.Object.FindObjectsOfType<CraftingBench>();
            _draining = true;
            _drainDeadlineFrame = Time.frameCount + DrainSafetyFrames;
            _lastTouchFrame = Time.frameCount;
            _lastResult = "<color=#55FF55>Finishing…</color>";
            Tick(); // act immediately on the press frame
        }

        private static void StopDrain(string message)
        {
            _draining = false;
            _targets = null;
            _lastResult = message;
        }

        // Writes each cached bench's progress to Recipe.CraftingTime; vanilla Update then deposits one
        // batch that tick and the bench advances. Returns how many benches were advanced this frame.
        private static int Tick()
        {
            try
            {
                if (_targets == null) return 0;

                int touched = 0;
                int pending = 0;
                for (int i = 0; i < _targets.Length; i++)
                {
                    var bench = _targets[i];
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
                    if (progress.Value < target)
                    {
                        progress.Value = target;
                        touched++;
                    }
                }

                _lastPending = pending;
                return touched;
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogError($"[InstantProduction] finish failed: {ex}");
                return 0;
            }
        }

        // [2026-10-09 09:07] FIX (user request: "instant production must affect campfire too"): the
        // station filter now asks the game itself instead of keeping a hardcoded whitelist. The old list
        // only covered workbench/alchemy, so campfire and woodfire-stove cooking crafts were ignored.
        // StorageContainerRules.IsCraftingStation is the game's own predicate (Traits bit CraftingStation):
        // verified against the shipped Traits jump table it is true for WorkBench, AlchemyTable,
        // WoodfireStove, Campfire and their Build* variants, while NPC crafting services (Myrll/Tamsin/
        // Rahaner = NPCService), chests (Chest) and loot (Corpse) are false — so this widens coverage to
        // every real crafting station without accidentally finishing NPC-service or chest crafts.
        private static bool IsFastStation(StorageContainers container)
        {
            /* [2026-10-09 09:07] Obsolete: hardcoded workbench/alchemy whitelist. It omitted the campfire
               and woodfire-stove cooking stations (user report). Superseded by the game's own
               StorageContainerRules.IsCraftingStation above; kept commented per repo rule.
            return container == StorageContainers.WorkBench
                || container == StorageContainers.AlchemyTable
                || container == StorageContainers.BuildWorkBench
                || container == StorageContainers.BuildAlchemyTable;
            */
            return StorageContainerRules.IsCraftingStation(container);
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

        /* [2026-10-06 14:30] ===== OBSOLETE (temporary diagnostics), kept per repo rule. =====
           Added 2026-10-06 14:00 to diagnose the "button doesn't work" report. The log output PROVED the
           button works: IsServer=True IsHost=True IsClient=True, bench isServer/isOwner both true, the
           write read-back went 0 -> 2 (target 2), and qty decremented 82->81->80->... one per frame (i.e.
           one batch completed per tick). The real complaint was the per-frame FindObjectsOfType stutter,
           now fixed by caching _targets in StartDrain. Removed here; kept commented per repo rule.

        using Unity.Netcode; // was needed only by LogDiag

        private static int _diagFrames;

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
        */
    }
}
