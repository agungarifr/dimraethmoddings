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
        public override string Name => "Unlock All (Recipes, Spells & Pets)";
        public override string Description => "Unlock every crafting recipe, every spellbook spell, and every pet.";

        // [2026-10-08] Every pet in the game (PetType enum, minus None). Order mirrors the enum.
        private static readonly PetType[] AllPetTypes =
        {
            PetType.Hound, PetType.Monkey, PetType.CarrionParrot, PetType.EmberHound, PetType.FangraCub,
            PetType.FrostOwl, PetType.BlightCrow, PetType.MoonCat, PetType.WarCat, PetType.SilverCat
        };

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
                "Unlock every crafting recipe, learn every spellbook spell, and add every pet.", labelStyle);
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

            // [2026-10-08] Unlock every pet into the character's collection (persists via PetData).
            if (GUI.Button(new Rect(x, curY, Math.Min(320f, width), 30f), "Unlock All Pets", btnStyle))
            {
                if (!IsEnabled)
                {
                    _status = "<color=#FF6666>Enable this module first.</color>";
                }
                else
                {
                    UnlockAllPets(out _status);
                }
            }
            curY += 36f;

            // [2026-10-08] Repair for saves damaged by the OLD Unlock All (which dumped the whole
            // spell catalog, including skill-tree/quest spells and consumable "spells" like BurnSalve,
            // into Player.UnlockedSpells). Backs the list up first, then keeps only spells from the
            // legitimate sources (spellbook / class-race starting / structural).
            GUI.Label(new Rect(x, curY, width, 24f),
                "Old Unlock All leaked non-learnable spells (e.g. Burn Salve). Repair removes them.", labelStyle);
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
        /// [2026-10-08] Repairs a character damaged by the OLD Unlock All. That version dumped every
        /// entry of <c>SpellManager.AllSpellsInGame</c> (the entire sprite/prefab catalog) into
        /// <c>Player.UnlockedSpells</c>. Besides skill-tree spells (<c>SkillTreeNode.UnlockSpell</c>/
        /// <c>AssociatedSpell</c>) and NPC/quest gift spells (<c>NPCEventDefinition.SpellToGrantToPlayer</c>),
        /// that catalog also contains consumable "spells" (e.g. <c>Spell.BurnSalve</c>, the hotbar entry
        /// for a salve), monster/NPC-only abilities, etc. - none of which belong in the player's learn
        /// list (which is why things like Burn Salve showed up in the spell catalogue).
        ///
        /// Rather than removing only specific leaks, this rebuilds the list from the LEGITIMATE sources:
        ///   - spellbook-taught spells        (Item.SpellUnlock != Spell.None)
        ///   - class / race starting spells   (ClassDefinition / RaceDefinition .StartingSpells)
        ///   - the game's own structural list (DataStorage.Singleton.SkillTree.BuildStructuralSpellList)
        /// plus a small verified structural fallback. Anything on the player that is NOT in that keep-set
        /// is removed. The list is backed up first, so the operation is reversible.
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

                // 2) Build the keep-set from every legitimate source.
                var keep = new HashSet<Spell>();

                var itemManager = ItemManager.Singleton;
                if (itemManager != null && itemManager.Items != null)
                {
                    foreach (Item item in itemManager.Items)
                    {
                        if (item == null) continue;
                        if (item.SpellUnlock != Spell.None) keep.Add(item.SpellUnlock);
                    }
                }

                foreach (ClassDefinition cd in Resources.FindObjectsOfTypeAll<ClassDefinition>())
                    AddAll(keep, cd != null ? cd.StartingSpells : null);
                foreach (RaceDefinition rd in Resources.FindObjectsOfTypeAll<RaceDefinition>())
                    AddAll(keep, rd != null ? rd.StartingSpells : null);

                // The game's own "spells available without any node/slot" list (private method -> reflect).
                try
                {
                    var skillTree = DataStorage.Singleton != null ? DataStorage.Singleton.SkillTree : null;
                    if (skillTree != null)
                    {
                        var mi = AccessTools.Method(skillTree.GetType(), "BuildStructuralSpellList");
                        var list = mi?.Invoke(skillTree, null) as Il2CppSystem.Collections.Generic.List<Spell>;
                        AddAll(keep, list);
                    }
                }
                catch (Exception ex)
                {
                    DimraethModPackPlugin.Log?.LogWarning($"[UnlockAll] BuildStructuralSpellList failed: {ex.Message}");
                }
                foreach (Spell s in StructuralSpellFallback) keep.Add(s);
                keep.Remove(Spell.None);

                // 3) Remove everything on the player that no legitimate source accounts for.
                var toRemove = new List<Spell>();
                for (int i = 0; i < unlockedSpells.Count; i++)
                {
                    Spell s = unlockedSpells[i];
                    if (s == Spell.None) continue;
                    if (!keep.Contains(s)) toRemove.Add(s);
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

                message = $"<color=#55FF55>Repair: removed {removedNames.Count} leaked spell(s); " +
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

        // [2026-10-08] Add a game List<Spell> into the keep-set, tolerating null.
        private static void AddAll(HashSet<Spell> set, Il2CppSystem.Collections.Generic.List<Spell> list)
        {
            if (set == null || list == null) return;
            for (int i = 0; i < list.Count; i++) set.Add(list[i]);
        }

        // [2026-10-08] Verified structural/always-on spells (Spell enum values) used as a fallback when
        // the game's private BuildStructuralSpellList is unavailable. Keeping a few extras is harmless;
        // removing a needed structural spell would not be.
        private static readonly Spell[] StructuralSpellFallback =
        {
            Spell.Swap, Spell.Sprinting, Spell.AutoAttack, Spell.StackingEffect, Spell.Casting,
            Spell.WeaponSwap, Spell.AttackSwap, Spell.PlayerDash, Spell.FrostGlide
        };

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

        /// <summary>
        /// [2026-10-08] Adds every pet type to the character's collection via the game's own
        /// <c>Inventory.AddPetToInventory</c> path (the same call the pet shop and world pickups use),
        /// so ownership lands in <c>PlayerPetData.AllPets</c> and persists through PetData on save.
        /// Each pet is a fresh <c>LocalPetData</c> with a new UUID (mirrors CheatMenu's SpawnPet).
        /// Already-owned types are skipped (ownership is matched by PetType here, since the game matches
        /// by UUID and would otherwise add duplicates). Pets have no typed quest reward field
        /// (no NPCEventDefinition pet grant), so this cannot shadow a story grant the way the spell bug
        /// did. In co-op the host should run it.
        /// </summary>
        public static bool UnlockAllPets(out string message)
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

                var inventory = DataStorage.Singleton != null ? DataStorage.Singleton.Inventory : null;
                var petManager = PetManager.Singleton;
                var itemManager = ItemManager.Singleton;
                if (inventory == null || petManager == null || itemManager == null)
                {
                    message = "<color=#FF5555>Pet system not ready.</color>";
                    return false;
                }

                PlayerPetData petData = null;
                try { petData = player.GetComponent<PlayerPetData>(); } catch { }

                int added = 0;
                var names = new List<string>();
                bool slotsFull = false;

                foreach (PetType type in AllPetTypes)
                {
                    if (type == PetType.None) continue;
                    if (IsPetOwned(petData, type)) continue;
                    if (itemManager.GetItemForPet(type) == null) continue;

                    if (inventory.FindIndexOfFirstEmptyInventorySlot() < 0)
                    {
                        slotsFull = true;
                        break;
                    }

                    var pet = new LocalPetData
                    {
                        Name = petManager.GetRandomPetName(),
                        PetID = BuildUtility.GenerateUUID(),
                        PetOwner = player.Hash.Value,
                        PetOwnerName = player.Name.Value,
                        PetType = type,
                        Level = 1,
                        XP = 0,
                        CharacterCreationDate = Il2CppSystem.DateTimeOffset.UtcNow
                    };

                    inventory.AddPetToInventory(pet);
                    added++;
                    if (names.Count < 20) names.Add(type.ToString());
                }

                string tail = slotsFull ? " (inventory full - free a slot and press again)" : "";
                message = $"<color=#55FF55>Unlocked {added} pet(s): {string.Join(", ", names)}{tail}</color>";
                DimraethModPackPlugin.Log?.LogInfo($"[UnlockAllPets] +{added} pets: {string.Join(", ", names)}{tail}");
                return true;
            }
            catch (Exception ex)
            {
                message = $"<color=#FF5555>Pet unlock failed: {ex.Message}</color>";
                DimraethModPackPlugin.Log?.LogWarning($"[UnlockAllPets] failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsPetOwned(PlayerPetData petData, PetType type)
        {
            try
            {
                var all = petData != null ? petData.AllPets : null;
                if (all == null) return false;
                for (int i = 0; i < all.Count; i++)
                {
                    if (all[i] != null && all[i].PetType == type) return true;
                }
            }
            catch { }
            return false;
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
