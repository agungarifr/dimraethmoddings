using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace CustomItems
{
    // [2026-10-07] Builds the game's own Item/Recipe ScriptableObjects from JSON defs
    // and registers them into ItemManager.Singleton.Items / .Recipes (plain mutable
    // Lists the whole game reads: inventory, crafting UI, shops, tooltips, drops).
    // Custom ItemType values are plain ints everywhere (InventoryEntry.Item is a 4-byte
    // field, saves/networking serialize it numerically), so values >= 1000 are safe.

    public static class ItemFactory
    {
        public static int RegisterAll(Action<string> log, Action<string> warn)
        {
            var manager = ItemManager.Singleton;
            if (manager == null) { warn("ItemManager.Singleton is null; cannot register."); return 0; }

            int registered = 0;
            foreach (var def in Registry.Items)
            {
                try
                {
                    manager.Items.Add(BuildItem(def, warn));
                    registered++;
                }
                catch (Exception ex) { warn($"Failed to build item '{def.Name}': {ex.Message}"); }
            }

            foreach (var def in Registry.Recipes)
            {
                try
                {
                    var recipe = BuildRecipe(def, warn);
                    if (recipe != null)
                    {
                        def.Instance = recipe;
                        manager.Recipes.Add(recipe);
                        registered++;
                    }
                }
                catch (Exception ex) { warn($"Failed to build recipe '{def.Name}': {ex.Message}"); }
            }

            log($"Registered {Registry.Items.Count} items and {Registry.Recipes.Count} recipes into ItemManager.");
            return registered;
        }

        static Item BuildItem(ItemDef def, Action<string> warn)
        {
            Item item = ScriptableObject.CreateInstance(Il2CppType.Of<Item>()).Cast<Item>();
            item.name = def.Name;

            item.Name = def.Type;
            item.ShopName = string.IsNullOrEmpty(def.ShopName) ? def.Name : def.ShopName;
            item.Description = def.Description ?? "";
            item.EffectDescription = def.EffectDescription ?? "";
            item.ItemTooltipCategory = ParseEnum(def.Category, ItemTooltipCategory.Resource, warn, def.Name);
            item.ItemBarEquippable = def.ItemBarEquippable;
            item.Amount = 1;
            item.StackLimit = Math.Max(1, def.StackLimit);
            item.Weight = def.Weight;
            item.BuyValue = def.BuyValue;
            item.SellValue = def.SellValue;
            item.Proficiency = ParseEnum(def.Proficiency, global::Proficiency.None, warn, def.Name);
            item.CooldownGroup = ParseEnum(def.CooldownGroup, ItemBarCooldownGroup.None, warn, def.Name);
            item.SlotGroup = ParseEnum(def.SlotGroup, ItemBarSlotGroup.None, warn, def.Name);
            item.DecayTime = def.DecayTime;
            if (!string.IsNullOrEmpty(def.DecayIntoItem) && Registry.ResolveItemType(def.DecayIntoItem, out var decayInto))
                item.DecayIntoItem = decayInto;
            item.RecipeUnlock = def.RecipeUnlock ?? "";
            item.UnlockRecipeOnPickUp = def.UnlockRecipeOnPickUp;

            if (def.Edible != null)
                item.Edible = BuildEdible(def, warn);

            item.Icon = ResolveIcon(def, warn);
            return item;
        }

        static Edible BuildEdible(ItemDef def, Action<string> warn)
        {
            var e = def.Edible;
            Edible edible = new Edible();
            edible.EdibleType = ParseEnum(e.Type, EdibleType.Food, warn, def.Name);
            edible.NourishmentAmount = e.Nourishment;
            edible.AlcoholAmount = e.AlcoholAmount;
            edible.BonusHealth = e.BonusHealth;
            edible.BonusConcentration = e.BonusConcentration;
            edible.BonusStamina = e.BonusStamina;
            edible.NoOverConsumption = e.NoOverConsumption;

            var effects = new Il2CppSystem.Collections.Generic.List<EffectEntry>();
            foreach (var fx in e.Effects ?? new List<EffectDef>())
            {
                var entry = new EffectEntry
                {
                    Effect = ParseEnum(fx.Effect, global::Effect.None, warn, def.Name),
                    Factor = fx.Factor,
                    EffectTime = fx.Time,
                };
                effects.Add(entry);
            }
            edible.Effects = effects;
            return edible;
        }

        static global::Recipe BuildRecipe(RecipeDef def, Action<string> warn)
        {
            if (!Registry.ResolveItemType(def.Result, out var result))
            {
                warn($"Recipe '{def.Name}': unknown result item '{def.Result}'.");
                return null;
            }

            global::Recipe recipe = ScriptableObject.CreateInstance(Il2CppType.Of<global::Recipe>()).Cast<global::Recipe>();
            recipe.name = def.Name;
            recipe.UUID = def.Uuid;

            var ingredients = new Il2CppSystem.Collections.Generic.List<RecipeItem>();
            foreach (var ing in def.Ingredients)
            {
                if (!Registry.ResolveItemType(ing.Item, out var ingType))
                {
                    warn($"Recipe '{def.Name}': unknown ingredient '{ing.Item}', skipping recipe.");
                    return null;
                }
                ingredients.Add(new RecipeItem { ItemType = ingType, Amount = Math.Max(1, ing.Amount) });
            }
            recipe.Ingredients = ingredients;
            recipe.Result = result;
            recipe.ResultQuantity = Math.Max(1, def.ResultQuantity);
            recipe.Proficiency = ParseEnum(def.Proficiency, global::Proficiency.None, warn, def.Name);
            recipe.BenchRequirement = ParseEnum(def.Bench, global::StorageContainers.None, warn, def.Name);
            recipe.CraftingTime = def.CraftingTime;
            recipe.AwardedXp = def.AwardedXp;
            recipe.LevelRequired = def.LevelRequired;
            recipe.CraftingStationRequired = def.CraftingStationRequired;
            recipe.IsJunk = def.IsJunk;
            recipe.ValidRerollResult = def.ValidRerollResult;
            recipe.GroupId = def.GroupId ?? "";
            recipe.OrderInGroup = def.OrderInGroup;
            return recipe;
        }

        static Sprite ResolveIcon(ItemDef def, Action<string> warn)
        {
            var icon = def.Icon;
            Sprite source = null;

            if (icon != null && !string.IsNullOrEmpty(icon.FromItem))
            {
                if (Registry.ResolveItemType(icon.FromItem, out var fromType))
                {
                    var fromItem = ItemManager.Singleton.GetItem(fromType);
                    source = fromItem != null ? fromItem.Icon : ItemManager.Singleton.GetItemSprite(fromType);
                    if (source == null) warn($"Icon source '{icon.FromItem}' has no sprite (item missing?).");
                }
                else
                {
                    warn($"Icon source '{icon.FromItem}' is not a known item name.");
                }
            }

            if (source == null)
                return SpriteTinter.SolidColor(new Color(0.8f, 0.2f, 0.15f, 1f)); // visible fallback so UI never breaks

            if (icon != null && SpriteTinter.TryParseColor(icon.Tint, out var tint))
                return SpriteTinter.TintFromSprite(source, tint, warn);

            return source;
        }

        static TEnum ParseEnum<TEnum>(string value, TEnum fallback, Action<string> warn, string owner) where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            if (Enum.TryParse(value, true, out TEnum result)) return result;
            warn($"'{owner}': unknown value '{value}' for {typeof(TEnum).Name}, using {fallback}.");
            return fallback;
        }
    }
}
