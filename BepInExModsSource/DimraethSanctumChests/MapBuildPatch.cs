using System;
using HarmonyLib;
using UnityEngine;

namespace DimraethSanctumChests
{
    // Postfix for the friend-made MapButtons.Build(MapContentView). Build returns the
    // root GameObject of the map quick-actions panel; we attach our own child component
    // to it. We never modify the returned object's layout or the friend's code - the
    // chest panel is a self-contained child that lays itself out below the existing
    // buttons and refreshes every time the panel is shown.
    internal static class MapBuildPatch
    {
        public static void Postfix(ref GameObject __result)
        {
            try
            {
                if (!Plugin.Enabled) return;
                if (!(bool)__result) return;

                if (!(bool)__result.GetComponent<ChestButtonPanel>())
                    __result.AddComponent<ChestButtonPanel>();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Sanctum chest panel attach failed: {ex}");
            }
        }
    }
}
