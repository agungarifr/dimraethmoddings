// [2026-09-30 02:47] OBSOLETE - kept per repo rule (removed code is preserved as comments).
//
// GearLegalityGuard used to install/remove a Harmony prefix forcing GearLegality.Inspect ->
// Violation.None while EquipmentStatEditor's own "Bypass mode" was ON. The anti-cheat bypass is
// now a separate standalone mod (AntiCheatBypassMod), which owns that GearLegality.Inspect patch
// UNCONDITIONALLY, so this guard is dead code and must not be reinstated here.
//
// Removed from EquipmentStatEditorPlugin.SetBypassMode (now SetCapMode) at [2026-09-30 02:47].
// The editor's toggle is now only the "Vanilla caps / Modded caps" ceiling switch.
//
// ---- original file below (obsolete) ----
/*
using System;
using HarmonyLib;

namespace EquipmentStatEditor.Core
{
    /// <summary>
    /// [2026-09-29 10:29] Added: Safe/Bypass mode - the "Bypass" half (design approved by the user
    /// 2026-09-29: bypass ceiling = 99x the per-stat vanilla ceiling, and the game's item
    /// destruction is disabled only while Bypass mode is ON).
    ///
    /// Background (from the GearLegality investigation, 2026-09-29): the game destroys
    /// "unobtainable" runes through its own anti-contraband system, GearLegality. This is NOT the
    /// character anti-cheat (CharacterPlausibility, patched separately by the modpack's
    /// AntiCheatBypass module) - it is a gear check. Every destruction path funnels through one
    /// choke point, GearLegality.Inspect(Rune) -> Violation:
    ///   Inventory.LoadInventory()         -> CollectContraband  (character/inventory load)
    ///   Inventory.PurgeUnobtainableRunes  -> CollectContraband  (netcode RPC purge)
    ///   Runes.Update() -> UpdateRuneBonuses() -> PurgeEquipped  (every tick, worn runes)
    ///   Storage.SetStorageData()          -> PurgeContainer     (storage opened/set)
    /// Anything Inspect flags != None is destroyed and counted in the local diagnostics log
    /// (ClientDiagnostics.NoteContrabandDestroyed -> Player.log; local only, nothing is sent out).
    ///
    /// So while Bypass mode is ON one Harmony prefix forces Inspect to return Violation.None: no
    /// purge path can see a violation and nothing gets destroyed. The patch is installed only
    /// while Bypass is ON and removed again in Safe mode, so Safe mode leaves the game logic
    /// completely untouched (as approved).
    ///
    /// Not touched on purpose: GearLegality's charge-based path (IsChargeContraband /
    /// DestroyIfChargesUnobtainable). This editor never writes item charges and edited runes keep
    /// their legitimate charge counts, so that path stays clean.
    ///
    /// Consequence the user accepted: items written above the vanilla ceiling are destroyed by the
    /// vanilla game when the mod is removed or the mode is turned off - they only survive while
    /// this mod runs with Bypass ON (and on somebody else's server the server rules apply).
    /// </summary>
    internal static class GearLegalityGuard
    {
        private const string HarmonyId = "com.dimraeth.equipmentstateditor.gearlegality";

        private static Harmony _harmony;

        /// <summary>True while the Inspect -> Violation.None patch is installed.</summary>
        public static bool BypassActive { get; private set; }

        /// <summary>
        /// Enable/disable the Inspect -> None patch. Idempotent (no-op when already in the wanted
        /// state). Returns false when the patch could not be applied (game method missing, Harmony
        /// failure) so the caller can warn the user.
        /// </summary>
        public static bool SetBypass(bool enabled)
        {
            if (enabled == BypassActive) return true;
            try
            {
                var target = AccessTools.Method(typeof(GearLegality), nameof(GearLegality.Inspect));
                if (target == null)
                {
                    EquipmentStatEditorPlugin.Log.LogError(
                        "[EquipmentStatEditor] GearLegality.Inspect not found - cannot toggle the bypass patch.");
                    return false;
                }

                if (enabled)
                {
                    _harmony ??= new Harmony(HarmonyId);
                    _harmony.Patch(target,
                        prefix: new HarmonyMethod(typeof(GearLegalityGuard), nameof(InspectPrefix)));
                    BypassActive = true;
                    EquipmentStatEditorPlugin.Log.LogInfo(
                        "[EquipmentStatEditor] Bypass ON: patched GearLegality.Inspect -> Violation.None (game item destruction disabled).");
                }
                else
                {
                    _harmony?.Unpatch(target, HarmonyPatchType.Prefix, HarmonyId);
                    BypassActive = false;
                    EquipmentStatEditorPlugin.Log.LogInfo(
                        "[EquipmentStatEditor] Bypass OFF: GearLegality.Inspect restored (vanilla item destruction active again).");
                }
                return true;
            }
            catch (Exception ex)
            {
                BypassActive = false;
                EquipmentStatEditorPlugin.Log.LogError($"[EquipmentStatEditor] GearLegality bypass patch error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Harmony prefix - while Bypass is active every "is this rune legal?" question gets the
        /// same answer: yes. The BypassActive re-check is a safety net so a stale patch can never
        /// keep suppressing destruction after the mode was turned off.
        /// </summary>
        private static bool InspectPrefix(ref GearLegality.Violation __result)
        {
            if (!BypassActive) return true;
            __result = GearLegality.Violation.None;
            return false;
        }
    }
}
*/
