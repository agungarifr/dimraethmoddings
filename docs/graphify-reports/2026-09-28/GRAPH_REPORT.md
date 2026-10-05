# Graph Report - BepInExModsSource  (2026-09-28)

## Corpus Check
- 201 files · ~91,075 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 283 file(s) not represented in the graph (top: .cache 119, .dll 67, .pdb 27)

## Summary
- 1731 nodes · 3480 edges · 111 communities (85 shown, 26 thin omitted)
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
- PerfectParryMod.cs
- global_system
- EquippedStatModifierModule.cs
- HarmonyPostfix
- DayNightToggleModPlugin
- LootPatches.cs
- .DrawToggle
- ChestAndInventoryModule
- HellModeLevel75Freebuff.cs
- .IsLocalPlayer
- AttackSpeedMod.cs
- EquippedStatModifierUI
- harmonylib
- .IsTargetPlayer
- AutoBackupSaveModule
- RuneEntry
- RahanerChestModPlugin
- HellModeModule
- UpgradeBonusStatIsNotRandomModule
- bepinex_bootstrap
- RuneMemory
- EditorEngine
- Dimraeth - Complete Equipment Upgrade & Enhancement Guide
- EquipmentStatEditor/Core/RuneMemory.cs
- ModPackManagerBehaviour
- Dimraeth QoL & Progression ModPack
- NativeBytePatcher
- DiagnosticsManager
- SkillPointMultiplierModule
- AntiCheatBypassModule
- ConfigurableLevelCapFreebuff
- ConfigurableLevelCapFreebuff
- EquipmentStatEditorPlugin
- BasePlugin
- Player
- UnrandomizerUI
- NoClickPickupModule
- ExpModule
- Patches
- RelationshipModule
- LootModV2Module
- ModPackUI
- .Prefix
- Patch_PlayerUpdate_Update
- AlwaysRegenModule
- UpgradeBonusStatIsNotRandomPlugin
- ObjectsCommon
- DeedUnlockerMod
- NavMeshFixMod.cs
- .Load
- DimraethModPack
- Equipment Stat Editor
- Patch_CalculateSprintingCostPerSecond
- UnrandomizerUI.cs
- Patches
- RuneSnapshot
- StatCatalog
- .Postfix
- DeedUnlockerPlugin
- .Postfix
- EquippedStatModifierPlugin
- AutoPickupModPlugin
- Patch_ItemDropCheck
- Patch_PlayerAwake
- LootModV2Plugin
- NavMeshFixModPlugin
- .Prefix
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
- ModModuleBase
- HarmonyPrefix
- CarryWeightModule
- DayNightToggleMod.cs
- Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)
- Patch_CreateNewStorageData
- .Multiply
- 2026-09-28
- .Prefix
- Patch_PlayerAwake
- .Postfix
- unityengine_ui
- Patch_InteractableReward
- LootAndExpModPlugin
- Patch_ItemDropCheck
- .ValidateLiveEdit

## God Nodes (most connected - your core abstractions)
1. `ModModuleBase` - 33 edges
2. `MinimapBehaviour` - 29 edges
3. `RuneEditorUI` - 28 edges
4. `EditorEngine` - 27 edges
5. `RuneEntry` - 26 edges
6. `PerfectParryModule` - 24 edges
7. `AutoBackupSaveModule` - 24 edges
8. `Memory` - 22 edges
9. `Draw` - 21 edges
10. `DimraethModPack.Core` - 20 edges

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

## Communities (111 total, 26 thin omitted)

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

### Community 8 - "PerfectParryMod.cs"
Cohesion: 0.10
Nodes (23): PerfectParryMod, BaseSpell, ConfigEntry, DamageType, ManualLogSource, ObjectsCommon, ParryPrefab, RetaliationPrefab (+15 more)

### Community 9 - "global_system"
Cohesion: 0.35
Nodes (7): global_system, global_system_collections_generic, global_system_io, global_system_linq, global_system_net_http, global_system_threading, global_system_threading_tasks

### Community 10 - "EquippedStatModifierModule.cs"
Cohesion: 0.15
Nodes (12): EquippedStatModifier.UI, EquipmentStatEditor.Core, EquippedStatModifier.Core, EquipmentStatEditor.UI, il2cppinterop_runtime_interoptypes, system_globalization, system_io, system_linq (+4 more)

### Community 11 - "HarmonyPostfix"
Cohesion: 0.24
Nodes (6): HarmonyPostfix, List, Patch_CalculateGoldDrop, Patch_DropChance, Patch_HarvestBonus, Patch_HarvestCalculate

### Community 12 - "DayNightToggleModPlugin"
Cohesion: 0.08
Nodes (22): Color, ConfigEntry, Key, KeyCode, ManualLogSource, PlayerUpdate, DayNightToggleModPlugin, CurrentMode (+14 more)

### Community 13 - "LootPatches.cs"
Cohesion: 0.17
Nodes (7): LootModV2.Patches, LootModV2, PluginInfo, InteractableRewardEntry, Patch_ApplyDropChanceModifiers, Patch_GoldDrop, Patch_InteractableReward

### Community 14 - ".DrawToggle"
Cohesion: 0.17
Nodes (10): Action, ConfigEntry, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle, GUIStyle (+2 more)

### Community 15 - "ChestAndInventoryModule"
Cohesion: 0.10
Nodes (20): Chest, ConfigEntry, ConfigFile, EventTrigger, GridLayoutGroup, Harmony, HarmonyPatch, HarmonyPostfix (+12 more)

### Community 16 - "HellModeLevel75Freebuff.cs"
Cohesion: 0.07
Nodes (28): Patch_SpellLibrarySetRegenTimer, bepinex, bepinex_logging, bepinex_unity_il2cpp, Patch_ExpMultiplier, Patch_ExpMultiplier, RahanerChestMod, AlwaysRegenMod (+20 more)

### Community 17 - ".IsLocalPlayer"
Cohesion: 0.14
Nodes (15): Il2CppObjectBase, IntPtr, PlayerIdentity, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon, Player (+7 more)

### Community 18 - "AttackSpeedMod.cs"
Cohesion: 0.11
Nodes (18): CustomStatModPlugin, Patch_CalculateAttackSpeed, Patch_CalculateBaseAttackSpeed, Patch_CalculateBaseMaxConcentration, Patch_CalculateBaseMaxHealth, Patch_CalculateBaseMaxStamina, Patch_CalculateBaseSpellHaste, Patch_CalculateCriticalRate (+10 more)

### Community 19 - "EquippedStatModifierUI"
Cohesion: 0.14
Nodes (11): Rune, EquippedStatModifierBehaviour, Color, GUIStyle, List, Player, Stat, Texture2D (+3 more)

### Community 20 - "harmonylib"
Cohesion: 0.20
Nodes (14): bepinex_configuration, DimraethModPack.Modules.Loot, DimraethModPack.Core, DimraethModPack.UI, DimraethModPack.Modules.SystemMod, DimraethModPack, AutoPickupMod, DimraethModPack.Modules.Gameplay (+6 more)

### Community 21 - ".IsTargetPlayer"
Cohesion: 0.17
Nodes (15): Attributes, ConfigEntry, ConfigFile, Harmony, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, ObjectsCommon (+7 more)

### Community 22 - "AutoBackupSaveModule"
Cohesion: 0.10
Nodes (14): Action, ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, List (+6 more)

### Community 23 - "RuneEntry"
Cohesion: 0.15
Nodes (14): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneConfigIO, RuneEntry (+6 more)

### Community 24 - "RahanerChestModPlugin"
Cohesion: 0.13
Nodes (15): Chest, ConfigEntry, EventTrigger, GridLayoutGroup, HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Inventory (+7 more)

### Community 25 - "HellModeModule"
Cohesion: 0.15
Nodes (9): ConfigEntry, ConfigFile, Harmony, HellModeModule, Category, Description, Instance, IsHellActive (+1 more)

### Community 26 - "UpgradeBonusStatIsNotRandomModule"
Cohesion: 0.12
Nodes (16): ConfigEntry, ConfigFile, Dictionary, GUIStyle, Harmony, HarmonyPatch, HarmonyPrefix, Rune (+8 more)

### Community 28 - "RuneMemory"
Cohesion: 0.19
Nodes (9): Dictionary, Type, Action, Dictionary, Il2CppObjectBase, IntPtr, Player, Type (+1 more)

### Community 29 - "EditorEngine"
Cohesion: 0.23
Nodes (7): HashSet, Il2CppObjectBase, List, Player, EditorEngine, PendingVerify, PendingVerify

### Community 30 - "Dimraeth - Complete Equipment Upgrade & Enhancement Guide"
Cohesion: 0.09
Nodes (21): 1. Maximum Upgrade Level, 1. The Seed Formula (GetUpgradeSeed), 2. Candidate Filtering (GetRandomSecondaryStat), 2. Primary Stat Scaling (Every Level), A. All Existing Secondary Stats Level Up, B. A New Random Secondary Stat is Rolled, Complete Equipment Stat Affix Catalog (Runes.Stat), Core Offensive & Penetration (+13 more)

### Community 31 - "EquipmentStatEditor/Core/RuneMemory.cs"
Cohesion: 0.19
Nodes (21): BindLogger(), Action, Il2CppObjectBase, IntPtr, Player, Rune, Type, DataPtr() (+13 more)

### Community 32 - "ModPackManagerBehaviour"
Cohesion: 0.22
Nodes (4): ModPackManagerBehaviour, EditorController, MonoBehaviour, UnrandomizerBehaviour

### Community 33 - "Dimraeth QoL & Progression ModPack"
Cohesion: 0.11
Nodes (16): 1. 🗡️ Stats Category, 2. 🎮 Gameplay Category, 3. 📦 Loot Category, 4. ⚙️ System Category, ✦ Controls & Configuration, Dimraeth QoL & Progression ModPack, ✦ Included Modules & Core Features, ✦ Installation (+8 more)

### Community 34 - "NativeBytePatcher"
Cohesion: 0.16
Nodes (11): DllImport, IntPtr, UIntPtr, GameConfigCapPatcher, NativeBytePatcher, maxAttributeLevel, maxLevel, offset (+3 more)

### Community 35 - "DiagnosticsManager"
Cohesion: 0.06
Nodes (27): ConfigEntry, DiagnosticsManager, AvgFps, CurrentFps, ManagedRamMb, MaxSpikeDurationMs, OnePercentLowFps, ProcessWorkingSetMb (+19 more)

### Community 36 - "SkillPointMultiplierModule"
Cohesion: 0.12
Nodes (14): ConfigEntry, ConfigFile, GUIStyle, Harmony, HarmonyPatch, HarmonyPostfix, Player, PlayerStats (+6 more)

### Community 37 - "AntiCheatBypassModule"
Cohesion: 0.12
Nodes (12): ConfigEntry, ConfigFile, GUIStyle, Harmony, List, Type, AntiCheatBypassModule, Category (+4 more)

### Community 38 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 39 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 40 - "EquipmentStatEditorPlugin"
Cohesion: 0.17
Nodes (9): Action, Harmony, SaveHooks, ConfigEntry, Harmony, KeyCode, ManualLogSource, EquipmentStatEditorPlugin (+1 more)

### Community 41 - "BasePlugin"
Cohesion: 0.22
Nodes (7): AlwaysRegenModPlugin, ConfigEntry, ManualLogSource, BasePlugin, CarryWeightModPlugin, ConfigEntry, ManualLogSource

### Community 42 - "Player"
Cohesion: 0.40
Nodes (3): Player, Patch_AddXPToPet, Patch_GrantXPToPlayer

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

### Community 48 - "LootModV2Module"
Cohesion: 0.15
Nodes (10): ConfigEntry, ConfigFile, GUIStyle, Harmony, LootModV2Module, Category, Description, Instance (+2 more)

### Community 49 - "ModPackUI"
Cohesion: 0.29
Nodes (6): Color, GUIStyle, List, Texture2D, Vector2, ModPackUI

### Community 50 - ".Prefix"
Cohesion: 0.24
Nodes (8): Dictionary, Rune, Stat, StatCatalog, HarmonyPrefix, Rune, Runes, Stat

### Community 51 - "Patch_PlayerUpdate_Update"
Cohesion: 0.33
Nodes (5): Patch_PlayerUpdate_Update, Key, KeyCode, Player, PlayerUpdate

### Community 52 - "AlwaysRegenModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, AlwaysRegenModule, Category, Description, Instance, Name

### Community 53 - "UpgradeBonusStatIsNotRandomPlugin"
Cohesion: 0.33
Nodes (6): ConfigEntry, Harmony, KeyCode, ManualLogSource, UpgradeBonusStatIsNotRandomPlugin, Instance

### Community 54 - "ObjectsCommon"
Cohesion: 0.29
Nodes (4): Patch_CalculateHealthRegeneration, Patch_CalculateStaminaRegeneration, Patch_CommonsZeroPointOneSecond, ObjectsCommon

### Community 55 - "DeedUnlockerMod"
Cohesion: 0.25
Nodes (7): Cara Kerja, Catatan, DeedUnlockerMod, Instalasi, Konfigurasi, Masalah yang Diperbaiki, Verifikasi Log

### Community 56 - "NavMeshFixMod.cs"
Cohesion: 0.22
Nodes (6): NavMeshFixMod, LaughingSlashesPrefab, MonsterMovement, Patch_LaughingSlashesPrefab_CleanupMovementState, Patch_MonsterMovement_Start, unityengine_ai

### Community 57 - ".Load"
Cohesion: 0.18
Nodes (9): ConfigFile, ConfigEntry, Harmony, KeyCode, List, ManualLogSource, DimraethModPackPlugin, Modules (+1 more)

### Community 58 - "DimraethModPack"
Cohesion: 0.25
Nodes (7): Build, Building from Source, DimraethModPack, Features, License, Modules, Prerequisites

### Community 59 - "Equipment Stat Editor"
Cohesion: 0.25
Nodes (7): Equipment Stat Editor, How values scale, Notes, Per-stat value caps, Stat value caps (the game's real limits), Stats with no roll entry in this build, What it does

### Community 61 - "UnrandomizerUI.cs"
Cohesion: 0.33
Nodes (4): UpgradeBonusStatIsNotRandom.UI, UpgradeBonusStatIsNotRandom.Patches, UpgradeBonusStatIsNotRandom.Core, UpgradeRunePatch

### Community 62 - "Patches"
Cohesion: 0.32
Nodes (5): HarmonyPatch, HarmonyPostfix, MonsterConfiguration, MonsterSetup, Patches

### Community 63 - "RuneSnapshot"
Cohesion: 0.29
Nodes (7): List, Rarity, RuneSet, SlotType, Stars, Stat, RuneSnapshot

### Community 64 - "StatCatalog"
Cohesion: 0.53
Nodes (4): Dictionary, List, Stat, StatCatalog

### Community 65 - ".Postfix"
Cohesion: 0.33
Nodes (4): DeedDefinition, DeedTierConfiguration, Patch_DeedLootCaps, Patch_UnlockedTierForDeed

### Community 66 - "DeedUnlockerPlugin"
Cohesion: 0.33
Nodes (5): ConfigEntry, ManualLogSource, Rarity, Stars, DeedUnlockerPlugin

### Community 67 - ".Postfix"
Cohesion: 0.40
Nodes (4): Patch_CalculateEncumbrance, Attributes, ObjectsCommon, Player

### Community 68 - "EquippedStatModifierPlugin"
Cohesion: 0.40
Nodes (5): ConfigEntry, KeyCode, ManualLogSource, EquippedStatModifierPlugin, Instance

### Community 69 - "AutoPickupModPlugin"
Cohesion: 0.50
Nodes (3): AutoPickupModPlugin, ConfigEntry, ManualLogSource

### Community 70 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 71 - "Patch_PlayerAwake"
Cohesion: 0.40
Nodes (3): Patch_PlayerAwake, Patch_XPGainBlocked, Player

### Community 72 - "LootModV2Plugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootModV2Plugin

### Community 73 - "NavMeshFixModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, NavMeshFixModPlugin

### Community 74 - ".Prefix"
Cohesion: 0.50
Nodes (3): ObjectsCommon, Patch_ObjectsCommon_SafeWarp, Vector3

### Community 95 - "ModModuleBase"
Cohesion: 0.18
Nodes (8): ConfigFile, Harmony, ModModuleBase, Category, Description, Enabled, IsEnabled, Name

### Community 96 - "HarmonyPrefix"
Cohesion: 0.20
Nodes (6): HarmonyPrefix, ItemType, Patch_ChestBasic_SpawnStarter, Patch_StorageData_AddItem, StorageData, StorageSpawn

### Community 97 - "CarryWeightModule"
Cohesion: 0.20
Nodes (8): ConfigEntry, ConfigFile, Harmony, CarryWeightModule, Category, Description, Instance, Name

### Community 98 - "DayNightToggleMod.cs"
Cohesion: 0.22
Nodes (6): DayNightToggleMod, DayNightLightAdjuster, Patch_DayNightLightAdjuster_Update, Patch_EnvironmentColor_Update, EnvironmentColor, unityengine_rendering_universal

### Community 99 - "Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)"
Cohesion: 0.22
Nodes (8): Build & install, Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`), Interactable di map (BARU) — dead body / box / tekan C, Konfigurasi (`BepInEx/config/com.custom.lootmodv2.cfg`), LootModV2 — Loot 10x (Monster + Node Panen + Interactable), Monster (dipertahankan), Node panen (BARU) — stone, wood/log, rumput, bush, plant, dll., Yang diubah vs LootAndExpMod lama

### Community 100 - "Patch_CreateNewStorageData"
Cohesion: 0.25
Nodes (5): HashSet, IntPtr, MonsterUtils, Patch_CreateNewStorageData, Patch_ItemDropCheck

### Community 101 - ".Multiply"
Cohesion: 0.32
Nodes (5): List, HarvestMultiplication, Mult, Patch_HarvestBonusItems, Patch_HarvestCalculateItems

### Community 102 - "2026-09-28"
Cohesion: 0.33
Nodes (5): 2026-09-28, Changelog, Dimraeth ModPack (`DimraethModPack.dll`), Equipment Stat Editor (standalone plugin), How to update

### Community 103 - ".Prefix"
Cohesion: 0.33
Nodes (4): Patch_ReturnRandomRuneData, Patch_MaxEquipmentRarity, Equipment rarity tertinggi (BARU), Rarity&gt;

### Community 104 - "Patch_PlayerAwake"
Cohesion: 0.40
Nodes (3): Patch_PlayerAwake, Patch_XPGainBlocked, Player

### Community 105 - ".Postfix"
Cohesion: 0.60
Nodes (3): Patch_DungeonChest_TrySpawn, IntRange, ItemDrop

### Community 108 - "LootAndExpModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootAndExpModPlugin

### Community 109 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

## Knowledge Gaps
- **233 isolated node(s):** `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk`, `AttackSpeedMod`, `net6.0` (+228 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 566 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **26 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ModModuleBase` connect `ModModuleBase` to `Memory`, `PerfectParryModule`, `.DrawToggle`, `ChestAndInventoryModule`, `harmonylib`, `.IsTargetPlayer`, `AutoBackupSaveModule`, `HellModeModule`, `UpgradeBonusStatIsNotRandomModule`, `SkillPointMultiplierModule`, `AntiCheatBypassModule`, `NoClickPickupModule`, `ExpModule`, `RelationshipModule`, `LootModV2Module`, `ModPackUI`, `AlwaysRegenModule`, `.Load`, `CarryWeightModule`?**
  _High betweenness centrality (0.071) - this node is a cross-community bridge._
- **Why does `ChestAndInventoryModule` connect `ChestAndInventoryModule` to `.Load`, `harmonylib`, `.DrawToggle`, `ModModuleBase`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Why does `NoClickPickupModule` connect `NoClickPickupModule` to `DiagnosticsManager`, `.DrawToggle`, `harmonylib`, `.Load`, `ModModuleBase`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._
- **What connects `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _233 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.06435137895812053 - nodes in this community are weakly interconnected._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.07423423423423424 - nodes in this community are weakly interconnected._
- **Should `MinimapBehaviour` be split into smaller, more focused modules?**
  _Cohesion score 0.05593607305936073 - nodes in this community are weakly interconnected._