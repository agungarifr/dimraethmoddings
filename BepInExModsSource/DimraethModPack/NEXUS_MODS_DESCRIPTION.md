# Dimraeth QoL & Progression ModPack

The definitive all-in-one enhancement pack for Dimraeth — 13 modular features in a single optimized DLL with an in-game configuration menu.

---

## ✦ Overview

Tired of juggling multiple separate plugins, duplicate Harmony hooks, desyncs, and restarting the game just to change values in text files?

**Dimraeth ModPack** unifies 13 essential quality-of-life, gameplay, loot, and system enhancements into **one single high-performance DLL** (`DimraethModPack.dll`) with a single central configuration file (`com.custom.dimraethmodpack.cfg`).

Press **`** (BackQuote / Tilde) or **F8** at any time during gameplay to open the in-game GUI: toggle modules, adjust precision steppers, configure filters, or hot-reload your config directly!

---

## ✦ Why This ModPack? (Stability & Performance)

- **1 Unified DLL & Config File:** Replaces loose standalone mods with a single coordinated, conflict-free plugin.
- **Zero Engine Crashes & Native Safety:** Built specifically for Unity IL2CPP with null-safety checks, exception shields, and object lifecycle guards to prevent crashes.
- **Non-Compounding Loot Scaling:** Temporary drop range multiplication with instant state restoration prevents exponential loot duplication glitches and memory overflow.
- **Smooth Performance & Zero Disk Hitching:** Pre-cached file checks eliminate micro-stutters during combat and monster spawns.
- **In-Game HUD & Diagnostics:** Real-time display of current FPS, 1% Lows, stutter spikes, and managed RAM usage inside the menu.
- **Live Hot-Reload Button (↻ Reload Disk CFG):** Tweak values in your `.cfg` file using Notepad and reload them instantly in-game without restarting.

---

## ✦ Included Modules & Core Features

### 1. 🗡️ Stats Category

- **Always Regen:**
  - Continuous passive HP and Stamina regeneration.
  - Configurable regeneration rate multipliers (1x to 100x).
  - **Infinite Sprint:** Run across the world without consuming stamina.
  - **No Regen Lockout:** Eliminates regeneration delay after casting spells or receiving damage.
  - Continuous 10Hz tick regeneration for the active player.

- **Custom Stats:**
  - Custom multipliers for character base stats: Attack Speed, Critical Strike Chance, Max Stamina, Max Concentration (Mana), and Max Health.
  - **Spell Haste:** Modify cast speed with precision `±1` and `±10` dual-speed stepper buttons.
  - Option to restrict changes strictly to the local player character.

- **Carry Weight:**
  - Scale maximum inventory carry capacity with a custom multiplier (up to 100x).
  - Optional **Unlimited Carry Weight** toggle (sets capacity to 1,000,000) so hoarders never get encumbered.

- **Upgrade Bonus Stat Is Not Random (Forge Milestone Planner):**
  - Eliminate blacksmith RNG when upgrading equipment and runes at the Forge.
  - Pre-select guaranteed bonus stat rolls for milestones: **+3**, **+6**, **+9**, and **+12** using a complete A–Z alphabetical stat picker.
  - **Instant Next Roll Override:** Direct-fire mode to force a specific stat on your next upgrade roll, then automatically reset.
  - **Duplicate Fallback Safety:** Automatically falls back to vanilla RNG if the chosen stat is already present on the item, preserving affix integrity.

---

### 2. 🎮 Gameplay Category

- **No-Click Pickup (AutoPickup / Loot Vacuum):**
  - Automatically vacuums ground items, equipment, runes, and gold directly into your inventory.
  - **Configurable Detection Radius:** From 1m up to 50m.
  - **Dual Modes:** Continuous background vacuum or Manual Keypress trigger (`Space`, `V`, `F`, `G`, `Alt`, `Ctrl`, etc.).
  - **Category Filters:** Toggle pickup independently for Equipment/Runes, Materials/Resources, Consumables (Food/Potions), and Recipes/Other.
  - **Minimum Rarity Threshold:** Filter equipment drops by rarity tier (Common+, Uncommon+, Rare+, Mythical+, Heroic+, Ancient).
  - Zero-allocation buffers with retry cooldowns to eliminate stutter when inventory is full.

- **Perfect Parry:**
  - Extends the timing window for parrying (vanilla 0.08s up to 5.0s).
  - **Always Perfect Parry On Hold:** Guarantee perfect parries whenever blocking.
  - **Multi-Hit Parry:** Allows chaining parries against rapid multi-hit attacks.
  - **Zero Stamina Drain:** Completely negates stamina cost on successful blocks and parries.
  - Full support for Minotaur (*Retaliation*), Human (*Parry*), and Rogue (*Vanish*).

- **Skill Point Multiplier:**
  - Multiplies the skill points granted on each level-up (vanilla: 2 per level, configurable 1x–10x).

---

### 3. 📦 Loot Category

- **Loot Mod V2:**
  - Independent drop rate multipliers for **Monster Drops**, **Dropped Gold**, **Harvesting Nodes** (Wood, Stone, Ore, Plants), and **Map Interactables**.
  - Properly scales World Containers, starter chests, and **Dungeon Chests**.
  - **100% Guaranteed Drops:** Optional toggle for guaranteed monster loot drops.
  - **Force Highest Rarity:** Equipment drops roll the highest possible tier permitted by the drop table.
  - Non-compounding state restoration protects static game assets from exponential multiplication.

- **Any Chest & Inventory:**
  - Expand storage chest capacity up to **216 slots** (supported on all containers: Pinewood, Oak, Cabinets, Barrels, and Guildhall/Rahaner chests).
  - Expand player backpack inventory slots up to **180 slots**.
  - Safe Netcode guards protect against inventory desyncs.

---

### 4. ⚙️ System Category

- **Hell Mode:**
  - High-difficulty combat mode for players seeking a true endgame challenge.
  - Configurable multipliers for Monster HP (up to 20x), Monster Attack Damage (up to 10x), Movement Speed, and Combat Pace.
  - **Bonus Level Over Player:** Dynamically scales enemy levels above the player character.
  - **Elite Empowerment Chance:** Multiplies empowered elite monster spawn rates.
  - Automatic world-name detection with zero disk I/O hitching during gameplay.

- **EXP Multiplier:**
  - Clean linear multiplier (1x to 100x) for character leveling speed.
  - Multiplies combat kills, quests, deeds, and pet experience.
  - Fixed math prevents double-scaling or exponential runaway leveling bugs.

- **Relationship Multiplier:**
  - Accelerates NPC friendship, romance, opinion, and value alignment gains (up to 20x).
  - Visual particle safety clamp prevents spawning thousands of floating icons that previously caused game freezes during dialogue.

- **Auto Backup Save:**
  - Snapshots your Characters, Worlds, and Settings save files to a timestamped folder every time you quit the game.
  - Rolling history (configurable, default last 10) stored outside the Steam Cloud-synced save folder, with change-detection so repeats are skipped.
  - Purely additive — originals are never touched; restore by copying a snapshot folder back.

---

## ✦ Controls & Configuration

- **` (BackQuote / Tilde)** or **F8**: Toggle the In-Game Configuration Menu.
- **Escape**: Close the In-Game Menu.
- **↻ Reload Disk CFG**: Click in the top-right of the menu to instantly reload changes made externally in `com.custom.dimraethmodpack.cfg`.
- **Config File Location:** `Dimraeth/BepInEx/config/com.custom.dimraethmodpack.cfg`

---

## ✦ Installation

1. Install **BepInEx 6 (IL2CPP)** for Dimraeth (Bleeding Edge Build #788+ recommended).
2. Download and extract `DimraethModPack.dll`.
3. Place `DimraethModPack.dll` into your `Dimraeth/BepInEx/plugins/` folder.
4. **Note:** If you have older standalone DLL versions of any included mods (such as standalone LootMod, AlwaysRegen, or CarryWeight), remove them to prevent duplicate hooking.
5. Launch the game, press **`** or **F8**, and enjoy!

---

## ✦ Uninstallation

Simply delete `DimraethModPack.dll` from your `BepInEx/plugins/` folder. Your vanilla save files remain completely safe and valid.
