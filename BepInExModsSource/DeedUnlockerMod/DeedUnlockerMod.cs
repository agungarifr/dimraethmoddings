using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace DeedUnlockerMod
{
    /// <summary>
    /// DeedUnlockerMod — unlocks the full Deed (Quest) Board tier ladder and raises the
    /// deed loot rarity/star caps.
    ///
    /// Fixes two vanilla walls:
    ///   1) The Deed Board only lets you play/complet tiers up to a world tier cap
    ///      (authored to 3 in shipped data, raised only by story milestones). This mod
    ///      forces the cap to the configured max so all authored tiers open immediately.
    ///   2) Deed equipment loot is clamped to Mythical rarity (and a star cap). This mod
    ///      raises the ceiling so Heroic / Ancient gear (up to 9 stars) can drop.
    ///
    /// Host-side patches, so it works in single-player and self-hosted multiplayer
    /// (the host's cap is the authoritative one and is synced to clients).
    /// </summary>
    [BepInPlugin("com.freebuff.deedunlocker", "DeedUnlockerMod", "1.0.0")]
    public class DeedUnlockerPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<int> MaxTierCap;
        public static ConfigEntry<Runes.Rarity> MaxLootRarity;
        public static ConfigEntry<Runes.Stars> MaxLootStars;

        public override void Load()
        {
            Log = base.Log;

            MaxTierCap = Config.Bind("DeedBoard", "MaxTierCap", 10,
                "Highest Deed Board tier that is unlocked regardless of story milestones. Vanilla caps this at 3 (range 1-10).");
            MaxLootRarity = Config.Bind("Loot", "MaxLootRarity", Runes.Rarity.Ancient,
                "Highest equipment rarity deed quests may drop. Vanilla caps this at Mythical.");
            MaxLootStars = Config.Bind("Loot", "MaxLootStars", Runes.Stars.Nine,
                "Highest equipment star rating deed quests may drop. Vanilla caps this lower.");

            var harmony = new Harmony("com.freebuff.deedunlocker");
            harmony.PatchAll(typeof(DeedUnlockerPlugin).Assembly);

            Log.LogInfo("=================================================");
            Log.LogInfo("DeedUnlockerMod v1.0.0 Initialized!");
            Log.LogInfo($"Deed Tier Cap: {MaxTierCap.Value} (all tiers unlocked)");
            Log.LogInfo($"Deed Loot Max Rarity: {MaxLootRarity.Value}");
            Log.LogInfo($"Deed Loot Max Stars: {MaxLootStars.Value}");
            Log.LogInfo("Single-player + self-hosted multiplayer (host-side patches).");
            Log.LogInfo("=================================================");
        }
    }

    /// <summary>
    /// Forces the world tier cap to the configured max so every authored tier is
    /// available regardless of BaseTierCap or story milestones.
    /// </summary>
    [HarmonyPatch(typeof(DeedManager), nameof(DeedManager.GetWorldTierCap))]
    public static class Patch_WorldTierCap
    {
        public static void Postfix(ref int __result)
        {
            int cap = DeedUnlockerPlugin.MaxTierCap != null ? DeedUnlockerPlugin.MaxTierCap.Value : 10;
            if (__result < cap)
            {
                __result = cap;
            }
        }
    }

    /// <summary>
    /// Treats every tier as unlocked so the server never rejects an accepted/higher tier.
    /// (Private method — patched by name.)
    /// </summary>
    [HarmonyPatch(typeof(DeedManager), "IsTierUnlocked")]
    public static class Patch_IsTierUnlocked
    {
        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

    /// <summary>
    /// Raises the per-deed "unlocked tier" to the full authored tier count so the UI
    /// slider and accept flow expose all tiers of every deed.
    /// </summary>
    [HarmonyPatch(typeof(DeedManager), nameof(DeedManager.GetUnlockedTierForDeed))]
    public static class Patch_UnlockedTierForDeed
    {
        public static void Postfix(DeedDefinition deed, ref int __result)
        {
            if (deed == null) return;
            int cap = DeedUnlockerPlugin.MaxTierCap != null ? DeedUnlockerPlugin.MaxTierCap.Value : 10;
            int authoredMax = Math.Max(1, deed.MaxTiers);
            int target = Math.Min(cap, authoredMax);
            if (__result < target)
            {
                __result = target;
            }
        }
    }

    /// <summary>
    /// Raises the deed equipment loot rarity + star ceiling to the configured max.
    /// GetConfigForTier is the accessor feeding both the reward roll and the board UI.
    /// </summary>
    [HarmonyPatch(typeof(DeedDefinition), nameof(DeedDefinition.GetConfigForTier))]
    public static class Patch_DeedLootCaps
    {
        public static void Postfix(DeedDefinition __instance, int tier, ref DeedTierConfiguration __result)
        {
            try
            {
                if (__result == null || __result.EquipmentLootConfig == null) return;

                var maxRarity = DeedUnlockerPlugin.MaxLootRarity != null ? DeedUnlockerPlugin.MaxLootRarity.Value : Runes.Rarity.Ancient;
                var maxStars = DeedUnlockerPlugin.MaxLootStars != null ? DeedUnlockerPlugin.MaxLootStars.Value : Runes.Stars.Nine;

                if (__result.EquipmentLootConfig.MaxRarity < maxRarity)
                {
                    __result.EquipmentLootConfig.MaxRarity = maxRarity;
                }
                if (__result.EquipmentLootConfig.MaxStars < maxStars)
                {
                    __result.EquipmentLootConfig.MaxStars = maxStars;
                }
            }
            catch (Exception ex)
            {
                DeedUnlockerPlugin.Log?.LogError($"[DeedUnlocker] Error in Patch_DeedLootCaps: {ex}");
            }
        }
    }
}
