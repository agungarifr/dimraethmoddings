# Graph Report - BepInExModsSource  (2026-09-23)

## Corpus Check
- 135 files · ~38,170 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 235 file(s) not represented in the graph (top: .cache 100, .dll 55, .pdb 23)

## Summary
- 761 nodes · 1557 edges · 52 communities (32 shown, 20 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 3 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- HarmonyPatch
- HarmonyPatch
- OverlayGui
- system
- NativeBytePatcher
- DayNightToggleModPlugin
- PerfectParryMod.cs
- AttackSpeedMod.cs
- RahanerChestModPlugin
- global_system
- ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.cs
- NativeBytePatcher
- HellModeMod.csproj
- DayNightToggleMod.cs
- AutoPickupModPlugin
- HellModeLevel75Freebuff.cs
- bepinex_logging
- bepinex_unity_il2cpp
- NavMeshFixMod.cs
- LootPatches.cs
- AlwaysRegenMod.cs
- CumisOverlay
- .Postfix
- Patch_ItemDropCheck
- ConfigurableLevelCapFreebuff
- ConfigurableLevelCapFreebuff
- BasePlugin
- bepinex_bootstrap
- DeedUnlockerMod
- DeedUnlockerPlugin
- AlwaysRegenModPlugin
- CarryWeightModPlugin
- LootAndExpModPlugin
- AlwaysRegenMod.csproj
- AttackSpeedMod.csproj
- AutoPickupMod.csproj
- CarryWeightMod.csproj
- ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.csproj
- ConfigurableLevelCapFreebuff_Debug/ConfigurableLevelCapFreebuff.csproj
- CumisModifier.csproj
- DayNightToggleMod.csproj
- DeedUnlockerMod.csproj
- HellModeLevel75Freebuff.csproj
- LootAndExpMod.csproj
- NavMeshFixMod.csproj
- PerfectParryMod.csproj
- RahanerChestMod.csproj
- LootModV2Plugin
- NavMeshFixModPlugin
- LootModV2.csproj
- il2cppinterop_runtime_injection

## God Nodes (most connected - your core abstractions)
1. `OverlayGui` - 21 edges
2. `HellModeLevel75FreebuffPlugin` - 20 edges
3. `HellModeModPlugin` - 18 edges
4. `DayNightToggleModPlugin` - 16 edges
5. `RahanerChestModPlugin` - 13 edges
6. `NativeBytePatcher` - 11 edges
7. `NativeBytePatcher` - 11 edges
8. `NativeBytePatcher` - 9 edges
9. `Patch_WorldLifecycle` - 9 edges
10. `Patch_WorldLifecycle` - 9 edges

## Surprising Connections (you probably didn't know these)
- `Equipment rarity tertinggi (BARU)` --references--> `Patch_MaxEquipmentRarity`  [INFERRED]
  LootModV2/README.md → LootModV2/Patches/LootPatches.cs

## Import Cycles
- None detected.

## Communities (52 total, 20 thin omitted)

### Community 0 - "HarmonyPatch"
Cohesion: 0.07
Nodes (34): ConfigEntry, DateTime, Difficulty, DifficultyManager, DifficultySlider, FinalSelection, HarmonyPatch, HarmonyPostfix (+26 more)

### Community 1 - "HarmonyPatch"
Cohesion: 0.08
Nodes (31): ConfigEntry, DateTime, Difficulty, DifficultyManager, DifficultySlider, FinalSelection, HarmonyPatch, HarmonyPostfix (+23 more)

### Community 2 - "OverlayGui"
Cohesion: 0.21
Nodes (8): ConfigEntryBase, Color, Type, OverlayGui, Dictionary, Exception, GUIStyle, Texture2D

### Community 4 - "NativeBytePatcher"
Cohesion: 0.07
Nodes (27): ConfigurableLevelCapPlugin, Instance, MethodTarget, NativeBytePatcher, AlreadyAtCap, ConfigEntry, DllImport, HashSet (+19 more)

### Community 5 - "DayNightToggleModPlugin"
Cohesion: 0.08
Nodes (22): Color, ConfigEntry, Key, KeyCode, ManualLogSource, PlayerUpdate, DayNightToggleModPlugin, CurrentMode (+14 more)

### Community 6 - "PerfectParryMod.cs"
Cohesion: 0.10
Nodes (23): BaseSpell, PerfectParryMod, DamageType, ParryPrefab, ConfigEntry, ManualLogSource, ObjectsCommon, Patch_ParryPrefab_HandleLocalIncomingHit (+15 more)

### Community 7 - "AttackSpeedMod.cs"
Cohesion: 0.11
Nodes (18): CustomStatModPlugin, Patch_CalculateAttackSpeed, Patch_CalculateBaseAttackSpeed, Patch_CalculateBaseMaxConcentration, Patch_CalculateBaseMaxHealth, Patch_CalculateBaseMaxStamina, Patch_CalculateBaseSpellHaste, Patch_CalculateCriticalRate (+10 more)

### Community 8 - "RahanerChestModPlugin"
Cohesion: 0.13
Nodes (15): Chest, EventTrigger, GridLayoutGroup, Inventory, InventoryPaneView, ConfigEntry, HarmonyPatch, HarmonyPostfix (+7 more)

### Community 9 - "global_system"
Cohesion: 0.39
Nodes (7): global_system, global_system_collections_generic, global_system_io, global_system_linq, global_system_net_http, global_system_threading, global_system_threading_tasks

### Community 10 - "ConfigurableLevelCapFreebuff/ConfigurableLevelCapFreebuff.cs"
Cohesion: 0.11
Nodes (13): Patch_ExpMultiplier, Patch_PlayerAwake, Patch_XPGainBlocked, Player, Patch_ExpMultiplier, Patch_PlayerAwake, Patch_XPGainBlocked, Player (+5 more)

### Community 11 - "NativeBytePatcher"
Cohesion: 0.16
Nodes (11): DllImport, IntPtr, UIntPtr, GameConfigCapPatcher, NativeBytePatcher, maxAttributeLevel, maxLevel, offset (+3 more)

### Community 13 - "DayNightToggleMod.cs"
Cohesion: 0.14
Nodes (11): bepinex, RahanerChestMod, DayNightToggleMod, DayNightLightAdjuster, Patch_DayNightLightAdjuster_Update, Patch_EnvironmentColor_Update, EnvironmentColor, unityengine_eventsystems (+3 more)

### Community 14 - "AutoPickupModPlugin"
Cohesion: 0.21
Nodes (8): AutoPickupModPlugin, Patch_PlayerUpdate_Update, ConfigEntry, Key, KeyCode, ManualLogSource, Player, PlayerUpdate

### Community 15 - "HellModeLevel75Freebuff.cs"
Cohesion: 0.15
Nodes (13): bepinex_configuration, HellModeMod, AutoPickupMod, HellModeLevel75Freebuff, CumisModifier, Patch_WorldSelection, Patch_Progression, Patch_WorldSelection (+5 more)

### Community 16 - "bepinex_logging"
Cohesion: 0.13
Nodes (11): bepinex_logging, DeedUnlockerMod, LootModV2, DeedDefinition, DeedTierConfiguration, Patch_DeedLootCaps, Patch_IsTierUnlocked, Patch_UnlockedTierForDeed (+3 more)

### Community 17 - "bepinex_unity_il2cpp"
Cohesion: 0.15
Nodes (8): bepinex_unity_il2cpp, LootAndExpMod, Player, Patch_AddXPToPet, Patch_ApplyDropChanceModifiers, Patch_CalculateGoldDrop, Patch_CalculateXPGained, Patch_GrantXPToPlayer

### Community 18 - "NavMeshFixMod.cs"
Cohesion: 0.15
Nodes (9): NavMeshFixMod, LaughingSlashesPrefab, MonsterMovement, ObjectsCommon, Patch_LaughingSlashesPrefab_CleanupMovementState, Patch_MonsterMovement_Start, Patch_ObjectsCommon_SafeWarp, unityengine_ai (+1 more)

### Community 19 - "LootPatches.cs"
Cohesion: 0.07
Nodes (24): LootModV2.Patches, InteractableRewardEntry, HashSet, List, MonsterUtils, HarvestMultiplication, Mult, Patch_ApplyDropChanceModifiers (+16 more)

### Community 20 - "AlwaysRegenMod.cs"
Cohesion: 0.15
Nodes (9): Patch_CalculateHealthRegeneration, Patch_CalculateSprintingCostPerSecond, Patch_CalculateStaminaRegeneration, Patch_CommonsZeroPointOneSecond, Patch_SpellLibrarySetRegenTimer, ObjectsCommon, Player, AlwaysRegenMod (+1 more)

### Community 21 - "CumisOverlay"
Cohesion: 0.31
Nodes (4): Key, KeyCode, CumisOverlay, MonoBehaviour

### Community 22 - ".Postfix"
Cohesion: 0.40
Nodes (4): Patch_CalculateEncumbrance, Attributes, ObjectsCommon, Player

### Community 23 - "Patch_ItemDropCheck"
Cohesion: 0.50
Nodes (3): HashSet, MonsterUtils, Patch_ItemDropCheck

### Community 24 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 25 - "ConfigurableLevelCapFreebuff"
Cohesion: 0.13
Nodes (14): Cara Kerja, Cara Mod Ini Menghapus Cap, ConfigurableLevelCapFreebuff, EXP Multiplier 10x (leveling cepat), Instalasi, Keamanan, Konfigurasi, Kostumerisasi (+6 more)

### Community 26 - "BasePlugin"
Cohesion: 0.39
Nodes (5): BasePlugin, List, ManualLogSource, CumisModifierPlugin, PluginEntry

### Community 28 - "DeedUnlockerMod"
Cohesion: 0.25
Nodes (7): Cara Kerja, Catatan, DeedUnlockerMod, Instalasi, Konfigurasi, Masalah yang Diperbaiki, Verifikasi Log

### Community 29 - "DeedUnlockerPlugin"
Cohesion: 0.33
Nodes (5): ConfigEntry, ManualLogSource, Rarity, DeedUnlockerPlugin, Stars

### Community 30 - "AlwaysRegenModPlugin"
Cohesion: 0.50
Nodes (3): AlwaysRegenModPlugin, ConfigEntry, ManualLogSource

### Community 31 - "CarryWeightModPlugin"
Cohesion: 0.50
Nodes (3): CarryWeightModPlugin, ConfigEntry, ManualLogSource

### Community 33 - "LootAndExpModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootAndExpModPlugin

### Community 48 - "LootModV2Plugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, LootModV2Plugin

### Community 49 - "NavMeshFixModPlugin"
Cohesion: 0.50
Nodes (3): ConfigEntry, ManualLogSource, NavMeshFixModPlugin

## Knowledge Gaps
- **93 isolated node(s):** `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk`, `AttackSpeedMod`, `net6.0` (+88 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 220 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `HellModeLevel75FreebuffPlugin` connect `HarmonyPatch` to `BasePlugin`, `NativeBytePatcher`, `HellModeLevel75Freebuff.cs`?**
  _High betweenness centrality (0.064) - this node is a cross-community bridge._
- **Why does `HellModeModPlugin` connect `HarmonyPatch` to `BasePlugin`, `HellModeLevel75Freebuff.cs`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **Why does `OverlayGui` connect `OverlayGui` to `.RebuildBuffers`, `CumisOverlay`, `HellModeLevel75Freebuff.cs`?**
  _High betweenness centrality (0.046) - this node is a cross-community bridge._
- **What connects `AlwaysRegenMod`, `net6.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _93 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.06556948798328109 - nodes in this community are weakly interconnected._
- **Should `HarmonyPatch` be split into smaller, more focused modules?**
  _Cohesion score 0.0776255707762557 - nodes in this community are weakly interconnected._
- **Should `system` be split into smaller, more focused modules?**
  _Cohesion score 0.08502415458937199 - nodes in this community are weakly interconnected._