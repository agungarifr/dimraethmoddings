using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace DimraethSanctumChests
{
    // Finds the chests placed in the player's base and opens one on demand.
    //
    // Sanctum == the PlayerBase scene. Its numeric value is 29 in the game's Scene enum
    // (the same way RahanerChestMod compares GuildHall == 24). We compare numerically so
    // we do not have to name the (internal) enum type.
    //
    // [2026-10-02] Area-independent listing. The base's chest GameObjects are despawned
    // while no player is in the PlayerBase scene, so a live-object scan returns nothing
    // when the player is elsewhere. We therefore build the list from StorageDirectory.BuildFor
    // -- the same directory the pet "Send To" menu uses -- which reads the base's save
    // records and therefore still sees the chests from any area. Each row is upgraded to a
    // live Storage via StorageDirectory.FindLive when it is spawned; if it is not, opening
    // asks the host to respawn the base's built objects first.
    internal static class ChestDirectory
    {
        private const int PlayerBaseScene = 29; // Areas.Scene.PlayerBase / Scene.PlayerBase (the Sanctum)

        internal sealed class ChestInfo
        {
            internal FixedString64Bytes Key; // save key used by StorageDirectory.FindLive
            internal string Name;
            internal int Used;
            internal int Total;
            internal Storage Live;           // non-null only while the chest is network-spawned

            internal bool Openable
            {
                get { try { return (bool)Live && Live.IsSpawned; } catch { return false; } }
            }

            internal string Label => $"{Name} ({Used}/{Total})";
        }

        // Area-independent chest list, newest behaviour. Returns every Sanctum chest the
        // local player may access, whether or not its GameObject is currently spawned.
        internal static List<ChestInfo> GetSanctumChests()
        {
            var result = new List<ChestInfo>();

            var requester = GetLocalPlayer();
            if (requester != null)
            {
                try
                {
                    var rows = StorageDirectory.BuildFor(requester);
                    if (rows != null)
                    {
                        for (int i = 0; i < rows.Count; i++)
                        {
                            ChestDirectoryEntry row = rows[i];

                            int scene;
                            try { scene = (int)row.Scene; } catch { continue; }
                            if (scene != PlayerBaseScene) continue;

                            var info = new ChestInfo
                            {
                                Key = row.StorageName,
                                Name = RowName(row),
                                Used = row.UsedSlots,
                                Total = row.TotalSlots,
                            };

                            try { info.Live = StorageDirectory.FindLive(row.StorageName); } catch { }
                            if (info.Total <= 0 && (bool)info.Live) info.Total = LiveTotal(info.Live);
                            if (string.IsNullOrEmpty(info.Name)) info.Name = "Chest";

                            result.Add(info);
                        }
                    }
                    else if (Plugin.VerboseLog)
                    {
                        Plugin.Log?.LogWarning("Sanctum chests: StorageDirectory.BuildFor returned null; using a live scan.");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"Sanctum chests: StorageDirectory.BuildFor failed ({ex.Message}); using a live scan.");
                }
            }

            // Fallback (and the whole job while the player is inside the base): scan spawned
            // chests directly, so the panel still works if the directory is unavailable.
            if (result.Count == 0)
                result = LiveScan();

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            if (Plugin.VerboseLog)
            {
                int openable = 0;
                for (int i = 0; i < result.Count; i++) if (result[i].Openable) openable++;
                Plugin.Log?.LogInfo($"Sanctum chest scan: {result.Count} chest(s) listed, {openable} spawned/openable.");
            }

            return result;
        }

        private static Player GetLocalPlayer()
        {
            try
            {
                var ds = DataStorage.Singleton;
                if ((bool)ds && (bool)ds.Player) return ds.Player;
            }
            catch { }
            return null;
        }

        // Scans spawned chest storages in the base. Kept as the fallback source and as the
        // only source when the player is inside the Sanctum, where every chest is live.
        private static List<ChestInfo> LiveScan()
        {
            var result = new List<ChestInfo>();
            int scanned = 0;
            int inScene = 0;
            int unspawned = 0;

            foreach (var s in Resources.FindObjectsOfTypeAll<Storage>())
            {
                if (!(bool)s) continue;
                scanned++;

                try
                {
                    if (!s.gameObject.scene.IsValid()) continue; // skip prefab assets
                    if (!IsChest(s)) continue;
                    if (!IsInSanctum(s)) continue;
                    inScene++;

                    if (!s.IsSpawned)
                    {
                        unspawned++;
                        continue; // no replicated contents yet, cannot open
                    }

                    if (Plugin.OnlyAccessible)
                    {
                        bool allowed = true;
                        try { allowed = s.CanLocalPlayerAccessContents(); } catch { allowed = true; }
                        if (!allowed) continue;
                    }

                    result.Add(BuildLiveInfo(s));
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"Sanctum chest scan skipped a storage: {ex.Message}");
                }
            }

            if (Plugin.VerboseLog)
                Plugin.Log?.LogInfo($"Sanctum live scan: {scanned} storage(s) loaded, {inScene} chest(s) in PlayerBase, {unspawned} not spawned, {result.Count} listed.");

            return result;
        }

        private static ChestInfo BuildLiveInfo(Storage s)
        {
            var info = new ChestInfo { Live = s, Name = LiveName(s) };
            try { info.Key = s.StorageName.Value; } catch { }
            info.Used = LiveUsed(s);
            info.Total = LiveTotal(s);
            if (info.Total <= 0) info.Total = 18;
            if (string.IsNullOrEmpty(info.Name)) info.Name = "Chest";
            return info;
        }

        private static int LiveUsed(Storage s)
        {
            int used = 0;
            try
            {
                if (s.Inventory != null)
                    for (int i = 0; i < s.Inventory.Count; i++)
                        if (!s.Inventory[i].IsEmpty) used++;
            }
            catch { }
            return used;
        }

        private static int LiveTotal(Storage s)
        {
            int total = 0;
            try { if (s.StorageData != null) total = Math.Max(total, s.StorageData.StorageSize); } catch { }
            try { if (s.Inventory != null) total = Math.Max(total, s.Inventory.Count); } catch { }
            return total;
        }

        private static bool IsChest(Storage s)
        {
            try { if (s.IsChestContainer) return true; } catch { }

            var c = s.Container;
            return c == StorageContainers.ChestBasic
                || c == StorageContainers.ChestBarrel
                || c == StorageContainers.PinewoodChest
                || c == StorageContainers.OakCabinet
                || c == StorageContainers.HeartOakChest;
        }

        private static bool IsInSanctum(Storage s)
        {
            // Networked scene value (set for spawned storages).
            try { if (s.Scene != null && (int)s.Scene.Value == PlayerBaseScene) return true; } catch { }

            // Server-side save record.
            try { if (s.StorageData != null && (int)s.StorageData.Scene == PlayerBaseScene) return true; } catch { }

            // Authored scene name fallback.
            try
            {
                var go = s.gameObject;
                if (go.scene.IsValid() && go.scene.name == "PlayerBase") return true;
            }
            catch { }

            return false;
        }

        // Name for a directory row. Prefer the managed StorageData.DisplayName (the name the
        // player assigned) over the raw FixedString fields.
        private static string RowName(ChestDirectoryEntry row)
        {
            try
            {
                var data = StorageDirectory.FindData(row.StorageName);
                if (data != null && !string.IsNullOrEmpty(data.DisplayName)) return data.DisplayName;
            }
            catch { }

            try
            {
                var d = row.DisplayName.ToString();
                if (!string.IsNullOrEmpty(d)) return d;
            }
            catch { }

            try
            {
                var n = row.StorageName.ToString();
                if (!string.IsNullOrEmpty(n)) return n;
            }
            catch { }

            return null;
        }

        // Player-facing name for a live storage: the renamed DisplayName first, then the
        // save record name, then the stable storage name, then the unique id / object name.
        private static string LiveName(Storage s)
        {
            try
            {
                if (s.DisplayName != null)
                {
                    var n = s.DisplayName.Value.ToString();
                    if (!string.IsNullOrEmpty(n)) return n;
                }
            }
            catch { }

            try { if (s.StorageData != null && !string.IsNullOrEmpty(s.StorageData.DisplayName)) return s.StorageData.DisplayName; } catch { }

            try
            {
                if (s.StorageName != null)
                {
                    var n = s.StorageName.Value.ToString();
                    if (!string.IsNullOrEmpty(n)) return n;
                }
            }
            catch { }

            try { if (!string.IsNullOrEmpty(s.UniqueID)) return s.UniqueID; } catch { }

            return null;
        }

        // Resolves the currently spawned Storage for a chest row, or null when the chest is
        // despawned because the player is not in the base.
        internal static Storage ResolveLive(ChestInfo info)
        {
            if (info == null) return null;
            try { if ((bool)info.Live && info.Live.IsSpawned) return info.Live; } catch { }
            try { return StorageDirectory.FindLive(info.Key); } catch { return null; }
        }

        // Opens a chest, or asks the host to respawn the base and defers the open until it
        // appears. Call from the UI thread.
        internal static void Open(ChestInfo info)
        {
            if (info == null) return;

            var ds = DataStorage.Singleton;
            if (!(bool)ds || !(bool)ds.PSM)
            {
                Plugin.Log?.LogWarning("Sanctum chests: local player UI is not ready.");
                return;
            }

            // Close the map first so the world/UI can switch cleanly.
            try { InGameMenuShellBinder.Close(); } catch { }

            var live = ResolveLive(info);
            if ((bool)live && live.IsSpawned)
            {
                OpenLive(live, info.Name);
                return;
            }

            // The chest exists in the save but its GameObject is despawned because no player
            // is in the PlayerBase scene. Ask the (host) server to respawn the base's built
            // objects, then let MapPatchInstaller open it once it appears.
            if (RequestBaseSpawn())
            {
                Plugin.Log?.LogInfo($"Sanctum chests: '{info.Name}' is not spawned; requested base respawn, waiting to open.");
                MapPatchInstaller.RequestDeferredOpen(info);
                return;
            }

            Plugin.Log?.LogWarning($"Sanctum chests: '{info.Name}' is not spawned and the base could not be respawned on this peer.");
        }

        // Routes through the game's own Storage.OpenChest so any installed chest-size mod
        // (DimraethModPack) expands the storage and picks the correct storage view. The old
        // direct view-binding is kept as a fallback for when a mod's prefix throws.
        internal static void OpenLive(Storage s, string name)
        {
            try
            {
                s.OpenChest();
                Plugin.Log?.LogInfo($"Sanctum chests: opened '{name}' via Storage.OpenChest().");
                return;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Sanctum chests: Storage.OpenChest() failed ({ex.Message}); using the direct view fallback.");
            }

            try
            {
                var ds = DataStorage.Singleton;
                StorageView view = null;
                if ((bool)ds.LargeChest) view = ds.LargeChest;
                else if ((bool)ds.Chest) view = ds.Chest;

                if ((bool)view)
                {
                    view.SetStorage(s);
                    ds.ActiveChestView = view;
                    ds.PSM.SwapToNewState(PlayerUIState.Storage);
                    Plugin.Log?.LogInfo($"Sanctum chests: opened '{name}' via {view.GetType().Name} fallback.");
                    return;
                }

                s.OpenStorage();
                ds.PSM.SwapToNewState(PlayerUIState.Storage);
                Plugin.Log?.LogInfo($"Sanctum chests: opened '{name}' via Storage.OpenStorage().");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Sanctum chests: open fallback failed: {ex}");
            }
        }

        private static float _lastSpawnRequest;

        // Asks the host to (re)spawn the base's missing network build objects, which brings
        // the chest storages back so they can be opened. Returns false when this peer is not
        // the server (a client cannot spawn them) or the manager is unavailable.
        internal static bool RequestBaseSpawn()
        {
            bool server = false;
            try
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                server = nm != null && nm.IsServer;
            }
            catch { }
            if (!server) return false;

            float now = Time.unscaledTime;
            if (now - _lastSpawnRequest < 5f) return true; // a request is already in flight

            try
            {
                var pbm = PlayerBaseManager.Singleton;
                if (!(bool)pbm) return false;

                pbm.SpawnMissingNetworkBuildObjects();
                _lastSpawnRequest = now;
                Plugin.Log?.LogInfo("Sanctum chests: requested PlayerBaseManager.SpawnMissingNetworkBuildObjects().");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Sanctum chests: base respawn request failed: {ex.Message}");
                return false;
            }
        }

        /* [2026-10-02] OBSOLETE -- superseded by GetSanctumChests + StorageDirectory.BuildFor
           above. Why obsolete: this scan only saw chests whose GameObjects were currently
           loaded, so it returned nothing (and the label read xx/18) whenever the player was
           outside the Sanctum. The new code lists from the area-independent save directory
           and respawns the base on demand. Kept for reference.
        internal static List<ChestInfo> GetSanctumChests_Legacy()
        {
            var result = new List<ChestInfo>();
            int scanned = 0;
            int inScene = 0;
            int unspawned = 0;

            foreach (var s in Resources.FindObjectsOfTypeAll<Storage>())
            {
                if (!(bool)s) continue;
                scanned++;

                try
                {
                    if (!s.gameObject.scene.IsValid()) continue;
                    if (!IsChest(s)) continue;
                    if (!IsInSanctum(s)) continue;
                    inScene++;

                    if (!s.IsSpawned) { unspawned++; continue; }

                    if (Plugin.OnlyAccessible)
                    {
                        bool allowed = true;
                        try { allowed = s.CanLocalPlayerAccessContents(); } catch { allowed = true; }
                        if (!allowed) continue;
                    }

                    result.Add(BuildInfo(s));
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"Sanctum chest scan skipped a storage: {ex.Message}");
                }
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            if (Plugin.VerboseLog)
                Plugin.Log?.LogInfo($"Sanctum chest scan: {scanned} storage(s) loaded, {inScene} chest(s) in PlayerBase, {unspawned} not spawned, {result.Count} listed.");

            return result;
        }

        private static ChestInfo BuildInfo(Storage s)
        {
            var info = new ChestInfo { Storage = s, Name = ResolveName(s) };

            try
            {
                if (s.Inventory != null)
                {
                    info.Total = s.Inventory.Count;
                    for (int i = 0; i < s.Inventory.Count; i++)
                        if (!s.Inventory[i].IsEmpty) info.Used++;
                }
            }
            catch { }

            if (info.Total <= 0)
            {
                try { if (s.StorageData != null) info.Total = s.StorageData.StorageSize; } catch { }
            }

            if (string.IsNullOrEmpty(info.Name)) info.Name = "Chest";
            return info;
        }

        internal static void Open(Storage s)
        {
            var ds = DataStorage.Singleton;
            if (!(bool)ds || !(bool)ds.PSM)
            {
                Plugin.Log?.LogWarning("Sanctum chests: local player UI is not ready.");
                return;
            }

            try { InGameMenuShellBinder.Close(); } catch { }

            int slots = 18;
            try { if (s.StorageData != null && s.StorageData.StorageSize > 0) slots = s.StorageData.StorageSize; } catch { }

            StorageView view = null;
            if (slots > 18 && (bool)ds.LargeChest) view = ds.LargeChest;
            else if ((bool)ds.Chest) view = ds.Chest;
            else if ((bool)ds.LargeChest) view = ds.LargeChest;

            if ((bool)view)
            {
                view.SetStorage(s);
                ds.ActiveChestView = view;
                ds.PSM.SwapToNewState(PlayerUIState.Storage);
                Plugin.Log?.LogInfo($"Sanctum chests: opened '{ResolveName(s)}' via {view.GetType().Name} ({slots} slots).");
                return;
            }

            s.OpenStorage();
            ds.PSM.SwapToNewState(PlayerUIState.Storage);
            Plugin.Log?.LogInfo($"Sanctum chests: opened '{ResolveName(s)}' via Storage.OpenStorage().");
        }
        */
    }
}
