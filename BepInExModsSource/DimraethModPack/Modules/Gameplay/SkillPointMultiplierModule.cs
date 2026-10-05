using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    /// <summary>
    /// [2026-09-26 14:09] New: Skill Point Multiplier (user request).
    /// [2026-09-26 14:36] Fix: the first build re-used one bonus source name, so only the first
    /// level-up actually granted points (log: 1x "granted", then Nx "refused"). The refusal was the
    /// game's one-shot source guard (Player.HasBonusSkillPointSource), NOT a grant ceiling, so the
    /// source name is now unique per level ("..._L{level}"). The per-level key also de-duplicates
    /// the multiple UpgradeLevelClientRpc fires a single level-up can produce.
    /// [2026-09-26 15:10] Fix: the 14:36 build also scaled Player.MaxGrantableSkillPoints() by the
    /// multiplier. That method is not just a grant ceiling — SkillTree.ReconcileSkillPoints() and the
    /// skill-tree respec/refund paths set the player's skill points to
    /// (MaxGrantableSkillPoints() - GetPlayerSpentPoints()), so scaling it injected x{multiplier}
    /// phantom points (user saw 100 -> 580 after respec: 580 = 5 x 116). TryGrantBonusSkillPoints has
    /// no ceiling check (it only guards the source), and our bonus grants already flow into
    /// GetTotalBonusSkillPoints(), which MaxGrantableSkillPoints() adds in — so the ceiling scales
    /// itself and the extra patch is unnecessary. It has been removed (kept commented below).
    ///
    /// Vanilla grants GameConfig.SkillPointsPerLevel = 2 skill points per level-up.
    ///
    /// [2026-10-04 00:00] New: "Grant Now" button that awards skill points directly from the Mod Manager
    /// UI, without needing a level-up. Uses the same proven Player.TryGrantBonusSkillPoints path as the
    /// level-up grant, with a unique one-shot source per click so each grant is accepted. Bonus points
    /// flow into GetTotalBonusSkillPoints() -> MaxGrantableSkillPoints(), so the availability ceiling
    /// grows by itself (never scaled manually - see the removed MaxGrantableSkillPoints patch below).
    /// </summary>
    public class SkillPointMultiplierModule : ModModuleBase
    {
        public static SkillPointMultiplierModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Skill Point Multiplier";
        public override string Description => "Multiply skill points gained on level-up, or grant points directly without leveling up";

        public ConfigEntry<int> SkillPointMultiplier;

        // [2026-10-04 00:00] New: points awarded per click of the "Grant Now" button.
        public ConfigEntry<int> GrantAmount;

        // Last "Grant Now" result, shown under the button. UI-thread only, so no locking needed.
        private static string _lastGrantMessage = "";
        // Bumped per grant so every source string is unique (TryGrantBonusSkillPoints accepts each
        // source only once; a reused source would be silently refused).
        private static int _directGrantCounter = 0;

        // Vanilla grant per level-up: GameConfig.SkillPointsPerLevel = 2 (verified from the game's
        // own constants). Kept as a literal because IL2CPP constants are inlined in native code.
        public const int VanillaSkillPointsPerLevel = 2;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.SkillPointMod";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable Skill Point Multiplier Module (Default: true, Vanilla: false)");

            SkillPointMultiplier = config.Bind(sec, "SkillPointMultiplier", 3,
                "Skill points per level-up = 2 x this multiplier (Default: 3, Vanilla: 1)");

            GrantAmount = config.Bind(sec, "GrantAmount", 5,
                "Skill points awarded per click of the 'Grant Now' button, no level-up needed (Default: 5)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntSpinner(x, curY, width, "Skill Point Multiplier", SkillPointMultiplier, 1, 1, 10, "Vanilla: 1x", labelStyle, btnStyle);
            curY += DrawIntSpinner(x, curY, width, "Grant Amount", GrantAmount, 1, 1, 100, "no level-up", labelStyle, btnStyle);

            // [2026-10-04 00:00] New: award points on demand, independent of leveling up.
            if (GUI.Button(new Rect(x, curY, width, 24f), $"Grant Now  (+{GrantAmount.Value} skill point{(GrantAmount.Value == 1 ? "" : "s")})", btnStyle))
            {
                GrantSkillPoints(GrantAmount.Value, out _lastGrantMessage);
            }
            curY += 28f;

            if (!string.IsNullOrEmpty(_lastGrantMessage))
            {
                GUI.Label(new Rect(x, curY, width, 20f), _lastGrantMessage, labelStyle);
                curY += 22f;
            }

            return curY - y;
        }

        /// <summary>
        /// [2026-10-04 00:00] Grants skill points to the local player immediately, no level-up required.
        /// Mirrors the level-up grant: a unique one-shot source per call, then TryGrantBonusSkillPoints.
        /// </summary>
        public static bool GrantSkillPoints(int amount, out string message)
        {
            message = "";
            if (amount <= 0)
            {
                message = "<color=#FFAA55>Amount must be 1 or more.</color>";
                return false;
            }

            try
            {
                Player player = ResolveLocalPlayer();
                if (player == null)
                {
                    message = "<color=#FFAA55>No local player in a loaded world.</color>";
                    return false;
                }

                string source = $"SkillPointMultiplierMod_Direct_{DateTime.UtcNow.Ticks}_{++_directGrantCounter}";
                int pointsBefore = player.SkillPoints.Value;

                bool granted = player.TryGrantBonusSkillPoints(source, amount);
                if (granted)
                    message = $"<color=#55FF55>Granted +{amount} (skill points {pointsBefore} -> {player.SkillPoints.Value}).</color>";
                else
                    message = $"<color=#FF5555>Grant refused (points={pointsBefore}, bonusTotal={player.GetTotalBonusSkillPoints()}).</color>";

                DimraethModPackPlugin.Log?.LogInfo($"[SkillPointMod] Direct grant +{amount}: {message}");
                return granted;
            }
            catch (Exception ex)
            {
                message = $"<color=#FF5555>Grant failed: {ex.Message}</color>";
                DimraethModPackPlugin.Log?.LogWarning($"[SkillPointMod] Direct grant failed: {ex.Message}");
                return false;
            }
        }

        // Local player for a UI-triggered grant: DataStorage.Singleton.Player is the loaded
        // character in single-player. Null while no world/character is active.
        private static Player ResolveLocalPlayer()
        {
            try
            {
                if (DataStorage.Singleton != null && DataStorage.Singleton.Player != null)
                    return DataStorage.Singleton.Player;
            }
            catch { }

            return null;
        }

        public static class Patches
        {
            // Last level we granted for, so the multiple UpgradeLevelClientRpc fires a single
            // level-up can produce don't double-grant. Reset automatically on a lower level
            // (character switch).
            private static int _lastGrantedLevel = -1;

            [HarmonyPatch(typeof(PlayerStats), "UpgradeLevelClientRpc")]
            [HarmonyPostfix]
            public static void Postfix_UpgradeLevelClientRpc(PlayerStats __instance)
            {
                if (Instance == null || !Instance.IsEnabled) return;

                try
                {
                    int multiplier = Instance.SkillPointMultiplier.Value;
                    if (multiplier <= 1) return;

                    // The RPC runs on every client for every player's PlayerStats proxy:
                    // only the owner's own level-up should grant the local player points.
                    if (!__instance.IsOwner) return;

                    Player player = ResolveLocalPlayer(__instance);
                    if (player == null) return;

                    int level = player.HighestLevel.Value;

                    // Character switch (lower level) resets the tracker.
                    if (level < _lastGrantedLevel) _lastGrantedLevel = -1;
                    if (level <= _lastGrantedLevel) return; // duplicate fire for the same level
                    _lastGrantedLevel = level;

                    string source = $"SkillPointMultiplierMod_L{level}";
                    if (player.HasBonusSkillPointSource(source)) return; // already granted for this level

                    int extra = VanillaSkillPointsPerLevel * (multiplier - 1);
                    int pointsBefore = player.SkillPoints.Value;

                    bool granted = player.TryGrantBonusSkillPoints(source, extra);

                    if (granted)
                        DimraethModPackPlugin.Log?.LogInfo(
                            $"[SkillPointMod] L{level}: granted +{extra} (skill points {pointsBefore} -> {player.SkillPoints.Value}).");
                    else
                        DimraethModPackPlugin.Log?.LogWarning(
                            $"[SkillPointMod] L{level}: grant refused (source already claimed?) points={pointsBefore}, bonusTotal={player.GetTotalBonusSkillPoints()}.");
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogWarning($"[SkillPointMod] Grant failed: {ex.Message}");
                }
            }

            /* [2026-09-26 15:10] Obsolete/REMOVED. This scaled MaxGrantableSkillPoints() by the
               multiplier to raise a "grant ceiling". That premise was wrong:
               - TryGrantBonusSkillPoints has no ceiling check (its only guard is the one-shot
                 source), so the v1 refusal was never the ceiling.
               - MaxGrantableSkillPoints() already grows on its own: it adds
                 GetTotalBonusSkillPoints(), which includes every source we grant.
               - The skill-tree respec/reconcile path sets available points to
                 (MaxGrantableSkillPoints() - GetPlayerSpentPoints()), so scaling it awarded
                 x{multiplier} phantom points (user saw 100 -> 580 = 5 x 116).
               Removed; do not reinstate.
            [HarmonyPatch(typeof(Player), nameof(Player.MaxGrantableSkillPoints))]
            [HarmonyPostfix]
            public static void Postfix_MaxGrantableSkillPoints(ref int __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                int multiplier = Instance.SkillPointMultiplier.Value;
                if (multiplier <= 1) return;
                __result *= multiplier;
            }
            */

            private static Player ResolveLocalPlayer(PlayerStats stats)
            {
                try
                {
                    var p = stats.GetComponent<Player>();
                    if (p != null) return p;
                }
                catch { }

                try
                {
                    if (DataStorage.Singleton != null && DataStorage.Singleton.Player != null)
                        return DataStorage.Singleton.Player;
                }
                catch { }

                return null;
            }
        }
    }
}
