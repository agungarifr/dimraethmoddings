using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Unity.Collections;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    public class RelationshipModule : ModModuleBase
    {
        public static RelationshipModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Relationship Multiplier";
        public override string Description => "Multiplier for NPC relationship gains (Opinion, Friendship, Romance, and Value Alignment)";

        public ConfigEntry<float> GainMultiplier;
        public ConfigEntry<float> TotalGainMultiplier;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.Relationship";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Relationship Multiplier Module (Default: false, Vanilla: false)");

            GainMultiplier = config.Bind(sec, "RelationshipGainMultiplier", 2.0f,
                "Multiplier for individual relationship components (Default: 2.0, Vanilla: 1.0)");

            TotalGainMultiplier = config.Bind(sec, "TotalRelationshipGainMultiplier", 1.0f,
                "Additional multiplier for total relationship gain (Default: 1.0, Vanilla: 1.0)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawFloatSpinner(x, curY, width, "Gain Multiplier", GainMultiplier, 0.5f, 1.0f, 20.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            curY += DrawFloatSpinner(x, curY, width, "Total Gain Multiplier", TotalGainMultiplier, 0.5f, 1.0f, 20.0f, "Vanilla: 1.0x", labelStyle, btnStyle, "0.0x");
            return curY - y;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(NPCRelationship), nameof(NPCRelationship.UpdateRelationship))]
            [HarmonyPrefix]
            public static void Prefix_UpdateRelationship(
                ref int opinionDelta,
                ref int friendshipDelta,
                ref int romanceDelta,
                ref int valueAlignmentDelta)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                try
                {
                    float mult = (Instance.GainMultiplier?.Value ?? 1f) * (Instance.TotalGainMultiplier?.Value ?? 1f);
                    if (mult <= 1f) return;

                    if (opinionDelta > 0) opinionDelta = Mathf.Max(1, Mathf.RoundToInt(opinionDelta * mult));
                    if (friendshipDelta > 0) friendshipDelta = Mathf.Max(1, Mathf.RoundToInt(friendshipDelta * mult));
                    if (romanceDelta > 0) romanceDelta = Mathf.Max(1, Mathf.RoundToInt(romanceDelta * mult));
                    if (valueAlignmentDelta > 0) valueAlignmentDelta = Mathf.Max(1, Mathf.RoundToInt(valueAlignmentDelta * mult));

                    DiagnosticsManager.Log("Relationship",
                        $"Multiplied gains ({mult:F1}x) -> Opinion: +{opinionDelta}, Friendship: +{friendshipDelta}, " +
                        $"Romance: +{romanceDelta}, ValueAlignment: +{valueAlignmentDelta}");
                }
                catch { }
            }

            [HarmonyPatch(typeof(NPCDialogues), nameof(NPCDialogues.UpdateRelationshipStatus))]
            [HarmonyPrefix]
            public static void Prefix_UpdateRelationshipStatus(ref RelationshipData gains)
            {
                try
                {
                    // Visual icons clamp:
                    // NPCDialogues.UpdateRelationshipStatus only uses 'gains' to spawn visual flying icon GameObjects
                    // via FlyRelationshipIcons (1 coroutine & GameObject per point).
                    // The actual relationship points are already multiplied and saved in NPCRelationship.UpdateRelationship.
                    // Multiplying 'gains' here was a fatal bug that squared the multiplier and spawned thousands of GameObjects.
                    // Clamping 'gains' to a safe visual limit (at most 3 icons per component) prevents
                    // spawning hundreds or thousands of floating icon GameObjects that cause endless point spam and freeze the game.
                    const int maxVisualIcons = 3;
                    bool neededClamp = gains.Opinion > maxVisualIcons || gains.Friendship > maxVisualIcons ||
                                       gains.Romance > maxVisualIcons || gains.ValueAlignment > maxVisualIcons;

                    if (gains.Opinion > maxVisualIcons) gains.Opinion = maxVisualIcons;
                    if (gains.Friendship > maxVisualIcons) gains.Friendship = maxVisualIcons;
                    if (gains.Romance > maxVisualIcons) gains.Romance = maxVisualIcons;
                    if (gains.ValueAlignment > maxVisualIcons) gains.ValueAlignment = maxVisualIcons;

                    if (neededClamp)
                    {
                        DiagnosticsManager.Log("Relationship",
                            $"Protected UI from freeze: Clamped visual floating icons to {maxVisualIcons} max.");
                    }
                }
                catch { }
            }
        }
    }
}
