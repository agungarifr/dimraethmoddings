# Graph Report - BepInExModsSource  (2026-09-27)

## Corpus Check
- 198 files · ~88,502 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 283 file(s) not represented in the graph (top: .cache 119, .dll 67, .pdb 27)

## Summary
- 1696 nodes · 3418 edges · 100 communities (75 shown, 25 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 50 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- HarmonyPatch
- HarmonyPatch
- MinimapBehaviour
- system
- Memory
- PerfectParryModule
- NativeBytePatcher
- RuneEditorUI
- HarmonyPostfix
- global_system
- system_collections_generic
- DayNightToggleModPlugin
- LootPatches.cs
- ChestAndInventoryModule
- .ShouldTriggerParry
- AttackSpeedMod.cs
- HellModeLevel75Freebuff.cs
- .IsTargetPlayer
- RuneEntry
- EquippedStatModifierUI
- harmonylib
- RahanerChestModPlugin
- RuneMemory
- HellModeModule
- Patches
- UpgradeBonusStatIsNotRandomModule
- AutoBackupSaveModule
- bepinex_bootstrap
- .DrawToggle
- Dimraeth - Complete Equipment Upgrade & Enhancement Guide
- EquipmentStatEditor/Core/RuneMemory.cs
- DiagnosticsManager
- Dimraeth QoL & Progression ModPack
- NativeBytePatcher
- DimraethModPackPlugin
- SkillPointMultiplierModule
- EditorEngine
- CarryWeightModule
- ConfigurableLevelCapFreebuff
- ConfigurableLevelCapFreebuff
- EquipmentStatEditorPlugin
- LootAndExpMod.cs
- Patches
- UnrandomizerUI
- NoClickPickupModule
- ExpModule
- Patches
- RelationshipModule
- .Prefix
- ModModuleBase
- LootModV2Module
- Patch_PlayerUpdate_Update
- AlwaysRegenModule
- UpgradeBonusStatIsNotRandomPlugin
- BasePlugin
- DayNightToggleMod.cs
- NavMeshFixMod.cs
- ObjectsCommon
- DeedUnlockerMod
- DimraethModPack
- Equipment Stat Editor
- RuneSnapshot
- StatCatalog
- UnrandomizerUI.cs
- .Postfix
- DeedUnlockerPlugin
- .Postfix
- Patch_PlayerAwake
- Patch_PlayerAwake
- EquippedStatModifierPlugin
- AutoPickupModPlugin
- unityengine_ui
- LootAndExpModPlugin
- Patch_ItemDropCheck
- LootModV2Plugin
- NavMeshFixModPlugin
- .Prefix
- PerfectParryModPlugin
- Patch_CalculateSprintingCostPerSecond
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
- .ValidateLiveEdit

## God Nodes (most connected - your core abstractions)
1. `ModModuleBase` - 32 edges
2. `MinimapBehaviour` - 29 edges
3. `RuneEditorUI` - 28 edges
4. `EditorEngine` - 26 edges
5. `RuneEntry` - 26 edges
6. `PerfectParryModule` - 24 edges
7. `Memory` - 22 edges
8. `AutoBackupSaveModule` - 21 edges
9. `Draw` - 21 edges
10. `ChestAndInventoryModule` - 20 edges

## Surprising Connections (you probably didn't know these)
- `DimraethModPack Integration` --references--> `EquippedStatModifierModule`  [INFERRED]
  EquippedStatModifier/README.md → DimraethModPack/Modules/Stats/EquippedStatModifierModule.cs
- `Equipment rarity tertinggi (BARU)` --references--> `Patch_MaxEquipmentRarity`  [INFERRED]
  LootModV2/README.md → LootModV2/Patches/LootPatches.cs
- `DimraethModPackPlugin` --references--> `ModModuleBase`  [EXTRACTED]
  DimraethModPack/DimraethModPackPlugin.cs → DimraethModPack/Core/ModModuleBase.cs
- `NoClickPickupModule` --inherits--> `ModModuleBase`  [EXTRACTED]
  DimraethModPack/Modules/Gameplay/NoClickPickupModule.cs → DimraethModPack/Core/ModModuleBase.cs
- `PerfectParryModule` --inherits--> `ModModuleBase`  [EXTRACTED]
  DimraethModPack/Modules/Gameplay/PerfectParryModule.cs → DimraethModPack/Core/ModModuleBase.cs

## Import Cycles
- None detected.

## Communities (100 total, 25 thin omitted)

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

### Community 5 - "PerfectParryModule"
Cohesion: 0.09
Nodes (27): BaseSpell, ConfigEntry, ConfigFile, DamageType, Harmony, ObjectsCommon, ParryPrefab, RetaliationPrefab (+19 more)

### Community 6 - "NativeBytePatcher"
Cohesion: 0.07
Nodes (27): ConfigurableLevelCapPlugin, Instance, MethodTarget, NativeBytePatcher, AlreadyAtCap, ConfigEntry, DllImport, HashSet (+19 more)

### Community 7 - "RuneEditorUI"
Cohesion: 0.11
Nodes (13): IntPtr, Stat, StatCapCatalog, Color, GUIStyle, List, Player, Rune (+5 more)

### Community 8 - "HarmonyPostfix"
Cohesion: 0.07
Nodes (22): HarmonyPostfix, HarmonyPrefix, HashSet, InteractableRewardEntry, IntPtr, ItemType, List, MonsterUtils (+14 more)

### Community 9 - "global_system"
Cohesion: 0.35
Nodes (7): global_system, global_system_collections_generic, global_system_io, global_system_linq, global_system_net_http, global_system_threading, global_system_threading_tasks

### Community 10 - "system_collections_generic"
Cohesion: 0.17
Nodes (13): EquippedStatModifier.UI, EquipmentStatEditor.Core, EquippedStatModifier.Core, EquipmentStatEditor.UI, il2cppinterop_runtime_interoptypes, system_collections_generic, system_globalization, system_io (+5 more)

### Community 11 - "DayNightToggleModPlugin"
Cohesion: 0.08
Nodes (22): Color, ConfigEntry, Key, KeyCode, ManualLogSource, PlayerUpdate, DayNightToggleModPlugin, CurrentMode (+14 more)

### Community 12 - "LootPatches.cs"
Cohesion: 0.06
Nodes (25): LootModV2.Patches, Patch_ReturnRandomRuneData, HashSet, InteractableRewardEntry, List, MonsterUtils, HarvestMultiplication, Mult (+17 more)

### Community 13 - "ChestAndInventoryModule"
Cohesion: 0.10
Nodes (20): Chest, ConfigEntry, ConfigFile, EventTrigger, GridLayoutGroup, Harmony, HarmonyPatch, HarmonyPostfix (+12 more)

### Community 14 - ".ShouldTriggerParry"
Cohesion: 0.11
Nodes (19): BaseSpell, DamageType, ObjectsCommon, ParryPrefab, RetaliationPrefab, Spell, VanishPrefab, Patch_ParryPrefab_HandleLocalIncomingHit (+11 more)

### Community 15 - "AttackSpeedMod.cs"
Cohesion: 0.11
Nodes (18): CustomStatModPlugin, Patch_CalculateAttackSpeed, Patch_CalculateBaseAttackSpeed, Patch_CalculateBaseMaxConcentration, Patch_CalculateBaseMaxHealth, Patch_CalculateBaseMaxStamina, Patch_CalculateBaseSpellHaste, Patch_CalculateCriticalRate (+10 more)

### Community 16 - "HellModeLevel75Freebuff.cs"
Cohesion: 0.08
Nodes (27): Patch_SpellLibrarySetRegenTimer, bepinex, bepinex_logging, bepinex_unity_il2cpp, Patch_ExpMultiplier, Patch_ExpMultiplier, RahanerChestMod, AlwaysRegenMod (+19 more)

### Community 17 - ".IsTargetPlayer"
Cohesion: 0.17
Nodes (15): Attributes, ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon (+7 more)

### Community 18 - "RuneEntry"
Cohesion: 0.14
Nodes (14): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneConfigIO, RuneEntry (+6 more)

### Community 19 - "EquippedStatModifierUI"
Cohesion: 0.14
Nodes (11): Rune, EquippedStatModifierBehaviour, Color, GUIStyle, List, Player, Stat, Texture2D (+3 more)

### Community 20 - "harmonylib"
Cohesion: 0.20
Nodes (13): bepinex_configuration, DimraethModPack.Modules.Loot, DimraethModPack.Core, DimraethModPack.UI, DimraethModPack.Modules.SystemMod, DimraethModPack, AutoPickupMod, DimraethModPack.Modules.Gameplay (+5 more)

### Community 21 - "RahanerChestModPlugin"
Cohesion: 0.13
Nodes (15): Chest, ConfigEntry, EventTrigger, GridLayoutGroup, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Inventory (+7 more)

### Community 22 - "RuneMemory"
Cohesion: 0.17
Nodes (10): Dictionary, Type, PendingVerify, Action, Dictionary, Il2CppObjectBase, IntPtr, Player (+2 more)

### Community 23 - "HellModeModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, MonsterConfiguration, MonsterSetup, HellModeModule (+6 more)

### Community 24 - "Patches"
Cohesion: 0.10
Nodes (17): Dictionary, HarmonyPatch, HarmonyPostfix, ItemType, List, Player, PlayerUpdate, Patches (+9 more)

### Community 25 - "UpgradeBonusStatIsNotRandomModule"
Cohesion: 0.12
Nodes (16): ConfigEntry, ConfigFile, Dictionary, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Rune (+8 more)

### Community 26 - "AutoBackupSaveModule"
Cohesion: 0.11
Nodes (13): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, List, AutoBackupSaveModule (+5 more)

### Community 28 - ".DrawToggle"
Cohesion: 0.15
Nodes (11): Action, ConfigEntry, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle (+3 more)

### Community 29 - "Dimraeth - Complete Equipment Upgrade & Enhancement Guide"
Cohesion: 0.09
Nodes (21): 1. Maximum Upgrade Level, 1. The Seed Formula (GetUpgradeSeed), 2. Candidate Filtering (GetRandomSecondaryStat), 2. Primary Stat Scaling (Every Level), A. All Existing Secondary Stats Level Up, B. A New Random Secondary Stat is Rolled, Complete Equipment Stat Affix Catalog (Runes.Stat), Core Offensive & Penetration (+13 more)

### Community 30 - "EquipmentStatEditor/Core/RuneMemory.cs"
Cohesion: 0.19
Nodes (21): BindLogger(), Action, Il2CppObjectBase, IntPtr, Player, Rune, Type, DataPtr() (+13 more)

### Community 31 - "DiagnosticsManager"
Cohesion: 0.11
Nodes (13): ConfigEntry, ConfigFile, DiagnosticsManager, AvgFps, CurrentFps, ManagedRamMb, MaxSpikeDurationMs, OnePercentLowFps (+5 more)

### Community 32 - "Dimraeth QoL & Progression ModPack"
Cohesion: 0.11
Nodes (16): 1. 🗡️ Stats Category, 2. 🎮 Gameplay Category, 3. 📦 Loot Category, 4. ⚙️ System Category, ✦ Controls & Configuration, Dimraeth QoL & Progression ModPack, ✦ Included Modules & Core Features, ✦ Installation (+8 more)

### Community 33 - "NativeBytePatcher"
Cohesion: 0.17
Nodes (11): DllImport, IntPtr, UIntPtr, GameConfigCapPatcher, NativeBytePatcher, maxAttributeLevel, maxLevel, offset (+3 more)

### Community 34 - "DimraethModPackPlugin"
Cohesion: 0.14
Nodes (14): ConfigEntry, Harmony, KeyCode, List, ManualLogSource, DimraethModPackPlugin, Modules, UI (+6 more)

### Community 35 - "SkillPointMultiplierModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, Player, PlayerStats (+6 more)

### Community 36 - "EditorEngine"
Cohesion: 0.26
Nodes (6): HashSet, Il2CppObjectBase, List, Player, EditorEngine, PendingVerify

### Community 37 - "CarryWeightModule"
Cohesion: 0.12
Nodes (14): Attributes, ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, ObjectsCommon, Player (+6 more)

### Community 38 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 39 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 40 - "EquipmentStatEditorPlugin"
Cohesion: 0.14
Nodes (10): Action, Harmony, SaveHooks, ConfigEntry, Harmony, KeyCode, ManualLogSource, EditorController (+2 more)

### Community 41 - "LootAndExpMod.cs"
Cohesion: 0.17
Nodes (7): LootAndExpMod, Player, Patch_AddXPToPet, Patch_ApplyDropChanceModifiers, Patch_CalculateGoldDrop, Patch_CalculateXPGained, Patch_GrantXPToPlayer

### Community 42 - "Patches"
Cohesion: 0.28
Nodes (7): HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon, Player, Patch_CommonsZeroPointOneSecond, Patches

### Community 43 - "UnrandomizerUI"
Cohesion: 0.28
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, UnrandomizerUI

### Community 44 - "NoClickPickupModule"
Cohesion: 0.17
Nodes (10): ConfigEntry, ConfigFile, Harmony, KeyCode, Rarity, NoClickPickupModule, Category, Description (+2 more)

### Community 45 - "ExpModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, ExpModule, Category, Description, Instance (+1 more)

### Community 46 - "Patches"
Cohesion: 0.24
Nodes (7): HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Player, Patch_GrantXP, Patches, HarmonyFinalizer

### Community 47 - "RelationshipModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, RelationshipModule, Category, Description, Instance (+1 more)

### Community 48 - ".Prefix"
Cohesion: 0.21
Nodes (9): Dictionary, Rune, Stat, StatCatalog, HarmonyPrefix, Rune, Runes, Stat (+1 more)

### Community 49 - "ModModuleBase"
Cohesion: 0.18
Nodes (8): ConfigFile, Harmony, ModModuleBase, Category, Description, Enabled, IsEnabled, Name

### Community 50 - "LootModV2Module"
Cohesion: 0.18
Nodes (9): ConfigEntry, ConfigFile, Harmony, LootModV2Module, Category, Description, Instance, IsSpawningChestLoot (+1 more)

### Community 51 - "Patch_PlayerUpdate_Update"
Cohesion: 0.33
Nodes (5): Patch_PlayerUpdate_Update, Key, KeyCode, Player, PlayerUpdate

### Community 52 - "AlwaysRegenModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, AlwaysRegenModule, Category, Description, Instance, Name

### Community 53 - "UpgradeBonusStatIsNotRandomPlugin"
Cohesion: 0.20
Nodes (7): ConfigEntry, Harmony, KeyCode, ManualLogSource, UnrandomizerBehaviour, UpgradeBonusStatIsNotRandomPlugin, Instance

### Community 54 - "BasePlugin"
Cohesion: 0.22
Nodes (7): AlwaysRegenModPlugin, ConfigEntry, ManualLogSource, BasePlugin, CarryWeightModPlugin, ConfigEntry, ManualLogSource

### Community 55 - "DayNightToggleMod.cs"
Cohesion: 0.22
Nodes (6): DayNightToggleMod, DayNightLightAdjuster, Patch_DayNightLightAdjuster_Update, Patch_EnvironmentColor_Update, EnvironmentColor, unityengine_rendering_universal

### Community 56 - "NavMeshFixMod.cs"
Cohesion: 0.22
Nodes (6): NavMeshFixMod, LaughingSlashesPrefab, MonsterMovement, Patch_LaughingSlashesPrefab_CleanupMovementState, Patch_MonsterMovement_Start, unityengine_ai

### Community 57 - "ObjectsCommon"
Cohesion: 0.29
Nodes (4): Patch_CalculateHealthRegeneration, Patch_CalculateStaminaRegeneration, Patch_CommonsZeroPointOneSecond, ObjectsCommon

### Community 58 - "DeedUnlockerMod"
Cohesion: 0.25
Nodes (7): Cara Kerja, Catatan, DeedUnlockerMod, Instalasi, Konfigurasi, Masalah yang Diperbaiki, Verifikasi Log

### Community 59 - "DimraethModPack"
Cohesion: 0.25
Nodes (7): Build, Building from Source, DimraethModPack, Features, License, Modules, Prerequisites

### Community 60 - "Equipment Stat Editor"
Cohesion: 0.25
Nodes (7): Equipment Stat Editor, How values scale, Notes, Per-stat value caps, Stat value caps (the game's real limits), Stats with no roll entry in this build, What it does

### Community 61 - "RuneSnapshot"
Cohesion: 0.29
Nodes (7): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneSnapshot

### Community 62 - "StatCatalog"
Cohesion: 0.43
Nodes (4): Dictionary, List, Stat, StatCatalog

### Community 63 - "UnrandomizerUI.cs"
Cohesion: 0.40
Nodes (3): UpgradeBonusStatIsNotRandom.UI, UpgradeBonusStatIsNotRandom.Patches, UpgradeBonusStatIsNotRandom.Core

### Community 64 - ".Postfix"
Cohesion: 0.33
Nodes (4): DeedDefinition, DeedTierConfiguration, Patch_DeedLootCaps, Patch_UnlockedTierForDeed

### Community 65 - "DeedUnlockerPlugin"
Cohesion: 0.33
Nodes (5): ConfigEntry, ManualLogSource, Rarity, Stars, DeedUnlockerPlugin

### Community 66 - ".Postfix"
Cohesion: 0.40
Nodes (4): Patch_CalculateEncumbrance, Attributes, ObjectsCommon, Player

### Community 67 - "Patch_PlayerAwake"
Cohesion: 0.40
Nodes (3): Patch_PlayerAwake, Patch_XPGainBlocked, Player

### Community 68 - "Patch_PlayerAwake"
Cohesion: 0.40
Nodes (3): Patch_PlayerAwake, Patch_XPGainBlocked, Player

### Community 69 - "EquippedStatModifierPlugin"
Cohesion: 0.40
Nodes (5): ConfigEntry, KeyCode, ManualLogSource, EquippedStatModifierPlugin, Instance

### Community 70 - "AutoPickupModPlugin"
Cohesion: 0.50
Nodes (3): AutoPickupModPlugin, ConfigEntry, ManualLogSource

### Community 72 - "LootAndExpModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootAndExpModPlugin

### Community 73 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 74 - "LootModV2Plugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootModV2Plugin

### Community 75 - "NavMeshFixModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, NavMeshFixModPlugin

### Community 76 - ".Prefix"
Cohesion: 0.50
Nodes (3): ObjectsCommon, Patch_ObjectsCommon_SafeWarp, Vector3

### Community 77 - "PerfectParryModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, PerfectParryModPlugin

## Knowledge Gaps
- **226 isolated node(s):** `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk`, `AttackSpeedMod`, `net6.0` (+221 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 548 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **25 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModModuleBase` connect `ModModuleBase` to `DimraethModPackPlugin`, `SkillPointMultiplierModule`, `Memory`, `PerfectParryModule`, `CarryWeightModule`, `NoClickPickupModule`, `ChestAndInventoryModule`, `ExpModule`, `RelationshipModule`, `.IsTargetPlayer`, `LootModV2Module`, `harmonylib`, `AlwaysRegenModule`, `HellModeModule`, `UpgradeBonusStatIsNotRandomModule`, `AutoBackupSaveModule`, `.DrawToggle`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **Why does `ChestAndInventoryModule` connect `ChestAndInventoryModule` to `ModModuleBase`, `harmonylib`, `.DrawToggle`, `DiagnosticsManager`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Why does `EquippedStatModifierModule` connect `Memory` to `ModModuleBase`, `system_collections_generic`?**
  _High betweenness centrality (0.054) - this node is a cross-community bridge._
- **What connects `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _226 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.06342072409488139 - nodes in this community are weakly interconnected._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.07423423423423424 - nodes in this community are weakly interconnected._
- **Should `MinimapBehaviour` be split into smaller, more focused modules?**
  _Cohesion score 0.05593607305936073 - nodes in this community are weakly interconnected._