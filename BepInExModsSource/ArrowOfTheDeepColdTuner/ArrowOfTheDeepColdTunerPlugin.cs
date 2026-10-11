using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
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
    /// [2026-10-11] SEQUENCED, not fanned. v1.1.0 spawned all N arrows in the same frame in a shotgun
    /// fan; that is NOT what Ember Cascade does. Ember Cascade runs a coroutine
    /// (<c>Passives+&lt;CastFireballsAtTarget&gt;d__192</c>) that yields <c>new WaitForSeconds(0.2f)</c>
    /// between spawns (constant <c>0x184678CEC = 0.2f</c>, resolved from the current GameAssembly.dll),
    /// so its projectiles come out ONE AT A TIME, 0.2s apart, each re-targeted at the live victim object.
    /// We now reproduce that cadence with a frame-driven driver (<see cref="ArrowSequenceDriver"/>):
    /// one arrow every <c>Arrows.SequenceDelay</c> seconds (default 0.2, matching Ember Cascade), each
    /// spawned at the victim's current position via the vanilla <c>SpawnArrowOfTheDeepCold</c>.
    ///
    /// <!-- [2026-10-11] SUPERSEDED: v1.1.0 fanned the arrows in the same frame. Kept for history:
    ///      "Geometry: ... Offsetting each spawn position perpendicular to the caster->victim line
    ///      therefore fans the arrows into a shotgun spread around the victim." The user pointed out
    ///      Ember Cascade sequences its projectiles, so the fan was replaced by the sequenced driver
    ///      below; the fan config keys are preserved (commented) further down. -->
    ///
    /// <!-- [2026-10-11] SUPERSEDED: "The trigger condition is deliberately UNCHANGED: the third ranged
    ///      attack, target must be Chilled. We only change HOW MANY arrows the existing trigger spawns."
    ///      The user later chose to ALSO widen the trigger to every ranged auto-attack against a Chilled
    ///      enemy, so the original "single third-attack trigger" wording no longer describes this mod.
    ///      The old wording is kept (commented) for history; the new behaviour is below. -->
    ///
    /// [2026-10-11] Trigger widened (config <c>Trigger/EveryRangedHit</c>, default true): the passive now
    /// fires on EVERY ranged auto-attack against a Chilled enemy instead of only the vanilla third attack of
    /// a combo. The vanilla gate is <c>Damage.FinalAttack</c> (field offset 0x44), which the game sets true
    /// only when the combo shot index equals 3 (see <c>RangedAutoAttack</c>). Inside
    /// <c>Damages.PassiveChecks</c> that flag is read in exactly one place - the Arrow branch - so we simply
    /// arm it for the duration of the call when the hit is a ranged auto-attack against a Chilled enemy, and
    /// restore it in the postfix. This keeps the vanilla spawn path (and therefore the arrow's own
    /// damage/effects) intact and cannot double-spawn: on the genuine third hit the flag is already true, so
    /// we leave it alone and the branch fires exactly once.
    ///
    /// Implementation:
    ///   * Prefix/Postfix on <c>Damages.PassiveChecks(Damage, ObjectsCommon victim, ObjectsCommon source)</c>.
    ///     The prefix captures the exact victim the branch is about to shoot at (the branch calls
    ///     <c>SpawnArrowOfTheDeepCold</c> synchronously inside the same invocation, so a static capture is
    ///     valid and cannot go stale - there is only one call site) and, when the trigger widening applies,
    ///     arms <c>Damage.FinalAttack</c> so the branch runs on every hit. The postfix restores it.
    ///   * Prefix on <c>Passives.SpawnArrowOfTheDeepCold(Vector3)</c>. It reads the captured victim's Chill
    ///     stacks, and if more than one arrow is due it hands the spawn to <see cref="ArrowSequenceDriver"/>
    ///     (which spawns one arrow every <c>SequenceDelay</c> seconds) and returns false so the original
    ///     single arrow is skipped. A re-entrancy guard makes the driver's own nested calls take the vanilla
    ///     path.
    ///
    /// Co-op: spawning happens through the game's own spell path, so run the SAME config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ArrowOfTheDeepColdTunerPlugin : BasePlugin
    {
        public const string GUID = "com.custom.arrowofthedeepcoldtuner";
        public const string NAME = "Arrow of the Deep Cold Tuner";
        public const string VERSION = "1.2.0";

        // [2026-10-11] Victim matching tolerances. The branch forwards victim.transform.position verbatim,
        // so a tight radius suffices; the search radius is only used to recover the enemy from the position
        // if the captured victim cannot be trusted (see ResolveVictim).
        internal const float VictimMatchRadius = 1.0f;
        internal const float VictimSearchRadius = 1.5f;

        internal static new ManualLogSource Log;
        internal static ArrowOfTheDeepColdTunerPlugin Instance;

        public static ConfigEntry<bool> Enabled;

        // [2026-10-11] Trigger widening. Vanilla fires the passive only on the third ranged attack of a
        // combo (Damage.FinalAttack). When true we arm that flag on every ranged auto-attack against a
        // Chilled enemy so the passive fires on each hit instead.
        public static ConfigEntry<bool> EveryRangedHit;

        // [2026-10-11] Arrow count scaling. Vanilla always spawns exactly one arrow; we spawn one per Chill
        // stack, capped here (Ember Cascade caps at Passives.EmberCascadeMaxFireballs == 10).
        public static ConfigEntry<int> MaxArrows;

        // [2026-10-11] Sequenced-spawn cadence. Ember Cascade yields WaitForSeconds(0.2f) between fireballs;
        // we match that with a per-arrow delay (default 0.2s).
        public static ConfigEntry<float> SequenceDelay;

        // <!-- [2026-10-11] SUPERSEDED by the sequenced driver: v1.1.0 exposed a same-frame fan geometry.
        //      Ember Cascade does not fan - it sequences - so these keys are obsolete. Kept (commented) so
        //      the history and the reason for the change are preserved; they are no longer bound or read.
        // public static ConfigEntry<bool> FanEnabled;
        // public static ConfigEntry<float> FanSpreadDegrees;
        // -->

        public static ConfigEntry<bool> ShowTunedStatsInTooltip;
        public static ConfigEntry<bool> DiagnosticLogging;

        // [2026-10-11] Per-damage context captured from the Damages.PassiveChecks prefix. The Arrow branch
        // consumes it synchronously (single call site), so this cannot leak across unrelated spawns.
        internal static ObjectsCommon CapturedVictim;

        // [2026-10-11] Re-entrancy guard: >0 while the sequenced driver is making its own
        // SpawnArrowOfTheDeepCold calls, so those calls fall through to the vanilla single-arrow body
        // instead of enqueueing another sequence. The call is synchronous on the Unity main thread, so a
        // genuine proc (from Damages.PassiveChecks) can never observe this depth mid-flight.
        private static int _sequenceSpawnDepth;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false Arrow of the Deep Cold spawns its vanilla single arrow. (Default: true)");

            EveryRangedHit = Config.Bind("Trigger", "EveryRangedHit", true,
                "If true Arrow of the Deep Cold fires on EVERY ranged auto-attack against a Chilled enemy, " +
                "instead of only the vanilla third ranged attack of a combo. If false the vanilla third-attack " +
                "trigger is kept and only the arrow count scaling applies. (Default: true)");

            MaxArrows = Config.Bind("Arrows", "MaxArrows", 10,
                "Maximum number of giant piercing ice arrows fired, one per Chill stack on the hit enemy. " +
                "Matches Ember Cascade's cap of 10 (Passives.EmberCascadeMaxFireballs). Values below 1 are " +
                "treated as 1. (Default: 10)");

            // [2026-10-11] Replaces the obsolete same-frame fan. Ember Cascade sequences its projectiles
            // (WaitForSeconds(0.2f) between each), so the arrows are now emitted one at a time on this delay.
            SequenceDelay = Config.Bind("Arrows", "SequenceDelay", 0.2f,
                "Seconds between each arrow of the sequence (Ember Cascade yields WaitForSeconds(0.2f) between " +
                "fireballs, so 0.2 matches it). Larger = a slower, more spread-out volley. (Default: 0.2)");

            // <!-- [2026-10-11] SUPERSEDED: the same-frame fan config. Replaced by Arrows/SequenceDelay
            //      because Ember Cascade sequences rather than fans.
            // FanEnabled = Config.Bind("Arrows", "FanEnabled", true,
            //     "If true the extra arrows are fanned out around the victim (perpendicular to the caster-to-victim " +
            //     "line) so the count is visible and they pierce a wider area. If false all arrows spawn at the " +
            //     "victim's exact position and overlap. (Default: true)");
            // FanSpreadDegrees = Config.Bind("Arrows", "FanSpreadDegrees", 40.0f,
            //     "Total angular spread (degrees) of the fan, centred on the caster-to-victim direction. " +
            //     "Larger = wider shotgun spread. Only used when FanEnabled is true. (Default: 40)");
            // -->

            ShowTunedStatsInTooltip = Config.Bind("Tooltip", "ShowTunedStats", true,
                "Append a short note describing the Chill-stack arrow scaling to the Arrow of the Deep Cold spell " +
                "description and skill-tree node text. (Default: true)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log each Arrow of the Deep Cold sequence (Chill stacks -> arrow count) to the BepInEx console. (Default: false)");

            // [2026-10-11] Frame-driven sequence driver. ClassInjector is required to host a managed
            // MonoBehaviour inside IL2CPP; DontDestroyOnLoad keeps it alive across scene changes.
            ClassInjector.RegisterTypeInIl2Cpp<ArrowSequenceDriver>();
            var go = new GameObject("ArrowOfTheDeepColdTuner");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ArrowSequenceDriver>();

            var harmony = new Harmony(GUID);
            PatchOrLog(harmony, typeof(Patch_Damages_PassiveChecks));
            PatchOrLog(harmony, typeof(Patch_Passives_SpawnArrowOfTheDeepCold));
            PatchOrLog(harmony, typeof(Patch_SpellTooltipDatabase_GetLocalizedDescription));
            PatchOrLog(harmony, typeof(Patch_SkillTreeNode_GetLocalizedDescription));

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"EveryRangedHit: {EveryRangedHit.Value}");
            Log.LogInfo($"Arrows: max={MaxArrows.Value}, sequence delay={FormatMultiplier(SequenceDelay.Value)}s");
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
        /// [2026-10-11] Widens the vanilla trigger: arms <c>Damage.FinalAttack</c> so the Arrow branch of
        /// <c>Damages.PassiveChecks</c> runs on every ranged auto-attack against a Chilled enemy, not just
        /// the third attack of a combo. Returns true when the flag was armed and must be restored by the
        /// caller (the <c>PassiveChecks</c> postfix).
        ///
        /// Safety: inside <c>Damages.PassiveChecks</c> <c>FinalAttack</c> is read only by the Arrow branch,
        /// and the flag is restored the moment the method returns, so no other damage logic observes the
        /// temporary value. On the genuine third hit the flag is already true, so we leave it untouched and
        /// the branch fires exactly once (no double spawn).
        /// </summary>
        internal static bool ArmEveryRangedHit(Damage damage, ObjectsCommon victim)
        {
            if (!IsActive || !EveryRangedHit.Value) return false;
            if (damage == null || damage.Pointer == IntPtr.Zero) return false;

            // Arrow only ever procs from auto attacks: the vanilla branch sits inside an IsFromAuto block.
            if (!damage.IsFromAuto) return false;

            // The genuine third ranged attack already has FinalAttack set; the branch will fire as-is.
            if (damage.FinalAttack) return false;

            // Only widen when the target is actually Chilled (the branch requires Chill > 0 anyway).
            if (GetChillStacks(victim) <= 0) return false;

            damage.FinalAttack = true;
            return true;
        }

        /// <summary>
        /// [2026-10-11] Resolves the enemy the Arrow branch is shooting at. Prefers the victim captured from
        /// <c>Damages.PassiveChecks</c>, but only when it is actually at <paramref name="position"/> (the
        /// position the branch passed through). Otherwise falls back to the game's own
        /// <c>Passives.FindRandomEnemyNear</c> so a stale/re-entrant capture can never scale off the wrong
        /// enemy. Returns null when no victim can be resolved (the vanilla single arrow then runs).
        /// </summary>
        internal static ObjectsCommon ResolveVictim(Passives passives, Vector3 position)
        {
            try
            {
                var captured = CapturedVictim;
                if (captured != null && captured.Pointer != IntPtr.Zero && PositionMatches(captured, position))
                    return captured;

                if (passives == null || passives.Pointer == IntPtr.Zero) return null;
                return passives.FindRandomEnemyNear(new Vector2(position.x, position.y), VictimSearchRadius, null);
            }
            catch (Exception ex)
            {
                Log?.LogError($"[ResolveVictim] {ex}");
                return null;
            }
        }

        /// <summary>[2026-10-11] True when the enemy's current position is within <see cref="VictimMatchRadius"/>
        /// of the spawn position (the branch forwards victim.transform.position verbatim, so this is exact).</summary>
        private static bool PositionMatches(ObjectsCommon enemy, Vector3 position)
        {
            try
            {
                if (enemy.transform == null) return false;
                Vector3 ep = enemy.transform.position;
                float dx = ep.x - position.x;
                float dy = ep.y - position.y;
                return (dx * dx + dy * dy) <= (VictimMatchRadius * VictimMatchRadius);
            }
            catch
            {
                return false;
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

            // [2026-10-11] Superseded wording (kept for history): it only described the Chill-stack scaling
            // and left the trigger implicit. The trigger is now widened to every ranged hit, so the note
            // below states both. Old text:
            // return "\n\n[Deep Cold Tuner] On hitting a Chilled enemy with a ranged attack, fires one giant " +
            //        $"piercing ice arrow per Chill stack, up to {max}.";
            string trigger = EveryRangedHit != null && EveryRangedHit.Value
                ? "Every ranged hit"
                : "The third ranged attack";
            return $"\n\n[Deep Cold Tuner] {trigger} against a Chilled enemy fires one giant " +
                   $"piercing ice arrow per Chill stack, in sequence, up to {max}.";
        }

        internal static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // [2026-10-11] Re-entrancy guard around the driver's own SpawnArrowOfTheDeepCold calls.
        internal static void BeginSequenceSpawn()
        {
            _sequenceSpawnDepth++;
        }

        internal static void EndSequenceSpawn()
        {
            if (_sequenceSpawnDepth > 0) _sequenceSpawnDepth--;
        }

        internal static bool IsSequenceSpawn => _sequenceSpawnDepth > 0;

        /// <summary>
        /// [2026-10-11] Queues a sequenced volley instead of the vanilla single arrow. Called from the
        /// SpawnArrowOfTheDeepCold prefix. Returns true when the vanilla single arrow should still run
        /// (not enabled / not scaling / on error / driver not ready).
        ///
        /// The sequence itself is driven frame-by-frame by <see cref="ArrowSequenceDriver"/> so the arrows
        /// come out one at a time (like Ember Cascade), rather than all in the same frame.
        /// </summary>
        internal static bool TryStartArrowSequence(Passives passives, Vector3 position)
        {
            try
            {
                if (!IsActive) return true;
                if (passives == null || passives.Pointer == IntPtr.Zero) return true;

                // The Arrow branch of Damages.PassiveChecks passes victim.transform.position straight
                // through, so the captured victim must sit at this position. If it does not (e.g. a nested
                // PassiveChecks overwrote the capture), fall back to the enemy actually at this position so
                // we never scale off the wrong target.
                var victim = ResolveVictim(passives, position);
                if (victim == null) return true;

                int chillStacks = GetChillStacks(victim);
                if (chillStacks <= 0) return true; // vanilla trigger would not fire anyway

                int count = Math.Min(chillStacks, Math.Max(1, MaxArrows.Value));
                if (count <= 1) return true; // one arrow == vanilla; let the original run

                var driver = ArrowSequenceDriver.Instance;
                if (driver == null) return true; // driver not ready yet; fall back to the vanilla single arrow

                float delay = Math.Max(0.01f, SequenceDelay.Value);
                driver.Enqueue(passives, victim, position, count, delay);

                if (DiagnosticLogging.Value)
                {
                    Log.LogInfo($"[ArrowOfTheDeepCold] chill={chillStacks} -> {count} arrows, " +
                                $"one every {FormatMultiplier(delay)}s (sequenced)");
                }

                return false; // skip the vanilla single arrow; the driver will emit the sequence
            }
            catch (Exception ex)
            {
                Log?.LogError($"[TryStartArrowSequence] {ex}");
                return true;
            }
        }
    }

    /// <summary>
    /// [2026-10-11] Frame-driven spawn driver. Ember Cascade sequences its fireballs with
    /// <c>WaitForSeconds(0.2f)</c> inside a coroutine; rather than risk managed-IEnumerator/coroutine
    /// interop we tick the same cadence here in <c>Update()</c>. Each queued sequence emits one arrow every
    /// <c>SequenceDelay</c> seconds at the victim's current position, until its count is exhausted - the
    /// same shape as <c>Passives+&lt;CastFireballsAtTarget&gt;d__192</c>.
    /// </summary>
    public sealed class ArrowSequenceDriver : MonoBehaviour
    {
        // Required so Il2CppInterop can construct the injected type.
        public ArrowSequenceDriver(IntPtr ptr) : base(ptr) { }

        internal static ArrowSequenceDriver Instance;

        private sealed class Sequence
        {
            public Passives Passives;
            public ObjectsCommon Victim;
            public Vector3 LastPosition;
            public int Remaining;
            public float Timer;
        }

        private readonly List<Sequence> _sequences = new List<Sequence>();

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>[2026-10-11] Queues a volley. Timer starts at <paramref name="delay"/> so the first
        /// arrow is emitted one delay after the proc, matching Ember Cascade (which waits before its first
        /// fireball).</summary>
        internal void Enqueue(Passives passives, ObjectsCommon victim, Vector3 position, int count, float delay)
        {
            // Safety cap: never let a pathological chain of procs grow this without bound.
            if (_sequences.Count >= 64) _sequences.RemoveAt(0);

            _sequences.Add(new Sequence
            {
                Passives = passives,
                Victim = victim,
                LastPosition = position,
                Remaining = count,
                Timer = delay,
            });
        }

        private void Update()
        {
            if (_sequences.Count == 0) return;

            float dt = Time.deltaTime;

            for (int i = _sequences.Count - 1; i >= 0; i--)
            {
                Sequence seq = _sequences[i];
                seq.Timer -= dt;
                if (seq.Timer > 0f) continue;

                // Re-target the live victim each tick (Ember Cascade passes the target object, so its
                // fireballs track it). If the victim is gone we keep firing at the last known position so
                // the remaining piercing arrows still land where it died.
                try
                {
                    if (seq.Victim != null && seq.Victim.Pointer != IntPtr.Zero)
                    {
                        var tr = seq.Victim.transform;
                        if (tr != null) seq.LastPosition = tr.position;
                    }
                }
                catch
                {
                    // Victim transform vanished mid-sequence; fall through and use the last position.
                }

                SpawnOne(seq.Passives, seq.LastPosition);

                seq.Remaining--;
                if (seq.Remaining <= 0)
                {
                    _sequences.RemoveAt(i);
                    continue;
                }

                seq.Timer = Math.Max(0.01f, ArrowOfTheDeepColdTunerPlugin.SequenceDelay.Value);
            }
        }

        private static void SpawnOne(Passives passives, Vector3 position)
        {
            if (passives == null || passives.Pointer == IntPtr.Zero) return;

            ArrowOfTheDeepColdTunerPlugin.BeginSequenceSpawn();
            try
            {
                // Re-enters the patched method; the guard makes it run the vanilla single-arrow body.
                passives.SpawnArrowOfTheDeepCold(position);
            }
            catch (Exception ex)
            {
                ArrowOfTheDeepColdTunerPlugin.Log?.LogError($"[ArrowSequenceDriver.SpawnOne] {ex}");
            }
            finally
            {
                ArrowOfTheDeepColdTunerPlugin.EndSequenceSpawn();
            }
        }
    }

    /// <summary>
    /// [2026-10-11] Prefix/Postfix on the private <c>Damages.PassiveChecks(Damage damage, ObjectsCommon victim,
    /// ObjectsCommon source)</c>.
    ///
    /// The prefix captures the victim so the Arrow of the Deep Cold branch's synchronous
    /// <c>SpawnArrowOfTheDeepCold</c> call can read that enemy's Chill stacks (the Arrow branch is the only
    /// caller of <c>SpawnArrowOfTheDeepCold</c>, so the capture cannot be consumed by an unrelated spawn).
    /// It also widens the trigger: when <c>Trigger/EveryRangedHit</c> is enabled and this call is a ranged
    /// auto-attack against a Chilled enemy, it arms <c>Damage.FinalAttack</c> so the branch fires on every
    /// hit rather than only the vanilla third attack. The postfix restores the flag.
    /// </summary>
    [HarmonyPatch(typeof(Damages), "PassiveChecks")]
    public static class Patch_Damages_PassiveChecks
    {
        // Positional binding: __0 = Damage, __1 = victim, __2 = source. We bind by index rather than
        // by name because this is a private method and interop parameter names are not guaranteed.
        public static void Prefix(Damage __0, ObjectsCommon __1, out bool __state)
        {
            __state = false;
            ArrowOfTheDeepColdTunerPlugin.CapturedVictim = __1;

            try
            {
                __state = ArrowOfTheDeepColdTunerPlugin.ArmEveryRangedHit(__0, __1);
            }
            catch (Exception ex)
            {
                ArrowOfTheDeepColdTunerPlugin.Log?.LogError($"[Patch_Damages_PassiveChecks.Prefix] {ex}");
            }
        }

        // Restores the vanilla FinalAttack value we temporarily armed so no code outside PassiveChecks
        // ever observes the widened trigger.
        public static void Postfix(Damage __0, bool __state)
        {
            if (!__state || __0 == null) return;

            try
            {
                __0.FinalAttack = false;
            }
            catch (Exception ex)
            {
                ArrowOfTheDeepColdTunerPlugin.Log?.LogError($"[Patch_Damages_PassiveChecks.Postfix] {ex}");
            }
        }
    }

    /// <summary>
    /// [2026-10-11] Prefix on <c>Passives.SpawnArrowOfTheDeepCold(Vector3 position)</c> - the single point
    /// that spawns the Arrow of the Deep Cold projectile. When the captured victim has multiple Chill stacks
    /// we queue a sequenced volley (one arrow per stack, one at a time) and skip the vanilla single arrow;
    /// otherwise the vanilla body runs unchanged.
    /// </summary>
    [HarmonyPatch(typeof(Passives), "SpawnArrowOfTheDeepCold")]
    public static class Patch_Passives_SpawnArrowOfTheDeepCold
    {
        // Positional binding: __0 = Vector3 position.
        public static bool Prefix(Passives __instance, Vector3 __0)
        {
            // Calls made by the sequenced driver must run the vanilla single-arrow body.
            if (ArrowOfTheDeepColdTunerPlugin.IsSequenceSpawn) return true;

            try
            {
                return ArrowOfTheDeepColdTunerPlugin.TryStartArrowSequence(__instance, __0);
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
