using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DimraethMapActionsShopPatch
{
    // [2026-10-01] Standalone Harmony patch plugin for the friend-made
    // DimraethMapActions v1.1.0 Extended DLL. It does NOT reference or rebuild that
    // plugin: the target type is resolved by name from whatever DimraethMapActions
    // assembly BepInEx has already loaded, then its private static
    // MapButtons.FindShop(NPCType) and MapButtons.OpenShop(NPCType, string) are patched.
    //
    // Why: the shop buttons only open when a live, spawned NPCShop exists in the world.
    // World NPCs are spawned by NPCManager/NPCScheduling; if the game has not spawned
    // the owner yet (e.g. right after loading, or the NPC is elsewhere), the original
    // only prints "unavailable". This patch can instead ask NPCManager to reconcile the
    // owner's group so the scheduled variant spawns, then open its shop.
    [BepInPlugin("dev.dimraeth.mapactionsshoppatch", "DimraethMapActions Shop Patch", "1.1.0")]
    [BepInProcess("Dimraeth.exe")]
    public sealed class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static Harmony Harmony;

        // [2026-10-01] Defaults favour making the buttons work; both are configurable.
        internal static bool ForceNpcSpawn;
        internal static bool VerboseLog;
        internal static float OpenWaitSeconds;

        public override void Load()
        {
            Log = base.Log;
            Harmony = new Harmony("dev.dimraeth.mapactionsshoppatch");

            ForceNpcSpawn = Config.Bind(
                "General", "ForceNpcSpawn", true,
                "If the target NPC is not spawned when a shop button is pressed, spawn its registered prefab (mirroring the game's NPC spawn path) and open the shop once stocked. Gated by the NPC's own schedule/quest state, so locked/off-duty NPCs are not conjured. Disable to restore the original 'unavailable' behaviour.").Value;

            VerboseLog = Config.Bind(
                "General", "VerboseLog", true,
                "Log every NPCShop candidate inspected while resolving a shop (helps diagnose missing shops).").Value;

            OpenWaitSeconds = Config.Bind(
                "General", "OpenWaitSeconds", 15f,
                "How long to wait for a requested NPC to spawn before giving up (seconds).").Value;

            ClassInjector.RegisterTypeInIl2Cpp<Patcher>();
            var go = new GameObject("DimraethMapActionsShopPatch");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Patcher>();

            Log.LogInfo("DimraethMapActions Shop Patch armed; waiting for DimraethMapActions to load.");
        }
    }

    // Waits until the original plugin's assembly is present (plugins load sequentially,
    // but we do not depend on load order), installs the prefixes once, and hosts the
    // deferred-open coroutine used when an NPC has to be spawned first.
    public sealed class Patcher : MonoBehaviour
    {
        internal static Patcher Instance;

        public Patcher(IntPtr ptr) : base(ptr) { }

        private bool _done;
        private bool _typeSeen;
        private float _nextAttempt;

        private bool _opening;
        private NPCType _owner;
        private string _caption;
        private float _deadline;
        private float _nextNudge;
        // [2026-10-02] Tracks whether we spawned the owner ourselves and when, so we can
        // wait for the shop's server-side stock to populate before opening it.
        private bool _forceSpawned;
        private float _spawnedAt;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!_done) TryPatch();
            if (_opening) TickDeferredOpen();
        }

        private void TryPatch()
        {
            float now = Time.unscaledTime;
            if (now < _nextAttempt) return;
            _nextAttempt = now + 1f;

            try
            {
                var mapButtons = FindMapButtonsType();
                if (mapButtons == null)
                {
                    if (!_typeSeen)
                    {
                        _typeSeen = true;
                        Plugin.Log.LogInfo("DimraethMapActions not loaded yet - retrying patch.");
                    }
                    return;
                }

                var findShop = AccessTools.Method(mapButtons, "FindShop");
                var openShop = AccessTools.Method(mapButtons, "OpenShop");
                if (findShop == null || openShop == null)
                {
                    Plugin.Log.LogError($"MapButtons found, but FindShop/OpenShop missing (FindShop={findShop != null}, OpenShop={openShop != null}). Patch NOT applied.");
                    _done = true;
                    return;
                }

                Plugin.Harmony.Patch(findShop, prefix: new HarmonyMethod(typeof(ShopFindPatch), nameof(ShopFindPatch.Prefix)));
                Plugin.Harmony.Patch(openShop, prefix: new HarmonyMethod(typeof(OpenShopPatch), nameof(OpenShopPatch.Prefix)));

                Plugin.Log.LogInfo("Patched MapButtons.FindShop + OpenShop -> DimraethMapActions Shop Patch active.");
                _done = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Shop patch attempt failed: {ex}");
            }
        }

        // Targeted type lookup: only inspect the DimraethMapActions assembly instead of
        // AccessTools.TypeByName, which scans every loaded assembly and logs a HarmonyX
        // "GetTypesFromAssembly" warning for each Unity module with unloadable types.
        private static Type FindMapButtonsType()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name;
                    try { name = asm.GetName().Name; }
                    catch { continue; }
                    if (string.IsNullOrEmpty(name) ||
                        name.IndexOf("DimraethMapActions", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    try
                    {
                        var t = asm.GetType("Dimraeth.MapActions.MapButtons", false);
                        if (t != null) return t;
                    }
                    catch { }
                }
            }
            catch { }

            // Only reached if the cheap scan found nothing; may emit HarmonyX warnings.
            return AccessTools.TypeByName("Dimraeth.MapActions.MapButtons");
        }

        internal void BeginDeferredOpen(NPCType owner, string caption, float waitSeconds)
        {
            if (_opening)
            {
                Plugin.Log.LogInfo($"[ShopPatch] already waiting to open {_owner}; ignoring {owner}.");
                return;
            }

            _opening = true;
            _owner = owner;
            _caption = caption;
            _deadline = Time.realtimeSinceStartup + waitSeconds;
            _nextNudge = 0f;
            _forceSpawned = false;
            _spawnedAt = 0f;

            // Close the map first so the world keeps ticking (NPC spawn coroutines use game time).
            try { InGameMenuShellBinder.Close(); }
            catch (Exception ex) { Plugin.Log.LogWarning($"[ShopPatch] could not close map shell: {ex.Message}"); }

            Plugin.Log.LogInfo($"[ShopPatch] {caption}: {owner} not spawned; requesting reconcile and waiting up to {waitSeconds:0}s.");

            // [2026-10-02] The owner is genuinely unspawned because it is in another area
            // (or no scene handler has players in its group), which RequestReconcile cannot
            // fix. Mirror the game's own spawn path here, but only when the owner's
            // schedule/quest state allows it. If it does, keep _spawnedAt so the poll loop
            // waits for the shop stock to populate before opening.
            try
            {
                _forceSpawned = NpcSpawner.TrySpawn(owner);
                if (_forceSpawned) _spawnedAt = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[ShopPatch] force-spawn {owner} error: {ex.Message}");
            }

            // Diagnostic: list every NPCShop candidate once, then dump the owner's
            // registered NPCScheduling group so we can see why it is not active.
            if (Plugin.VerboseLog)
            {
                try { ShopFindPatch.Find(owner, true); } catch { }
                LogNpcState(owner, "begin");
            }
        }

        // [2026-10-01] Temporary diagnostic: shows whether the game has a scheduling
        // group for this owner and whether the schedule/quest state says it should be
        // active. Used to distinguish "not registered" from "scheduled inactive".
        private static void LogNpcState(NPCType owner, string tag)
        {
            try
            {
                var ds = DataStorage.Singleton;
                string pscene = "(no player)";
                if ((bool)ds && (bool)ds.Player) pscene = ds.Player.gameObject.scene.name;

                var tm = TimeManager.Singleton;
                string time = (bool)tm ? tm.CurrentGameTime.ToString() : "(no TimeManager)";
                Plugin.Log.LogInfo($"[ShopPatch][{tag}] owner={owner} playerScene={pscene} time={time}");

                // Every NPCShop candidate whose shared-owner or object name matches.
                int shops = 0;
                string key = owner.ToString();
                foreach (var shop in Resources.FindObjectsOfTypeAll<NPCShop>())
                {
                    if (!(bool)shop) continue;
                    bool nameMatch = shop.gameObject.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (shop.NPCSharedShop != owner && !nameMatch) continue;
                    shops++;
                    var go = shop.gameObject;
                    var npc = shop.GetComponentInParent<NPC>();
                    Plugin.Log.LogInfo($"[ShopPatch][{tag}]   SHOP '{go.name}' scene={go.scene.name} activeHier={go.activeInHierarchy} shopSpawned={shop.IsSpawned} npcType={((bool)npc ? npc.NPCType.ToString() : "none")} npcSpawned={((bool)npc && npc.IsSpawned)} shared={shop.NPCSharedShop}");
                }
                Plugin.Log.LogInfo($"[ShopPatch][{tag}] matching NPCShop instances={shops}");

                // Environment (sprite) schedulers that govern world NPCs.
                int envs = 0;
                foreach (var env in Resources.FindObjectsOfTypeAll<EnvironmentNPCScheduling>())
                {
                    if (!(bool)env) continue;
                    var go = env.gameObject;
                    if (go.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    envs++;
                    bool should = false;
                    try { if ((bool)tm) should = env.ShouldBeActive(tm.CurrentGameTime); } catch { }
                    Plugin.Log.LogInfo($"[ShopPatch][{tag}]   ENV '{go.name}' goScene={go.scene.name} areaScene={env.Scene} activeHier={go.activeInHierarchy} ShouldBeActive={should}");
                }
                Plugin.Log.LogInfo($"[ShopPatch][{tag}] matching EnvironmentNPCScheduling instances={envs}");

                // Networked schedulers (story/quest NPCs).
                int count = 0;
                foreach (var s in Resources.FindObjectsOfTypeAll<NPCScheduling>())
                {
                    if (!(bool)s || s.RootType != owner) continue;
                    count++;
                    bool should = false;
                    try { if ((bool)tm) should = s.ShouldBeActive(tm.CurrentGameTime); } catch { }
                    bool active = false;
                    try { if (s.IsActive != null) active = s.IsActive.Value; } catch { }
                    Plugin.Log.LogInfo($"[ShopPatch][{tag}]   SCHED root={s.RootType} obj='{s.gameObject.name}' scene={s.gameObject.scene.name} spawned={s.IsSpawned} server={s.IsServer} activeHier={s.gameObject.activeInHierarchy} IsActive={active} ShouldBeActive={should} env={s.EnvironmentNPC}");
                }
                Plugin.Log.LogInfo($"[ShopPatch][{tag}] NPCScheduling({owner}) instances={count}");

                var act = NPCManager.GetActive(owner);
                Plugin.Log.LogInfo($"[ShopPatch][{tag}] GetActive={((bool)act ? act.gameObject.name : "null")}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[ShopPatch][{tag}] state dump failed: {ex.Message}");
            }
        }

        // Polls for the requested NPC after a force-spawn / reconcile request. Runs from
        // Update so we avoid interop coroutine plumbing.
        private void TickDeferredOpen()
        {
            float now = Time.realtimeSinceStartup;
            if (now >= _deadline)
            {
                _opening = false;
                if (Plugin.VerboseLog) LogNpcState(_owner, "timeout");
                Plugin.Log.LogInfo($"[ShopPatch] {_caption}: {_owner} is not present in the world right now (not unlocked, off-duty, or in another area), so the shop was not opened.");
                return;
            }

            if (now >= _nextNudge)
            {
                _nextNudge = now + 1f;

                // [2026-10-02] When we spawned the owner ourselves, do NOT also request a
                // reconcile: that could spawn a second variant of the same NPC. Fall back
                // to the original reconcile nudge only when force-spawn was not possible
                // (e.g. the owner is in the active area but not reconciled yet).
                // Original reconcile-only behaviour kept below for reference:
                //   var tm = TimeManager.Singleton;
                //   if ((bool)tm) NPCManager.RequestReconcile(_owner, tm.CurrentGameTime, false, 0.08f);
                if (!_forceSpawned)
                {
                    try
                    {
                        var tm = TimeManager.Singleton;
                        if ((bool)tm) NPCManager.RequestReconcile(_owner, tm.CurrentGameTime, false, 0.08f);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogWarning($"[ShopPatch] RequestReconcile failed: {ex.Message}");
                    }
                }
            }

            var shop = ShopFinder.Find(_owner);
            if ((bool)shop)
            {
                // [2026-10-02] A freshly spawned shop starts empty: its server-side
                // InitializeShopAfterSpawn coroutine populates the NetworkLists one frame
                // later. Opening before stock arrives would show an empty shop, so wait
                // until at least one list has entries (bounded by a short grace period in
                // case a shop legitimately sells nothing).
                if (_forceSpawned && !StockReady(shop) && (now - _spawnedAt) < 3f)
                    return;

                _opening = false;
                OpenNow(shop, _caption);
            }
        }

        // True once the spawned shop's replicated stock has been filled. Returns true on
        // any read error so we never block opening because of an inspection failure.
        private static bool StockReady(NPCShop shop)
        {
            try
            {
                if (shop.ItemsForSale != null && shop.ItemsForSale.Count > 0) return true;
                if (shop.EquipmentForSale != null && shop.EquipmentForSale.Count > 0) return true;
                return false;
            }
            catch { return true; }
        }

        private void OpenNow(NPCShop shop, string caption)
        {
            var ds = DataStorage.Singleton;
            if (!(bool)ds || !(bool)ds.Player || !(bool)ds.PSM)
            {
                Plugin.Log.LogWarning($"[ShopPatch] {caption}: local player UI not ready.");
                return;
            }

            var itemShop = (bool)ds.ItemShop ? ds.ItemShop : ds.Player.GetComponent<ItemShop>();
            if (!(bool)itemShop)
            {
                Plugin.Log.LogWarning($"[ShopPatch] {caption}: local ItemShop UI not found.");
                return;
            }

            try { InGameMenuShellBinder.Close(); }
            catch { }

            ds.PSM.SwapToNewState(PlayerUIState.NPCBuy);
            itemShop.SetShop(shop.gameObject);
            Plugin.Log.LogInfo($"[ShopPatch] opened {caption} -> '{shop.gameObject.name}' scene={shop.gameObject.scene.name}");
        }
    }
}
