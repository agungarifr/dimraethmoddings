# DimraethModPack

A modular Quality of Life (QoL) and Progression ModPack for **Dimraeth** built on BepInEx 6 (IL2CPP) and Harmony.

## Features

- **13 Integrated Modules** in a single optimized DLL (`DimraethModPack.dll`).
- **In-Game GUI Menu**: Press **`** (BackQuote / Tilde) or **F8** to open the mod manager menu.
- **Diagnostics & Profiler**: Real-time FPS, 1% lows, frame-time spikes, and RAM usage monitoring.
- **Config Hot-Reload**: Live disk config reload button in-game.

### Modules

1. **Stats**
   - **Always Regen**: Continuous HP & stamina regeneration, configurable multipliers, infinite sprint, no regen lockout.
   - **Carry Weight**: Configurable inventory carry capacity multiplier or infinite carry capacity.
   - **Custom Stats**: Base stat multipliers (Health, Stamina, Mana, Attack Speed, Spell Haste, Crit Chance).
   - **Upgrade Bonus Stat Is Not Random**: Guaranteed forge upgrade milestone bonus rolls (+3, +6, +9, +12) or instant next-roll override.

2. **Gameplay**
   - **No-Click Pickup**: Configurable radius auto-vacuum or keypress loot pickup with category & rarity filtering.
   - **Perfect Parry**: Extended parry window, multi-hit parry chaining, zero stamina drain on parry, and always-parry mode.
   - **Skill Point Multiplier**: Multiply skill points gained per level-up (vanilla: 2 per level).

3. **Loot**
   - **Chest & Inventory Scaling**: Expand player inventory grid and storage chest slots.
   - **Loot Mod V2**: Multipliers for monster drops and container item quantities with safety guards. Recipes (`Recipe*` items) are excluded from all multipliers and always drop exactly 1.
   - **Equipment Drop Count (nested form)** *(2026-09-30)*: Per-type equipment (rune) drop counts for monsters, chest boxes, and quest/rewards — absolute count (exactly N pieces). Override chain: **Set+Slot > Set > Slot > Global** (0 = inherit). Extra pieces are independently re-rolled at the max rarity & max stars vanilla permits for that drop. This form is authoritative for equipment (`EquipmentDropCount*` config keys); the old `RuneDropCount x ItemMultiplier` coupling is disabled.

4. **System**
   - **Exp Multiplier**: Configurable combat and discovery experience gain multipliers.
   - **Hell Mode**: Challenge difficulty modifiers for increased enemy aggression, health, and damage.
   - **Relationship Mod**: Friendship/affinity gain multipliers with NPCs.
   - **Auto Backup Save**: Snapshot save files to a timestamped folder every time you quit the game (keeps the last N, skipped when unchanged).

## Building from Source

### Prerequisites
- [.NET 6.0+ SDK](https://dotnet.microsoft.com/download)
- Dimraeth game installation with BepInEx 6 (IL2CPP)

### Build
```bash
dotnet build DimraethModPack.csproj -c Release
```

The compiled `DimraethModPack.dll` will be placed in the game's `BepInEx/plugins/` directory.

## License
MIT
