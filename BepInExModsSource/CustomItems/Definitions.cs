using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomItems
{
    // [2026-10-07] JSON definition models + registry for the custom item framework.
    // The game's Item/Recipe are ScriptableObjects keyed by the ItemType enum (an int
    // under the hood), so custom items register under free int values (>= 1000) and
    // resolve their string names through this registry (see Patches.cs).

    public class ItemDef
    {
        public string Name { get; set; }              // unique key, e.g. "SatayMadura"
        public int NumericId { get; set; }            // (ItemType) value; auto-assigned >= 1000 when 0
        public string Category { get; set; }          // ItemTooltipCategory: Food, Resource, HarvestTool...
        public string ShopName { get; set; }
        public string Description { get; set; }
        public string EffectDescription { get; set; }
        public bool ItemBarEquippable { get; set; }
        public int StackLimit { get; set; } = 10;
        public float Weight { get; set; }
        public int BuyValue { get; set; }
        public int SellValue { get; set; }
        public string Proficiency { get; set; }
        public string CooldownGroup { get; set; }     // ItemBarCooldownGroup
        public string SlotGroup { get; set; }         // ItemBarSlotGroup
        public int DecayTime { get; set; }
        public string DecayIntoItem { get; set; }     // item name (vanilla or custom)
        public string RecipeUnlock { get; set; }      // comma-separated recipe result names
        public bool UnlockRecipeOnPickUp { get; set; }
        public IconDef Icon { get; set; }
        public EdibleDef Edible { get; set; }

        [JsonIgnore] public ItemType Type;
    }

    public class IconDef
    {
        public string FromItem { get; set; }          // vanilla ItemType name or custom item name
        public string Tint { get; set; }              // "#RRGGBB" or "#RRGGBBAA"; null = no tint
    }

    public class EdibleDef
    {
        public string Type { get; set; } = "Food";    // EdibleType: Food, Drink, RecipeUnlock, MapFragment
        public int Nourishment { get; set; }
        public float AlcoholAmount { get; set; }
        public int BonusHealth { get; set; }
        public int BonusConcentration { get; set; }
        public int BonusStamina { get; set; }
        public bool NoOverConsumption { get; set; }
        public List<EffectDef> Effects { get; set; }  // vanilla EffectEntry pass-through
        public RegenOverTimeDef RegenOverTime { get; set; }  // custom HoT (see RegenOverTime.cs)
    }

    public class EffectDef
    {
        public string Effect { get; set; }            // Effect enum name
        public float Factor { get; set; }
        public float Time { get; set; }
    }

    public class RegenOverTimeDef
    {
        public float HealthFraction { get; set; }        // fraction of max restored over DurationSeconds
        public float StaminaFraction { get; set; }
        public float ConcentrationFraction { get; set; }
        public float DurationSeconds { get; set; } = 1800f;
    }

    public class RecipeDef
    {
        public string Uuid { get; set; }
        public string Name { get; set; }              // name used by name-based lookups; defaults to Result
        public string Result { get; set; }            // item name (vanilla or custom)
        public int ResultQuantity { get; set; } = 1;
        public List<IngredientDef> Ingredients { get; set; } = new();
        public string Proficiency { get; set; }
        public string Bench { get; set; }             // StorageContainers
        public int CraftingTime { get; set; }
        public int AwardedXp { get; set; }
        public int LevelRequired { get; set; }
        public bool CraftingStationRequired { get; set; }
        public bool IsJunk { get; set; }
        public bool ValidRerollResult { get; set; } = true;
        public string GroupId { get; set; }
        public int OrderInGroup { get; set; }
        public bool UnlockedByDefault { get; set; }

        [JsonIgnore] public Recipe Instance;
    }

    public class IngredientDef
    {
        public string Item { get; set; }              // item name (vanilla or custom)
        public int Amount { get; set; } = 1;
    }

    public class DefinitionsFile
    {
        public List<ItemDef> Items { get; set; } = new();
        public List<RecipeDef> Recipes { get; set; } = new();
    }

    public static class Registry
    {
        public static readonly List<ItemDef> Items = new();
        public static readonly List<RecipeDef> Recipes = new();

        static readonly Dictionary<string, ItemDef> _itemsByName = new(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, RecipeDef> _recipesByName = new(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, RecipeDef> _recipesByUuid = new(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<ItemType, ItemDef> _itemsByType = new();

        public const int CustomIdBase = 1000; // vanilla ItemType enum tops out at 393

        public static bool TryGetItem(string name, out ItemDef def) => _itemsByName.TryGetValue(name ?? "", out def);
        public static bool TryGetItem(ItemType type, out ItemDef def) => _itemsByType.TryGetValue(type, out def);
        public static bool TryGetRecipe(string name, out RecipeDef def) =>
            _recipesByName.TryGetValue(name ?? "", out def) || _recipesByUuid.TryGetValue(name ?? "", out def);

        /// <summary>
        /// Resolves an item name to an ItemType: custom names map to our registry ids,
        /// everything else falls through to the vanilla ItemType enum parse.
        /// </summary>
        public static bool ResolveItemType(string name, out ItemType type)
        {
            type = ItemType.None;
            if (string.IsNullOrEmpty(name)) return false;
            if (_itemsByName.TryGetValue(name, out var def))
            {
                type = def.Type;
                return true;
            }
            return Enum.TryParse(name, true, out type);
        }

        public static void Load(string path, Action<string> log, Action<string> warn)
        {
            Items.Clear();
            Recipes.Clear();
            _itemsByName.Clear();
            _recipesByName.Clear();
            _recipesByUuid.Clear();
            _itemsByType.Clear();

            DefinitionsFile file;
            try
            {
                file = JsonSerializer.Deserialize<DefinitionsFile>(File.ReadAllText(path), JsonOptions);
            }
            catch (Exception ex)
            {
                warn($"Failed to parse {path}: {ex.Message}");
                return;
            }

            int nextId = CustomIdBase;
            foreach (var item in file.Items ?? new List<ItemDef>())
            {
                if (string.IsNullOrWhiteSpace(item.Name)) { warn("Skipping item with no Name."); continue; }
                if (_itemsByName.ContainsKey(item.Name)) { warn($"Duplicate item name '{item.Name}', skipping."); continue; }

                if (item.NumericId == 0)
                {
                    while (_itemsByType.ContainsKey((ItemType)nextId)) nextId++;
                    item.NumericId = nextId++;
                }
                item.Type = (ItemType)item.NumericId;

                Items.Add(item);
                _itemsByName[item.Name] = item;
                _itemsByType[item.Type] = item;
                log($"item '{item.Name}' -> (ItemType){item.NumericId} [{item.Category}]");
            }

            foreach (var recipe in file.Recipes ?? new List<RecipeDef>())
            {
                if (string.IsNullOrWhiteSpace(recipe.Result)) { warn("Skipping recipe with no Result."); continue; }
                if (string.IsNullOrWhiteSpace(recipe.Name)) recipe.Name = recipe.Result;
                if (string.IsNullOrWhiteSpace(recipe.Uuid)) recipe.Uuid = "custom_" + recipe.Name.ToLowerInvariant();
                if (_recipesByName.ContainsKey(recipe.Name)) { warn($"Duplicate recipe name '{recipe.Name}', skipping."); continue; }

                Recipes.Add(recipe);
                _recipesByName[recipe.Name] = recipe;
                _recipesByUuid[recipe.Uuid] = recipe;
                log($"recipe '{recipe.Name}' ({recipe.Uuid}) -> {recipe.Result} x{recipe.ResultQuantity}");
            }
        }

        static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
