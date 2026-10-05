using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace NavMeshFixMod
{
    [BepInPlugin("com.custom.navmeshfixmod", "NavMeshFixMod", "1.0.0")]
    public class NavMeshFixModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> SafeWarpGuard;
        public static ConfigEntry<bool> SpellResumeGuard;
        public static ConfigEntry<bool> MonsterSnapGuard;
        public static ConfigEntry<float> NavMeshSnapRadius;

        public override void Load()
        {
            Log = base.Log;

            ModEnabled = Config.Bind("General", "ModEnabled", true, "Enable or disable NavMeshFixMod (Default: true)");
            SafeWarpGuard = Config.Bind("General", "SafeWarpGuard", true, "Safely clamp and place characters/agents onto valid NavMesh during SafeWarp (Default: true)");
            SpellResumeGuard = Config.Bind("General", "SpellResumeGuard", true, "Prevent InvalidOperationException when spells resume movement on off-mesh agents (Default: true)");
            MonsterSnapGuard = Config.Bind("General", "MonsterSnapGuard", true, "Auto-snap monsters spawned slightly off-mesh onto the nearest NavMesh surface (Default: true)");
            NavMeshSnapRadius = Config.Bind("General", "NavMeshSnapRadius", 5.0f, "Radius in meters to search for valid NavMesh surface when snapping (Default: 5.0)");

            Harmony.CreateAndPatchAll(typeof(Patch_ObjectsCommon_SafeWarp));
            Harmony.CreateAndPatchAll(typeof(Patch_LaughingSlashesPrefab_CleanupMovementState));
            Harmony.CreateAndPatchAll(typeof(Patch_MonsterMovement_Start));

            Log.LogInfo("=================================================");
            Log.LogInfo("NavMeshFixMod v1.0.0 (BepInEx 6) Initialized!");
            Log.LogInfo($"Mod Enabled: {ModEnabled.Value}");
            Log.LogInfo($"SafeWarp Guard: {SafeWarpGuard.Value}");
            Log.LogInfo($"Spell Resume Guard: {SpellResumeGuard.Value}");
            Log.LogInfo($"Monster Snap Guard: {MonsterSnapGuard.Value}");
            Log.LogInfo($"Snap Radius: {NavMeshSnapRadius.Value}m");
            Log.LogInfo("Zero-lag event-driven architecture active.");
            Log.LogInfo("=================================================");
        }
    }

    /// <summary>
    /// Patches ObjectsCommon.SafeWarp.
    /// Vanilla SafeWarp calls agent.Warp(position) unconditionally, even if the scene's NavMesh
    /// is not loaded/baked or if the position is slightly off the NavMesh polygon.
    /// In Unity, calling agent.Warp while !isOnNavMesh triggers the native engine warning:
    /// 'Failed to create agent because it is not close enough to the NavMesh' and fails the warp.
    /// This patch samples the nearest NavMesh point and repositions the transform safely.
    /// </summary>
    [HarmonyPatch(typeof(ObjectsCommon), nameof(ObjectsCommon.SafeWarp))]
    public static class Patch_ObjectsCommon_SafeWarp
    {
        public static bool Prefix(ObjectsCommon __instance, ref Vector3 position)
        {
            try
            {
                if (NavMeshFixModPlugin.ModEnabled == null || !NavMeshFixModPlugin.ModEnabled.Value)
                    return true;

                if (NavMeshFixModPlugin.SafeWarpGuard == null || !NavMeshFixModPlugin.SafeWarpGuard.Value)
                    return true;

                if (__instance == null)
                    return true;

                float radius = NavMeshFixModPlugin.NavMeshSnapRadius != null ? NavMeshFixModPlugin.NavMeshSnapRadius.Value : 5.0f;

                // 1. If NavMesh has active geometry near target, snap position to valid polygon surface
                if (NavMesh.SamplePosition(position, out var hit, radius, NavMesh.AllAreas))
                {
                    position = hit.position;
                }

                var agent = __instance.Agent;
                if (agent != null)
                {
                    // 2. If the agent is not on a NavMesh yet (e.g. during scene transition before NavMesh is registered),
                    // calling agent.Warp() native code triggers "Failed to create agent because it is not close enough to the NavMesh".
                    // Temporarily disable the agent, set transform.position directly, and re-enable if target is valid on mesh.
                    if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
                    {
                        agent.enabled = false;
                        __instance.transform.position = position;

                        if (NavMesh.SamplePosition(position, out var validHit, 2.0f, NavMesh.AllAreas))
                        {
                            __instance.transform.position = validHit.position;
                            agent.enabled = true;
                        }
                        return false; // Handled safely without triggering Unity native failure!
                    }
                }
            }
            catch (Exception ex)
            {
                NavMeshFixModPlugin.Log?.LogDebug($"SafeWarp guard handled exception: {ex.Message}");
            }
            return true;
        }
    }

    /// <summary>
    /// Patches LaughingSlashesPrefab.CleanupMovementState.
    /// Vanilla CleanupMovementState calls _ownerAgent.Resume() or isStopped = false.
    /// If the agent was not placed on a NavMesh (or if scene transition cleared NavMesh data),
    /// calling Resume() throws:
    /// InvalidOperationException: "Resume" can only be called on an active agent that has been placed on a NavMesh.
    /// This uncaught exception crashes the transition / AoE cleanup loop, freezing the game.
    /// </summary>
    [HarmonyPatch(typeof(LaughingSlashesPrefab), nameof(LaughingSlashesPrefab.CleanupMovementState))]
    public static class Patch_LaughingSlashesPrefab_CleanupMovementState
    {
        public static bool Prefix(LaughingSlashesPrefab __instance)
        {
            try
            {
                if (NavMeshFixModPlugin.ModEnabled == null || !NavMeshFixModPlugin.ModEnabled.Value)
                    return true;

                if (NavMeshFixModPlugin.SpellResumeGuard == null || !NavMeshFixModPlugin.SpellResumeGuard.Value)
                    return true;

                if (__instance == null)
                    return true;

                var agent = __instance._ownerAgent;
                if (agent != null)
                {
                    if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
                    {
                        // Agent is off-mesh or unplaced. Avoid calling vanilla Resume() which crashes!
                        __instance._movementRestored = true;
                        __instance._dashActive = false;
                        return false; // Skip vanilla method safely!
                    }
                }
            }
            catch (Exception ex)
            {
                NavMeshFixModPlugin.Log?.LogDebug($"SpellResume guard handled exception: {ex.Message}");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Patches MonsterMovement.Start.
    /// If a monster spawns on coordinates slightly outside the baked NavMesh polygon boundaries,
    /// its NavMeshAgent fails to create or place. This postfix checks if the agent is off-mesh
    /// and smoothly snaps it to the nearest valid NavMesh polygon within search radius.
    /// </summary>
    [HarmonyPatch(typeof(MonsterMovement), nameof(MonsterMovement.Start))]
    public static class Patch_MonsterMovement_Start
    {
        public static void Postfix(MonsterMovement __instance)
        {
            try
            {
                if (NavMeshFixModPlugin.ModEnabled == null || !NavMeshFixModPlugin.ModEnabled.Value)
                    return;

                if (NavMeshFixModPlugin.MonsterSnapGuard == null || !NavMeshFixModPlugin.MonsterSnapGuard.Value)
                    return;

                if (__instance == null)
                    return;

                var agent = __instance._agent;
                if (agent != null && !agent.isOnNavMesh)
                {
                    float radius = NavMeshFixModPlugin.NavMeshSnapRadius != null ? NavMeshFixModPlugin.NavMeshSnapRadius.Value : 5.0f;
                    if (NavMesh.SamplePosition(__instance.transform.position, out var hit, radius, NavMesh.AllAreas))
                    {
                        agent.enabled = false;
                        __instance.transform.position = hit.position;
                        agent.enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                NavMeshFixModPlugin.Log?.LogDebug($"MonsterMovement.Start guard handled exception: {ex.Message}");
            }
        }
    }
}
