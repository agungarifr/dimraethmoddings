using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    // [2026-10-06 11:55] Module 1 (user request): fast production on the WORKBENCH and ALCHEMY TABLE.
    // Implemented as a postfix on CraftingBench.Update(). Vanilla Update (ISIL:
    // modding\cpp2il_new_isil\IsilDump\Assembly-CSharp\CraftingBench.txt, ISIL ~189-205) does
    // _currentProgress.Value += Time.deltaTime in the same frame it completes the piece at
    // Recipe.CraftingTime (Recipe.CraftingTime field offset 0x38). Running after that add and pushing
    // the extra (SpeedMultiplier - 1) frames of progress yields a net rate of Time.deltaTime * multiplier
    // with no IL rewriting (safer than a transpiler).
    //
    // Station identity: Storage.Container (StorageContainers). WorkBench=2 / AlchemyTable=3 are the
    // placed stations and BuildWorkBench=10 / BuildAlchemyTable=11 the player-built spellings; both are
    // the same two stations, so both are sped up. Campfire/WoodfireStove and all chests keep vanilla.
    // Ported from staging project FastProduction (author: subagent, reviewed by caller).
    public class FastProductionModule : ModModuleBase
    {
        public static FastProductionModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Fast Production";
        public override string Description => "Speed up crafting at the workbench and alchemy table with a configurable multiplier";

        public ConfigEntry<int> SpeedMultiplier;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.FastProduction";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Fast Production (Default: false, Vanilla: false)");

            SpeedMultiplier = config.Bind(sec, "SpeedMultiplier", 5,
                "Crafting speed multiplier at workbench/alchemy stations (Default: 5, Vanilla: 1)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawIntSpinner(x, curY, width, "Crafting Speed Multiplier", SpeedMultiplier, 1, 1, 100, "Vanilla: 1x", labelStyle, btnStyle);
            return curY - y;
        }

        [HarmonyPatch(typeof(CraftingBench), "Update")]
        public static class Patches
        {
            // [2026-10-06 11:45] CraftingBench._currentProgress is a private readonly NetworkVariable<Single>
            // (runtime slot at byte offset 392 / 0x188 per the ISIL dump, NOT the 0x170 shown in the cpp2il
            // C# output). Il2CppInterop exposes game fields publicly, so it is accessed directly - compile-time
            // checked. Its Value get/set are the NetworkVariable virtuals, so this is a proper NetworkVariable
            // write (server/host only, fine for this single-player/host game).
            private static bool _loggedFirstBench;

            private static void Postfix(CraftingBench __instance)
            {
                try
                {
                    if (Instance == null || !Instance.IsEnabled) return;

                    int mult = Instance.SpeedMultiplier.Value;
                    if (mult <= 1) return; // 1 = vanilla rate; nothing to add

                    if (__instance == null) return;

                    // One-time diagnostic: confirms which StorageContainers value benches carry at runtime.
                    if (!_loggedFirstBench)
                    {
                        _loggedFirstBench = true;
                        DimraethModPackPlugin.Log?.LogInfo(
                            $"[FastProduction] first CraftingBench.Container = {__instance.Container} ({(int)__instance.Container})");
                    }

                    if (!IsFastStation(__instance.Container)) return;

                    // Vanilla Update pauses progress while the output is full; keep that pause behaviour.
                    if (__instance.IsOutputBlocked) return;

                    Recipe recipe = __instance.CurrentRecipe;
                    if (recipe == null) return;          // no active job
                    if (recipe.CraftingTime <= 0) return; // instant craft; leave vanilla alone

                    var progress = __instance._currentProgress;
                    if (progress == null) return;

                    float current = progress.Value;
                    if (current >= recipe.CraftingTime) return; // vanilla completes it this frame

                    float next = current + Time.deltaTime * (mult - 1);
                    if (next > recipe.CraftingTime) next = recipe.CraftingTime;
                    progress.Value = next;
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogError($"[FastProduction] CraftingBench.Update postfix failed: {ex}");
                }
            }

            // Workbench + Alchemy Table only (placed and player-built spellings).
            private static bool IsFastStation(StorageContainers container)
            {
                return container == StorageContainers.WorkBench
                    || container == StorageContainers.AlchemyTable
                    || container == StorageContainers.BuildWorkBench
                    || container == StorageContainers.BuildAlchemyTable;
            }
        }
    }
}
