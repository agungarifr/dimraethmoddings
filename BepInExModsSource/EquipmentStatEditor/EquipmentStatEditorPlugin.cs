using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using EquipmentStatEditor.Core;
// [2026-09-26 01:59] Added: in-game rune editor panel (ported from the modpack's
// EquippedStatModifierModule UI, which was unregistered from DimraethModPack the same day).
using EquipmentStatEditor.UI;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace EquipmentStatEditor
{
    // [2026-09-26 01:59] Obsolete: v1.0.1 - the editor had no in-game UI yet (config file only).
    // v1.0.2 adds the F8 rune editor panel ported from the modpack module.
    // [BepInPlugin("com.dimraeth.equipmentstateditor", "Equipment Stat Editor", "1.0.1")]
    // [2026-09-29 10:29] Changed: v1.0.2 -> v1.0.3 - adds the Safe/Bypass mode toggle (config
    // "General.BypassMode"): Bypass raises the per-stat ceiling to 99x vanilla and disables the
    // game's GearLegality item destruction while active.
    // [BepInPlugin("com.dimraeth.equipmentstateditor", "Equipment Stat Editor", "1.0.2")]
    // [2026-09-30 02:47] Changed: v1.0.3 -> v1.0.4 - renamed the toggle to Vanilla/Modded caps and
    // removed the GearLegality item-destruction bypass (GearLegalityGuard.cs): that bypass is now
    // owned unconditionally by the separate standalone AntiCheatBypassMod, so the toggle only
    // chooses the UI/validation ceiling.
    // [BepInPlugin("com.dimraeth.equipmentstateditor", "Equipment Stat Editor", "1.0.3")]
    [BepInPlugin("com.dimraeth.equipmentstateditor", "Equipment Stat Editor", "1.0.4")]
    public class EquipmentStatEditorPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static EquipmentStatEditorPlugin Instance { get; private set; }

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> AutoApplyOnLoad;
        public static ConfigEntry<bool> RewriteDumpAfterSave;
        public static ConfigEntry<bool> SaveAfterApply;
        public static ConfigEntry<bool> IncludeInventory;
        public static ConfigEntry<KeyCode> ReloadKey;
        // [2026-09-26 01:59] Added: hotkey for the in-game rune editor panel.
        public static ConfigEntry<KeyCode> EditorKey;
        // [2026-09-29 10:29] Added: Safe/Bypass mode. Safe (default) = the vanilla per-stat
        // ceilings exactly as before. Bypass = UI/validation ceiling raised to 99x the vanilla
        // per-stat ceiling AND the game's GearLegality destruction disabled (one Harmony patch on
        // GearLegality.Inspect, see Core/GearLegalityGuard.cs). WARNING: items created above the
        // vanilla caps are destroyed by the game when this mod is removed or the mode is turned
        // off - they only survive while the mod runs with Bypass ON.
        // [2026-09-30 02:47] Renamed to Vanilla/Modded caps: this toggle now ONLY chooses the
        // UI/validation stat ceiling (Vanilla = the real per-stat caps, Modded = 99x those caps).
        // The game's item destruction (GearLegality.Inspect) is no longer handled here - it is
        // owned unconditionally by the separate standalone AntiCheatBypassMod, so over-cap items
        // survive while that mod is installed regardless of this toggle.
        // /* [2026-09-30 02:47] Obsolete: Safe/Bypass field (superseded by UseModdedCaps below).
        // public static ConfigEntry<bool> BypassMode;
        // */
        public static ConfigEntry<bool> UseModdedCaps;

        private Harmony _harmony;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            ModEnabled = Config.Bind("General", "ModEnabled", true,
                "Enable or disable the Equipment Stat Editor");
            AutoApplyOnLoad = Config.Bind("General", "AutoApplyOnLoad", true,
                "Apply edited config entries automatically when a character finishes loading");
            // [2026-09-27 15:15] Changed: default true -> false. Rewriting the whole rune dump on
            // EVERY native save (the game autosaves often) churned the multi-MB .cfg constantly.
            // The dump is now written on demand from the panel's "Dump Runes to File" button
            // (and once at first launch when no config exists yet). Set true to restore the old
            // self-updating behavior.
            // /* [2026-09-27 15:15] Obsolete default:
            // RewriteDumpAfterSave = Config.Bind("General", "RewriteDumpAfterSave", true,
            //     "Rewrite the dump file after each native game save so its Hash keys mark edits as consumed (self-updating dump)");
            // */
            RewriteDumpAfterSave = Config.Bind("General", "RewriteDumpAfterSave", false,
                "Rewrite the dump file after each native game save (self-updating dump). Off by default: use the panel's Dump button instead of dumping on every autosave");
            SaveAfterApply = Config.Bind("General", "SaveAfterApply", false,
                "Trigger a native character save immediately after applying edits (otherwise save manually via main menu / quit)");
            IncludeInventory = Config.Bind("General", "IncludeInventory", true,
                "Include unequipped runes (rune inventory) in the dump, not just the 12 equipped slots");
            ReloadKey = Config.Bind("General", "ReloadKey", KeyCode.F9,
                "Hotkey: re-read the config file, apply edits and trigger a native save (no restart needed)");
            // [2026-09-26 01:59] Added: toggle the in-game rune editor panel.
            EditorKey = Config.Bind("General", "EditorKey", KeyCode.F8,
                "Hotkey: toggle the in-game rune stat editor panel (add/change secondary stats and values)");
            // [2026-09-29 10:29] Added: Safe/Bypass mode binding (see the field doc above for the
            // consequences). Default false = Safe mode, i.e. completely vanilla behavior.
            // [2026-09-30 02:47] Renamed: config key "General.BypassMode" -> "General.UseModdedCaps"
            // (the setting now only selects the cap; item destruction is the standalone mod's job).
            // /* [2026-09-30 02:47] Obsolete: BypassMode binding (superseded by UseModdedCaps).
            // BypassMode = Config.Bind("General", "BypassMode", false,
            //     "Bypass mode: raise the per-stat value ceiling to 99x the vanilla cap and disable the game's item destruction (GearLegality). WARNING: items created above vanilla caps are destroyed by the game if this mod is removed or the mode is turned off");
            // */
            UseModdedCaps = Config.Bind("General", "UseModdedCaps", false,
                "Modded caps: raise the per-stat value ceiling to 99x the vanilla cap (Vanilla caps = the real per-stat ceilings). Item destruction is handled by the separate AntiCheatBypassMod, not here.");

            EditorEngine.BindLogger(
                m => Log.LogInfo(m),
                m => Log.LogWarning(m),
                m => Log.LogError(m));
            EditorEngine.AutoApplyOnLoad = AutoApplyOnLoad.Value;
            EditorEngine.RewriteDumpAfterSave = RewriteDumpAfterSave.Value;
            EditorEngine.SaveAfterApply = SaveAfterApply.Value;
            EditorEngine.IncludeInventory = IncludeInventory.Value;

            // [2026-09-29 10:29] Added: wire Safe/Bypass mode - apply the persisted value at boot
            // and follow later config edits live. SetBypassMode is idempotent, so the .Value
            // write inside it (persist path) coming back through SettingChanged is harmless.
            // [2026-09-29 10:36] Changed: inline lambda -> named handler (see OnUseModdedCapsSettingChanged).
            // [2026-09-30 02:47] Renamed: BypassMode -> UseModdedCaps, SetBypassMode -> SetCapMode.
            // /* [2026-09-30 02:47] Obsolete: BypassMode wiring (superseded by UseModdedCaps).
            // BypassMode.SettingChanged += OnBypassModeSettingChanged;
            // SetBypassMode(BypassMode.Value, persist: false);
            // */
            UseModdedCaps.SettingChanged += OnUseModdedCapsSettingChanged;
            SetCapMode(UseModdedCaps.Value, persist: false);

            _harmony = new Harmony("com.dimraeth.equipmentstateditor");
            SaveHooks.Install(_harmony, m => Log.LogInfo(m), m => Log.LogWarning(m));

            ClassInjector.RegisterTypeInIl2Cpp<EditorController>();
            var go = new GameObject("EquipmentStatEditorController");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<EditorController>();

            // [2026-09-26 01:59] Obsolete: v1.0.1 log line.
            // Log.LogInfo("Equipment Stat Editor v1.0.1 loaded!");
            // [2026-09-29 10:29] Changed: v1.0.2 -> v1.0.3 (Safe/Bypass mode).
            // Log.LogInfo("Equipment Stat Editor v1.0.2 loaded!");
            // [2026-09-30 02:47] Changed: v1.0.3 -> v1.0.4 (Vanilla/Modded caps rename).
            // Log.LogInfo("Equipment Stat Editor v1.0.3 loaded!");
            Log.LogInfo("Equipment Stat Editor v1.0.4 loaded!");
            Log.LogInfo($"  config file: BepInEx/config/EquipmentStatEditor.<CharacterName>.cfg");
            Log.LogInfo($"  flow: launch -> auto dump -> edit file -> launch (auto apply) -> normal save. Press {ReloadKey.Value} to apply live.");
            Log.LogInfo($"  in-game editor: press {EditorKey.Value} to edit secondary stats & values on equipped gear.");
            // [2026-09-30 02:47] Changed: "mode" -> "caps" (bypass/destruction is the standalone mod's).
            Log.LogInfo($"  caps: {(UseModdedCaps.Value ? "MODDED (99x vanilla ceilings)" : "VANILLA (real per-stat ceilings)")}");
        }

        // [2026-09-29 10:29] Added: single entry point for the Safe/Bypass mode switch (used by
        // the config SettingChanged hook and the panel's Mode button). Sets the live ceiling flag
        // first, then installs/removes the GearLegality.Inspect patch; when the patch cannot be
        // applied the mode still raises the UI ceiling but the game may destroy over-cap items,
        // so that case is logged loudly.
        // [2026-09-30 02:47] Renamed to SetCapMode and stripped the GearLegalityGuard call: this
        // method now only flips the ceiling flag (vanilla <-> modded). The item-destruction bypass
        // is owned unconditionally by the standalone AntiCheatBypassMod, so there is nothing to
        // install/remove here anymore.
        // /* [2026-09-30 02:47] Obsolete: Safe/Bypass entry point (superseded by SetCapMode).
        // internal static void SetBypassMode(bool on, bool persist)
        // {
        //     StatCapCatalog.BypassMode = on;
        //     bool patched = GearLegalityGuard.SetBypass(on);
        //     if (on && !patched)
        //         Log.LogWarning("[EquipmentStatEditor] Bypass mode is ON but the GearLegality patch failed - over-cap items may still be destroyed by the game. Check the log above.");
        //     if (persist && BypassMode != null && BypassMode.Value != on)
        //         BypassMode.Value = on; // persists to the config file; SettingChanged -> SetBypassMode(on, false), idempotent
        // }
        // */
        internal static void SetCapMode(bool modded, bool persist)
        {
            StatCapCatalog.ModdedCaps = modded;
            if (persist && UseModdedCaps != null && UseModdedCaps.Value != modded)
                UseModdedCaps.Value = modded; // persists to the config file; SettingChanged -> SetCapMode(modded, false), idempotent
        }

        // [2026-09-29 10:36] Added: named SettingChanged handler. The first implementation used an
        // inline lambda, which failed to compile with CS0656 (Missing compiler required member
        // 'System.Runtime.CompilerServices.NullableAttribute..ctor'): binding an implicitly-typed
        // lambda to BepInEx's NRT-annotated EventHandler<SettingChangedEventArgs> makes the
        // compiler emit NullableAttribute, and Il2Cppmscorlib declares an incompatible copy of
        // that attribute. Obsolete line kept per repo rule:
        // BypassMode.SettingChanged += (sender, args) => SetBypassMode(BypassMode.Value, persist: false);
        // A named method with oblivious (non-annotated) parameter types needs no attribute
        // emission, so it compiles cleanly.
        // [2026-09-29 10:38] Fixed: ConfigEntry.SettingChanged is plain System.EventHandler
        // (object, EventArgs), not EventHandler<SettingChangedEventArgs> (CS0123).
        // [2026-09-30 02:47] Renamed: OnBypassModeSettingChanged -> OnUseModdedCapsSettingChanged.
        // /* [2026-09-30 02:47] Obsolete: BypassMode handler (superseded by UseModdedCaps).
        // private static void OnBypassModeSettingChanged(object sender, EventArgs args)
        // {
        //     SetBypassMode(BypassMode.Value, persist: false);
        // }
        // */
        private static void OnUseModdedCapsSettingChanged(object sender, EventArgs args)
        {
            SetCapMode(UseModdedCaps.Value, persist: false);
        }
    }

    public class EditorController : MonoBehaviour
    {
        public EditorController(IntPtr ptr) : base(ptr) { }

        private float _nextPollTime;

        private void Update()
        {
            if (!EquipmentStatEditorPlugin.ModEnabled.Value) return;

            try
            {
                // [2026-09-27 11:28] Added: one-shot runtime dump of the game's per-stat
                // roll / ceiling table (used to build the NEXUS_DESCRIPTION.md cap table).
                StatRollTableDump.TryDumpOnce(m => EquipmentStatEditorPlugin.Log.LogInfo(m));

                // [2026-09-26 01:59] Added: in-game rune editor panel toggle.
                if (Input.GetKeyDown(EquipmentStatEditorPlugin.EditorKey.Value))
                    RuneEditorUI.Toggle();

                if (Input.GetKeyDown(EquipmentStatEditorPlugin.ReloadKey.Value))
                {
                    var player = EditorEngine.GetLocalPlayer();
                    if (player != null)
                    {
                        int applied = EditorEngine.RunCycle(player, manual: true);
                        EquipmentStatEditorPlugin.Log.LogInfo(
                            $"[EquipmentStatEditor] reload key: {applied} rune(s) modified - triggering native save.");
                        EditorEngine.SaveNow(player);
                    }
                }

                if (Time.unscaledTime < _nextPollTime) return;
                _nextPollTime = Time.unscaledTime + 1.0f;

                // [2026-09-26 02:46] Added: re-verify recent UI edits every tick - the game can
                // strip a rejected item seconds after the write (rollback handler).
                EditorEngine.VerifyPending();

                if (EditorEngine.AutoApplyOnLoad)
                    EditorEngine.Poll();
            }
            catch (Exception ex)
            {
                EquipmentStatEditorPlugin.Log.LogError($"[EquipmentStatEditor] controller error: {ex}");
            }
        }

        // [2026-09-26 01:59] Added: renders the in-game rune editor panel (port of the modpack
        // module's UI). Unlike the old module (empty catch { }), UI errors are logged here so a
        // blank panel can no longer hide a crash - that was the suspected "no buttons" cause.
        private void OnGUI()
        {
            try
            {
                RuneEditorUI.Draw();
            }
            catch (Exception ex)
            {
                EquipmentStatEditorPlugin.Log.LogError($"[EquipmentStatEditor] UI error: {ex}");
            }
        }
    }
}
