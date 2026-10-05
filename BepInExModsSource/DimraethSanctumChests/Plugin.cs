using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DimraethSanctumChests
{
    // [2026-10-02] Standalone Harmony plugin, deliberately separate from the friend-made
    // DimraethMapActions DLL and from DimraethMapActionsShopPatch.
    //
    // What it does: appends a "Sanctum Chests" list underneath the map quick-actions
    // panel that DimraethMapActions already draws. It lists one clickable button per
    // chest placed in the player's base (the PlayerBase scene, "Sanctum"), labelled with
    // the chest's display name and slot usage, and opens that chest remotely via the
    // game's own storage UI.
    //
    // How it hooks without touching the friend's DLL: the target type
    // Dimraeth.MapActions.MapButtons is resolved by name from whatever DimraethMapActions
    // assembly BepInEx has loaded, then its private static MapButtons.Build(MapContentView)
    // is Harmony-postfixed to attach our ChestButtonPanel to the panel it returns.
    [BepInPlugin("dev.dimraeth.sanctumchests", "Dimraeth Sanctum Chest Buttons", "1.0.0")]
    [BepInProcess("Dimraeth.exe")]
    public sealed class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static Harmony Harmony;

        internal static bool Enabled;
        internal static bool VerboseLog;
        internal static bool OnlyAccessible;
        internal static float PanelHeight;

        public override void Load()
        {
            Log = base.Log;
            Harmony = new Harmony("dev.dimraeth.sanctumchests");

            Enabled = Config.Bind(
                "General", "Enabled", true,
                "Add the Sanctum Chests button list under the map quick-actions panel.").Value;

            VerboseLog = Config.Bind(
                "General", "VerboseLog", true,
                "Log the storage scan while building the Sanctum chest list (helps diagnose missing chests).").Value;

            OnlyAccessible = Config.Bind(
                "General", "OnlyAccessible", true,
                "Only list chests whose contents the local player is allowed to open (skips other players' locked chests).").Value;

            PanelHeight = Config.Bind(
                "General", "PanelHeight", 190f,
                "Height in pixels of the scrollable Sanctum Chests panel.").Value;

            ClassInjector.RegisterTypeInIl2Cpp<ChestButtonPanel>();
            ClassInjector.RegisterTypeInIl2Cpp<MapPatchInstaller>();
            var go = new GameObject("DimraethSanctumChests");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<MapPatchInstaller>();

            Log.LogInfo("Dimraeth Sanctum Chest Buttons armed; waiting for DimraethMapActions to load.");
        }
    }

    // Waits until the friend-made DimraethMapActions assembly is present (we do not
    // depend on plugin load order), then installs a single postfix on MapButtons.Build.
    public sealed class MapPatchInstaller : MonoBehaviour
    {
        public MapPatchInstaller(IntPtr ptr) : base(ptr) { }

        internal static MapPatchInstaller Instance;

        private bool _done;
        private bool _typeSeen;
        private float _nextAttempt;

        // [2026-10-02] A chest that exists in the save but is not spawned (player outside
        // the Sanctum). After the host respawns the base's build objects we poll for the
        // live storage here, on a persistent object, because the map panel is closed when
        // a chest is opened.
        private ChestDirectory.ChestInfo _pendingChest;
        private float _pendingDeadline;
        private float _pendingNextTry;

        private void Awake()
        {
            Instance = this;
        }

        internal static void RequestDeferredOpen(ChestDirectory.ChestInfo chest)
        {
            try
            {
                if (Instance != null) Instance.BeginDeferredOpen(chest);
            }
            catch { }
        }

        private void BeginDeferredOpen(ChestDirectory.ChestInfo chest)
        {
            _pendingChest = chest;
            _pendingDeadline = Time.realtimeSinceStartup + 6f;
            _pendingNextTry = 0f;
        }

        private void TickDeferredOpen()
        {
            if (_pendingChest == null) return;

            float now = Time.realtimeSinceStartup;
            if (now >= _pendingDeadline)
            {
                Plugin.Log?.LogWarning($"Sanctum chests: '{_pendingChest.Name}' did not spawn in time; open cancelled.");
                _pendingChest = null;
                return;
            }

            if (now < _pendingNextTry) return;
            _pendingNextTry = now + 0.3f;

            var live = ChestDirectory.ResolveLive(_pendingChest);
            if ((bool)live && live.IsSpawned)
            {
                var chest = _pendingChest;
                _pendingChest = null;
                ChestDirectory.OpenLive(live, chest.Name);
            }
        }

        private void Update()
        {
            TickDeferredOpen();

            if (_done) return;
            if (!Plugin.Enabled) return;

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

                var build = AccessTools.Method(mapButtons, "Build");
                if (build == null)
                {
                    Plugin.Log.LogError("MapButtons found, but Build(MapContentView) missing. Patch NOT applied.");
                    _done = true;
                    return;
                }

                Plugin.Harmony.Patch(build, postfix: new HarmonyMethod(typeof(MapBuildPatch), nameof(MapBuildPatch.Postfix)));
                Plugin.Log.LogInfo("Patched MapButtons.Build -> Dimraeth Sanctum Chest Buttons active.");
                _done = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Sanctum chest patch attempt failed: {ex}");
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
    }
}
