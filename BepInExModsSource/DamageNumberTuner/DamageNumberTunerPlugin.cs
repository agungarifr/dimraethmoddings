using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DamageNumberTuner
{
    /// <summary>
    /// How floating damage numbers are filtered. Cycled in-game with the hotkey.
    /// </summary>
    public enum DamageNumberMode
    {
        /// <summary>Vanilla - show every number.</summary>
        All = 0,
        /// <summary>Hide your outgoing non-crit hits; keep all crits and all incoming damage (default).</summary>
        Smart = 1,
        /// <summary>Show only critical hits.</summary>
        CritsOnly = 2,
        /// <summary>Hide every number for damage you deal; keep everything else.</summary>
        OutgoingOff = 3,
        /// <summary>Hide every floating damage number.</summary>
        Off = 4,
    }

    /// <summary>
    /// BepInEx 6 (IL2CPP) plugin that hides floating damage numbers so fast multi-hit builds
    /// (e.g. a "sonic blade elf") stop covering enemy attack telegraphs.
    ///
    /// It is a single Harmony prefix on <c>Damages.TriggerDamageFloatingNumbers(Damage)</c> - the one
    /// client-side method that decides whether to spawn a <c>DamagePopUp</c>. Returning false skips
    /// the number entirely. This is purely cosmetic: no damage, gameplay, multiplayer or stat value
    /// is touched, so it cannot desync or give an advantage.
    ///
    /// The default "Smart" mode hides numbers for non-critical damage the local player deals
    /// (normal hits, auto-attacks, damage-over-time ticks) while always keeping critical hits and
    /// every number for damage the player takes - so the screen clears but danger feedback stays.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class DamageNumberTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.damagenumbertuner";
        public const string NAME = "Damage Number Tuner";
        public const string VERSION = "1.0.0";

        public static DamageNumberTunerPlugin Instance;
        public static new ManualLogSource Log;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<DamageNumberMode> Mode;
        public static ConfigEntry<bool> HideAutoAttacks;
        public static ConfigEntry<bool> HideDamageOverTime;
        public static ConfigEntry<bool> KeepIncomingDamage;
        public static ConfigEntry<KeyCode> CycleModeKey;
        public static ConfigEntry<bool> DiagnosticLogging;

        // Cached local-player network object id. Refreshed at most once per second so a respawn
        // or scene load is picked up, while avoiding a native property read on every damage event.
        private static ulong _localId;
        private static float _nextLocalIdAt;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false, every floating damage number shows exactly as vanilla. (Default: true)");

            Mode = Config.Bind("General", "Mode", DamageNumberMode.Smart,
                "How floating damage numbers are filtered:\n" +
                "  All         = vanilla (show every number)\n" +
                "  Smart       = hide your outgoing non-crit hits; keep all crits and all incoming damage (default)\n" +
                "  CritsOnly   = show only critical hits\n" +
                "  OutgoingOff = hide all damage you deal; keep everything else\n" +
                "  Off         = hide every floating number\n" +
                "Cycle modes in-game with the hotkey. (Default: Smart)");

            HideAutoAttacks = Config.Bind("General", "HideAutoAttacks", true,
                "In Smart mode, also hide auto-attack numbers even when they crit. (Default: true)");

            HideDamageOverTime = Config.Bind("General", "HideDamageOverTime", true,
                "In Smart mode, also hide damage-over-time (DoT) tick numbers even when they crit. (Default: true)");

            KeepIncomingDamage = Config.Bind("General", "KeepIncomingDamage", true,
                "In Smart mode, always show numbers for damage YOU take, even when they are not crits. (Default: true)");

            CycleModeKey = Config.Bind("Hotkey", "CycleModeKey", KeyCode.F6,
                "Keyboard key that cycles the damage-number mode in-game. Set to None to disable the shortcut. (Default: F6)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log every damage event's filter decision (type, crit, auto, DoT, in/out, ids) to the BepInEx console. (Default: false)");

            try
            {
                var harmony = new Harmony(GUID);
                harmony.PatchAll(typeof(Patch_Damages_TriggerDamageFloatingNumbers));
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch TriggerDamageFloatingNumbers: {ex}");
            }

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<DamageNumberHotkeyBehaviour>();
                var go = new GameObject("DamageNumberTuner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<DamageNumberHotkeyBehaviour>();
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to start hotkey behaviour: {ex}");
            }

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"Mode: {Mode.Value}");
            Log.LogInfo($"Hotkey: {CycleModeKey.Value}");
            Log.LogInfo("=================================================");
        }

        /// <summary>
        /// Decides whether a floating number should be shown for this damage event.
        /// Returns true to let the vanilla method run, false to skip the popup.
        /// </summary>
        public static bool ShouldShow(Damage damage)
        {
            if (damage == null) return false;

            var mode = Mode.Value;
            switch (mode)
            {
                case DamageNumberMode.All: return true;
                case DamageNumberMode.Off: return false;
                case DamageNumberMode.CritsOnly: return damage.IsCritical;
            }

            ulong localId = LocalPlayerId();
            bool haveLocal = localId != 0UL;
            bool incoming = haveLocal && damage.VictimId == localId;
            bool outgoing = haveLocal && damage.AttackerId == localId;
            bool crit = damage.IsCritical;

            if (mode == DamageNumberMode.OutgoingOff)
            {
                bool showAll = !outgoing;
                if (DiagnosticLogging.Value) LogDecision(damage, incoming, outgoing, showAll);
                return showAll;
            }

            // Smart: keep incoming (optionally), keep crits, hide outgoing non-crits.
            bool show;
            if (incoming && KeepIncomingDamage.Value) show = true;
            else if (crit) show = true;
            else if (outgoing) show = false;
            else show = true;

            // In Smart mode, additionally suppress auto-attack / DoT numbers even when they crit.
            if (show && outgoing)
            {
                if (HideAutoAttacks.Value && damage.IsFromAuto) show = false;
                if (HideDamageOverTime.Value && damage.IsDamageOverTime) show = false;
            }

            if (DiagnosticLogging.Value) LogDecision(damage, incoming, outgoing, show);
            return show;
        }

        /// <summary>
        /// The local player's network object id (the same id space as <c>Damage.AttackerId</c> /
        /// <c>Damage.VictimId</c>), or 0 when unavailable. Cached for up to one second.
        /// </summary>
        public static ulong LocalPlayerId()
        {
            float now = Time.unscaledTime;
            if (now < _nextLocalIdAt && _localId != 0UL) return _localId;
            _nextLocalIdAt = now + 1f;

            try
            {
                var storage = DataStorage.Singleton;
                var player = storage != null ? storage.Player : null;
                _localId = player != null ? player.NetworkObjectId : 0UL;
            }
            catch
            {
                _localId = 0UL;
            }
            return _localId;
        }

        private static void LogDecision(Damage damage, bool incoming, bool outgoing, bool show)
        {
            try
            {
                Log.LogInfo($"[Filter] show={show} type={damage.DamageType} crit={damage.IsCritical} " +
                            $"auto={damage.IsFromAuto} dot={damage.IsDamageOverTime} " +
                            $"in={incoming} out={outgoing} atk={damage.AttackerId} vic={damage.VictimId} " +
                            $"local={_localId} val={damage.DamageValue:F1}");
            }
            catch { /* diagnostics must never break gameplay */ }
        }
    }

    /// <summary>
    /// Prefix on <c>Damages.TriggerDamageFloatingNumbers(Damage)</c> - the single client-side entry
    /// point that spawns the floating damage numbers. Returning false skips spawning entirely.
    /// </summary>
    [HarmonyPatch(typeof(Damages), "TriggerDamageFloatingNumbers")]
    public static class Patch_Damages_TriggerDamageFloatingNumbers
    {
        public static bool Prefix(Damage damage)
        {
            try
            {
                if (!DamageNumberTunerPlugin.Enabled.Value) return true;
                return DamageNumberTunerPlugin.ShouldShow(damage);
            }
            catch (Exception ex)
            {
                // Fail open: if anything goes wrong, fall back to vanilla behaviour.
                DamageNumberTunerPlugin.Log?.LogError($"[TriggerDamageFloatingNumbers] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Polls the cycle hotkey and shows a short on-screen toast when the mode changes.
    /// </summary>
    public class DamageNumberHotkeyBehaviour : MonoBehaviour
    {
        public DamageNumberHotkeyBehaviour(IntPtr ptr) : base(ptr) { }

        private float _toastUntil;
        private string _toastText = "";

        private void Update()
        {
            try
            {
                var key = DamageNumberTunerPlugin.CycleModeKey.Value;
                if (key != KeyCode.None && Input.GetKeyDown(key))
                {
                    CycleMode();
                }
            }
            catch (Exception ex)
            {
                DamageNumberTunerPlugin.Log?.LogError($"[Hotkey] {ex}");
            }
        }

        private void CycleMode()
        {
            var cur = DamageNumberTunerPlugin.Mode.Value;
            var next = (DamageNumberMode)(((int)cur + 1) % 5);
            DamageNumberTunerPlugin.Mode.Value = next;
            DamageNumberTunerPlugin.Instance?.Config.Save();

            _toastText = $"Damage numbers: {next}";
            _toastUntil = Time.unscaledTime + 2f;
            DamageNumberTunerPlugin.Log?.LogInfo($"[DamageNumberTuner] Mode -> {next}");
        }

        private void OnGUI()
        {
            if (Time.unscaledTime > _toastUntil) return;
            try
            {
                if (GUI.skin == null) return;
                float alpha = Mathf.Clamp01((_toastUntil - Time.unscaledTime) / 0.5f);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);

                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                };
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(0f, Screen.height * 0.12f, Screen.width, 30f), _toastText, style);
                GUI.color = prev;
            }
            catch { /* toast is best-effort only */ }
        }
    }
}
