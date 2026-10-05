# Graph Report - BepInExModsSource  (2026-10-01)

## Corpus Check
- 216 files · ~106,245 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 295 file(s) not represented in the graph (top: .cache 124, .dll 70, .pdb 28)

## Summary
- 1961 nodes · 3972 edges · 114 communities (88 shown, 26 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 60 edges (avg confidence: 0.86)
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
- PerfectParryMod.cs
- AttackSpeedMod.cs
- .IsTargetPlayer
- EquippedStatModifierUI
- AutoBackupSaveModule
- DayNightToggleMod.cs
- EditorEngine
- RuneEntry
- RahanerChestModPlugin
- unityengine
- HellModeModule
- ModPackUI
- .DrawToggle
- UpgradeBonusStatIsNotRandomModule
- CarryWeightModule
- RuneMemory
- bepinex_bootstrap
- Dimraeth - Complete Equipment Upgrade & Enhancement Guide
- EquipmentStatEditor/Core/RuneMemory.cs
- Dimraeth QoL & Progression ModPack
- NativeBytePatcher
- NoClickPickupModule
- SkillPointMultiplierModule
- EquipmentDropCounts
- EditorController
- HellModeLevel75Freebuff.cs
- LevelCapModule
- ConfigurableLevelCapFreebuff
- ConfigurableLevelCapFreebuff
- .Load
- LootAndExpMod.cs
- UnrandomizerUI
- ConfigurableLevelCapPlugin
- ExpModule
- ModModuleBase
- RelationshipModule
- .Prefix
- EquipmentDropBoost
- HarmonyPostfix
- LootModV2Module
- HarmonyPrefix
- .DrainWorld
- Patch_PlayerUpdate_Update
- .Prefix
- AlwaysRegenModule
- DimraethModPackPlugin
- Changelog
- WeatherControlModule
- LootPatches.cs
- ObjectsCommon
- DeedUnlockerMod
- LootModV2Plugin
- DimraethModPack
- EquipmentStatEditorPlugin
- Equipment Stat Editor
- PetCargoModule
- MonoBehaviour
- NavMeshFixMod.cs
- RuneSnapshot
- UnrandomizerUI.cs
- .Postfix
- DeedUnlockerPlugin
- .Resolve
- StatCatalog
- UpgradeBonusStatIsNotRandomPlugin
- BasePlugin
- .Postfix
- .Postfix
- EquippedStatModifierPlugin
- .Postfix_UpgradeLevelClientRpc
- AntiCheatBypassPatches
- Patch_PlayerAwake
- LootAndExpModPlugin
- Patch_ItemDropCheck
- AutoPickupModPlugin
- Patch_PlayerAwake
- NavMeshFixModPlugin
- AntiCheatBypassModPlugin
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
- AntiCheatBypassMod.csproj
- Patch_CalculateSprintingCostPerSecond
- EquippedStatModifierModule.cs

## God Nodes (most connected - your core abstractions)
1. `ModModuleBase` - 36 edges
2. `MinimapBehaviour` - 29 edges
3. `RuneEditorUI` - 28 edges
4. `EditorEngine` - 27 edges
5. `RuneEntry` - 26 edges
6. `PerfectParryModule` - 24 edges
7. `AutoBackupSaveModule` - 24 edges
8. `DimraethModPack.Core` - 23 edges
9. `EquipmentDropBoost` - 23 edges
10. `ChestAndInventoryModule` - 22 edges

## Surprising Connections (you probably didn't know these)
- `DimraethModPack Integration` --references--> `EquippedStatModifierModule`  [INFERRED]
  EquippedStatModifier/README.md → DimraethModPack/Modules/Stats/EquippedStatModifierModule.cs
- `DimraethModPack (`DimraethModPack.dll`)` --references--> `NativeBytePatcher`  [INFERRED]
  DimraethModPack/CHANGELOG.md → DimraethModPack/Modules/System/LevelCapModule.cs
- `DimraethModPack (`DimraethModPack.dll`)` --references--> `DimraethModPackPlugin`  [INFERRED]
  DimraethModPack/CHANGELOG.md → DimraethModPack/DimraethModPackPlugin.cs
- `Equipment rarity tertinggi (BARU)` --references--> `Patch_MaxEquipmentRarity`  [INFERRED]
  LootModV2/README.md → LootModV2/Patches/LootPatches.cs
- `DimraethModPackPlugin` --references--> `ModModuleBase`  [EXTRACTED]
  DimraethModPack/DimraethModPackPlugin.cs → DimraethModPack/Core/ModModuleBase.cs

## Import Cycles
- None detected.

## Communities (114 total, 26 thin omitted)

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
Cohesion: 0.11
Nodes (13): IntPtr, Stat, StatCapCatalog, Color, GUIStyle, List, Player, Rune (+5 more)

### Community 6 - "PerfectParryModule"
Cohesion: 0.09
Nodes (27): BaseSpell, ConfigEntry, ConfigFile, DamageType, Harmony, ObjectsCommon, ParryPrefab, RetaliationPrefab (+19 more)

### Community 7 - "NativeBytePatcher"
Cohesion: 0.07
Nodes (28): MethodTarget, NativeBytePatcher, AlreadyAtCap, DllImport, HashSet, IntPtr, Patched, Type (+20 more)

### Community 8 - "DayNightToggleModPlugin"
Cohesion: 0.08
Nodes (22): Color, ConfigEntry, Key, KeyCode, ManualLogSource, PlayerUpdate, DayNightToggleModPlugin, CurrentMode (+14 more)

### Community 9 - "global_system"
Cohesion: 0.34
Nodes (7): global_system, global_system_collections_generic, global_system_io, global_system_linq, global_system_net_http, global_system_threading, global_system_threading_tasks

### Community 10 - "ChestAndInventoryModule"
Cohesion: 0.10
Nodes (22): Chest, ConfigEntry, ConfigFile, EventTrigger, GridLayoutGroup, Harmony, HarmonyPatch, HarmonyPostfix (+14 more)

### Community 11 - "PerfectParryMod.cs"
Cohesion: 0.10
Nodes (23): PerfectParryMod, BaseSpell, ConfigEntry, DamageType, ManualLogSource, ObjectsCommon, ParryPrefab, RetaliationPrefab (+15 more)

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

### Community 16 - "DayNightToggleMod.cs"
Cohesion: 0.22
Nodes (6): DayNightToggleMod, DayNightLightAdjuster, Patch_DayNightLightAdjuster_Update, Patch_EnvironmentColor_Update, EnvironmentColor, unityengine_rendering_universal

### Community 17 - "EditorEngine"
Cohesion: 0.18
Nodes (10): Dictionary, HashSet, Il2CppObjectBase, IntPtr, List, Player, Type, EditorEngine (+2 more)

### Community 18 - "RuneEntry"
Cohesion: 0.15
Nodes (14): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneConfigIO, RuneEntry (+6 more)

### Community 19 - "RahanerChestModPlugin"
Cohesion: 0.13
Nodes (15): Chest, ConfigEntry, EventTrigger, GridLayoutGroup, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Inventory (+7 more)

### Community 20 - "unityengine"
Cohesion: 0.18
Nodes (15): bepinex_configuration, DimraethModPack.Modules.Loot, DimraethModPack.Core, DimraethModPack.UI, DimraethModPack.Modules.SystemMod, DimraethModPack, AutoPickupMod, DimraethModPack.Modules.Pets (+7 more)

### Community 21 - "HellModeModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, MonsterConfiguration, MonsterSetup, HellModeModule (+6 more)

### Community 22 - "ModPackUI"
Cohesion: 0.29
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, ModPackUI

### Community 23 - ".DrawToggle"
Cohesion: 0.14
Nodes (12): DimraethModPack (`DimraethModPack.dll`), Action, ConfigEntry, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle (+4 more)

### Community 24 - "UpgradeBonusStatIsNotRandomModule"
Cohesion: 0.12
Nodes (16): ConfigEntry, ConfigFile, Dictionary, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Rune (+8 more)

### Community 25 - "CarryWeightModule"
Cohesion: 0.09
Nodes (23): Il2CppObjectBase, IntPtr, PlayerIdentity, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon, Player (+15 more)

### Community 26 - "RuneMemory"
Cohesion: 0.22
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

### Community 32 - "NoClickPickupModule"
Cohesion: 0.07
Nodes (23): ConfigEntry, ConfigFile, Dictionary, Harmony, HarmonyPatch, HarmonyPostfix, ItemType, KeyCode (+15 more)

### Community 33 - "SkillPointMultiplierModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, SkillPointMultiplierModule, Category, Description, Instance (+1 more)

### Community 34 - "EquipmentDropCounts"
Cohesion: 0.22
Nodes (7): Action, ConfigEntry, GUIStyle, RuneSet, EquipmentDropCounts, Global, Func

### Community 35 - "EditorController"
Cohesion: 0.25
Nodes (4): Action, Harmony, SaveHooks, EditorController

### Community 36 - "HellModeLevel75Freebuff.cs"
Cohesion: 0.08
Nodes (28): Patch_SpellLibrarySetRegenTimer, bepinex, bepinex_logging, bepinex_unity_il2cpp, Patch_ExpMultiplier, Patch_ExpMultiplier, RahanerChestMod, AlwaysRegenMod (+20 more)

### Community 37 - "LevelCapModule"
Cohesion: 0.12
Nodes (13): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Player, LevelCapModule (+5 more)

### Community 38 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 39 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 40 - ".Load"
Cohesion: 0.14
Nodes (11): ConfigEntry, ConfigFile, DiagnosticsManager, AvgFps, CurrentFps, ManagedRamMb, MaxSpikeDurationMs, OnePercentLowFps (+3 more)

### Community 41 - "LootAndExpMod.cs"
Cohesion: 0.17
Nodes (7): LootAndExpMod, Player, Patch_AddXPToPet, Patch_ApplyDropChanceModifiers, Patch_CalculateGoldDrop, Patch_CalculateXPGained, Patch_GrantXPToPlayer

### Community 42 - "UnrandomizerUI"
Cohesion: 0.28
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, UnrandomizerUI

### Community 43 - "ConfigurableLevelCapPlugin"
Cohesion: 0.18
Nodes (8): ConfigurableLevelCapPlugin, Instance, ConfigEntry, ManualLogSource, ConfigurableLevelCapPlugin, Instance, ConfigEntry, ManualLogSource

### Community 44 - "ExpModule"
Cohesion: 0.10
Nodes (16): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Player (+8 more)

### Community 45 - "ModModuleBase"
Cohesion: 0.18
Nodes (8): ConfigFile, Harmony, ModModuleBase, Category, Description, Enabled, IsEnabled, Name

### Community 46 - "RelationshipModule"
Cohesion: 0.17
Nodes (9): ConfigEntry, ConfigFile, GUIStyle, Harmony, RelationshipModule, Category, Description, Instance (+1 more)

### Community 47 - ".Prefix"
Cohesion: 0.24
Nodes (8): Dictionary, Rune, Stat, StatCatalog, HarmonyPrefix, Rune, Runes, Stat

### Community 48 - "EquipmentDropBoost"
Cohesion: 0.14
Nodes (16): MonsterConfiguration, Rarity, RuneDrop, RuneSet, SlotType, Stars, EquipmentDropBoost, Active (+8 more)

### Community 49 - "HarmonyPostfix"
Cohesion: 0.14
Nodes (9): HarmonyPostfix, InteractableRewardEntry, List, Patch_CalculateGoldDrop, Patch_ChestBasic_SpawnStarter, Patch_DropChance, Patch_HarvestBonus, Patch_HarvestCalculate (+1 more)

### Community 50 - "LootModV2Module"
Cohesion: 0.09
Nodes (18): ConfigEntry, ConfigFile, Harmony, HashSet, IntPtr, ItemType, MonsterUtils, StorageData (+10 more)

### Community 51 - "HarmonyPrefix"
Cohesion: 0.13
Nodes (8): HarmonyPrefix, RuneDrop, Patch_Pools_GenerateRandomRune, Patch_ReturnRandomRuneData, Patch_SrcFlag_GrantRuneReward, Patch_SrcFlag_RewardPlayer, Patch_SrcFlag_SpawnLoot, Patch_SrcFlag_SpawnPersonalLoot

### Community 52 - ".DrainWorld"
Cohesion: 0.32
Nodes (5): Inventory, Rune, Scene, ServerRPC, Vector2

### Community 53 - "Patch_PlayerUpdate_Update"
Cohesion: 0.33
Nodes (5): Patch_PlayerUpdate_Update, Key, KeyCode, Player, PlayerUpdate

### Community 54 - ".Prefix"
Cohesion: 0.26
Nodes (9): HarmonyPatch, Inventory, Rune, Scene, ServerRPC, Vector2, Patch_AddRuneToInventory, Patch_AddRuneToWorld (+1 more)

### Community 55 - "AlwaysRegenModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, AlwaysRegenModule, Category, Description, Instance, Name

### Community 56 - "DimraethModPackPlugin"
Cohesion: 0.20
Nodes (10): 2026-09-28 (20:50), DimraethModPack (`DimraethModPack.dll`), ConfigEntry, Harmony, KeyCode, List, ManualLogSource, DimraethModPackPlugin (+2 more)

### Community 57 - "Changelog"
Cohesion: 0.22
Nodes (8): 2026-09-28, 2026-09-29, 2026-09-29 (09:28), Changelog, Dimraeth ModPack (`DimraethModPack.dll`), DimraethModPack (`DimraethModPack.dll`), Equipment Stat Editor (standalone plugin), How to update

### Community 58 - "WeatherControlModule"
Cohesion: 0.15
Nodes (10): ConfigEntry, ConfigFile, GUIStyle, Harmony, WeatherControlModule, Category, Description, Instance (+2 more)

### Community 59 - "LootPatches.cs"
Cohesion: 0.07
Nodes (23): LootModV2.Patches, HashSet, InteractableRewardEntry, List, MonsterUtils, HarvestMultiplication, Mult, Patch_ApplyDropChanceModifiers (+15 more)

### Community 60 - "ObjectsCommon"
Cohesion: 0.29
Nodes (4): Patch_CalculateHealthRegeneration, Patch_CalculateStaminaRegeneration, Patch_CommonsZeroPointOneSecond, ObjectsCommon

### Community 61 - "DeedUnlockerMod"
Cohesion: 0.25
Nodes (7): Cara Kerja, Catatan, DeedUnlockerMod, Instalasi, Konfigurasi, Masalah yang Diperbaiki, Verifikasi Log

### Community 62 - "LootModV2Plugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootModV2Plugin

### Community 63 - "DimraethModPack"
Cohesion: 0.25
Nodes (7): Build, Building from Source, DimraethModPack, Features, License, Modules, Prerequisites

### Community 64 - "EquipmentStatEditorPlugin"
Cohesion: 0.25
Nodes (7): ConfigEntry, Harmony, KeyCode, ManualLogSource, EquipmentStatEditorPlugin, Instance, EventArgs

### Community 65 - "Equipment Stat Editor"
Cohesion: 0.25
Nodes (7): Equipment Stat Editor, How values scale, Notes, Per-stat value caps, Stat value caps (the game's real limits), Stats with no roll entry in this build, What it does

### Community 66 - "PetCargoModule"
Cohesion: 0.06
Nodes (34): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Inventory (+26 more)

### Community 67 - "MonoBehaviour"
Cohesion: 0.25
Nodes (4): BypassOverlayBehaviour, GUIStyle, ModPackManagerBehaviour, MonoBehaviour

### Community 68 - "NavMeshFixMod.cs"
Cohesion: 0.22
Nodes (6): NavMeshFixMod, LaughingSlashesPrefab, MonsterMovement, Patch_LaughingSlashesPrefab_CleanupMovementState, Patch_MonsterMovement_Start, unityengine_ai

### Community 69 - "RuneSnapshot"
Cohesion: 0.29
Nodes (7): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneSnapshot

### Community 70 - "UnrandomizerUI.cs"
Cohesion: 0.33
Nodes (4): UpgradeBonusStatIsNotRandom.UI, UpgradeBonusStatIsNotRandom.Patches, UpgradeBonusStatIsNotRandom.Core, UpgradeRunePatch

### Community 71 - ".Postfix"
Cohesion: 0.33
Nodes (4): DeedDefinition, DeedTierConfiguration, Patch_DeedLootCaps, Patch_UnlockedTierForDeed

### Community 72 - "DeedUnlockerPlugin"
Cohesion: 0.33
Nodes (5): ConfigEntry, ManualLogSource, Rarity, Stars, DeedUnlockerPlugin

### Community 73 - ".Resolve"
Cohesion: 0.20
Nodes (5): MonsterConfiguration, SlotType, Dictionary, Patch_SrcFlag_RuneDropCheck, global

### Community 74 - "StatCatalog"
Cohesion: 0.53
Nodes (4): Dictionary, List, Stat, StatCatalog

### Community 75 - "UpgradeBonusStatIsNotRandomPlugin"
Cohesion: 0.20
Nodes (7): ConfigEntry, Harmony, KeyCode, ManualLogSource, UnrandomizerBehaviour, UpgradeBonusStatIsNotRandomPlugin, Instance

### Community 76 - "BasePlugin"
Cohesion: 0.22
Nodes (7): AlwaysRegenModPlugin, ConfigEntry, ManualLogSource, BasePlugin, CarryWeightModPlugin, ConfigEntry, ManualLogSource

### Community 77 - ".Postfix"
Cohesion: 0.19
Nodes (7): Patch_CalculateEncumbrance, Attributes, ObjectsCommon, Player, ObjectsCommon, Patch_ObjectsCommon_SafeWarp, Vector3

### Community 78 - ".Postfix"
Cohesion: 0.60
Nodes (3): Patch_DungeonChest_TrySpawn, IntRange, ItemDrop

### Community 79 - "EquippedStatModifierPlugin"
Cohesion: 0.40
Nodes (5): ConfigEntry, KeyCode, ManualLogSource, EquippedStatModifierPlugin, Instance

### Community 80 - ".Postfix_UpgradeLevelClientRpc"
Cohesion: 0.38
Nodes (5): HarmonyPatch, HarmonyPostfix, Player, PlayerStats, Patches

### Community 81 - "AntiCheatBypassPatches"
Cohesion: 0.15
Nodes (4): AntiCheatBypassPatches, List, AntiCheatBypassMod, Violation

### Community 82 - "Patch_PlayerAwake"
Cohesion: 0.40
Nodes (3): Patch_PlayerAwake, Patch_XPGainBlocked, Player

### Community 83 - "LootAndExpModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootAndExpModPlugin

### Community 84 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 85 - "AutoPickupModPlugin"
Cohesion: 0.50
Nodes (3): AutoPickupModPlugin, ConfigEntry, ManualLogSource

### Community 86 - "Patch_PlayerAwake"
Cohesion: 0.40
Nodes (3): Patch_PlayerAwake, Patch_XPGainBlocked, Player

### Community 87 - "NavMeshFixModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, NavMeshFixModPlugin

### Community 89 - "AntiCheatBypassModPlugin"
Cohesion: 0.52
Nodes (3): AntiCheatBypassModPlugin, Harmony, ManualLogSource

### Community 118 - "EquippedStatModifierModule.cs"
Cohesion: 0.15
Nodes (12): EquippedStatModifier.UI, EquipmentStatEditor.Core, EquippedStatModifier.Core, EquipmentStatEditor.UI, il2cppinterop_runtime_interoptypes, system_globalization, system_io, system_linq (+4 more)

## Knowledge Gaps
- **253 isolated node(s):** `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk`, `net6.0`, `Microsoft.NET.Sdk` (+248 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 631 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **26 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModModuleBase` connect `ModModuleBase` to `Memory`, `PerfectParryModule`, `ChestAndInventoryModule`, `.IsTargetPlayer`, `AutoBackupSaveModule`, `unityengine`, `HellModeModule`, `ModPackUI`, `.DrawToggle`, `UpgradeBonusStatIsNotRandomModule`, `CarryWeightModule`, `NoClickPickupModule`, `SkillPointMultiplierModule`, `LevelCapModule`, `ExpModule`, `RelationshipModule`, `LootModV2Module`, `AlwaysRegenModule`, `DimraethModPackPlugin`, `WeatherControlModule`, `PetCargoModule`?**
  _High betweenness centrality (0.091) - this node is a cross-community bridge._
- **Why does `EquippedStatModifierModule` connect `Memory` to `ModModuleBase`, `EquippedStatModifierModule.cs`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._
- **Why does `PerfectParryModule` connect `PerfectParryModule` to `.Load`, `unityengine`, `ModModuleBase`, `.DrawToggle`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **What connects `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _253 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.06342072409488139 - nodes in this community are weakly interconnected._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.07423423423423424 - nodes in this community are weakly interconnected._
- **Should `MinimapBehaviour` be split into smaller, more focused modules?**
  _Cohesion score 0.05593607305936073 - nodes in this community are weakly interconnected._