using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DimraethModPack.Core;
using DimraethModPack.Modules.Gameplay;
using DimraethModPack.Modules.Loot;
using DimraethModPack.Modules.Pets;
using DimraethModPack.Modules.Stats;
using DimraethModPack.Modules.SystemMod;
using DimraethModPack.UI;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DimraethModPack
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class DimraethModPackPlugin : BasePlugin
    {
        public const string PLUGIN_GUID = "com.custom.dimraethmodpack";
        public const string PLUGIN_NAME = "Dimraeth ModPack";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static new ManualLogSource Log;
        internal static DimraethModPackPlugin Instance;
        private Harmony _harmony;

        public static List<ModModuleBase> Modules { get; private set; }
        public static ModPackUI UI { get; private set; }
        public static ConfigEntry<KeyCode> MenuKey;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            MenuKey = Config.Bind("General", "MenuKey", KeyCode.BackQuote, "Keyboard shortcut to toggle Mod Manager menu. (Default: BackQuote / `)");
            DiagnosticsManager.Init(Config);

            Log.LogInfo("=================================================");
            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} - Loading...");
            Log.LogInfo("=================================================");

            _harmony = new Harmony(PLUGIN_GUID);
            Modules = new List<ModModuleBase>();

            // 1. Stats Category
            Modules.Add(new AlwaysRegenModule());
            Modules.Add(new CustomStatModule());
            Modules.Add(new CarryWeightModule());
            Modules.Add(new UpgradeBonusStatIsNotRandomModule());
            /* [2026-09-26 01:48] Obsolete: EquippedStatModifierModule ("Equipped Stat Modifier") removed from the
               ModPack per user request. Equipment stat editing now lives in the STANDALONE plugin
               BepInEx/plugins/EquipmentStatEditor.dll (source: modding/BepInExModsSource/EquipmentStatEditor),
               which edits rune stats via the config file BepInEx/config/EquipmentStatEditor.<char>.cfg.
               The module's live UI editor also had issues (right-hand edit panel could silently fail after
               GUI.BeginScrollView was stripped from the Il2Cpp build, see its own [2026-09-26 00:54] notes).
               Registration kept here as commented-out code per repo rule; full source preserved in
               Modules/Stats/EquippedStatModifierModule.cs - do not delete.
            Modules.Add(new EquippedStatModifierModule()));
            */

            // 2. Gameplay Category
            Modules.Add(new NoClickPickupModule());
            Modules.Add(new PerfectParryModule());
            // [2026-10-01 15:00] New: force all authored Deed Board tiers (normal + boss/bounty) to
            // report as unlocked, skipping the one-step completion ladder. Host-side postfixes on
            // DeedManager.GetWorldTierCap / IsTierUnlocked / GetUnlockedTierForDeed. Config: Gameplay.DeedProgression.
            Modules.Add(new DeedProgressionModule());
            // [2026-10-07 09:00] New: manual "Refresh Bounty Board" button. Rerolls every Deed & Bounty
            // board on demand via DeedManager.RefreshAllBoardsServerRpc() - the same path the game's own
            // daily OnTimeChanged refresh uses. No Harmony patches, no per-frame work. Config: Gameplay.BountyBoardRefresh.
            Modules.Add(new BountyBoardRefreshModule());
            // [2026-10-04 00:00] RE-ENABLED by user request: "Skill Point Multiplier" (@Gameplay.SkillPointMod)
            // registered again. Safe now because over-cap characters are kept visible by the STANDALONE
            // AntiCheatBypassMod.dll (BepInEx/plugins), which replaced the in-pack AntiCheatBypassModule
            // that was unregistered 2026-09-30. So bonus skill points no longer hide characters from the
            // character-select list.
            // NOTE: cfg [Gameplay.SkillPointMod] SkillPointMultiplier = 1 (vanilla) -> grants nothing until
            // raised above 1 from the Mod Manager UI.
            Modules.Add(new SkillPointMultiplierModule()); // [2026-09-26 14:09] New: x3 skill points per level-up (configurable).
            // [2026-10-06 13:05] Revamped: "Instant Crops" (was "Fast Crops") — no longer a growth-rate
            // multiplier; it is a manual button that calls PlayerBaseManager.AdvanceAllFarmGrowth so
            // there is zero per-frame overhead. Config section kept as Gameplay.FastCrops.
            Modules.Add(new FastCropsModule());
            // [2026-10-06 13:20] Revamped: "Instant Production" (was "Fast Production") — no longer a
            // per-frame CraftingBench.Update postfix; it is a manual button that finishes current crafts
            // through the game's own deposit path. Config section kept as Gameplay.FastProduction.
            // [2026-10-09 09:07] Now covers campfire & woodfire-stove cooking too: the station filter uses
            // the game's own StorageContainerRules.IsCraftingStation instead of a workbench/alchemy-only list.
            Modules.Add(new FastProductionModule());
            // [2026-10-07 10:15] New: "Unlock All (Recipes & Spells)" manual button — unlocks every
            // crafting recipe (recipe.UUID into Player.UnlockedRecipes) and learns every spellbook
            // spell (Item.SpellUnlock into Player.UnlockedSpells).
            // [2026-10-08 12:00] FIX: no longer uses SpellManager.AllSpellsInGame (that included
            // skill-tree + quest spells and broke main-quest progression); spellbooks only now.
            // [2026-10-08 12:30] Added a "Repair Learned Spells" button that strips the tree/quest
            // spell copies the old version leaked into existing saves (backs the list up first).
            // [2026-10-08 13:10] Added an "Unlock All Pets" button (Inventory.AddPetToInventory for
            // all 10 PetType values; persists via PetData). Module renamed to "(Recipes, Spells & Pets)".
            // No Harmony patches, no per-frame work; lists persist via PlayerData on save.
            // Config: Gameplay.UnlockAll.
            Modules.Add(new UnlockAllModule());
            // [2026-10-07 11:30] New: "Free Spellbook Crafting" — empties Recipe.Ingredients on every
            // spellbook recipe (result item has SpellUnlock != None) so spellbooks cost no materials.
            // Applied/restored on toggle via its OnUpdate hook (wired in ModPackManagerBehaviour.Update).
            // Config: Gameplay.FreeSpellbooks.
            Modules.Add(new FreeSpellbookCraftModule());

            /* [2026-09-28 13:59] OBSOLETE (superseded 2026-10-04 00:00, module re-enabled above).
               Kept the original "why disabled" reasoning for history/reference only - do not act on it.
               Why it was obsolete: the module grants bonus skill points via Player.TryGrantBonusSkillPoints
               with source "SkillPointMultiplierMod_L{level}" (recorded in the save in
               characterBonusSkillPointSources). At 5x that is +8 per level; across a run this pushed
               the total skill points SPENT in the tree past the game's plausibility ceiling of 84 in
               CharacterPlausibility.ExceedsPlausibleCaps, so CharacterPlausibility.FilterImplausible
               hid the affected characters (Meng spent 87, Yoink 119) from the character-select list.
               Those bonus points are permanent in existing saves, so visibility is restored by the new
               AntiCheatBypassModule; removing this registration only stops NEW points from exceeding
               vanilla. The cfg multiplier is also left at 1 (vanilla).
               Source kept in Modules/Gameplay/SkillPointMultiplierModule.cs - do not delete.
            Modules.Add(new SkillPointMultiplierModule()); // [2026-09-26 14:09] New: x3 skill points per level-up (configurable).
            */

            // 3. Loot Category
            Modules.Add(new LootModV2Module());
            Modules.Add(new ChestAndInventoryModule());
            // [2026-10-01 14:00] New: pet-cargo diagnostics (all-item-types investigation).
            // Logs which pet transfer/deposit path is taken and how each entry is classified,
            // so the real 'gear only' restriction can be located from an in-game reproduction.
            // Listed under "Loot" because ModPackUI's tab set is fixed to Stats/Gameplay/Loot/System.
            Modules.Add(new PetCargoModule());

            // 4. System Category
            /* [2026-09-28 20:50] RESTORED: LevelCapModule ("Configurable Level Cap") re-registered.
               It was registered here on/before 2026-09-25 (see modding/Backup_2026-09-25_09-15_fix_elf_move_crash_untested)
               but vanished from both source and compiled DLL without an Obsolete note -- most likely lost
               during the 2026-09-25 backup/restore cycle for the elf_error_move_random_crash fix. The stale
               [System.LevelCap] cfg section (Enabled=true, MaxLevelCap=45, MaxAttributeCap=75) kept binding to
               nothing while it was missing. Source restored verbatim from that backup into
               Modules/System/LevelCapModule.cs (the broken standalone customlevelcap.dll was NOT used as
               reference, per user instruction). */
            Modules.Add(new LevelCapModule()); // [2026-09-28 20:50] Restored: configurable level/attribute caps (System.LevelCap).
            Modules.Add(new HellModeModule());
            Modules.Add(new ExpModule());
            Modules.Add(new RelationshipModule());
            Modules.Add(new WeatherControlModule()); // [2026-10-01 00:00] New: select + apply any weather for the local player's kingdom.
            // [2026-10-05] New: photosensitivity recon. Read-only scan that inventories particle systems,
            // lights and emission colors to BepInEx\VfxFlashDump.txt so the flashy spell/monster-attack
            // VFX (Minotaur yellow hit first) can be precisely identified before clamping. Off by default.
            Modules.Add(new VfxFlashDiagnosticModule());
            // [2026-10-05] New: Tier-1 photosensitivity mitigation. Disables URP post-processing (bloom)
            // on the main camera so bright effects stop blooming across the screen. Off by default.
            Modules.Add(new VisualComfortModule());
            Modules.Add(new TimeSkipModule()); // [2026-10-01 00:00] New: instantly skip the world clock to a preset hour or forward by N hours.
            Modules.Add(new AutoBackupSaveModule()); // [2026-09-27 00:00] New: timestamped save backups on game quit.
            // [2026-09-30 02:47] Removed: Anti-Cheat Bypass moved out of the modpack into the
            // standalone AntiCheatBypassMod, which patches CharacterPlausibility (FilterImplausible
            // / IsImplausible / RejectsJoiningCharacter) unconditionally plus the rest of the
            // anti-cheat surface. Registration kept per repo rule; module source preserved in
            // Modules/System/AntiCheatBypassModule.cs - do not delete.
            // Modules.Add(new AntiCheatBypassModule()); // [2026-09-28 14:20] New: stop the game hiding over-cap/edited characters from the character list.

            // Bind single config file
            foreach (var mod in Modules)
            {
                try
                {
                    mod.BindConfig(Config);
                    Log.LogInfo($"[{mod.Category}] {mod.Name} config bound.");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Error binding config for {mod.Name}: {ex}");
                }
            }

            // Apply Harmony patches for each module
            foreach (var mod in Modules)
            {
                try
                {
                    mod.ApplyPatches(_harmony);
                    Log.LogInfo($"[{mod.Category}] {mod.Name} patches applied.");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Error applying patches for {mod.Name}: {ex}");
                }
            }

            // Setup UI
            UI = new ModPackUI(Modules);

            ClassInjector.RegisterTypeInIl2Cpp<ModPackManagerBehaviour>();
            var uiGo = new GameObject("DimraethModPackManager");
            uiGo.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(uiGo);
            uiGo.AddComponent<ModPackManagerBehaviour>();

            Log.LogInfo("=================================================");
            Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} - Ready!");
            Log.LogInfo($"Total Modules: {Modules.Count} across 4 categories (Stats, Gameplay, Loot, System)");
            Log.LogInfo("Press ` (BackQuote) in-game to open Mod Manager");
            Log.LogInfo("=================================================");
        }
    }

    public class ModPackManagerBehaviour : MonoBehaviour
    {
        public ModPackManagerBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            try
            {
                KeyCode toggleKey = DimraethModPackPlugin.MenuKey != null ? DimraethModPackPlugin.MenuKey.Value : KeyCode.BackQuote;
                if (Input.GetKeyDown(toggleKey))
                {
                    DimraethModPackPlugin.UI?.Toggle();
                }
                else if (DimraethModPackPlugin.UI != null && DimraethModPackPlugin.UI.Visible && Input.GetKeyDown(KeyCode.Escape))
                {
                    DimraethModPackPlugin.UI.Toggle();
                }

                DiagnosticsManager.OnUpdate();
                /* [2026-10-06 13:05] Obsolete: Fast Crops is no longer a continuous growth-rate modifier,
                   so its per-frame OnUpdate hook is gone. The revamped module ("Instant Crops") instead
                   exposes a manual button that calls PlayerBaseManager.AdvanceAllFarmGrowth once, which
                   is why nothing needs to run here each frame. Kept commented per repo rule.
                // [2026-10-06 12:35] Fast Crops: one-time rescale of already-loaded plots when the
                // module is enabled mid-session (newly planted plots are handled by its own patch).
                FastCropsModule.OnUpdate();
                */
                /* [2026-10-06 13:35] Superseded: the drain was originally a fixed ~10-frame window, which was
                   too short for large stacks (e.g. a queued "water x99" is one BenchQueueItem with
                   Quantity=99 that the game crafts one batch per tick). The drain now runs until every target
                   bench is idle (with a safety cap), so one click finishes any stack size. Original wording
                   kept commented per repo rule.
                // [2026-10-06 13:20] Instant Production: dormant unless a button press armed its short
                // drain window (~10 frames), during which it finishes queued crafts via the game's own
                // deposit path. The rest of the time this is a single bool check.
                */
                // [2026-10-06 13:35] Instant Production: dormant unless a button press armed the drain,
                // during which it finishes queued crafts one batch per tick via the game's own deposit path
                // until all target benches are idle. The rest of the time this is a single bool check.
                FastProductionModule.OnUpdate();
                // [2026-10-07 11:30] Free Spellbook Crafting: applies/restores the empty-ingredients
                // state when the toggle flips (and waits for ItemManager at startup). One bool check
                // the rest of the time.
                FreeSpellbookCraftModule.OnUpdate();
                /* [2026-09-26 01:48] Obsolete: module unregistered (see Modules list in DimraethModPackPlugin.Load).
                   Feature moved to standalone EquipmentStatEditor plugin; kept commented for reference.
                EquippedStatModifierModule.OnUpdate();
                */
            }
            catch { }
        }

        private void OnGUI()
        {
            try
            {
                DimraethModPackPlugin.UI?.Draw();
                /* [2026-09-26 01:48] Obsolete: module unregistered (see Modules list in DimraethModPackPlugin.Load).
                   Feature moved to standalone EquipmentStatEditor plugin; kept commented for reference.
                EquippedStatModifierModule.OnGUI();
                */
            }
            catch { }
        }
    }
}
