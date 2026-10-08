using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
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
    /// [2026-10-08 12:00] FIX: spells are learned ONLY from items that teach one
    /// (Item.SpellUnlock != Spell.None), NOT from SpellManager.AllSpellsInGame. The latter is
    /// the full sprite/prefab catalog and includes skill-tree and quest/story spells; dumping
    /// those into Player.UnlockedSpells is what caused the reported main-quest bug (see the
    /// inline comment in UnlockAll()).
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

            // [2026-10-08 12:30] Repair for saves damaged by the OLD Unlock All (which dumped the
            // whole spell catalog, including skill-tree + quest spells, into Player.UnlockedSpells).
            // Backs the list up first, then removes the leaked tree/quest copies so the game's own
            // sources can re-grant them. Spellbook-taught spells are never removed.
            GUI.Label(new Rect(x, curY, width, 24f),
                "Old Unlock All leaked skill-tree/quest spells into this character. Repair removes them.", labelStyle);
            curY += 28f;
            if (GUI.Button(new Rect(x, curY, Math.Min(320f, width), 30f), "Repair Learned Spells", btnStyle))
            {
                if (!IsEnabled)
                {
                    _status = "<color=#FF6666>Enable this module first.</color>";
                }
                else
                {
                    RepairLearnedSpells(out _status);
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
                var itemManager = ItemManager.Singleton;
                var recipes = itemManager != null ? itemManager.Recipes : null;
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

                // [2026-10-08 12:00] FIX (main-quest bug): only learn spells that a SPELLBOOK actually
                // teaches, i.e. Item.SpellUnlock != Spell.None. The previous implementation iterated
                // SpellManager.AllSpellsInGame, which is the FULL sprite/prefab catalog and therefore
                // also contained skill-tree spells (SkillTreeNode.UnlockSpell), quest/story reward
                // spells (NPCEventDefinition.SpellToGrantToPlayer), monster/NPC-only abilities, and
                // consumable "spells" (flasks/potions). Dumping all of those into Player.UnlockedSpells
                // is wrong: skill-tree spells belong in SkillTree._unlockedSpells, not the player list
                // (the game itself calls the player-side copy a "leak" and strips it on respec/load),
                // and an already-present spell makes ServerRPC.UnlockSpellClientRpc short-circuit, so a
                // later legitimate story/quest grant never fires its unlock notification. The quest-side
                // symptom (a main quest that gates on the player *learning* a spell) is evaluated from
                // quest/dialogue assets, which the decompile can't read, but pre-learning the spell is
                // the observable cause. Spellbooks are the only intended source for this button, so we
                // enumerate ItemManager.Items and take each item's SpellUnlock.
                // Obsolete code preserved per repo rule:
                // // --- Spells: every spell a spellbook could teach. ---
                // var spellManager = SpellManager.Singleton;
                // var unlockedSpells = player.UnlockedSpells;
                // if (spellManager != null && spellManager.AllSpellsInGame != null && unlockedSpells != null)
                // {
                //     foreach (Spell spell in spellManager.AllSpellsInGame)
                //     {
                //         if (spell == Spell.None || unlockedSpells.Contains(spell)) continue;
                //         unlockedSpells.Add(spell);
                //         spellsUnlocked++;
                //     }
                // }
                var unlockedSpells = player.UnlockedSpells;
                if (itemManager != null && itemManager.Items != null && unlockedSpells != null)
                {
                    foreach (Item item in itemManager.Items)
                    {
                        if (item == null) continue;
                        Spell teaches = item.SpellUnlock;
                        if (teaches == Spell.None || unlockedSpells.Contains(teaches)) continue;
                        unlockedSpells.Add(teaches);
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

        /// <summary>
        /// [2026-10-08 12:30] Repairs a character damaged by the OLD Unlock All. That version dumped
        /// every entry of <c>SpellManager.AllSpellsInGame</c> (the full sprite/prefab catalog) into
        /// <c>Player.UnlockedSpells</c>, which wrongly included skill-tree spells
        /// (<c>SkillTreeNode.UnlockSpell</c>/<c>AssociatedSpell</c>) and NPC/quest gift spells
        /// (<c>NPCEventDefinition.SpellToGrantToPlayer</c>). Those belong to their own sources, and an
        /// already-present spell makes the game's <c>UnlockSpellClientRpc</c> short-circuit, which is
        /// what broke main-quest progression.
        ///
        /// This method:
        ///   1. Backs up the current spell list to <c>BepInEx/config/DimraethModPack/</c> (reversible).
        ///   2. Builds the "spellbook spells" set from <c>Item.SpellUnlock</c> - never removed.
        ///   3. Builds the "leaked" set from all loaded <c>SkillTreeNode</c> and <c>NPCEventDefinition</c>
        ///      assets (Resources.FindObjectsOfTypeAll).
        ///   4. Removes any leaked spell that is NOT spellbook-taught.
        /// Starting, structural and spellbook-taught spells are all preserved. The game re-grants tree
        /// spells on load and quest spells when their event fires again.
        /// </summary>
        public static bool RepairLearnedSpells(out string message)
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

                var unlockedSpells = player.UnlockedSpells;
                if (unlockedSpells == null)
                {
                    message = "<color=#FF5555>No spell list on the player.</color>";
                    return false;
                }

                // 1) Back up first - the repair is destructive but reversible from this file.
                string backupPath = WriteSpellBackup(player, unlockedSpells);

                // 2) Spells taught by a spellbook item: always legitimate, never remove.
                var spellbookSpells = new HashSet<Spell>();
                var itemManager = ItemManager.Singleton;
                if (itemManager != null && itemManager.Items != null)
                {
                    foreach (Item item in itemManager.Items)
                    {
                        if (item == null) continue;
                        if (item.SpellUnlock != Spell.None) spellbookSpells.Add(item.SpellUnlock);
                    }
                }

                // 3) The two sources the old Unlock All wrongly copied from.
                var leaked = new HashSet<Spell>();
                foreach (SkillTreeNode node in Resources.FindObjectsOfTypeAll<SkillTreeNode>())
                {
                    if (node == null) continue;
                    if (node.UnlockSpell != Spell.None) leaked.Add(node.UnlockSpell);
                    if (node.AssociatedSpell != Spell.None) leaked.Add(node.AssociatedSpell);
                }
                foreach (NPCEventDefinition ev in Resources.FindObjectsOfTypeAll<NPCEventDefinition>())
                {
                    if (ev == null) continue;
                    if (ev.SpellToGrantToPlayer != Spell.None) leaked.Add(ev.SpellToGrantToPlayer);
                }

                // 4) Remove leaked, non-spellbook spells (snapshot first, then mutate).
                var toRemove = new List<Spell>();
                for (int i = 0; i < unlockedSpells.Count; i++)
                {
                    Spell s = unlockedSpells[i];
                    if (s == Spell.None) continue;
                    if (!leaked.Contains(s)) continue;
                    if (spellbookSpells.Contains(s)) continue; // legit spellbook spell - keep
                    toRemove.Add(s);
                }

                var removedNames = new List<string>();
                foreach (Spell s in toRemove)
                {
                    try
                    {
                        unlockedSpells.Remove(s);
                        removedNames.Add(s.ToString());
                    }
                    catch (Exception ex)
                    {
                        DimraethModPackPlugin.Log?.LogWarning($"[UnlockAll] Repair could not remove {s}: {ex.Message}");
                    }
                }

                message = $"<color=#55FF55>Repair: removed {removedNames.Count} leaked tree/quest spell(s); " +
                          $"{unlockedSpells.Count} remain. Backup saved.</color>";
                DimraethModPackPlugin.Log?.LogInfo(
                    $"[UnlockAll] Repair removed {removedNames.Count} spell(s): {string.Join(", ", removedNames)} | backup={backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                message = $"<color=#FF5555>Repair failed: {ex.Message}</color>";
                DimraethModPackPlugin.Log?.LogWarning($"[UnlockAll] Repair failed: {ex.Message}");
                return false;
            }
        }

        // [2026-10-08 12:30] Writes the current unlocked-spell list to a timestamped file so the
        // repair is reversible. Never throws: a backup failure must not block the repair.
        private static string WriteSpellBackup(Player player, Il2CppSystem.Collections.Generic.List<Spell> spells)
        {
            try
            {
                string dir = Path.Combine(Paths.ConfigPath, "DimraethModPack");
                Directory.CreateDirectory(dir);

                string who = "player";
                try { who = player.Hash.Value.ToString(); } catch { }

                string path = Path.Combine(dir, $"UnlockedSpells-{who}-{DateTime.Now:yyyyMMdd-HHmmss}.backup");
                var sb = new StringBuilder();
                for (int i = 0; i < spells.Count; i++) sb.AppendLine(spells[i].ToString());
                File.WriteAllText(path, sb.ToString());
                return path;
            }
            catch (Exception ex)
            {
                DimraethModPackPlugin.Log?.LogWarning($"[UnlockAll] Spell backup failed: {ex.Message}");
                return "(backup failed)";
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
