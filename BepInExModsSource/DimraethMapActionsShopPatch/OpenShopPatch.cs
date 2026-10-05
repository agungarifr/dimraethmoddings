using System;
using HarmonyLib;

namespace DimraethMapActionsShopPatch
{
    // Prefix for Dimraeth.MapActions.MapButtons.OpenShop(NPCType, string).
    //
    // If the owner's NPC is already spawned, the original code runs unchanged.
    // If it is not, the game has simply not spawned that NPC (it only spawns world NPCs
    // for scenes that have players in their group, so an owner in another area is left
    // unspawned) and the original would only print "unavailable". Instead we close the
    // map and hand off to Patcher, which mirrors the game's spawn path (NpcSpawner),
    // waits for the shop's stock to populate, and then opens it exactly like the
    // original would.
    internal static class OpenShopPatch
    {
        public static bool Prefix(NPCType owner, string caption)
        {
            try
            {
                if ((bool)ShopFinder.Find(owner)) return true; // already available: original opens it
                if (!Plugin.ForceNpcSpawn) return true;        // opt-out: keep the original "unavailable" message
                if (Patcher.Instance == null) return true;

                Patcher.Instance.BeginDeferredOpen(owner, caption, Plugin.OpenWaitSeconds);
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[ShopPatch] OpenShop prefix error: {ex}");
                return true;
            }
        }
    }

    // Kept as a tiny alias so the finder can be shared without exposing it publicly.
    internal static class ShopFinder
    {
        internal static NPCShop Find(NPCType owner) => ShopFindPatch.Find(owner);
    }
}
