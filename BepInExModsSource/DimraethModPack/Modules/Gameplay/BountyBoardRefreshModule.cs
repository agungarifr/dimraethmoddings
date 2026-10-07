using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    /// <summary>
    /// BountyBoardRefreshModule - "Refresh Bounty Board".
    ///
    /// The Bounty Board is the game's Deed Board (DeedBoard / DeedBoardUI, tab "Bounties"). Boards are
    /// re-rolled by the game itself on two occasions (verified against the ISIL dump, 2026-10-07):
    ///   * DeedManager.OnNetworkSpawn() calls DeedManager.RefreshAllBoardsInternal() once, on spawn.
    ///   * DeedManager.OnTimeChanged(GameTime) compares the current in-game day against _lastRefreshDay
    ///     and, when the day rolls over, calls RefreshAllBoardsInternal() again. So vanilla rerolls are
    ///     once per in-game day.
    ///
    /// RefreshAllBoardsInternal iterates DeedManager.DeedBoards and calls RefreshBoard(boardDef) for each,
    /// which rebuilds that board's active deed pool (IsTierUnlocked + IsDeedOnCooldownFor filters, then
    /// WeightedRandomSelection up to the board's max) and stores it in activeDeedsPerBoard before firing
    /// the OnBoardRefreshed event the board UI listens to.
    ///
    /// This module adds a manual trigger. [2026-10-07 10:10] On the host it now calls the private
    /// DeedManager.RefreshAllBoardsInternal() directly (via reflection) - the very method the daily
    /// OnTimeChanged tick uses - rather than routing through the generated ServerRpc wrapper, whose
    /// loopback delivery from the host was not reliably re-entering the game. A remote client still uses
    /// the public ServerRpc. Either way it is the game's own reroll, with no Harmony patch and no per-frame work.
    /// </summary>
    public class BountyBoardRefreshModule : ModModuleBase
    {
        public static BountyBoardRefreshModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Refresh Bounty Board";
        public override string Description => "One button that rerolls every Deed & Bounty board on demand, using the game's own daily-refresh path";

        // Result text shown under the button after the last press.
        private static string _lastResult = "";

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.BountyBoardRefresh";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable the Refresh Bounty Board button (Default: false, Vanilla: false). Purely a manual button; nothing runs unless pressed.");
        }

        // No Harmony patches: the refresh taps an existing public game method (the same one the daily
        // tick uses), so there is no per-spawn/per-frame hook and therefore no gameplay overhead.
        public override void ApplyPatches(Harmony harmony)
        {
            // intentionally empty - see the class comment for why.
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            GUI.Label(new Rect(x, curY, width, 24f),
                "Rerolls every Deed & Bounty board via the game's own refresh path (same as the daily reset). Host-side.", labelStyle);
            curY += 28f;

            if (GUI.Button(new Rect(x, curY, Math.Min(320f, width), 30f), "Refresh All Bounty / Deed Boards", btnStyle))
            {
                if (!IsEnabled)
                {
                    _lastResult = "<color=#FF6666>Enable this module first.</color>";
                }
                else
                {
                    RefreshAll();
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

        // [2026-10-07 10:10] Cached handle to DeedManager's private RefreshAllBoardsInternal - the exact
        // method the daily OnTimeChanged tick calls. Invoking it directly on the host is guaranteed to run,
        // unlike routing through the generated ServerRpc wrapper whose loopback delivery is deferred/uncertain.
        private static System.Reflection.MethodInfo _refreshAllInternal;

        // Rerolls every board.
        // [2026-10-07 10:10] Changed: on the host (single-player) we now call RefreshAllBoardsInternal
        // directly via reflection - the same path the daily reset uses - instead of RefreshAllBoardsServerRpc.
        // The old ServerRpc call is kept below (commented) per repo rule; it is still used for a remote client,
        // where only the server may reroll. A per-board active-deed count is logged for verification.
        private static void RefreshAll()
        {
            try
            {
                var mgr = DeedManager.Singleton;
                if (mgr == null)
                {
                    _lastResult = "<color=#FF6666>Deed Manager not loaded yet.</color>";
                    return;
                }

                if (mgr.IsServer || mgr.IsHost)
                {
                    if (_refreshAllInternal == null)
                        _refreshAllInternal = HarmonyLib.AccessTools.Method(typeof(DeedManager), "RefreshAllBoardsInternal");

                    if (_refreshAllInternal == null)
                    {
                        _lastResult = "<color=#FF6666>Failed - refresh method not found.</color>";
                        DimraethModPackPlugin.Log?.LogError("[BountyBoardRefresh] RefreshAllBoardsInternal not found via reflection.");
                        return;
                    }

                    _refreshAllInternal.Invoke(mgr, null);
                    LogBoardCounts(mgr, "after direct internal refresh");
                    _lastResult = "<color=#55FF55>Bounty / Deed boards refreshed (host path).</color>";
                }
                else
                {
                    /* [2026-10-07 10:10] Obsolete for the host: routed through the generated ServerRpc, whose
                       loopback delivery from the host was not reliably re-entering the game. Kept for the
                       remote-client case, where the client legitimately cannot run the server-side reroll.
                    mgr.RefreshAllBoardsServerRpc();
                    _lastResult = "<color=#55FF55>Bounty / Deed boards refreshed.</color>";
                    DimraethModPackPlugin.Log?.LogInfo("[BountyBoardRefresh] RefreshAllBoardsServerRpc sent (host runs the daily refresh path).");
                    */
                    mgr.RefreshAllBoardsServerRpc();
                    _lastResult = "<color=#55FF55>Refresh requested from server.</color>";
                    DimraethModPackPlugin.Log?.LogInfo("[BountyBoardRefresh] RefreshAllBoardsServerRpc sent (client asks server to reroll).");
                }
            }
            catch (Exception ex)
            {
                _lastResult = "<color=#FF6666>Failed - see log.</color>";
                DimraethModPackPlugin.Log?.LogError($"[BountyBoardRefresh] refresh failed: {ex}");
            }
        }

        // Diagnostic: log the active-deed count for every board type so a reroll (or a no-op) is visible
        // in the BepInEx log without opening the UI. GetActiveDeedsForBoard is the same accessor the UI uses.
        private static void LogBoardCounts(DeedManager mgr, string label)
        {
            try
            {
                foreach (DeedBoardType t in Enum.GetValues(typeof(DeedBoardType)))
                {
                    var deeds = mgr.GetActiveDeedsForBoard(t);
                    int n = deeds == null ? 0 : deeds.Count;
                    DimraethModPackPlugin.Log?.LogInfo($"[BountyBoardRefresh] {label}: {t} = {n} deed(s).");
                }
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogWarning($"[BountyBoardRefresh] LogBoardCounts failed: {ex.Message}");
            }
        }
    }
}
