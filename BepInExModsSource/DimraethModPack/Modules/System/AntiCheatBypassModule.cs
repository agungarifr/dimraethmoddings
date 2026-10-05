// [2026-09-30 02:47] OBSOLETE - kept per repo rule (removed code is preserved as comments).
//
// AntiCheatBypassModule used to patch CharacterPlausibility.FilterImplausible / IsImplausible /
// RejectsJoiningCharacter from inside the modpack. The anti-cheat bypass is now a separate
// standalone mod (AntiCheatBypassMod), which covers CharacterPlausibility UNCONDITIONALLY plus
// the whole anti-cheat surface (GearLegality, identity, XP/rune HMAC, save-block/kick, ban list,
// telemetry). The module was unregistered in DimraethModPackPlugin.cs at [2026-09-30 02:47].
//
// Kept (comment-only) for provenance; do not reinstate here - the standalone mod is the single
// owner of anti-cheat bypass behavior.
//
// ---- original file below (obsolete) ----
/*
using System;
using System.Reflection;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    /// <summary>
    /// [2026-09-28 14:20] New: Anti-Cheat Bypass (user request).
    ///
    /// Why the characters vanished (verified by decrypting the saves with the game's own key):
    ///   The .jrf files were never corrupt - every file MAC verified. The game simply HIDES
    ///   characters from the character-select list when CharacterPlausibility deems them
    ///   impossible. FilterImplausible (the only caller, from CharacterSelection) inlines two
    ///   checks:
    ///     * CharacterIdentityIntegrity.FailsIdentityCheck(...)  - stale/missing identity tag, and
    ///     * ExceedsPlausibleCaps(level, highestLevel, skillPointsSpent, attributeTotal, ...)
    ///         caps: level &lt;= 37, attribute total &lt;= 84, skill points spent in the tree &lt;= 126.
    ///   Meng (attributes [8,7,6,5,46,5,4,6], total 87) and Yoink ([7,7,6,6,46,7,7,6], total 92)
    ///   tripped the attribute ceiling; the three visible characters were all &lt;= 84.
    ///
    /// What this module does: makes FilterImplausible return its input unchanged, so nothing is
    /// hidden. It does NOT touch the file codec (encryption/HMAC) - those were already valid.
    ///
    /// Patching is done by name via AccessTools (not "typeof") on purpose: CharacterPlausibility is
    /// a type added by the 2026-09-26 game update, so it is absent from the interop snapshot this
    /// project compiles against (2026-09-18). Name-based patching compiles now and keeps working
    /// across future game updates.
    ///
    /// [2026-09-28 14:10] Changed by user request: the multiplayer join gate
    /// (SpawnManager -&gt; RejectsJoiningCharacter) is now ALSO bypassed by default, so the same
    /// anti-cheat is fully off for solo and multiplayer. This is a client-side mod, so it only
    /// affects instances running it (e.g. when you host); a host without the mod still applies
    /// its own check. Set BypassMultiplayerJoinCheck = false in the cfg to keep the vanilla join
    /// gate (boosted characters would then be refused when spawning into multiplayer).
    /// </summary>
    public class AntiCheatBypassModule : ModModuleBase
    {
        public static AntiCheatBypassModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Anti-Cheat Bypass";
        public override string Description => "Stop the game hiding edited/boosted characters from the character-select list";

        public ConfigEntry<bool> BypassMultiplayerJoinCheck;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.AntiCheatBypass";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable Anti-Cheat Bypass (Default: true, Vanilla: false)");

            BypassMultiplayerJoinCheck = config.Bind(sec, "BypassMultiplayerJoinCheck", true,
                "Also let implausible characters join multiplayer games (Default: true). " +
                "Client-side only: a host without this mod still applies its own check. " +
                "Set false to keep the vanilla join gate (refuses boosted characters in multiplayer).");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            var type = AccessTools.TypeByName("CharacterPlausibility");
            if (type == null)
            {
                DimraethModPackPlugin.Log?.LogWarning(
                    "[AntiCheatBypass] CharacterPlausibility not found; bypass not applied.");
                return;
            }

            TryPatch(harmony, type, "FilterImplausible", nameof(Patches.Prefix_FilterImplausible));
            TryPatch(harmony, type, "IsImplausible", nameof(Patches.Prefix_IsImplausible));
            TryPatch(harmony, type, "RejectsJoiningCharacter", nameof(Patches.Prefix_RejectsJoiningCharacter));
        }

        private static void TryPatch(Harmony harmony, Type type, string methodName, string prefixName)
        {
            try
            {
                var method = AccessTools.Method(type, methodName);
                if (method == null)
                {
                    DimraethModPackPlugin.Log?.LogWarning(
                        $"[AntiCheatBypass] CharacterPlausibility.{methodName} not found; skipped.");
                    return;
                }

                var prefix = typeof(Patches).GetMethod(prefixName, BindingFlags.Public | BindingFlags.Static);
                harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                DimraethModPackPlugin.Log?.LogInfo(
                    $"[AntiCheatBypass] Patched CharacterPlausibility.{methodName}.");
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogError(
                    $"[AntiCheatBypass] Failed to patch {methodName}: {ex.Message}");
            }
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            GUI.Label(new Rect(x, curY, width, 24f),
                "Hidden characters (over-cap attributes) are shown again.", labelStyle);
            curY += 26f;
            curY += DrawToggle(x, curY, width, "Bypass multiplayer join check", BypassMultiplayerJoinCheck,
                "Default: ON (vanilla: refused)", labelStyle, btnStyle);
            return curY - y;
        }

        public static class Patches
        {
            // Character list: FilterImplausible is the ONLY place the game hides saves, so returning
            // the list untouched (skip original) is enough to un-hide every character.
            public static bool Prefix_FilterImplausible(
                Il2CppSystem.Collections.Generic.List<PlayerSaveFile> __0,
                ref Il2CppSystem.Collections.Generic.List<PlayerSaveFile> __result)
            {
                if (Instance == null || !Instance.IsEnabled) return true;
                __result = __0;
                return false;
            }

            // Single-save predicate (other/legacy callers): always plausible.
            public static bool Prefix_IsImplausible(out string reason, ref bool __result)
            {
                reason = null;
                if (Instance == null || !Instance.IsEnabled) return true;
                __result = false;
                return false;
            }

            // Multiplayer join gate - opt-in only (default off), see class notes.
            public static bool Prefix_RejectsJoiningCharacter(out string reason, ref bool __result)
            {
                reason = null;
                if (Instance == null || !Instance.IsEnabled) return true;
                if (Instance.BypassMultiplayerJoinCheck == null || !Instance.BypassMultiplayerJoinCheck.Value)
                    return true;
                __result = false;
                return false;
            }
        }
    }
}
*/
