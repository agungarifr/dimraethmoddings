using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace DragonBreathTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that tunes the vanilla Dragon's Breath skill while the
    /// "Channelled Dragon" upgrade is active:
    ///
    ///   * Movement while channeling -> MoveWhileChanneling (default true)
    ///         Vanilla roots the caster for the whole channel. This mod short-circuits the three
    ///         movement gates (WASD.CannotUseMovement, Movement.CanEnablePlayerMovement,
    ///         Movement.PlayerMovementNotValid) for the local player only while their own
    ///         Channelled Dragon breath is active.
    ///   * Burning stacks -> BurningMultiplier (default x3)
    ///         Every Burning stack the Dragon's Breath projectile applies flows through
    ///         BaseSpellLibrary.AddStacksToTarget; we scale it for the breath prefabs only.
    ///   * Range -> RangeMultiplier (default x1.5, i.e. +50%)
    ///         The channel's range is the spell's cast radius (Spells.GetCastRadius ->
    ///         SpellLibrary.BaseCastRadius). We scale BaseCastRadius in ApplyDefinition for the
    ///         Dragon's Breath spell only.
    ///
    /// Every lever is behind a config entry so it can be reverted without rebuilding. Co-op: run the
    /// SAME config on host and client so both peers agree on the numbers.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class DragonBreathTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.dragonbreathtuner";
        public const string NAME = "Dragon's Breath Tuner";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static DragonBreathTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<bool> MoveWhileChanneling;
        // [2026-10-09 22:55] New: keep the breath prefab (and therefore its projectile spawn point) on the
        // caster while the channel is active, so the breath follows the player once MoveWhileChanneling
        // un-roots them.
        public static ConfigEntry<bool> FollowPlayerWhileChanneling;
        public static ConfigEntry<float> BurningMultiplier;
        public static ConfigEntry<float> RangeMultiplier;
        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

        // Raw IL2CPP field offsets for this game build (resolved from the ISIL dump).
        // BaseSpellLibrary._owner (public ObjectsCommon) -> 0xA0.
        internal const int OwnerOffset = 0xA0;
        // WASD._player and Movement._player (private Player) -> 0x88.
        internal const int PlayerOffset = 0x88;
        // WASD._movement (private Movement) -> 0xA8.
        internal const int WasdMovementOffset = 0xA8;
        // Movement.ScriptedWalkActive (public bool) -> 0xBC.
        internal const int ScriptedWalkOffset = 0xBC;

        // Owners (ObjectsCommon pointers) of players whose Channelled Dragon breath is currently active.
        private static readonly HashSet<IntPtr> _channelOwners = new HashSet<IntPtr>();

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false no Dragon's Breath value is modified and movement stays rooted. (Default: true)");

            MoveWhileChanneling = Config.Bind("Channel", "MoveWhileChanneling", true,
                "If true the local player can walk while channeling the Channelled Dragon breath (vanilla roots them). (Default: true)");

            // [2026-10-09 22:55] New: vanilla roots the caster, so the breath prefab is spawned once at the
            // caster's position and never moves; SpawnProjectilesChannelled spawns every projectile at that
            // prefab's transform, so once movement is enabled the breath would keep firing from where the
            // channel began. This keeps the prefab on the caster so the breath follows the player.
            FollowPlayerWhileChanneling = Config.Bind("Channel", "FollowPlayerWhileChanneling", true,
                "If true the Channelled Dragon breath stays on the caster and follows them while they move " +
                "(needs MoveWhileChanneling, since vanilla roots the caster so the breath never had to follow). (Default: true)");

            // [2026-10-09 22:37] BepInEx rejects ' in section/key names ("Cannot use any of the following
            // characters in section and key names: = \n \t \ " ' [ ]"), so the plugin failed to load with
            // section "Dragon's Breath". Renamed the section to "DragonBreath" (no apostrophe).
            // Old (invalid) lines kept for reference:
            // BurningMultiplier = Config.Bind("Dragon's Breath", "BurningMultiplier", 3f,
            //     "Multiplier for the number of Burning stacks the Dragon's Breath applies. 1 = vanilla. (Default: 3)");
            // RangeMultiplier = Config.Bind("Dragon's Breath", "RangeMultiplier", 1.5f,
            //     "Multiplier for the Dragon's Breath range (cast/targeting radius). 1 = vanilla, 1.5 = +50%. (Default: 1.5)");
            BurningMultiplier = Config.Bind("DragonBreath", "BurningMultiplier", 3f,
                "Multiplier for the number of Burning stacks the Dragon's Breath applies. 1 = vanilla. (Default: 3)");

            RangeMultiplier = Config.Bind("DragonBreath", "RangeMultiplier", 1.5f,
                "Multiplier for the Dragon's Breath range (cast/targeting radius). 1 = vanilla, 1.5 = +50%. (Default: 1.5)");

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a line to the Dragon's Breath spell description listing the tuned values. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "LogValues", false,
                "Log range/burning changes and channel start/stop to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);

            PatchOrLog(harmony, typeof(Patch_SpellLibrary_ApplyDefinition));
            PatchOrLog(harmony, typeof(Patch_BaseSpellLibrary_AddStacksToTarget));
            PatchOrLog(harmony, typeof(Patch_DragonsBreathPrefab_SetControllerChannelRange));
            PatchOrLog(harmony, typeof(Patch_DragonsBreathPrefab_OnActivateRelease));
            PatchOrLog(harmony, typeof(Patch_DragonsBreathPrefab_HandleBeforeSpellDestroyed));
            PatchOrLog(harmony, typeof(Patch_DragonsBreathPrefab_Update_FollowPlayer));
            PatchOrLog(harmony, typeof(Patch_WASD_CannotUseMovement));
            PatchOrLog(harmony, typeof(Patch_Movement_CanEnablePlayerMovement));
            PatchOrLog(harmony, typeof(Patch_Movement_PlayerMovementNotValid));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"MoveWhileChanneling: {MoveWhileChanneling.Value}");
            Log.LogInfo($"FollowPlayerWhileChanneling: {FollowPlayerWhileChanneling.Value}");
            Log.LogInfo($"BurningMultiplier: {BurningMultiplier.Value}");
            Log.LogInfo($"RangeMultiplier: {RangeMultiplier.Value}");
            Log.LogInfo($"ShowTunedStatsInTooltip: {ShowTunedStatsInTooltip.Value}");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo("=================================================");
        }

        private void PatchOrLog(Harmony harmony, Type patchType)
        {
            try
            {
                harmony.PatchAll(patchType);
            }
            catch (Exception ex)
            {
                Log.LogError($"[DragonBreathTuner] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        // ---- channel owner registry ------------------------------------------------------------

        internal static void AddChannelOwner(IntPtr owner)
        {
            if (owner == IntPtr.Zero) return;
            lock (_channelOwners) { _channelOwners.Add(owner); }
        }

        internal static void RemoveChannelOwner(IntPtr owner)
        {
            if (owner == IntPtr.Zero) return;
            lock (_channelOwners) { _channelOwners.Remove(owner); }
        }

        internal static bool IsChannelOwner(IntPtr owner)
        {
            if (owner == IntPtr.Zero) return false;
            lock (_channelOwners) { return _channelOwners.Contains(owner); }
        }

        /// <summary>
        /// True when the given player-side component (WASD / Movement) belongs to a player whose own
        /// Channelled Dragon breath is active. The player pointer is read straight from the raw
        /// <c>_player</c> field so no interop property name is assumed.
        /// </summary>
        internal static bool IsLocalChanneling(Il2CppObjectBase component)
        {
            if (component == null) return false;
            IntPtr player = ReadPtr(PlayerOffset, component);
            return IsChannelOwner(player);
        }

        internal static unsafe IntPtr ReadPtr(int offset, Il2CppObjectBase obj)
        {
            if (obj == null) return IntPtr.Zero;
            IntPtr p = IL2CPP.Il2CppObjectBaseToPtr(obj);
            if (p == IntPtr.Zero) return IntPtr.Zero;
            return *(IntPtr*)((byte*)p + offset);
        }

        internal static unsafe void WriteBool(IntPtr ptr, int offset, bool value)
        {
            if (ptr == IntPtr.Zero) return;
            *(byte*)((byte*)ptr + offset) = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Best-effort check that the object is one of the Dragon's Breath prefabs. The channel prefab
        /// is "DragonsBreathPrefab"; the projectile that applies Burning is "DragonsBreadthProjectilePrefab"
        /// (note the engine's "Breadth" typo), so both spellings are accepted.
        /// </summary>
        internal static bool IsDragonBreathPrefab(BaseSpellLibrary instance)
        {
            try
            {
                var type = instance.GetIl2CppType();
                if (type == null) return false;
                string name = type.Name;
                if (string.IsNullOrEmpty(name)) return false;
                return name.StartsWith("DragonsBreath", StringComparison.Ordinal)
                    || name.StartsWith("DragonsBreadth", StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        internal static string BuildTooltipSuffix()
        {
            if (!ShowTunedStatsInTooltip.Value) return null;

            var parts = new List<string>();
            if (MoveWhileChanneling.Value)
                parts.Add(FollowPlayerWhileChanneling.Value ? "move while channeling (breath follows you)" : "move while channeling");
            if (BurningMultiplier.Value > 0f && BurningMultiplier.Value != 1f)
                parts.Add($"burning x{FormatMultiplier(BurningMultiplier.Value)}");
            if (RangeMultiplier.Value > 0f && RangeMultiplier.Value != 1f)
                parts.Add($"range x{FormatMultiplier(RangeMultiplier.Value)}");

            if (parts.Count == 0) return null;
            return "\n\n[Dragon's Breath Tuner] " + string.Join(", ", parts) + ".";
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Postfix on the private <c>SpellLibrary.ApplyDefinition</c>. This is where a spell component copies
    /// its definition values onto the runtime fields (CastRadius -> BaseCastRadius). We scale the cast
    /// radius for the Dragon's Breath spell only. Called once per spell component, so it is not
    /// double-applied. The channelled Dragon's Breath range (Spells.GetCastRadius) reads this same
    /// BaseCastRadius (0x1A8), so this covers both the targeting reticle and WithinCastRadius.
    /// </summary>
    [HarmonyPatch(typeof(SpellLibrary), "ApplyDefinition")]
    public static class Patch_SpellLibrary_ApplyDefinition
    {
        public static void Postfix(SpellLibrary __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!DragonBreathTunerPlugin.Enabled.Value) return;
                if (__instance.Spell != Spell.DragonsBreath) return;

                float mult = DragonBreathTunerPlugin.RangeMultiplier.Value;
                if (mult <= 0f || mult == 1f) return;

                float baseRadius = __instance.BaseCastRadius;
                if (baseRadius <= 0f) return;

                float newRadius = baseRadius * mult;
                __instance.BaseCastRadius = newRadius;

                if (DragonBreathTunerPlugin.DiagnosticLogging.Value)
                {
                    DragonBreathTunerPlugin.Log.LogInfo(
                        $"[ApplyDefinition] Dragon's Breath cast radius {baseRadius} -> {newRadius}");
                }
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_SpellLibrary_ApplyDefinition] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on the public <c>BaseSpellLibrary.AddStacksToTarget</c>. The Dragon's Breath projectile
    /// (DragonsBreadthProjectilePrefab) applies its Burning stacks through here (both the direct hit and
    /// SpreadBurningToNearby), so we scale Burning stacks coming from a Dragon's Breath prefab only.
    /// </summary>
    [HarmonyPatch(typeof(BaseSpellLibrary), nameof(BaseSpellLibrary.AddStacksToTarget))]
    public static class Patch_BaseSpellLibrary_AddStacksToTarget
    {
        public static void Prefix(BaseSpellLibrary __instance, StackingEffect effect, ref int amount)
        {
            try
            {
                if (__instance == null) return;
                if (!DragonBreathTunerPlugin.Enabled.Value) return;
                if (effect != StackingEffect.Burning) return;
                if (amount <= 0) return;

                float mult = DragonBreathTunerPlugin.BurningMultiplier.Value;
                if (mult <= 0f || mult == 1f) return;

                if (!DragonBreathTunerPlugin.IsDragonBreathPrefab(__instance)) return;

                int newAmount = (int)Math.Round(amount * (double)mult, MidpointRounding.AwayFromZero);
                if (newAmount < 1) newAmount = 1;

                if (DragonBreathTunerPlugin.DiagnosticLogging.Value)
                {
                    DragonBreathTunerPlugin.Log.LogInfo(
                        $"[AddStacksToTarget] {__instance.GetIl2CppType().Name} Burning {amount} -> {newAmount}");
                }

                amount = newAmount;
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_BaseSpellLibrary_AddStacksToTarget] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>DragonsBreathPrefab.SetControllerChannelRange</c>. The channelled branch of OnStart
    /// calls this with active=true to widen the controller targeting range; it is the single reliable
    /// "the local player's Channelled Dragon breath just started" signal. We record the caster's
    /// ObjectsCommon pointer so the movement patches can recognise that exact player.
    /// </summary>
    [HarmonyPatch(typeof(DragonsBreathPrefab), "SetControllerChannelRange")]
    public static class Patch_DragonsBreathPrefab_SetControllerChannelRange
    {
        public static void Prefix(DragonsBreathPrefab __instance, bool active)
        {
            try
            {
                if (!DragonBreathTunerPlugin.Enabled.Value) return;
                if (!DragonBreathTunerPlugin.MoveWhileChanneling.Value) return;
                if (!active) return;

                IntPtr owner = DragonBreathTunerPlugin.ReadPtr(DragonBreathTunerPlugin.OwnerOffset, __instance);
                if (owner == IntPtr.Zero) return;

                DragonBreathTunerPlugin.AddChannelOwner(owner);

                if (DragonBreathTunerPlugin.DiagnosticLogging.Value)
                {
                    DragonBreathTunerPlugin.Log.LogInfo($"[Channel] start owner=0x{owner.ToInt64():X}");
                }
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_DragonsBreathPrefab_SetControllerChannelRange] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>DragonsBreathPrefab.OnActivateRelease</c>. Fired when the player releases the breath
    /// (or it times out). Stops treating the caster as channeling so movement returns to vanilla gating.
    /// </summary>
    [HarmonyPatch(typeof(DragonsBreathPrefab), "OnActivateRelease")]
    public static class Patch_DragonsBreathPrefab_OnActivateRelease
    {
        public static void Prefix(DragonsBreathPrefab __instance)
        {
            try
            {
                IntPtr owner = DragonBreathTunerPlugin.ReadPtr(DragonBreathTunerPlugin.OwnerOffset, __instance);
                DragonBreathTunerPlugin.RemoveChannelOwner(owner);

                if (DragonBreathTunerPlugin.DiagnosticLogging.Value && owner != IntPtr.Zero)
                {
                    DragonBreathTunerPlugin.Log.LogInfo($"[Channel] release owner=0x{owner.ToInt64():X}");
                }
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_DragonsBreathPrefab_OnActivateRelease] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>DragonsBreathPrefab.HandleBeforeSpellDestroyed</c>. Safety net: if the channel is cut
    /// short (stun, death, cancel, scene change) the prefab is destroyed without a normal release, so we
    /// clear the owner here too.
    /// </summary>
    [HarmonyPatch(typeof(DragonsBreathPrefab), "HandleBeforeSpellDestroyed")]
    public static class Patch_DragonsBreathPrefab_HandleBeforeSpellDestroyed
    {
        public static void Prefix(DragonsBreathPrefab __instance)
        {
            try
            {
                IntPtr owner = DragonBreathTunerPlugin.ReadPtr(DragonBreathTunerPlugin.OwnerOffset, __instance);
                DragonBreathTunerPlugin.RemoveChannelOwner(owner);
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_DragonsBreathPrefab_HandleBeforeSpellDestroyed] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-09 22:55] Postfix on <c>DragonsBreathPrefab.Update</c>. Vanilla roots the caster for the whole
    /// Channelled Dragon breath, so the prefab is spawned once at the caster
    /// (<c>BaseSpellLibrary.SpawnSpellAtCasterPosition</c>) and never moved again. The channel coroutine
    /// <c>SpawnProjectilesChannelled</c> spawns every projectile at THIS prefab's own transform position, so
    /// once <see cref="DragonBreathTunerPlugin.MoveWhileChanneling"/> lets the player walk, the breath (and its
    /// projectiles) kept firing from where the channel began. We keep the prefab on the caster so the breath
    /// follows the player. The projectiles are target-homing (<c>Targeting.TravelToTarget</c>), so only the
    /// spawn point moves - the aim is unchanged. Only casters already registered as an active channel (see
    /// <see cref="Patch_DragonsBreathPrefab_SetControllerChannelRange"/>) are moved.
    /// </summary>
    [HarmonyPatch(typeof(DragonsBreathPrefab), "Update")]
    public static class Patch_DragonsBreathPrefab_Update_FollowPlayer
    {
        public static void Postfix(DragonsBreathPrefab __instance)
        {
            try
            {
                if (!DragonBreathTunerPlugin.Enabled.Value) return;
                if (!DragonBreathTunerPlugin.MoveWhileChanneling.Value) return;
                if (!DragonBreathTunerPlugin.FollowPlayerWhileChanneling.Value) return;
                if (__instance == null) return;

                IntPtr ownerPtr = DragonBreathTunerPlugin.ReadPtr(DragonBreathTunerPlugin.OwnerOffset, __instance);
                if (ownerPtr == IntPtr.Zero) return;
                if (!DragonBreathTunerPlugin.IsChannelOwner(ownerPtr)) return;

                var owner = new ObjectsCommon(ownerPtr);
                var ownerTransform = owner.transform;
                var prefabTransform = __instance.transform;
                if (ownerTransform == null || prefabTransform == null) return;

                Vector3 ownerPos = ownerTransform.position;
                Vector3 prefabPos = prefabTransform.position;
                // Keep the prefab's z so the breath stays on the same plane; only x/y follow the caster.
                prefabTransform.position = new Vector3(ownerPos.x, ownerPos.y, prefabPos.z);
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_DragonsBreathPrefab_Update_FollowPlayer] {ex}");
            }
        }
    }

    /// <summary>
    /// Prefix on <c>WASD.CannotUseMovement</c>. The vanilla body returns true (and calls
    /// WASD.StopAgentMovement) while the player is rooted/channeling. For the local player who is
    /// channeling their own Channelled Dragon breath we skip the body entirely and report "can move".
    /// </summary>
    [HarmonyPatch(typeof(WASD), "CannotUseMovement")]
    public static class Patch_WASD_CannotUseMovement
    {
        public static bool Prefix(WASD __instance, ref bool __result)
        {
            try
            {
                if (!DragonBreathTunerPlugin.Enabled.Value) return true;
                if (!DragonBreathTunerPlugin.MoveWhileChanneling.Value) return true;
                if (!DragonBreathTunerPlugin.IsLocalChanneling(__instance)) return true;

                // WASD.Update also skips input when Movement.ScriptedWalkActive is set (0xBC), which the
                // channel choreography may use to hold the caster. Clear it for the duration of our
                // channel so player input is not silently dropped after this gate.
                IntPtr movement = DragonBreathTunerPlugin.ReadPtr(DragonBreathTunerPlugin.WasdMovementOffset, __instance);
                DragonBreathTunerPlugin.WriteBool(movement, DragonBreathTunerPlugin.ScriptedWalkOffset, false);

                __result = false;
                return false; // skip original so StopAgentMovement is not called
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_WASD_CannotUseMovement] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Prefix on <c>Movement.CanEnablePlayerMovement</c>. The vanilla body returns false and calls
    /// Movement.StopCharacter while rooted. For the local channeling player we skip the body and report
    /// true so Movement.Update keeps driving the nav agent.
    /// </summary>
    [HarmonyPatch(typeof(Movement), "CanEnablePlayerMovement")]
    public static class Patch_Movement_CanEnablePlayerMovement
    {
        public static bool Prefix(Movement __instance, ref bool __result)
        {
            try
            {
                if (!DragonBreathTunerPlugin.Enabled.Value) return true;
                if (!DragonBreathTunerPlugin.MoveWhileChanneling.Value) return true;
                if (!DragonBreathTunerPlugin.IsLocalChanneling(__instance)) return true;

                __result = true;
                return false; // skip original so StopCharacter is not called
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_Movement_CanEnablePlayerMovement] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Prefix on <c>Movement.PlayerMovementNotValid</c>. Same gates as CanEnablePlayerMovement but used by
    /// the movement validity check; report "valid" for the local channeling player.
    /// </summary>
    [HarmonyPatch(typeof(Movement), "PlayerMovementNotValid")]
    public static class Patch_Movement_PlayerMovementNotValid
    {
        public static bool Prefix(Movement __instance, ref bool __result)
        {
            try
            {
                if (!DragonBreathTunerPlugin.Enabled.Value) return true;
                if (!DragonBreathTunerPlugin.MoveWhileChanneling.Value) return true;
                if (!DragonBreathTunerPlugin.IsLocalChanneling(__instance)) return true;

                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_Movement_PlayerMovementNotValid] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// Postfix on <c>SpellTooltipDatabase.GetLocalizedDescription(Spell)</c> so the in-game description
    /// reflects the tuned values for Dragon's Breath.
    /// </summary>
    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedDescription")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedDescription
    {
        public static void Postfix(Spell spell, ref string __result)
        {
            try
            {
                if (!DragonBreathTunerPlugin.Enabled.Value) return;
                if (spell != Spell.DragonsBreath) return;

                string suffix = DragonBreathTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;
            }
            catch (Exception ex)
            {
                DragonBreathTunerPlugin.Log?.LogError($"[Patch_SpellTooltipDatabase_GetLocalizedDescription] {ex}");
            }
        }
    }
}
