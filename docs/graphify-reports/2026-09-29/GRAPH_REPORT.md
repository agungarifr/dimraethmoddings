# Graph Report - BepInExModsSource  (2026-09-29)

## Corpus Check
- 202 files · ~94,569 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 283 file(s) not represented in the graph (top: .cache 119, .dll 67, .pdb 27)

## Summary
- 1771 nodes · 3549 edges · 110 communities (87 shown, 23 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 57 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- HarmonyPatch
- HarmonyPatch
- MinimapBehaviour
- system
- Memory
- NativeBytePatcher
- PerfectParryModule
- DiagnosticsManager
- RuneEditorUI
- global_system
- RuneConfigIO.cs
- HarmonyPostfix
- DayNightToggleModPlugin
- LootPatches.cs
- ChestAndInventoryModule
- .ShouldTriggerParry
- HellModeLevel75Freebuff.cs
- AttackSpeedMod.cs
- .IsTargetPlayer
- EquippedStatModifierUI
- harmonylib
- AutoBackupSaveModule
- EditorEngine
- RuneEntry
- RahanerChestModPlugin
- HellModeModule
- .DrawToggle
- bepinex_bootstrap
- UpgradeBonusStatIsNotRandomModule
- RuneMemory
- Dimraeth - Complete Equipment Upgrade & Enhancement Guide
- EquipmentStatEditor/Core/RuneMemory.cs
- Dimraeth QoL & Progression ModPack
- NativeBytePatcher
- SkillPointMultiplierModule
- AntiCheatBypassModule
- ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.cs
- .IsLocalPlayer
- LevelCapModule
- CarryWeightModule
- ConfigurableLevelCapFreebuff
- ConfigurableLevelCapFreebuff
- Player
- UpgradeBonusStatIsNotRandomPlugin
- UnrandomizerUI
- NoClickPickupModule
- ExpModule
- Patches
- RelationshipModule
- Patches
- ModModuleBase
- LootModV2Module
- DimraethModPackPlugin
- .Prefix
- Patch_PlayerUpdate_Update
- .Load
- NavMeshFixMod.cs
- BasePlugin
- HarmonyPrefix
- EditorController
- ObjectsCommon
- DeedUnlockerMod
- DimraethModPack
- Equipment Stat Editor
- DayNightToggleMod.cs
- UnrandomizerUI.cs
- RuneSnapshot
- .Postfix
- DeedUnlockerPlugin
- EquipmentStatEditorPlugin
- StatCatalog
- .Postfix
- EquippedStatModifierPlugin
- AutoPickupModPlugin
- CarryWeightModPlugin
- Patch_ItemDropCheck
- LootModV2Plugin
- NavMeshFixModPlugin
- .Prefix
- PerfectParryModPlugin
- AlwaysRegenMod.csproj
- AttackSpeedMod.csproj
- AutoPickupMod.csproj
- CarryWeightMod.csproj
- ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.csproj
- ConfigurableLevelCapFreebuff_Debug/ConfigurableLevelCapFreebuff.csproj
- DayNightToggleMod.csproj
- DeedUnlockerMod.csproj
- DimraethModPack.csproj
- EquipmentStatEditor.csproj
- EquippedStatModifier.csproj
- HellModeLevel75Freebuff.csproj
- HellModeMod.csproj
- LootAndExpMod.csproj
- LootModV2.csproj
- NavMeshFixMod.csproj
- PerfectParryMod.csproj
- RahanerChestMod.csproj
- RenosUtilities.csproj
- UpgradeBonusStatIsNotRandom.csproj
- Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)
- Patch_CreateNewStorageData
- RahanerChestMod.cs
- .Prefix_UpdateRelationshipStatus
- .Multiply
- .Prefix
- .Postfix_PlayerUpdate
- .Postfix
- Patch_InteractableReward
- Patch_ItemDropCheck

## God Nodes (most connected - your core abstractions)
1. `ModModuleBase` - 34 edges
2. `MinimapBehaviour` - 29 edges
3. `RuneEditorUI` - 28 edges
4. `EditorEngine` - 27 edges
5. `RuneEntry` - 26 edges
6. `PerfectParryModule` - 24 edges
7. `AutoBackupSaveModule` - 24 edges
8. `Memory` - 22 edges
9. `DimraethModPack.Core` - 21 edges
10. `Draw` - 21 edges

## Surprising Connections (you probably didn't know these)
- `DimraethModPack Integration` --references--> `EquippedStatModifierModule`  [INFERRED]
  EquippedStatModifier/README.md → DimraethModPack/Modules/Stats/EquippedStatModifierModule.cs
- `DimraethModPack (`DimraethModPack.dll`)` --references--> `DimraethModPackPlugin`  [INFERRED]
  DimraethModPack/CHANGELOG.md → DimraethModPack/DimraethModPackPlugin.cs
- `DimraethModPack (`DimraethModPack.dll`)` --references--> `NativeBytePatcher`  [INFERRED]
  DimraethModPack/CHANGELOG.md → DimraethModPack/Modules/System/LevelCapModule.cs
- `Equipment rarity tertinggi (BARU)` --references--> `Patch_MaxEquipmentRarity`  [INFERRED]
  LootModV2/README.md → LootModV2/Patches/LootPatches.cs
- `DimraethModPackPlugin` --references--> `ModModuleBase`  [EXTRACTED]
  DimraethModPack/DimraethModPackPlugin.cs → DimraethModPack/Core/ModModuleBase.cs

## Import Cycles
- None detected.

## Communities (110 total, 23 thin omitted)

### Community 0 - "HarmonyPatch"
Cohesion: 0.06
Nodes (35): ConfigEntry, DateTime, Difficulty, DifficultyManager, DifficultySlider, FinalSelection, HarmonyPatch, HarmonyPostfix (+27 more)

### Community 1 - "HarmonyPatch"
Cohesion: 0.07
Nodes (33): ConfigEntry, DateTime, Difficulty, DifficultyManager, DifficultySlider, FinalSelection, HarmonyPatch, HarmonyPostfix (+25 more)

### Community 2 - "MinimapBehaviour"
Cohesion: 0.06
Nodes (37): Band, Camera, Canvas, CanvasScaler, Build, Config (`BepInEx/config/com.dimraeth.equipmentstateditor.cfg`), Equipment Stat Editor (standalone), How it works (+29 more)

### Community 4 - "Memory"
Cohesion: 0.06
Nodes (36): Color, ConfigEntry, ConfigFile, Dictionary, GUIStyle, Harmony, Il2CppObjectBase, IntPtr (+28 more)

### Community 5 - "NativeBytePatcher"
Cohesion: 0.07
Nodes (27): ConfigurableLevelCapPlugin, Instance, MethodTarget, NativeBytePatcher, AlreadyAtCap, ConfigEntry, DllImport, HashSet (+19 more)

### Community 6 - "PerfectParryModule"
Cohesion: 0.09
Nodes (27): BaseSpell, ConfigEntry, ConfigFile, DamageType, Harmony, ObjectsCommon, ParryPrefab, RetaliationPrefab (+19 more)

### Community 7 - "DiagnosticsManager"
Cohesion: 0.15
Nodes (10): ConfigEntry, DiagnosticsManager, AvgFps, CurrentFps, ManagedRamMb, MaxSpikeDurationMs, OnePercentLowFps, ProcessWorkingSetMb (+2 more)

### Community 8 - "RuneEditorUI"
Cohesion: 0.11
Nodes (13): IntPtr, Stat, StatCapCatalog, Color, GUIStyle, List, Player, Rune (+5 more)

### Community 9 - "global_system"
Cohesion: 0.35
Nodes (7): global_system, global_system_collections_generic, global_system_io, global_system_linq, global_system_net_http, global_system_threading, global_system_threading_tasks

### Community 10 - "RuneConfigIO.cs"
Cohesion: 0.21
Nodes (7): EquippedStatModifier.UI, EquipmentStatEditor.Core, EquippedStatModifier.Core, EquipmentStatEditor.UI, system_globalization, system_io, system_text

### Community 11 - "HarmonyPostfix"
Cohesion: 0.24
Nodes (6): HarmonyPostfix, List, Patch_CalculateGoldDrop, Patch_DropChance, Patch_HarvestBonus, Patch_HarvestCalculate

### Community 12 - "DayNightToggleModPlugin"
Cohesion: 0.08
Nodes (22): Color, ConfigEntry, Key, KeyCode, ManualLogSource, PlayerUpdate, DayNightToggleModPlugin, CurrentMode (+14 more)

### Community 13 - "LootPatches.cs"
Cohesion: 0.20
Nodes (6): LootModV2.Patches, InteractableRewardEntry, Patch_ApplyDropChanceModifiers, Patch_GoldDrop, Patch_HarvestBonusItems, Patch_InteractableReward

### Community 14 - "ChestAndInventoryModule"
Cohesion: 0.10
Nodes (20): Chest, ConfigEntry, ConfigFile, EventTrigger, GridLayoutGroup, Harmony, HarmonyPatch, HarmonyPostfix (+12 more)

### Community 15 - ".ShouldTriggerParry"
Cohesion: 0.11
Nodes (19): BaseSpell, DamageType, ObjectsCommon, ParryPrefab, RetaliationPrefab, Spell, VanishPrefab, Patch_ParryPrefab_HandleLocalIncomingHit (+11 more)

### Community 16 - "HellModeLevel75Freebuff.cs"
Cohesion: 0.07
Nodes (26): Patch_CalculateSprintingCostPerSecond, Patch_SpellLibrarySetRegenTimer, Player, bepinex, bepinex_logging, bepinex_unity_il2cpp, AlwaysRegenMod, PerfectParryMod (+18 more)

### Community 17 - "AttackSpeedMod.cs"
Cohesion: 0.11
Nodes (18): CustomStatModPlugin, Patch_CalculateAttackSpeed, Patch_CalculateBaseAttackSpeed, Patch_CalculateBaseMaxConcentration, Patch_CalculateBaseMaxHealth, Patch_CalculateBaseMaxStamina, Patch_CalculateBaseSpellHaste, Patch_CalculateCriticalRate (+10 more)

### Community 18 - ".IsTargetPlayer"
Cohesion: 0.17
Nodes (15): Attributes, ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon (+7 more)

### Community 19 - "EquippedStatModifierUI"
Cohesion: 0.14
Nodes (11): Rune, EquippedStatModifierBehaviour, Color, GUIStyle, List, Player, Stat, Texture2D (+3 more)

### Community 20 - "harmonylib"
Cohesion: 0.20
Nodes (14): bepinex_configuration, DimraethModPack.Modules.Loot, DimraethModPack.Core, DimraethModPack.UI, DimraethModPack.Modules.SystemMod, DimraethModPack, AutoPickupMod, DimraethModPack.Modules.Gameplay (+6 more)

### Community 21 - "AutoBackupSaveModule"
Cohesion: 0.10
Nodes (14): Action, ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, List (+6 more)

### Community 22 - "EditorEngine"
Cohesion: 0.18
Nodes (10): Dictionary, HashSet, Il2CppObjectBase, IntPtr, List, Player, Type, EditorEngine (+2 more)

### Community 23 - "RuneEntry"
Cohesion: 0.15
Nodes (14): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneConfigIO, RuneEntry (+6 more)

### Community 24 - "RahanerChestModPlugin"
Cohesion: 0.13
Nodes (15): Chest, ConfigEntry, EventTrigger, GridLayoutGroup, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Inventory (+7 more)

### Community 25 - "HellModeModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, MonsterConfiguration, MonsterSetup, HellModeModule (+6 more)

### Community 26 - ".DrawToggle"
Cohesion: 0.16
Nodes (11): DimraethModPack (`DimraethModPack.dll`), Action, ConfigEntry, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle (+3 more)

### Community 28 - "UpgradeBonusStatIsNotRandomModule"
Cohesion: 0.12
Nodes (16): ConfigEntry, ConfigFile, Dictionary, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Rune (+8 more)

### Community 29 - "RuneMemory"
Cohesion: 0.21
Nodes (7): Action, Dictionary, Il2CppObjectBase, IntPtr, Player, Type, RuneMemory

### Community 30 - "Dimraeth - Complete Equipment Upgrade & Enhancement Guide"
Cohesion: 0.09
Nodes (21): 1. Maximum Upgrade Level, 1. The Seed Formula (GetUpgradeSeed), 2. Candidate Filtering (GetRandomSecondaryStat), 2. Primary Stat Scaling (Every Level), A. All Existing Secondary Stats Level Up, B. A New Random Secondary Stat is Rolled, Complete Equipment Stat Affix Catalog (Runes.Stat), Core Offensive & Penetration (+13 more)

### Community 31 - "EquipmentStatEditor/Core/RuneMemory.cs"
Cohesion: 0.19
Nodes (21): BindLogger(), Action, Il2CppObjectBase, IntPtr, Player, Rune, Type, DataPtr() (+13 more)

### Community 32 - "Dimraeth QoL & Progression ModPack"
Cohesion: 0.11
Nodes (16): 1. 🗡️ Stats Category, 2. 🎮 Gameplay Category, 3. 📦 Loot Category, 4. ⚙️ System Category, ✦ Controls & Configuration, Dimraeth QoL & Progression ModPack, ✦ Included Modules & Core Features, ✦ Installation (+8 more)

### Community 33 - "NativeBytePatcher"
Cohesion: 0.17
Nodes (11): DllImport, IntPtr, UIntPtr, GameConfigCapPatcher, NativeBytePatcher, maxAttributeLevel, maxLevel, offset (+3 more)

### Community 34 - "SkillPointMultiplierModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, Player, PlayerStats (+6 more)

### Community 35 - "AntiCheatBypassModule"
Cohesion: 0.12
Nodes (12): ConfigEntry, ConfigFile, GUIStyle, Harmony, List, Type, AntiCheatBypassModule, Category (+4 more)

### Community 36 - "ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.cs"
Cohesion: 0.10
Nodes (16): Patch_ExpMultiplier, Patch_PlayerAwake, Patch_XPGainBlocked, Player, Patch_ExpMultiplier, Patch_PlayerAwake, Patch_XPGainBlocked, Player (+8 more)

### Community 37 - ".IsLocalPlayer"
Cohesion: 0.14
Nodes (15): Il2CppObjectBase, IntPtr, PlayerIdentity, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon, Player (+7 more)

### Community 38 - "LevelCapModule"
Cohesion: 0.12
Nodes (13): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Player, LevelCapModule (+5 more)

### Community 39 - "CarryWeightModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, CarryWeightModule, Category, Description, Instance, Name

### Community 40 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 41 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 42 - "Player"
Cohesion: 0.40
Nodes (3): Player, Patch_AddXPToPet, Patch_GrantXPToPlayer

### Community 43 - "UpgradeBonusStatIsNotRandomPlugin"
Cohesion: 0.15
Nodes (9): ModPackManagerBehaviour, MonoBehaviour, ConfigEntry, Harmony, KeyCode, ManualLogSource, UnrandomizerBehaviour, UpgradeBonusStatIsNotRandomPlugin (+1 more)

### Community 44 - "UnrandomizerUI"
Cohesion: 0.28
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, UnrandomizerUI

### Community 45 - "NoClickPickupModule"
Cohesion: 0.17
Nodes (10): ConfigEntry, ConfigFile, Harmony, KeyCode, Rarity, NoClickPickupModule, Category, Description (+2 more)

### Community 46 - "ExpModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, ExpModule, Category, Description, Instance (+1 more)

### Community 47 - "Patches"
Cohesion: 0.24
Nodes (7): HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Player, Patch_GrantXP, Patches, HarmonyFinalizer

### Community 48 - "RelationshipModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, RelationshipModule, Category, Description, Instance (+1 more)

### Community 49 - "Patches"
Cohesion: 0.16
Nodes (10): Dictionary, ItemType, List, Player, Patches, GoldDropPickup, ItemObject, ItemTooltipCategory (+2 more)

### Community 50 - "ModModuleBase"
Cohesion: 0.18
Nodes (8): ConfigFile, Harmony, ModModuleBase, Category, Description, Enabled, IsEnabled, Name

### Community 51 - "LootModV2Module"
Cohesion: 0.15
Nodes (10): ConfigEntry, ConfigFile, GUIStyle, Harmony, LootModV2Module, Category, Description, Instance (+2 more)

### Community 52 - "DimraethModPackPlugin"
Cohesion: 0.06
Nodes (33): 2026-09-28, 2026-09-28 (20:50), 2026-09-29, 2026-09-29 (09:28), Changelog, Dimraeth ModPack (`DimraethModPack.dll`), DimraethModPack (`DimraethModPack.dll`), DimraethModPack (`DimraethModPack.dll`) (+25 more)

### Community 53 - ".Prefix"
Cohesion: 0.21
Nodes (9): Dictionary, Rune, Stat, StatCatalog, HarmonyPrefix, Rune, Runes, Stat (+1 more)

### Community 54 - "Patch_PlayerUpdate_Update"
Cohesion: 0.33
Nodes (5): Patch_PlayerUpdate_Update, Key, KeyCode, Player, PlayerUpdate

### Community 55 - ".Load"
Cohesion: 0.15
Nodes (9): ConfigFile, ConfigEntry, ConfigFile, Harmony, AlwaysRegenModule, Category, Description, Instance (+1 more)

### Community 56 - "NavMeshFixMod.cs"
Cohesion: 0.22
Nodes (6): NavMeshFixMod, LaughingSlashesPrefab, MonsterMovement, Patch_LaughingSlashesPrefab_CleanupMovementState, Patch_MonsterMovement_Start, unityengine_ai

### Community 57 - "BasePlugin"
Cohesion: 0.22
Nodes (7): AlwaysRegenModPlugin, ConfigEntry, ManualLogSource, BasePlugin, ConfigEntry, ManualLogSource, LootAndExpModPlugin

### Community 58 - "HarmonyPrefix"
Cohesion: 0.20
Nodes (6): HarmonyPrefix, ItemType, Patch_ChestBasic_SpawnStarter, Patch_StorageData_AddItem, StorageData, StorageSpawn

### Community 59 - "EditorController"
Cohesion: 0.25
Nodes (4): Action, Harmony, SaveHooks, EditorController

### Community 60 - "ObjectsCommon"
Cohesion: 0.29
Nodes (4): Patch_CalculateHealthRegeneration, Patch_CalculateStaminaRegeneration, Patch_CommonsZeroPointOneSecond, ObjectsCommon

### Community 61 - "DeedUnlockerMod"
Cohesion: 0.25
Nodes (7): Cara Kerja, Catatan, DeedUnlockerMod, Instalasi, Konfigurasi, Masalah yang Diperbaiki, Verifikasi Log

### Community 62 - "DimraethModPack"
Cohesion: 0.25
Nodes (7): Build, Building from Source, DimraethModPack, Features, License, Modules, Prerequisites

### Community 63 - "Equipment Stat Editor"
Cohesion: 0.25
Nodes (7): Equipment Stat Editor, How values scale, Notes, Per-stat value caps, Stat value caps (the game's real limits), Stats with no roll entry in this build, What it does

### Community 64 - "DayNightToggleMod.cs"
Cohesion: 0.22
Nodes (6): DayNightToggleMod, DayNightLightAdjuster, Patch_DayNightLightAdjuster_Update, Patch_EnvironmentColor_Update, EnvironmentColor, unityengine_rendering_universal

### Community 65 - "UnrandomizerUI.cs"
Cohesion: 0.40
Nodes (3): UpgradeBonusStatIsNotRandom.UI, UpgradeBonusStatIsNotRandom.Patches, UpgradeBonusStatIsNotRandom.Core

### Community 66 - "RuneSnapshot"
Cohesion: 0.29
Nodes (7): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneSnapshot

### Community 67 - ".Postfix"
Cohesion: 0.33
Nodes (4): DeedDefinition, DeedTierConfiguration, Patch_DeedLootCaps, Patch_UnlockedTierForDeed

### Community 68 - "DeedUnlockerPlugin"
Cohesion: 0.33
Nodes (5): ConfigEntry, ManualLogSource, Rarity, Stars, DeedUnlockerPlugin

### Community 69 - "EquipmentStatEditorPlugin"
Cohesion: 0.33
Nodes (6): ConfigEntry, Harmony, KeyCode, ManualLogSource, EquipmentStatEditorPlugin, Instance

### Community 70 - "StatCatalog"
Cohesion: 0.53
Nodes (4): Dictionary, List, Stat, StatCatalog

### Community 71 - ".Postfix"
Cohesion: 0.40
Nodes (4): Patch_CalculateEncumbrance, Attributes, ObjectsCommon, Player

### Community 72 - "EquippedStatModifierPlugin"
Cohesion: 0.40
Nodes (5): ConfigEntry, KeyCode, ManualLogSource, EquippedStatModifierPlugin, Instance

### Community 73 - "AutoPickupModPlugin"
Cohesion: 0.50
Nodes (3): AutoPickupModPlugin, ConfigEntry, ManualLogSource

### Community 74 - "CarryWeightModPlugin"
Cohesion: 0.50
Nodes (3): CarryWeightModPlugin, ConfigEntry, ManualLogSource

### Community 75 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 76 - "LootModV2Plugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootModV2Plugin

### Community 77 - "NavMeshFixModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, NavMeshFixModPlugin

### Community 78 - ".Prefix"
Cohesion: 0.50
Nodes (3): ObjectsCommon, Patch_ObjectsCommon_SafeWarp, Vector3

### Community 79 - "PerfectParryModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, PerfectParryModPlugin

### Community 100 - "Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)"
Cohesion: 0.22
Nodes (8): Build & install, Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`), Interactable di map (BARU) — dead body / box / tekan C, Konfigurasi (`BepInEx/config/com.custom.lootmodv2.cfg`), LootModV2 — Loot 10x (Monster + Node Panen + Interactable), Monster (dipertahankan), Node panen (BARU) — stone, wood/log, rumput, bush, plant, dll., Yang diubah vs LootAndExpMod lama

### Community 101 - "Patch_CreateNewStorageData"
Cohesion: 0.25
Nodes (5): HashSet, IntPtr, MonsterUtils, Patch_CreateNewStorageData, Patch_ItemDropCheck

### Community 102 - "RahanerChestMod.cs"
Cohesion: 0.33
Nodes (4): RahanerChestMod, DimraethMinimap, unityengine_scenemanagement, unityengine_ui

### Community 103 - ".Prefix_UpdateRelationshipStatus"
Cohesion: 0.43
Nodes (4): HarmonyPatch, HarmonyPrefix, Patches, RelationshipData

### Community 104 - ".Multiply"
Cohesion: 0.38
Nodes (4): List, HarvestMultiplication, Mult, Patch_HarvestCalculateItems

### Community 105 - ".Prefix"
Cohesion: 0.33
Nodes (4): Patch_ReturnRandomRuneData, Patch_MaxEquipmentRarity, Equipment rarity tertinggi (BARU), Rarity&gt;

### Community 106 - ".Postfix_PlayerUpdate"
Cohesion: 0.40
Nodes (3): HarmonyPatch, HarmonyPostfix, PlayerUpdate

### Community 107 - ".Postfix"
Cohesion: 0.60
Nodes (3): Patch_DungeonChest_TrySpawn, IntRange, ItemDrop

### Community 109 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

## Knowledge Gaps
- **237 isolated node(s):** `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk`, `AttackSpeedMod`, `net6.0` (+232 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 582 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **23 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModModuleBase` connect `ModModuleBase` to `Memory`, `PerfectParryModule`, `ChestAndInventoryModule`, `.IsTargetPlayer`, `harmonylib`, `AutoBackupSaveModule`, `HellModeModule`, `.DrawToggle`, `UpgradeBonusStatIsNotRandomModule`, `SkillPointMultiplierModule`, `AntiCheatBypassModule`, `LevelCapModule`, `CarryWeightModule`, `NoClickPickupModule`, `ExpModule`, `RelationshipModule`, `LootModV2Module`, `DimraethModPackPlugin`, `.Load`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **Why does `ChestAndInventoryModule` connect `ChestAndInventoryModule` to `ModModuleBase`, `.DrawToggle`, `harmonylib`, `.Load`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Why does `DimraethModPackPlugin` connect `DimraethModPackPlugin` to `BasePlugin`, `ModModuleBase`, `harmonylib`, `.Load`?**
  _High betweenness centrality (0.047) - this node is a cross-community bridge._
- **What connects `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _237 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.06342072409488139 - nodes in this community are weakly interconnected._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.07423423423423424 - nodes in this community are weakly interconnected._
- **Should `MinimapBehaviour` be split into smaller, more focused modules?**
  _Cohesion score 0.05593607305936073 - nodes in this community are weakly interconnected._