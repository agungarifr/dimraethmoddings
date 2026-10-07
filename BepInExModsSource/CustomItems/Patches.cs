using HarmonyLib;

namespace CustomItems
{
    // [2026-10-07] Name-based lookups in the game resolve via Utility.ReturnItemFromString /
    // Utility.ReturnItemType / Item.ReturnItemType (string -> ItemType enum parse). Custom
    // items use int values with no enum member, so we intercept the string paths and map
    // our registered names first. GetRecipeByString is patched for recipe unlocks/crafting
    // lookups that pass names ("SatayMadura") instead of UUIDs.

    public static class Patches
    {
        [HarmonyPatch(typeof(Utility), nameof(Utility.ReturnItemFromString))]
        public static class Patch_Utility_ReturnItemFromString
        {
            static bool Prefix(string item, ref ItemType __result)
            {
                if (Registry.TryGetItem(item, out var def))
                {
                    __result = def.Type;
                    return false;
                }
                return true;
            }
        }

        // [2026-10-07] OBSOLETE - Utility.ReturnItemType does not exist in the interop
        // assembly (ReturnItemType lives on Item; Utility only has ReturnItemFromString).
        // This block never compiled; kept commented for reference per house rules.
        // [HarmonyPatch(typeof(Utility), nameof(Utility.ReturnItemType))]
        // public static class Patch_Utility_ReturnItemType
        // {
        //     static bool Prefix(string name, ref ItemType __result)
        //     {
        //         if (Registry.TryGetItem(name, out var def))
        //         {
        //             __result = def.Type;
        //             return false;
        //         }
        //         return true;
        //     }
        // }

        [HarmonyPatch(typeof(Item), nameof(Item.ReturnItemType))]
        public static class Patch_Item_ReturnItemType
        {
            static bool Prefix(string name, ref ItemType __result)
            {
                if (Registry.TryGetItem(name, out var def))
                {
                    __result = def.Type;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.GetRecipeByString))]
        public static class Patch_ItemManager_GetRecipeByString
        {
            static bool Prefix(string recipeName, ref Recipe __result)
            {
                if (Registry.TryGetRecipe(recipeName, out var def) && def.Instance != null)
                {
                    __result = def.Instance;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ItemBar), nameof(ItemBar.UseConsumable))]
        public static class Patch_ItemBar_UseConsumable
        {
            // Runs on the consuming player's client (ItemBar is the local HUD quick bar),
            // so identification by item type is exact and the HoT ticks on the right machine.
            static void Postfix(Item item)
            {
                if (item == null) return;
                if (!Registry.TryGetItem(item.Name, out var def)) return;
                if (def.Edible?.RegenOverTime == null) return;

                var player = DataStorage.Singleton != null ? DataStorage.Singleton.Player : null;
                RegenOverTimeTracker.Add(player, def, CustomItemsPlugin.LogInfo, CustomItemsPlugin.LogWarn);
            }
        }
    }
}
