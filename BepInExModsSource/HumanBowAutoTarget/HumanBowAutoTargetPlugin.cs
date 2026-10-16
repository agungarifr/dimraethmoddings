using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using Il2CppGenerics = Il2CppSystem.Collections.Generic;

namespace HumanBowAutoTarget
{
    /// <summary>
    /// Standalone BepInEx 6 (IL2CPP) plugin that makes the Human bow's normal attack auto-aim at
    /// the nearest enemy instead of wherever the mouse points.
    ///
    /// [2026-10-16] Vanilla behaviour: on left-click, <c>MouseCombat</c> calls
    /// <c>Utility.ReturnMousePosition()</c> (the cursor's world position) and passes it to
    /// <c>Attack.ActivateAutoAttack(direction, lungeOverride)</c>. That <c>direction</c> parameter is
    /// not a direction vector - it is the world-space AIM POINT: inside the method the game forwards it
    /// to <c>PlayerAnimations.UpdateFacingDirectionAttack</c>, which calls
    /// <c>Utility.NormalizeAttackDirectionVector(Vector2 target, Vector2 playerPosition)</c>, and it is
    /// also stored in <c>Attack.StoredMouseDirection</c> (field offset 0xD8), which
    /// <c>Attack.CreateProjectile</c> later reads to build the projectile flight direction. So overriding
    /// that single aim point forces BOTH the character facing and the bow projectile toward the target.
    ///
    /// [2026-10-16] Tuned behaviour: when a Human bow normal attack is triggered and a live enemy is
    /// within the Human bow's own attack range, the aim point is overwritten with that enemy's position.
    /// When no enemy is in range the parameter is left untouched, so the vanilla mouse aim is preserved.
    ///
    /// Scope: Human only, by design (see Targeting/OnlyHuman). Co-op: the patch runs on the owning client
    /// (MouseCombat only drives the local player), so run the same config on host and client.
    /// </summary>
    [BepInPlugin(GUID, NAME, VERSION)]
    public class HumanBowAutoTargetPlugin : BasePlugin
    {
        public const string GUID = "com.custom.humanbowautotarget";
        public const string NAME = "Human Bow Auto-Target";
        public const string VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static HumanBowAutoTargetPlugin Instance;

        public static ConfigEntry<bool> Enabled;

        // [2026-10-16] Scope gate. The request is specifically the Human bow, so by default only Human
        // races get auto-aim; other races keep the vanilla mouse aim.
        public static ConfigEntry<bool> OnlyHuman;

        // [2026-10-16] Detection range control. The range is derived from the Human bow's own vanilla
        // attack range (CharacterManager.GetPlayerRaceAttackRange) so auto-aim never reaches past the
        // weapon's effective reach; this multiplier lets the user widen or narrow it.
        public static ConfigEntry<float> RangeMultiplier;

        // [2026-10-16] Used only when the vanilla range cannot be read (e.g. CharacterManager unavailable).
        public static ConfigEntry<float> FallbackRange;

        public static ConfigEntry<bool> DiagnosticLogging;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Enabled = Config.Bind("General", "Enabled", true,
                "Master switch. When false the bow aims at the mouse pointer like vanilla. (Default: true)");

            OnlyHuman = Config.Bind("Targeting", "OnlyHuman", true,
                "If true auto-aim applies to the Human race only (the race with the bow). Set false to let " +
                "every ranged race auto-aim at the nearest enemy. (Default: true)");

            RangeMultiplier = Config.Bind("Targeting", "RangeMultiplier", 1.0f,
                "Multiplier applied to the Human bow's vanilla attack range when searching for the nearest " +
                "enemy. 1.0 = exactly the bow's reach; larger = pick enemies further away. (Default: 1.0)");

            FallbackRange = Config.Bind("Targeting", "FallbackRange", 15.0f,
                "Enemy-search radius used only if the vanilla bow range cannot be read. 15 matches the " +
                "RangedAutoAttack default travel distance. (Default: 15)");

            DiagnosticLogging = Config.Bind("Diagnostics", "DiagnosticLogging", false,
                "Log each auto-aim (origin, chosen target, range and which lookup path was used) to the " +
                "BepInEx console. (Default: false)");

            var harmony = new Harmony(GUID);
            try
            {
                harmony.PatchAll(typeof(Patch_Attack_ActivateAutoAttack));
            }
            catch (Exception ex)
            {
                Log.LogError($"[{NAME}] Failed to apply ActivateAutoAttack patch: {ex}");
            }

            Log.LogInfo("=================================================");
            Log.LogInfo($"{NAME} v{VERSION} loaded.");
            Log.LogInfo($"Enabled: {Enabled.Value}");
            Log.LogInfo($"OnlyHuman: {OnlyHuman.Value}");
            Log.LogInfo($"RangeMultiplier: {RangeMultiplier.Value}, FallbackRange: {FallbackRange.Value}");
            Log.LogInfo($"DiagnosticLogging: {DiagnosticLogging.Value}");
            Log.LogInfo("=================================================");
        }

        internal static bool IsActive => Instance != null && Enabled != null && Enabled.Value;
    }

    /// <summary>
    /// Prefix on <c>Attack.ActivateAutoAttack(Vector2 direction, bool lungeOverride)</c>. The
    /// <c>direction</c> parameter is the world-space aim point; overwriting it here re-aims the character
    /// AND the bow projectile at the nearest enemy while leaving every vanilla combo / lunge / final-attack
    /// step intact (the original method still runs).
    /// </summary>
    [HarmonyPatch(typeof(Attack), "ActivateAutoAttack")]
    internal static class Patch_Attack_ActivateAutoAttack
    {
        // [2026-10-16] Proof-of-life: the first successful re-aim line is NOT gated by DiagnosticLogging,
        // so the log confirms the patch actually fires in-game even with diagnostics off.
        private static bool _announced;

        [HarmonyPrefix]
        private static void Prefix(Attack __instance, ref Vector2 direction)
        {
            if (!HumanBowAutoTargetPlugin.IsActive) return;

            try
            {
                var player = __instance._player;
                if (player == null || player.Pointer == IntPtr.Zero) return;

                // Player.Race is a NetworkVariable<int> holding the Race ordinal (Human == 1).
                if (HumanBowAutoTargetPlugin.OnlyHuman.Value && player.Race.Value != (int)Race.Human) return;

                Vector3 p = player.transform.position;
                Vector2 origin = new Vector2(p.x, p.y);

                float range = ResolveRange();
                if (range <= 0f) return;

                if (!EnemyFinder.TryFindNearest(player, origin, range, out Vector2 target, out string path))
                    return; // no enemy in range -> keep the vanilla mouse aim

                direction = target;

                if (!_announced)
                {
                    _announced = true;
                    HumanBowAutoTargetPlugin.Log?.LogInfo(
                        $"[AutoTarget] First re-aim: origin=({origin.x:0.##},{origin.y:0.##}) " +
                        $"target=({target.x:0.##},{target.y:0.##}) range={range:0.##} via {path}");
                }
                else if (HumanBowAutoTargetPlugin.DiagnosticLogging.Value)
                {
                    HumanBowAutoTargetPlugin.Log?.LogInfo(
                        $"[AutoTarget] origin=({origin.x:0.##},{origin.y:0.##}) " +
                        $"target=({target.x:0.##},{target.y:0.##}) range={range:0.##} via {path}");
                }
            }
            catch (Exception ex)
            {
                HumanBowAutoTargetPlugin.Log?.LogError($"[AutoTarget] {ex}");
            }
        }

        /// <summary>
        /// The Human bow's own attack range (CharacterManager.GetPlayerRaceAttackRange), scaled by the
        /// RangeMultiplier config; falls back to FallbackRange when it cannot be read.
        /// </summary>
        private static float ResolveRange()
        {
            float vanilla = 0f;
            try
            {
                var cm = CharacterManager.Singleton;
                if (cm != null) vanilla = cm.GetPlayerRaceAttackRange(Race.Human);
            }
            catch (Exception ex)
            {
                HumanBowAutoTargetPlugin.Log?.LogError($"[AutoTarget] range lookup: {ex}");
            }

            if (vanilla <= 0f) vanilla = HumanBowAutoTargetPlugin.FallbackRange.Value;
            return vanilla * HumanBowAutoTargetPlugin.RangeMultiplier.Value;
        }
    }

    /// <summary>
    /// Nearest-live-enemy lookup around a world position.
    ///
    /// Primary path reuses the game's own team-filtered query,
    /// <c>BaseSpellLibrary.GetAllEnemiesInRangeOfPosition(Vector2, float)</c> (same API as other mods),
    /// invoked on a spell instance owned by the local player so "enemy" means a monster hostile to the
    /// player. If no player-owned spell is registered, it falls back to a 2D physics overlap filtered to
    /// <c>MonsterBehaviour</c> (the game's monster component), which is always available.
    /// </summary>
    internal static class EnemyFinder
    {
        private static MethodInfo _getEnemiesOfPosition;
        private static bool _lookupFailed;

        public static bool TryFindNearest(Player owner, Vector2 center, float range,
            out Vector2 target, out string path)
        {
            target = default;
            path = "none";
            if (range <= 0f) return false;

            var caster = FindPlayerCaster(owner);
            if (caster != null && EnsureLookup())
            {
                try
                {
                    var list = (Il2CppGenerics.List<ObjectsCommon>)_getEnemiesOfPosition.Invoke(
                        caster, new object[] { center, range });

                    if (list != null && TryNearestFromList(list, center, out target))
                    {
                        path = "spell-query";
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    HumanBowAutoTargetPlugin.Log?.LogError($"[EnemyFinder] spell query: {ex}");
                }
            }

            if (TryFindNearestMonster(center, range, out target))
            {
                path = "physics";
                return true;
            }
            return false;
        }

        private static bool EnsureLookup()
        {
            if (_getEnemiesOfPosition != null) return true;
            if (_lookupFailed) return false;

            _getEnemiesOfPosition = AccessTools.Method(
                typeof(BaseSpellLibrary), "GetAllEnemiesInRangeOfPosition");
            if (_getEnemiesOfPosition == null)
            {
                _lookupFailed = true;
                HumanBowAutoTargetPlugin.Log?.LogError(
                    "[EnemyFinder] BaseSpellLibrary.GetAllEnemiesInRangeOfPosition not found - " +
                    "falling back to the physics monster lookup.");
                return false;
            }
            return true;
        }

        /// <summary>First registered spell owned by the local player (correct enemy/team filtering).</summary>
        private static BaseSpellLibrary FindPlayerCaster(Player owner)
        {
            try
            {
                var mgr = SpellManager.Singleton;
                if (mgr == null) return null;

                var spells = mgr.GetAllActiveSpells();
                if (spells == null) return null;

                int n = spells.Count;
                for (int i = 0; i < n; i++)
                {
                    var s = spells[i];
                    if (s == null || s.Pointer == IntPtr.Zero) continue;

                    var spellOwner = s._owner;
                    if (spellOwner != null && spellOwner.Pointer == owner.Pointer) return s;
                }
                return null;
            }
            catch (Exception ex)
            {
                HumanBowAutoTargetPlugin.Log?.LogError($"[EnemyFinder] player caster: {ex}");
                return null;
            }
        }

        private static bool TryNearestFromList(Il2CppGenerics.List<ObjectsCommon> list,
            Vector2 center, out Vector2 target)
        {
            target = default;
            ObjectsCommon best = null;
            float bestSqr = float.MaxValue;

            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var e = list[i];
                if (e == null || e.Pointer == IntPtr.Zero) continue;
                if (e.IsDead()) continue;

                Vector3 ep = e.transform.position;
                float dx = ep.x - center.x;
                float dy = ep.y - center.y;
                float sqr = dx * dx + dy * dy;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }

            if (best == null) return false;
            Vector3 bp = best.transform.position;
            target = new Vector2(bp.x, bp.y);
            return true;
        }

        private static bool TryFindNearestMonster(Vector2 center, float range, out Vector2 target)
        {
            target = default;
            try
            {
                var hits = Physics2D.OverlapCircleAll(center, range);
                if (hits == null) return false;

                ObjectsCommon best = null;
                float bestSqr = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    var col = hits[i];
                    if (col == null || col.Pointer == IntPtr.Zero) continue;

                    var monster = col.GetComponentInParent<MonsterBehaviour>();
                    if (monster == null) continue;

                    var oc = monster.GetComponent<ObjectsCommon>();
                    if (oc == null) continue;
                    if (oc.IsDead()) continue;

                    Vector3 p = oc.transform.position;
                    float dx = p.x - center.x;
                    float dy = p.y - center.y;
                    float sqr = dx * dx + dy * dy;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        best = oc;
                    }
                }

                if (best == null) return false;
                Vector3 bp = best.transform.position;
                target = new Vector2(bp.x, bp.y);
                return true;
            }
            catch (Exception ex)
            {
                HumanBowAutoTargetPlugin.Log?.LogError($"[EnemyFinder] physics: {ex}");
                return false;
            }
        }
    }
}
