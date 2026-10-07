using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Gameplay
{
    /// <summary>
    /// [2026-10-07] "Unlock All" - one button that unlocks every crafting recipe and
    /// learns every spell (i.e. the effect of all spellbooks) for the loaded character.
    ///
    /// How the game stores unlocks (decoded from the IL2CPP dumps):
    /// - Player.UnlockedRecipes : List&lt;String&gt; holds recipe UUID strings
    ///   (Inventory.UnlockRecipeByName adds recipe.UUID after ItemManager.GetRecipeByString).
    /// - Player.UnlockedSpells : List&lt;Spell&gt; holds learned spells; spellbook items teach
    ///   spells through Item.SpellUnlock.
    /// Both lists persist into PlayerData.characterUnlockedRecipes / characterUnlockedSpells
    /// on save, so the unlocks stick to the character.
    ///
    /// No Harmony patches - this module is a manual button, like TimeSkipModule /
    /// FastCropsModule. The lists are mutated directly (like CheatMenu's UnlockSpells)
    /// instead of calling Inventory.UnlockRecipeByName per recipe, which would fire a
    /// "Recipe Unlocked" toast + sound for each of ~380 recipes.
    /// </summary>
    public class UnlockAllModule : ModModuleBase
    {
        public static UnlockAllModule Instance { get; private set; }

        public override string Category => "Gameplay";
        public override string Name => "Unlock All (Recipes & Spells)";
        public override string Description => "One button: unlock every crafting recipe and learn every spell.";

        // [2026-10-07 10:20] NOTE: `Enabled` lives on ModModuleBase (protected set) — assign the
        // base property in BindConfig instead of declaring a shadowing field (was CS0108).
        // public ConfigEntry<bool> Enabled;

        private string _status;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Gameplay.UnlockAll";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable Unlock All Module (Default: true, Vanilla: false)");
        }

        // [2026-10-07] No Harmony patches required; unlocks are applied on-demand from the UI.
        public override void ApplyPatches(Harmony harmony)
        {
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            GUI.Label(new Rect(x, curY, width, 24f),
                "One button: unlock every crafting recipe and learn every spell (all spellbooks).", labelStyle);
            curY += 28f;

            if (GUI.Button(new Rect(x, curY, Math.Min(320f, width), 30f), "Unlock All Recipes & Spellbooks", btnStyle))
            {
                if (!IsEnabled)
                {
                    _status = "<color=#FF6666>Enable this module first.</color>";
                }
                else
                {
                    UnlockAll(out _status);
                }
            }
            curY += 36f;

            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x, curY, width, 24f), _status, labelStyle);
                curY += 28f;
            }
            return curY - y;
        }

        /// <summary>
        /// [2026-10-07] Adds every recipe UUID and every spell to the local player's
        /// unlocked lists, then notifies the crafting/codex UIs via the game's own
        /// Inventory.NotifyRecipeUnlocked event. Idempotent: already-unlocked entries
        /// are skipped, so repeated clicks just report 0 new unlocks.
        /// </summary>
        public static bool UnlockAll(out string message)
        {
            message = "";
            try
            {
                Player player = ResolveLocalPlayer();
                if (player == null)
                {
                    message = "<color=#FFAA55>No local player in a loaded world.</color>";
                    return false;
                }

                int recipesUnlocked = 0;
                int spellsUnlocked = 0;

                // --- Recipes: player.UnlockedRecipes stores recipe.UUID strings. ---
                var recipes = ItemManager.Singleton != null ? ItemManager.Singleton.Recipes : null;
                var unlockedRecipes = player.UnlockedRecipes;
                if (recipes != null && unlockedRecipes != null)
                {
                    foreach (Recipe recipe in recipes)
                    {
                        if (recipe == null || string.IsNullOrEmpty(recipe.UUID)) continue;
                        if (unlockedRecipes.Contains(recipe.UUID)) continue;
                        unlockedRecipes.Add(recipe.UUID);
                        recipesUnlocked++;
                    }
                }

                // --- Spells: every spell a spellbook could teach. ---
                var spellManager = SpellManager.Singleton;
                var unlockedSpells = player.UnlockedSpells;
                if (spellManager != null && spellManager.AllSpellsInGame != null && unlockedSpells != null)
                {
                    foreach (Spell spell in spellManager.AllSpellsInGame)
                    {
                        if (spell == Spell.None || unlockedSpells.Contains(spell)) continue;
                        unlockedSpells.Add(spell);
                        spellsUnlocked++;
                    }
                }

                // Refresh crafting UI / codex through the game's own unlock event.
                try
                {
                    var inventory = DataStorage.Singleton != null ? DataStorage.Singleton.Inventory : null;
                    inventory?.NotifyRecipeUnlocked();
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogWarning($"[UnlockAll] NotifyRecipeUnlocked failed: {ex.Message}");
                }

                message = $"<color=#55FF55>Unlocked {recipesUnlocked} recipes and {spellsUnlocked} spells " +
                          $"({unlockedRecipes?.Count ?? 0} recipes / {unlockedSpells?.Count ?? 0} spells total).</color>";
                DimraethModPackPlugin.Log?.LogInfo($"[UnlockAll] +{recipesUnlocked} recipes, +{spellsUnlocked} spells.");
                return true;
            }
            catch (Exception ex)
            {
                message = $"<color=#FF5555>Unlock failed: {ex.Message}</color>";
                DimraethModPackPlugin.Log?.LogWarning($"[UnlockAll] failed: {ex.Message}");
                return false;
            }
        }

        private static Player ResolveLocalPlayer()
        {
            try
            {
                if (DataStorage.Singleton != null && DataStorage.Singleton.Player != null)
                    return DataStorage.Singleton.Player;
            }
            catch { }

            return null;
        }
    }
}
