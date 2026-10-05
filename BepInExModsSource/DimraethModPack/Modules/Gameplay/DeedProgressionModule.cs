using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    /// <summary>
    /// DeedProgressionModule - "Instant Deed Tiers".
    ///
    /// How vanilla deed progression works (verified against the ISIL dump, 2026-10-01):
    ///   * Tiers live in DeedManager.GetUnlockedTierForDeed(DeedDefinition) - the single accessor
    ///     feeding the board UI slider, the accept flow and the client sync.
    ///   * It is bounded by DeedManager.GetWorldTierCap() (authored to 3 in shipped data, raised
    ///     only by story milestones) and, for sequential deeds, by GetEarnedTierForDeed() which
    ///     reads the per-deed world tier from ServerPersistentData (field BossDeedUnlockedTiers,
    ///     which despite the name holds ALL deeds' world tiers, keyed by deed name).
    ///   * DeedManager.IsTierUnlocked(deed, tier) gates accepting/completing a specific tier.
    ///   * Tiers only ever advance ONE step per completion via UnlockNextWorldDeedTier, called from
    ///     MarkDeedCompletedServerRpc (normal board) and RecordDeedCompletionForQuest (quest turn-in).
    ///     Boss/bounty deeds (DeedType.Boss == 3) use UnlockNextBossTierServerRpc, which routes into
    ///     the SAME UnlockNextWorldDeedTier but against the boss deed's own dictionary key. Boss
    ///     tiers are therefore NOT tied to normal quest progress - each deed advances independently.
    ///
    /// This module forces the world tier cap high and reports every authored tier as unlocked, so
    /// all tiers of every deed (normal AND boss/bounty) are immediately selectable, skipping the
    /// one-step completion ladder. Host-side, matching the game's authority model (the host's
    /// values are synced to clients). Non-destructive: it changes only what the accessors report,
    /// never the persisted world tier / completion data.
    /// </summary>
    public class DeedProgressionModule : ModModuleBase
    {
        public static DeedProgressionModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Instant Deed Tiers";
        public override string Description => "Unlock every Deed Board tier instantly (normal + boss/bounty), skipping the one-step completion ladder";

        public ConfigEntry<int> MaxTierCap;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.DeedProgression";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Instant Deed Tiers (Default: false, Vanilla: false). Reports all authored tiers of every deed as unlocked immediately.");

            MaxTierCap = config.Bind(sec, "MaxTierCap", 10,
                "Highest Deed Board tier treated as unlocked regardless of story milestones or completions (Default: 10, Vanilla: 3, range 1-10).");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
            DimraethModPackPlugin.Log?.LogInfo(
                $"[DeedProgression] Status: Enabled={IsEnabled}, MaxTierCap={MaxTierCap?.Value} (host-side; unlocks normal + boss tiers)");
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntDualSpinner(x, curY, width, "Max Tier Cap", MaxTierCap, 1, 10, "Vanilla: 3", labelStyle, btnStyle);
            GUI.Label(new Rect(x + 10f, curY, width - 10f, 20f),
                "<color=#AAAAAA><i>All authored tiers of every deed (normal + boss) are shown as unlocked while enabled. Host-side.</i></color>",
                labelStyle);
            curY += 24f;
            return curY - y;
        }

        public static class Patches
        {
            /// <summary>
            /// Raise the world tier cap so nothing downstream clamps the authored ladder back to 3.
            /// </summary>
            [HarmonyPatch(typeof(DeedManager), nameof(DeedManager.GetWorldTierCap))]
            [HarmonyPostfix]
            public static void Post_GetWorldTierCap(ref int __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                int cap = Instance.MaxTierCap != null ? Instance.MaxTierCap.Value : 10;
                if (__result < cap) __result = cap;
            }

            /// <summary>
            /// Treat every tier as unlocked so the host never rejects accepting/completing a high tier.
            /// (Private method - patched by name.)
            /// </summary>
            [HarmonyPatch(typeof(DeedManager), "IsTierUnlocked")]
            [HarmonyPostfix]
            public static void Post_IsTierUnlocked(ref bool __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                __result = true;
            }

            /// <summary>
            /// Report the full authored tier count as the highest unlocked tier, so the board UI slider
            /// and accept flow expose every tier of both normal and boss deeds.
            /// </summary>
            [HarmonyPatch(typeof(DeedManager), nameof(DeedManager.GetUnlockedTierForDeed))]
            [HarmonyPostfix]
            public static void Post_GetUnlockedTierForDeed(DeedDefinition deed, ref int __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (deed == null) return;

                int cap = Instance.MaxTierCap != null ? Instance.MaxTierCap.Value : 10;
                // GetTierCount() = number of authored DeedTierConfiguration entries, i.e. the true
                // top of the ladder (MaxTiers is the editor-side declared max and can exceed it).
                int authored = deed.GetTierCount();
                int target = Math.Min(cap, Math.Max(1, authored));
                if (__result < target) __result = target;
            }
        }
    }
}
