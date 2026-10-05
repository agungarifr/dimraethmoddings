# Graph Report - BepInExModsSource  (2026-09-29)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 1782 nodes · 3568 edges · 110 communities (87 shown, 23 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 57 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- HarmonyPatch
- HarmonyPatch
- MinimapBehaviour
- system
- Memory
- RuneEditorUI
- PerfectParryModule
- NativeBytePatcher
- DayNightToggleModPlugin
- global_system
- ChestAndInventoryModule
- .ShouldTriggerParry
- AttackSpeedMod.cs
- .IsTargetPlayer
- EquippedStatModifierUI
- AutoBackupSaveModule
- HellModeLevel75Freebuff.cs
- EditorEngine
- RuneEntry
- RahanerChestModPlugin
- harmonylib
- HellModeModule
- NativeBytePatcher
- .DrawToggle
- UpgradeBonusStatIsNotRandomModule
- .IsLocalPlayer
- RuneMemory
- bepinex_bootstrap
- Dimraeth - Complete Equipment Upgrade & Enhancement Guide
- EquipmentStatEditor/Core/RuneMemory.cs
- Dimraeth QoL & Progression ModPack
- NativeBytePatcher
- Patches
- SkillPointMultiplierModule
- AntiCheatBypassModule
- ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.cs
- system_collections_generic
- LevelCapModule
- ConfigurableLevelCapFreebuff
- ConfigurableLevelCapFreebuff
- DiagnosticsManager
- LootAndExpMod.cs
- UnrandomizerUI
- NoClickPickupModule
- ExpModule
- Patches
- RelationshipModule
- .Prefix
- .Load
- ModModuleBase
- LootModV2Module
- HarmonyPostfix
- ModPackUI
- Patch_PlayerUpdate_Update
- HarmonyPrefix
- AlwaysRegenModule
- NavMeshFixMod.cs
- CarryWeightModule
- LootPatches.cs
- Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)
- ObjectsCommon
- DeedUnlockerMod
- Patch_CreateNewStorageData
- DimraethModPack
- DayNightToggleMod.cs
- Equipment Stat Editor
- .Multiply
- .Prefix_UpdateRelationshipStatus
- ModPackManagerBehaviour
- RuneSnapshot
- UnrandomizerUI.cs
- .Postfix
- DeedUnlockerPlugin
- .Prefix
- StatCatalog
- UpgradeBonusStatIsNotRandomPlugin
- BasePlugin
- .Postfix
- .Postfix
- EquippedStatModifierPlugin
- AutoPickupModPlugin
- CarryWeightModPlugin
- Patch_InteractableReward
- LootAndExpModPlugin
- Patch_ItemDropCheck
- LootModV2Plugin
- Patch_ItemDropCheck
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

### Community 5 - "RuneEditorUI"
Cohesion: 0.06
Nodes (28): Action, Harmony, SaveHooks, Harmony, GearLegalityGuard, BypassActive, IntPtr, Stat (+20 more)

### Community 6 - "PerfectParryModule"
Cohesion: 0.09
Nodes (27): BaseSpell, ConfigEntry, ConfigFile, DamageType, Harmony, ObjectsCommon, ParryPrefab, RetaliationPrefab (+19 more)

### Community 7 - "NativeBytePatcher"
Cohesion: 0.07
Nodes (27): ConfigurableLevelCapPlugin, Instance, MethodTarget, NativeBytePatcher, AlreadyAtCap, ConfigEntry, DllImport, HashSet (+19 more)

### Community 8 - "DayNightToggleModPlugin"
Cohesion: 0.08
Nodes (22): Color, ConfigEntry, Key, KeyCode, ManualLogSource, PlayerUpdate, DayNightToggleModPlugin, CurrentMode (+14 more)

### Community 9 - "global_system"
Cohesion: 0.35
Nodes (7): global_system, global_system_collections_generic, global_system_io, global_system_linq, global_system_net_http, global_system_threading, global_system_threading_tasks

### Community 10 - "ChestAndInventoryModule"
Cohesion: 0.10
Nodes (20): Chest, ConfigEntry, ConfigFile, EventTrigger, GridLayoutGroup, Harmony, HarmonyPatch, HarmonyPostfix (+12 more)

### Community 11 - ".ShouldTriggerParry"
Cohesion: 0.11
Nodes (19): BaseSpell, DamageType, ObjectsCommon, ParryPrefab, RetaliationPrefab, Spell, VanishPrefab, Patch_ParryPrefab_HandleLocalIncomingHit (+11 more)

### Community 12 - "AttackSpeedMod.cs"
Cohesion: 0.11
Nodes (18): CustomStatModPlugin, Patch_CalculateAttackSpeed, Patch_CalculateBaseAttackSpeed, Patch_CalculateBaseMaxConcentration, Patch_CalculateBaseMaxHealth, Patch_CalculateBaseMaxStamina, Patch_CalculateBaseSpellHaste, Patch_CalculateCriticalRate (+10 more)

### Community 13 - ".IsTargetPlayer"
Cohesion: 0.17
Nodes (15): Attributes, ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon (+7 more)

### Community 14 - "EquippedStatModifierUI"
Cohesion: 0.14
Nodes (11): Rune, EquippedStatModifierBehaviour, Color, GUIStyle, List, Player, Stat, Texture2D (+3 more)

### Community 15 - "AutoBackupSaveModule"
Cohesion: 0.10
Nodes (14): Action, ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, List (+6 more)

### Community 16 - "HellModeLevel75Freebuff.cs"
Cohesion: 0.08
Nodes (24): Patch_CalculateSprintingCostPerSecond, Patch_SpellLibrarySetRegenTimer, Player, bepinex, bepinex_logging, bepinex_unity_il2cpp, AlwaysRegenMod, PerfectParryMod (+16 more)

### Community 17 - "EditorEngine"
Cohesion: 0.18
Nodes (10): Dictionary, HashSet, Il2CppObjectBase, IntPtr, List, Player, Type, EditorEngine (+2 more)

### Community 18 - "RuneEntry"
Cohesion: 0.15
Nodes (14): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneConfigIO, RuneEntry (+6 more)

### Community 19 - "RahanerChestModPlugin"
Cohesion: 0.13
Nodes (15): Chest, ConfigEntry, EventTrigger, GridLayoutGroup, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Inventory (+7 more)

### Community 20 - "harmonylib"
Cohesion: 0.18
Nodes (15): bepinex_configuration, RahanerChestMod, DimraethModPack.Modules.Loot, DimraethModPack.Core, DimraethModPack.UI, DimraethModPack.Modules.SystemMod, DimraethModPack, AutoPickupMod (+7 more)

### Community 21 - "HellModeModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, MonsterConfiguration, MonsterSetup, HellModeModule (+6 more)

### Community 22 - "NativeBytePatcher"
Cohesion: 0.10
Nodes (19): 2026-09-28, 2026-09-28 (20:50), 2026-09-29, 2026-09-29 (09:28), Changelog, Dimraeth ModPack (`DimraethModPack.dll`), DimraethModPack (`DimraethModPack.dll`), DimraethModPack (`DimraethModPack.dll`) (+11 more)

### Community 23 - ".DrawToggle"
Cohesion: 0.14
Nodes (12): DimraethModPack (`DimraethModPack.dll`), Action, ConfigEntry, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle (+4 more)

### Community 24 - "UpgradeBonusStatIsNotRandomModule"
Cohesion: 0.12
Nodes (16): ConfigEntry, ConfigFile, Dictionary, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Rune (+8 more)

### Community 25 - ".IsLocalPlayer"
Cohesion: 0.14
Nodes (15): Il2CppObjectBase, IntPtr, PlayerIdentity, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon, Player (+7 more)

### Community 26 - "RuneMemory"
Cohesion: 0.21
Nodes (7): Action, Dictionary, Il2CppObjectBase, IntPtr, Player, Type, RuneMemory

### Community 28 - "Dimraeth - Complete Equipment Upgrade & Enhancement Guide"
Cohesion: 0.09
Nodes (21): 1. Maximum Upgrade Level, 1. The Seed Formula (GetUpgradeSeed), 2. Candidate Filtering (GetRandomSecondaryStat), 2. Primary Stat Scaling (Every Level), A. All Existing Secondary Stats Level Up, B. A New Random Secondary Stat is Rolled, Complete Equipment Stat Affix Catalog (Runes.Stat), Core Offensive & Penetration (+13 more)

### Community 29 - "EquipmentStatEditor/Core/RuneMemory.cs"
Cohesion: 0.19
Nodes (21): BindLogger(), Action, Il2CppObjectBase, IntPtr, Player, Rune, Type, DataPtr() (+13 more)

### Community 30 - "Dimraeth QoL & Progression ModPack"
Cohesion: 0.11
Nodes (16): 1. 🗡️ Stats Category, 2. 🎮 Gameplay Category, 3. 📦 Loot Category, 4. ⚙️ System Category, ✦ Controls & Configuration, Dimraeth QoL & Progression ModPack, ✦ Included Modules & Core Features, ✦ Installation (+8 more)

### Community 31 - "NativeBytePatcher"
Cohesion: 0.17
Nodes (11): DllImport, IntPtr, UIntPtr, GameConfigCapPatcher, NativeBytePatcher, maxAttributeLevel, maxLevel, offset (+3 more)

### Community 32 - "Patches"
Cohesion: 0.13
Nodes (13): Dictionary, HarmonyPatch, HarmonyPostfix, ItemType, List, Player, PlayerUpdate, Patches (+5 more)

### Community 33 - "SkillPointMultiplierModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, Player, PlayerStats (+6 more)

### Community 34 - "AntiCheatBypassModule"
Cohesion: 0.12
Nodes (12): ConfigEntry, ConfigFile, GUIStyle, Harmony, List, Type, AntiCheatBypassModule, Category (+4 more)

### Community 35 - "ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.cs"
Cohesion: 0.13
Nodes (10): Patch_ExpMultiplier, Patch_PlayerAwake, Patch_XPGainBlocked, Player, Patch_ExpMultiplier, Patch_PlayerAwake, Patch_XPGainBlocked, Player (+2 more)

### Community 36 - "system_collections_generic"
Cohesion: 0.15
Nodes (14): EquippedStatModifier.UI, EquipmentStatEditor.Core, EquippedStatModifier.Core, EquipmentStatEditor.UI, il2cppinterop_runtime, il2cppinterop_runtime_interoptypes, system_collections_generic, system_globalization (+6 more)

### Community 37 - "LevelCapModule"
Cohesion: 0.12
Nodes (13): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Player, LevelCapModule (+5 more)

### Community 38 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 39 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 40 - "DiagnosticsManager"
Cohesion: 0.15
Nodes (10): ConfigEntry, DiagnosticsManager, AvgFps, CurrentFps, ManagedRamMb, MaxSpikeDurationMs, OnePercentLowFps, ProcessWorkingSetMb (+2 more)

### Community 41 - "LootAndExpMod.cs"
Cohesion: 0.17
Nodes (7): LootAndExpMod, Player, Patch_AddXPToPet, Patch_ApplyDropChanceModifiers, Patch_CalculateGoldDrop, Patch_CalculateXPGained, Patch_GrantXPToPlayer

### Community 42 - "UnrandomizerUI"
Cohesion: 0.28
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, UnrandomizerUI

### Community 43 - "NoClickPickupModule"
Cohesion: 0.17
Nodes (10): ConfigEntry, ConfigFile, Harmony, KeyCode, Rarity, NoClickPickupModule, Category, Description (+2 more)

### Community 44 - "ExpModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, ExpModule, Category, Description, Instance (+1 more)

### Community 45 - "Patches"
Cohesion: 0.24
Nodes (7): HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Player, Patch_GrantXP, Patches, HarmonyFinalizer

### Community 46 - "RelationshipModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, RelationshipModule, Category, Description, Instance (+1 more)

### Community 47 - ".Prefix"
Cohesion: 0.21
Nodes (9): Dictionary, Rune, Stat, StatCatalog, HarmonyPrefix, Rune, Runes, Stat (+1 more)

### Community 48 - ".Load"
Cohesion: 0.18
Nodes (9): ConfigFile, ConfigEntry, Harmony, KeyCode, List, ManualLogSource, DimraethModPackPlugin, Modules (+1 more)

### Community 49 - "ModModuleBase"
Cohesion: 0.18
Nodes (8): ConfigFile, Harmony, ModModuleBase, Category, Description, Enabled, IsEnabled, Name

### Community 50 - "LootModV2Module"
Cohesion: 0.18
Nodes (9): ConfigEntry, ConfigFile, Harmony, LootModV2Module, Category, Description, Instance, IsSpawningChestLoot (+1 more)

### Community 51 - "HarmonyPostfix"
Cohesion: 0.24
Nodes (6): HarmonyPostfix, List, Patch_CalculateGoldDrop, Patch_DropChance, Patch_HarvestBonus, Patch_HarvestCalculate

### Community 52 - "ModPackUI"
Cohesion: 0.29
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, ModPackUI

### Community 53 - "Patch_PlayerUpdate_Update"
Cohesion: 0.33
Nodes (5): Patch_PlayerUpdate_Update, Key, KeyCode, Player, PlayerUpdate

### Community 54 - "HarmonyPrefix"
Cohesion: 0.20
Nodes (6): HarmonyPrefix, ItemType, Patch_ChestBasic_SpawnStarter, Patch_StorageData_AddItem, StorageData, StorageSpawn

### Community 55 - "AlwaysRegenModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, AlwaysRegenModule, Category, Description, Instance, Name

### Community 56 - "NavMeshFixMod.cs"
Cohesion: 0.22
Nodes (6): NavMeshFixMod, LaughingSlashesPrefab, MonsterMovement, Patch_LaughingSlashesPrefab_CleanupMovementState, Patch_MonsterMovement_Start, unityengine_ai

### Community 57 - "CarryWeightModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, CarryWeightModule, Category, Description, Instance, Name

### Community 58 - "LootPatches.cs"
Cohesion: 0.22
Nodes (5): LootModV2.Patches, InteractableRewardEntry, Patch_ApplyDropChanceModifiers, Patch_GoldDrop, Patch_InteractableReward

### Community 59 - "Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)"
Cohesion: 0.22
Nodes (8): Build & install, Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`), Interactable di map (BARU) — dead body / box / tekan C, Konfigurasi (`BepInEx/config/com.custom.lootmodv2.cfg`), LootModV2 — Loot 10x (Monster + Node Panen + Interactable), Monster (dipertahankan), Node panen (BARU) — stone, wood/log, rumput, bush, plant, dll., Yang diubah vs LootAndExpMod lama

### Community 60 - "ObjectsCommon"
Cohesion: 0.29
Nodes (4): Patch_CalculateHealthRegeneration, Patch_CalculateStaminaRegeneration, Patch_CommonsZeroPointOneSecond, ObjectsCommon

### Community 61 - "DeedUnlockerMod"
Cohesion: 0.25
Nodes (7): Cara Kerja, Catatan, DeedUnlockerMod, Instalasi, Konfigurasi, Masalah yang Diperbaiki, Verifikasi Log

### Community 62 - "Patch_CreateNewStorageData"
Cohesion: 0.25
Nodes (5): HashSet, IntPtr, MonsterUtils, Patch_CreateNewStorageData, Patch_ItemDropCheck

### Community 63 - "DimraethModPack"
Cohesion: 0.25
Nodes (7): Build, Building from Source, DimraethModPack, Features, License, Modules, Prerequisites

### Community 64 - "DayNightToggleMod.cs"
Cohesion: 0.22
Nodes (6): DayNightToggleMod, DayNightLightAdjuster, Patch_DayNightLightAdjuster_Update, Patch_EnvironmentColor_Update, EnvironmentColor, unityengine_rendering_universal

### Community 65 - "Equipment Stat Editor"
Cohesion: 0.25
Nodes (7): Equipment Stat Editor, How values scale, Notes, Per-stat value caps, Stat value caps (the game's real limits), Stats with no roll entry in this build, What it does

### Community 66 - ".Multiply"
Cohesion: 0.32
Nodes (5): List, HarvestMultiplication, Mult, Patch_HarvestBonusItems, Patch_HarvestCalculateItems

### Community 67 - ".Prefix_UpdateRelationshipStatus"
Cohesion: 0.43
Nodes (4): HarmonyPatch, HarmonyPrefix, Patches, RelationshipData

### Community 68 - "ModPackManagerBehaviour"
Cohesion: 0.29
Nodes (3): ModPackManagerBehaviour, MonoBehaviour, UnrandomizerBehaviour

### Community 69 - "RuneSnapshot"
Cohesion: 0.29
Nodes (7): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneSnapshot

### Community 70 - "UnrandomizerUI.cs"
Cohesion: 0.40
Nodes (3): UpgradeBonusStatIsNotRandom.UI, UpgradeBonusStatIsNotRandom.Patches, UpgradeBonusStatIsNotRandom.Core

### Community 71 - ".Postfix"
Cohesion: 0.33
Nodes (4): DeedDefinition, DeedTierConfiguration, Patch_DeedLootCaps, Patch_UnlockedTierForDeed

### Community 72 - "DeedUnlockerPlugin"
Cohesion: 0.33
Nodes (5): ConfigEntry, ManualLogSource, Rarity, Stars, DeedUnlockerPlugin

### Community 73 - ".Prefix"
Cohesion: 0.33
Nodes (4): Patch_ReturnRandomRuneData, Patch_MaxEquipmentRarity, Equipment rarity tertinggi (BARU), Rarity&gt;

### Community 74 - "StatCatalog"
Cohesion: 0.53
Nodes (4): Dictionary, List, Stat, StatCatalog

### Community 75 - "UpgradeBonusStatIsNotRandomPlugin"
Cohesion: 0.33
Nodes (6): ConfigEntry, Harmony, KeyCode, ManualLogSource, UpgradeBonusStatIsNotRandomPlugin, Instance

### Community 76 - "BasePlugin"
Cohesion: 0.40
Nodes (4): AlwaysRegenModPlugin, ConfigEntry, ManualLogSource, BasePlugin

### Community 77 - ".Postfix"
Cohesion: 0.40
Nodes (4): Patch_CalculateEncumbrance, Attributes, ObjectsCommon, Player

### Community 78 - ".Postfix"
Cohesion: 0.60
Nodes (3): Patch_DungeonChest_TrySpawn, IntRange, ItemDrop

### Community 79 - "EquippedStatModifierPlugin"
Cohesion: 0.40
Nodes (5): ConfigEntry, KeyCode, ManualLogSource, EquippedStatModifierPlugin, Instance

### Community 80 - "AutoPickupModPlugin"
Cohesion: 0.50
Nodes (3): AutoPickupModPlugin, ConfigEntry, ManualLogSource

### Community 81 - "CarryWeightModPlugin"
Cohesion: 0.50
Nodes (3): CarryWeightModPlugin, ConfigEntry, ManualLogSource

### Community 83 - "LootAndExpModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootAndExpModPlugin

### Community 84 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 85 - "LootModV2Plugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootModV2Plugin

### Community 86 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 87 - "NavMeshFixModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, NavMeshFixModPlugin

### Community 88 - ".Prefix"
Cohesion: 0.50
Nodes (3): ObjectsCommon, Patch_ObjectsCommon_SafeWarp, Vector3

### Community 89 - "PerfectParryModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, PerfectParryModPlugin

## Knowledge Gaps
- **238 isolated node(s):** `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk`, `AttackSpeedMod`, `net6.0` (+233 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 586 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **23 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModModuleBase` connect `ModModuleBase` to `Memory`, `PerfectParryModule`, `ChestAndInventoryModule`, `.IsTargetPlayer`, `AutoBackupSaveModule`, `harmonylib`, `HellModeModule`, `.DrawToggle`, `UpgradeBonusStatIsNotRandomModule`, `SkillPointMultiplierModule`, `AntiCheatBypassModule`, `LevelCapModule`, `NoClickPickupModule`, `ExpModule`, `RelationshipModule`, `.Load`, `LootModV2Module`, `ModPackUI`, `AlwaysRegenModule`, `CarryWeightModule`?**
  _High betweenness centrality (0.084) - this node is a cross-community bridge._
- **Why does `ChestAndInventoryModule` connect `ChestAndInventoryModule` to `.Load`, `ModModuleBase`, `harmonylib`, `.DrawToggle`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Why does `DimraethModPackPlugin` connect `.Load` to `BasePlugin`, `ModModuleBase`, `harmonylib`, `ModPackUI`, `NativeBytePatcher`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **What connects `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _238 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.06342072409488139 - nodes in this community are weakly interconnected._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.07423423423423424 - nodes in this community are weakly interconnected._
- **Should `MinimapBehaviour` be split into smaller, more focused modules?**
  _Cohesion score 0.05593607305936073 - nodes in this community are weakly interconnected._