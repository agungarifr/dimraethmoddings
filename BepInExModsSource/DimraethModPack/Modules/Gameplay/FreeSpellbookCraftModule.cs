using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;
using RecipeList = Il2CppSystem.Collections.Generic.List<RecipeItem>;

namespace DimraethModPack.Modules.Gameplay
{
    /// <summary>
    /// [2026-10-07] "Free Spellbook Crafting" - spellbook recipes cost no materials.
    ///
    /// Recipe.Ingredients (List&lt;RecipeItem&gt;) is the single source of truth for crafting
    /// costs (decoded from the IL2CPP dumps): PersonalCrafting.GetRequiredItems derives what
    /// to consume from it (personal crafting + inventory fallback via
    /// RemoveRequiredItemsFromInputSlots), CraftingBench charges/refunds it through the bench
    /// supply path (CraftSelectedServerRpc / AddJobsForQuantity / RefundIngredients), and the
    /// recipe rows and codex strip display it. So replacing the list with an empty one makes
    /// the recipe free everywhere at once.
    ///
    /// Spellbook recipes are identified by their result item teaching a spell
    /// (Item.SpellUnlock != Spell.None) - i.e. crafting the spellbook is what grants the spell.
    ///
    /// Why an empty list and NOT zeroed amounts: PersonalCrafting.FindMatchingRecipe divides
    /// placed/needed per ingredient (idiv inside the per-ingredient loop), so Amount=0 would
    /// risk a DivideByZeroException in the crafting-grid UI. An empty list just skips those
    /// loops. Why not Harmony patches: the cost surface has three consumers (personal crafting,
    /// bench supply, refund) - data mutation covers all of them with zero hooks.
    ///
    /// Original ingredient lists are saved and restored when the module is toggled off.
    /// In co-op the HOST must run this module (bench crafts are validated server-side).
    /// </summary>
    public class FreeSpellbookCraftModule : ModModuleBase
    {
        public static FreeSpellbookCraftModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Free Spellbook Crafting";
        public override string Description => "Spellbook recipes cost no materials.";

        // Saved original ingredient lists, keyed by recipe, for restore on disable.
        private static readonly Dictionary<Recipe, RecipeList> _originals = new();
        // Memoized "is this result a spellbook" lookups (GetRequiredItems-era UI polls often).
        private static readonly Dictionary<ItemType, bool> _isSpellbook = new();
        private static bool _applied;
        private static string _lastResult = "";

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.FreeSpellbooks";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable Free Spellbook Crafting (Default: true, Vanilla: false)");
        }

        // [2026-10-07] No Harmony patches: costs are removed by rewriting Recipe.Ingredients
        // at apply time, which covers every consumer (craft, bench, refund, UI) at once.
        public override void ApplyPatches(Harmony harmony)
        {
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            GUI.Label(new Rect(x, curY, width, 24f),
                "Spellbook recipes (results that teach a spell) cost no materials while enabled.", labelStyle);
            curY += 28f;

            if (!string.IsNullOrEmpty(_lastResult))
            {
                GUI.Label(new Rect(x, curY, width, 24f), _lastResult, labelStyle);
                curY += 28f;
            }
            return curY - y;
        }

        // Called every frame from ModPackManagerBehaviour.Update. A single bool check until
        // the toggle flips; then applies/restores once. ItemManager may not exist yet at
        // plugin load (its Recipes list is populated in its Awake), so Apply() retries until
        // the singleton appears.
        public static void OnUpdate()
        {
            bool want = Instance != null && Instance.IsEnabled;
            if (want == _applied) return;
            if (want) Apply(); else Restore();
        }

        static void Apply()
        {
            var manager = ItemManager.Singleton;
            if (manager == null) return; // not ready yet; retried next frame

            int count = 0;
            var names = new List<string>();
            foreach (Recipe recipe in manager.Recipes)
            {
                if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Count == 0) continue;
                if (!IsSpellbookRecipe(manager, recipe)) continue;

                _originals[recipe] = recipe.Ingredients;
                recipe.Ingredients = new RecipeList();
                count++;
                if (names.Count < 20) names.Add(recipe.Result.ToString());
            }

            _applied = true;
            _lastResult = $"<color=#55FF55>Applied: {count} spellbook recipes now cost no materials.</color>";
            DimraethModPackPlugin.Log?.LogInfo($"[FreeSpellbooks] Emptied ingredients on {count} spellbook recipes: {string.Join(", ", names)}");
        }

        static void Restore()
        {
            foreach (var kv in _originals)
            {
                // Per-entry: one bad instance (e.g. destroyed meanwhile) must not block the rest.
                try { kv.Key.Ingredients = kv.Value; }
                catch (Exception ex) { DimraethModPackPlugin.Log?.LogWarning($"[FreeSpellbooks] Restore failed: {ex.Message}"); }
            }
            _originals.Clear();
            _applied = false;
            _lastResult = "<color=#AAAAAA>Vanilla material costs restored.</color>";
            DimraethModPackPlugin.Log?.LogInfo("[FreeSpellbooks] Restored original ingredients.");
        }

        static bool IsSpellbookRecipe(ItemManager manager, Recipe recipe)
        {
            if (_isSpellbook.TryGetValue(recipe.Result, out bool cached)) return cached;
            Item item = manager.GetItem(recipe.Result);
            bool isBook = item != null && item.SpellUnlock != Spell.None;
            _isSpellbook[recipe.Result] = isBook;
            return isBook;
        }
    }
}
