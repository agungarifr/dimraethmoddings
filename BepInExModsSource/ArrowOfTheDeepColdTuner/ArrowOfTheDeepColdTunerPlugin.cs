using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ArrowOfTheDeepColdTuner
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that makes the Elemental Ranger keystone passive
    /// "Arrow of the Deep Cold" behave like the Ember Cascade keystone.
    ///
    /// [2026-10-11] Vanilla behaviour: <c>Damages.PassiveChecks</c> fires the passive once, only on the
    /// third ranged attack, and only when the victim has at least one Chill stack
    /// (<c>Stacking.GetStackCount(StackingEffect.Chill) &gt; 0</c>); it then calls
    /// <c>Passives.SpawnArrowOfTheDeepCold(victim.transform.position)</c>, which spawns exactly ONE
    /// giant piercing ice arrow (<c>PassiveSpell(this, Spell.ArrowOfTheDeepCold).Cast(position, true)</c>).
    ///
    /// [2026-10-11] Tuned behaviour: when that one call fires, we spawn one arrow per Chill stack on the
    /// victim instead of a single arrow, capped at <c>Arrows.MaxArrows</c> (default 10) - the exact shape
    /// of Ember Cascade, which spawns <c>min(burningStacks, EmberCascadeMaxFireballs)</c> fireballs
    /// (<c>Passives.EmberCascadeCheck</c> -&gt; <c>CastFireballsAtTarget</c>).
    ///
    /// The trigger condition is deliberately UNCHANGED: the third ranged attack, target must be Chilled.
    /// We only change HOW MANY arrows the existing trigger spawns.
    ///
    /// Implementation:
    ///   * Prefix on <c>Damages.PassiveChecks(Damage, ObjectsCommon victim, ObjectsCommon source)</c> to
    ///     capture the exact victim the branch is about to shoot at. The branch calls
    ///     <c>SpawnArrowOfTheDeepCold</c> synchronously inside the same invocation, so a static capture is
    ///     valid and cannot go stale (there is only one call site).
    ///   * Prefix on <c>Passives.SpawnArrowOfTheDeepCold(Vector3)</c>. It reads the captured victim's Chill
    ///     stacks, and if more than one arrow is due it fans <c>N</c> calls to the SAME method (which then
    ///     run the vanilla single-arrow body) and returns false so the original single arrow is skipped.
    ///     A re-entrancy guard makes the nested calls take the vanilla path.
    ///
    /// Geometry: the arrow prefab erupts AT the target location and travels outward, away from the caster
    /// (<c>ArrowOfTheDeepColdPrefab.OnStart</c> sets <c>_targetPosition = self + normalize(self - _owner) * d</c>).
    /// Offsetting each spawn position perpendicular to the caster-&gt;victim line therefore fans the arrows
    /// into a shotgun spread around the victim; the caster position is the Passives component's own
    /// transform (Passives lives on the player GameObject - <c>Damages.PassiveChecks</c> obtains it via
    /// <c>source.GetComponent&lt;Passives&gt;()</c>).
    ///
    /// Co-op: spawning happens through the game's own spell path, so run the SAME config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ArrowOfTheDeepColdTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.arrowofthedeepcoldtuner";
        public const string NAME = "Arrow of the Deep Cold Tuner";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static ArrowOfTheDeepColdTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;

        // [2026-10-11] Arrow count scaling. Vanilla always spawns exactly one arrow; we spawn one per Chill
        // stack, capped here (Ember Cascade caps at Passives.EmberCascadeMaxFireballs == 10).
        public static ConfigEntry<int> MaxArrows;

        // [2026-10-11] Fan geometry for the extra arrows.
        public static ConfigEntry<bool> FanEnabled;
        public static ConfigEntry<float> FanSpreadDegrees;

        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

        // [2026-10-11] Per-damage context captured from the Damages.PassiveChecks prefix. The Arrow branch
        // consumes it synchronously (single call site), so this cannot leak across unrelated spawns.
        internal static ObjectsCommon CapturedVictim;

        // [2026-10-11] Re-entrancy guard: true while we are fanning out the N nested SpawnArrowOfTheDeepCold
        // calls, so those nested calls fall through to the vanilla single-arrow body instead of fanning again.
        private static bool _fanningOut;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false Arrow of the Deep Cold spawns its vanilla single arrow. (Default: true)");

            MaxArrows = Config.Bind("Arrows", "MaxArrows", 10,
                "Maximum number of giant piercing ice arrows fired, one per Chill stack on the hit enemy. " +
                "Matches Ember Cascade's cap of 10 (Passives.EmberCascadeMaxFireballs). Values below 1 are " +
                "treated as 1. (Default: 10)");

            FanEnabled = Config.Bind("Arrows", "FanEnabled", true,
                "If true the extra arrows are fanned out around the victim (perpendicular to the caster-to-victim " +
                "line) so the count is visible and they pierce a wider area. If false all arrows spawn at the " +
                "victim's exact position and overlap. (Default: true)");

            FanSpreadDegrees = Config.Bind("Arrows", "FanSpreadDegrees", 40.0f,
                "Total angular spread (degrees) of the fan, centred on the caster-to-victim direction. " +
                "Larger = wider shotgun spread. Only used when FanEnabled is true. (Default: 40)");

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a short note describing the Chill-stack arrow scaling to the Arrow of the Deep Cold spell " +
                "description and skill-tree node text. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log each Arrow of the Deep Cold spawn (Chill stacks -> arrow count) to the BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_Damages_PassiveChecks));
            PatchOrLog(harmony, typeof(Patch_Passives_SpawnArrowOfTheDeepCold));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedDescription));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"Arrows: max={MaxArrows.Value}, fan={FanEnabled.Value} (spread={FormatMultiplier(FanSpreadDegrees.Value)} deg)");
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
                Log.LogError($"[{NAME}] Failed to apply patch {patchType.Name}: {ex}");
            }
        }

        internal static bool IsActive => Instance != null && Enabled != null && Enabled.Value;

        /// <summary>
        /// [2026-10-11] Reads the Chill stack count from an enemy's <c>Stacking</c> component - the same
        /// <c>Stacking.GetStackCount(StackingEffect.Chill)</c> call the vanilla Arrow branch uses to gate the
        /// passive. Returns 0 when the victim has no Stacking component.
        /// </summary>
        internal static int GetChillStacks(ObjectsCommon victim)
        {
            if (victim == null || victim.Pointer == IntPtr.Zero) return 0;
            try
            {
                var stacking = victim.GetComponent<Stacking>();
                if (stacking == null) return 0;
                return stacking.GetStackCount(StackingEffect.Chill);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[GetChillStacks] {ex}");
                return 0;
            }
        }

        /// <summary>
        /// [2026-10-11] Appends the tuned-behaviour note to the Arrow of the Deep Cold description so the
        /// in-game text reflects what this mod changes (the authored string never changes on its own).
        /// </summary>
        internal static string BuildTooltipSuffix()
        {
            if (!ShowTunedStatsInTooltip.Value) return null;

            int max = Math.Max(1, MaxArrows.Value);
            return "\n\n[Deep Cold Tuner] On hitting a Chilled enemy with a ranged attack, fires one giant " +
                   $"piercing ice arrow per Chill stack, up to {max}.";
        }

        internal static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// [2026-10-11] Fans out the Arrow of the Deep Cold spawn: instead of the vanilla single arrow we call
        /// <c>SpawnArrowOfTheDeepCold</c> once per Chill stack (capped) with positions spread perpendicular to
        /// the caster-to-victim line. Called from the SpawnArrowOfTheDeepCold prefix. Returns true when the
        /// vanilla single arrow should still run (not enabled / not scaling / on error).
        /// </summary>
        internal static bool TrySpawnFannedArrows(Passives passives, Vector3 position)
        {
            try
            {
                if (!IsActive) return true;
                if (passives == null || passives.Pointer == IntPtr.Zero) return true;

                var victim = CapturedVictim;
                if (victim == null || victim.Pointer == IntPtr.Zero) return true;

                int chillStacks = GetChillStacks(victim);
                if (chillStacks <= 0) return true; // vanilla trigger would not fire anyway

                int count = Math.Min(chillStacks, Math.Max(1, MaxArrows.Value));
                if (count <= 1) return true; // one arrow == vanilla; let the original run

                // Caster position: Passives is a component on the player GameObject, so its own transform is
                // the caster. (Damages.PassiveChecks reaches it with source.GetComponent<Passives>().)
                Vector3 origin = passives.transform != null ? passives.transform.position : position;
                Vector2 toVictim = new Vector2(position.x - origin.x, position.y - origin.y);
                Vector2 dir = toVictim.sqrMagnitude > 0.0001f ? toVictim.normalized : Vector2.right;
                Vector2 perp = new Vector2(-dir.y, dir.x);

                float spreadRad = (FanEnabled.Value ? FanSpreadDegrees.Value : 0f) * Mathf.Deg2Rad;
                float halfWidth = (float)Math.Tan(spreadRad * 0.5) * Math.Max(toVictim.magnitude, 1f);

                _fanningOut = true;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        // -0.5 .. +0.5 across the fan, symmetric around the victim.
                        float t = count > 1 ? (i / (float)(count - 1)) - 0.5f : 0f;
                        Vector2 off = perp * (t * 2f * halfWidth);
                        Vector3 p = new Vector3(position.x + off.x, position.y + off.y, position.z);

                        // Re-enters this prefix; the guard makes it run the vanilla single-arrow body.
                        passives.SpawnArrowOfTheDeepCold(p);
                    }
                }
                finally
                {
                    _fanningOut = false;
                }

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[ArrowOfTheDeepCold] chill={chillStacks} -> {count} arrows " +
                                $"(spread={FormatMultiplier(FanSpreadDegrees.Value)} deg, fan={FanEnabled.Value})");
                }

                return false; // skip the vanilla single arrow; we already spawned the fan
            }
            catch (Exception ex)
            {
                _fanningOut = false;
                Log?.LogError($"[TrySpawnFannedArrows] {ex}");
                return true;
            }
        }

        internal static bool IsFanningOut => _fanningOut;
    }

    /// <summary>
    /// [2026-10-11] Prefix on the private <c>Damages.PassiveChecks(Damage damage, ObjectsCommon victim,
    /// ObjectsCommon source)</c>. Captures the victim so the Arrow of the Deep Cold branch's synchronous
    /// <c>SpawnArrowOfTheDeepCold</c> call can read that enemy's Chill stacks. The Arrow branch is the only
    /// caller of <c>SpawnArrowOfTheDeepCold</c>, so the capture cannot be consumed by an unrelated spawn.
    /// </summary>
    [HarmonyPatch(typeof(Damages), "PassiveChecks")]
    public static class Patch_Damages_PassiveChecks
    {
        // Positional binding: __0 = Damage, __1 = victim, __2 = source. We bind by index rather than
        // by name because this is a private method and interop parameter names are not guaranteed.
        public static void Prefix(ObjectsCommon __1)
        {
            ArrowOfTheDeepColdTunerPlugin.CapturedVictim = __1;
        }
    }

    /// <summary>
    /// [2026-10-11] Prefix on <c>Passives.SpawnArrowOfTheDeepCold(Vector3 position)</c> - the single point
    /// that spawns the Arrow of the Deep Cold projectile. When the captured victim has multiple Chill stacks
    /// we spawn one arrow per stack (fanned) and skip the vanilla single arrow; otherwise the vanilla body
    /// runs unchanged.
    /// </summary>
    [HarmonyPatch(typeof(Passives), "SpawnArrowOfTheDeepCold")]
    public static class Patch_Passives_SpawnArrowOfTheDeepCold
    {
        // Positional binding: __0 = Vector3 position.
        public static bool Prefix(Passives __instance, Vector3 __0)
        {
            // Nested calls made while fanning out must run the vanilla single-arrow body.
            if (ArrowOfTheDeepColdTunerPlugin.IsFanningOut) return true;

            try
            {
                return ArrowOfTheDeepColdTunerPlugin.TrySpawnFannedArrows(__instance, __0);
            }
            catch (Exception ex)
            {
                ArrowOfTheDeepColdTunerPlugin.Log?.LogError($"[Patch_Passives_SpawnArrowOfTheDeepCold] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// [2026-10-11] Postfix on <c>SpellTooltipDatabase.GetLocalizedDescription(Spell)</c> - the single text
    /// producer used by the spellbook, spell catalogue, skill-tree node tooltip, character creation and pet
    /// panel. Appends the tuned note for Arrow of the Deep Cold only.
    /// </summary>
    [HarmonyPatch(typeof(SpellTooltipDatabase), "GetLocalizedDescription")]
    public static class Patch_SpellTooltipDatabase_GetLocalizedDescription
    {
        // Positional binding: __0 = Spell spell.
        public static void Postfix(Spell __0, ref string __result)
        {
            try
            {
                if (!ArrowOfTheDeepColdTunerPlugin.IsActive) return;
                if (__0 != Spell.ArrowOfTheDeepCold) return;

                string suffix = ArrowOfTheDeepColdTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;
            }
            catch (Exception ex)
            {
                ArrowOfTheDeepColdTunerPlugin.Log?.LogError($"[Patch_SpellTooltipDatabase_GetLocalizedDescription] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-11] Postfix on <c>SkillTreeNode.GetLocalizedDescription()</c> - the skill-tree node tooltip.
    /// The Elemental Ranger keystone node (<c>ElementalRanger_KeystoneSW</c>) is matched by its associated
    /// spell, its granted passive, or its node id.
    /// </summary>
    [HarmonyPatch(typeof(SkillTreeNode), "GetLocalizedDescription")]
    public static class Patch_SkillTreeNode_GetLocalizedDescription
    {
        public static void Postfix(SkillTreeNode __instance, ref string __result)
        {
            try
            {
                if (!ArrowOfTheDeepColdTunerPlugin.IsActive) return;
                if (__instance == null) return;

                bool matches = __instance.AssociatedSpell == Spell.ArrowOfTheDeepCold
                    || __instance.UnlockSpell == Spell.ArrowOfTheDeepCold
                    || __instance.Passive == Passive.ArrowOfTheDeepCold
                    || (__instance.NodeId != null && __instance.NodeId.Contains("DeepCold"));
                if (!matches) return;

                string suffix = ArrowOfTheDeepColdTunerPlugin.BuildTooltipSuffix();
                if (string.IsNullOrEmpty(suffix)) return;

                __result = (__result ?? string.Empty) + suffix;
            }
            catch (Exception ex)
            {
                ArrowOfTheDeepColdTunerPlugin.Log?.LogError($"[Patch_SkillTreeNode_GetLocalizedDescription] {ex}");
            }
        }
    }
}
