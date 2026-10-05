using System;
using Unity.Netcode;
using UnityEngine;

namespace DimraethMapActionsShopPatch
{
    // [2026-10-02] Opens shops for owners the game has not network-spawned in the
    // player's current area (e.g. Befr while the player is in the farmlands).
    //
    // The map button only opens a shop when a live, *stocked* NPCShop exists. The game
    // spawns world NPCs from registered prefabs in NetworkPrefabManager.NPCPrefabs via
    // SceneHandler.SpawnNPCsCoroutine, but it only runs that for a scene whose group has
    // players in it. When the player is elsewhere the owner is left as an inactive,
    // unspawned scene object, so its NPCShop has no populated stock and the button does
    // nothing (NPCManager.RequestReconcile cannot spawn an NPC outside the active area
    // either). This mirrors the game's own spawn path instead, but ONLY for owners whose
    // own scheduling/quest gate (NPCScheduling.ShouldBeActive) says they should be
    // visible right now. Quest-locked or off-duty NPCs - e.g. Saqi before she is unlocked
    // - are therefore NOT conjured; their buttons keep behaving as before.
    internal static class NpcSpawner
    {
        internal static bool TrySpawn(NPCType owner)
        {
            var log = Plugin.Log;
            try
            {
                var nm = NetworkManager.Singleton;
                if ((bool)nm && !nm.IsServer)
                {
                    log?.LogInfo($"[ShopPatch] {owner}: not the network server; cannot spawn.");
                    return false;
                }

                var mgr = NetworkPrefabManager.Singleton;
                if (!(bool)mgr || mgr.NPCPrefabs == null)
                {
                    log?.LogWarning("[ShopPatch] NetworkPrefabManager/NPCPrefabs unavailable; cannot force-spawn.");
                    return false;
                }

                GameTime time = default;
                var tm = TimeManager.Singleton;
                if ((bool)tm) time = tm.CurrentGameTime;

                // Respect the game's own visibility gate (schedule + quests). This is the
                // key safety check: a locked or off-duty NPC reports false and we stop.
                if (!AnyActiveSchedule(owner, time))
                {
                    log?.LogInfo($"[ShopPatch] {owner}: schedule/quests say it should not be active now; not spawning.");
                    return false;
                }

                NPCType chosen;
                var prefab = FindPrefab(mgr, owner, time, out chosen);
                if (!(bool)prefab)
                {
                    log?.LogWarning($"[ShopPatch] no registered NPC prefab found for {owner}.");
                    return false;
                }

                GetPose(owner, prefab, out Vector3 pos, out Quaternion rot);

                var clone = UnityEngine.Object.Instantiate(prefab, pos, rot);
                if (!(bool)clone)
                {
                    log?.LogWarning("[ShopPatch] Instantiate returned null.");
                    return false;
                }
                clone.name = $"ShopPatch_{owner}";
                try { if (!clone.gameObject.activeSelf) clone.gameObject.SetActive(true); } catch { }

                if (!clone.IsSpawned) clone.Spawn(false);

                log?.LogInfo($"[ShopPatch] spawned {owner} from prefab '{chosen}' at {pos}.");
                return true;
            }
            catch (Exception ex)
            {
                log?.LogError($"[ShopPatch] force-spawn {owner} failed: {ex}");
                return false;
            }
        }

        // True if any live (scene-placed) NPCScheduling for this owner group is currently
        // allowed to be visible. Prefab assets (invalid scene) are ignored so a default
        // schedule on an asset cannot accidentally unlock a quest-gated NPC.
        internal static bool AnyActiveSchedule(NPCType owner, GameTime time)
        {
            try
            {
                foreach (var s in Resources.FindObjectsOfTypeAll<NPCScheduling>())
                {
                    if (!(bool)s || s.RootType != owner) continue;
                    var go = s.gameObject;
                    if (!go.scene.IsValid()) continue; // skip prefab assets
                    try { if (s.ShouldBeActive(time)) return true; } catch { }
                }
            }
            catch { }
            return false;
        }

        // Pick a registered prefab whose NPCShop belongs to the owner, preferring the
        // variant the schedule currently wants active.
        private static NetworkObject FindPrefab(NetworkPrefabManager mgr, NPCType owner, GameTime time, out NPCType chosen)
        {
            NetworkObject best = null;
            NPCType bestType = NPCType.None;
            int bestScore = -1;
            var prefabs = mgr.NPCPrefabs;

            foreach (var kv in prefabs)
            {
                var no = kv.Value;
                if (!(bool)no) continue;
                var shop = no.GetComponent<NPCShop>();
                if (!(bool)shop) continue;
                if (!ShopFindPatch.MatchesOwner(shop.NPCSharedShop, owner)) continue;

                int score = shop.NPCSharedShop == owner ? 2 : 1;
                var sched = no.GetComponent<NPCScheduling>();
                if ((bool)sched)
                {
                    try { if (sched.ShouldBeActive(time)) score += 4; } catch { }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = no;
                    bestType = kv.Key;
                }
            }

            chosen = bestType;
            return best;
        }

        // Use the owner's authored scene placement when available so the NPC appears at
        // its real spot; otherwise fall back to the prefab transform.
        private static void GetPose(NPCType owner, NetworkObject prefab, out Vector3 pos, out Quaternion rot)
        {
            try
            {
                foreach (var shop in Resources.FindObjectsOfTypeAll<NPCShop>())
                {
                    if (!(bool)shop) continue;
                    if (!ShopFindPatch.MatchesOwner(shop.NPCSharedShop, owner)) continue;
                    var go = shop.gameObject;
                    if (!go.scene.IsValid()) continue;
                    pos = go.transform.position;
                    rot = go.transform.rotation;
                    return;
                }
            }
            catch { }
            pos = prefab.transform.position;
            rot = prefab.transform.rotation;
        }
    }
}
