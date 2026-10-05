using System;
using HarmonyLib;
using UnityEngine;

namespace DimraethMapActionsShopPatch
{
    // Prefix for Dimraeth.MapActions.MapButtons.FindShop(NPCType).
    //
    // The original implementation only accepts an NPCShop whose NetworkBehaviour
    // IsSpawned flag is true. World NPCs (Befr, Saqi, ...) are ordinary scene objects
    // that the game spawns/despawns through NPCManager/NPCScheduling; until the game
    // reconciles them they are present but not spawned, so the original lookup returns
    // null and the button reports "unavailable". This finder returns the same shops the
    // game's own interaction uses, and is reused by OpenShopPatch to know whether an
    // NPC is already available.
    internal static class ShopFindPatch
    {
        public static bool Prefix(NPCType owner, ref NPCShop __result)
        {
            var log = Plugin.Log;
            try
            {
                var shop = Find(owner, Plugin.VerboseLog);
                if ((bool)shop)
                {
                    log?.LogInfo($"[ShopPatch] resolved {owner}: '{shop.gameObject.name}' scene={shop.gameObject.scene.name}");
                    __result = shop;
                    return false; // skip the original (narrower) lookup
                }
                log?.LogInfo($"[ShopPatch] no spawned NPCShop for {owner} yet");
            }
            catch (Exception ex)
            {
                log?.LogError($"[ShopPatch] prefix error: {ex}");
            }
            return true; // let the original run (it shows its own message if it also fails)
        }

        // Returns a *usable* (spawned) NPCShop for the owner, or null.
        // Matching is by NPCSharedShop, which is the reliable owner key: it is set on
        // every prefab variant (Befr/BefrSitting/BefrInPlayerBaseA all share "Befr"),
        // unlike NPC.NPCType which is only populated once the NPC is activated.
        // logCandidates is only enabled for a single button press; the polling loop
        // calls this silently every frame.
        internal static NPCShop Find(NPCType owner, bool logCandidates = false)
        {
            var active = NPCManager.GetActive(owner);
            if ((bool)active)
            {
                var activeShop = ShopOn(active.gameObject);
                if ((bool)activeShop) return activeShop;
            }

            NPCShop loose = null;
            int scanned = 0;
            foreach (var s in Resources.FindObjectsOfTypeAll<NPCShop>())
            {
                if (!(bool)s) continue;
                scanned++;

                if (logCandidates)
                {
                    var npc = s.GetComponentInParent<NPC>();
                    NPCType nt = (bool)npc ? npc.NPCType : NPCType.None;
                    Plugin.Log?.LogInfo($"[ShopPatch]   NPCShop '{s.gameObject.name}' owner={nt} shared={s.NPCSharedShop} spawned={s.IsSpawned} loaded={s.gameObject.scene.isLoaded} scene={s.gameObject.scene.name} shopName={s.ShopName}");
                }

                if (!s.IsSpawned || !s.gameObject.scene.isLoaded) continue;
                if (MatchesOwner(s.NPCSharedShop, owner))
                {
                    if (s.gameObject.activeInHierarchy) return s;
                    if (!(bool)loose) loose = s;
                }
                else if (!(bool)loose && NameHint(s, owner))
                {
                    loose = s;
                }
            }

            if (logCandidates) Plugin.Log?.LogInfo($"[ShopPatch] scanned {scanned} NPCShop instance(s)");
            return loose;
        }

        // NPCShop rides on the NPC prefab; look on the object itself then its children.
        private static NPCShop ShopOn(GameObject go)
        {
            if (!(bool)go) return null;
            var direct = go.GetComponent<NPCShop>();
            if ((bool)direct && direct.IsSpawned) return direct;
            var child = go.GetComponentInChildren<NPCShop>(true);
            if ((bool)child && child.IsSpawned) return child;
            return null;
        }

        // [2026-10-02] Made internal so NpcSpawner can reuse the same owner-variant
        // mapping when choosing which registered NPC prefab to spawn.
        internal static bool MatchesOwner(NPCType candidate, NPCType owner)
        {
            if (candidate == owner) return true;
            switch (owner)
            {
                case NPCType.Befr:
                    return candidate == NPCType.BefrIndoors
                        || candidate == NPCType.BefrSitting
                        || candidate == NPCType.BefrInPlayerBaseA;
                case NPCType.Saqi:
                    return candidate == NPCType.SaqiGuildHall
                        || candidate == NPCType.SaqiCooking
                        || candidate == NPCType.SaqiCleaningMug
                        || candidate == NPCType.SaqiSweeping;
                default:
                    return false;
            }
        }

        private static bool NameHint(NPCShop shop, NPCType owner)
        {
            string name = owner.ToString();
            if (!string.IsNullOrEmpty(shop.ShopName) && shop.ShopName.Contains(name, StringComparison.OrdinalIgnoreCase)) return true;
            return shop.gameObject.name.Contains(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
