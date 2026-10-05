# Dimraeth QoL & Progression ModPack - Nexus Mods Description

You can copy either the **BBCode version** (recommended for Nexus Mods description box) or the **Markdown version** below.

---

## 📋 BBCode Version (Copy this into Nexus Mods Description)

```bbcode
[center]
[size=6][b]Dimraeth QoL & Progression ModPack[/b][/size]
[size=3][i]The all-in-one unified enhancement pack for Dimraeth — 14 modular tweaks in a single DLL with in-game F8 configuration.[/i][/size]
[/center]

[line]

[size=4][b]✦ Overview[/b][/size]
Tired of juggling a dozen separate mods, conflicting configs, and restarting the game just to tweak values? 
[b]Dimraeth ModPack[/b] merges 14 essential quality-of-life, loot, gameplay, and system mods into [b]one single optimized DLL[/b] with a unified config file and a real-time in-game configuration menu.

Press [b]F8[/b] at any time during gameplay to tweak settings, toggle modules, adjust precision steppers, or hot-reload your config directly!

[line]

[size=4][b]✦ Changelog & Improvements vs. Standalone Versions[/b][/size]

If you previously used the standalone versions of these mods, here is what is new and improved in this unified pack:

[list]
[*] [b]1 Unified DLL & Config File:[/b] Replaced 14 separate DLLs and 14 separate config files with a single coordinated DLL ([font=Courier New]DimraethModPack.dll[/font]) and one central configuration file ([font=Courier New]com.custom.dimraethmodpack.cfg[/font]).
[*] [b]Zero Hook Conflicts:[/b] Solved race conditions and conflicting Harmony patches on shared game systems ([font=Courier New]MonsterUtils[/font], [font=Courier New]Storage[/font], [font=Courier New]HarvestClient[/font], [font=Courier New]SceneHandler[/font], [font=Courier New]XP[/font]).
[*] [b]Performance & Stutter Elimination:[/b]
    • [i]Zero Disk I/O Hitching:[/i] Fixed a severe stutter bug where non-Hell worlds performed continuous synchronous [font=Courier New]File.Exists[/font] disk reads on the main thread during combat and monster spawns.
    • [i]Dual-Mode Garbage Collection:[/i] Seamless non-blocking GC during active gameplay prevents combat freezes, while deep RAM purges run during map/scene transitions.
    • [i]Zero-Allocation Loot Vacuum:[/i] Cooldown buffers and category caches are fully pre-allocated to prevent GC spikes while vacuuming ground items.
    • [i]Formula Short-Circuiting:[/i] Bypassed expensive IL2CPP type casting on monsters and NPCs during combat formula checks.
[*] [b]EXP Multiplier Math Fix:[/b] Fixed a double-dipping bug that caused monster kill EXP to square itself (e.g. 5x became 25x). Combat, quest, and deed EXP now strictly scale at your exact chosen multiplier.
[*] [b]Item Type & Rarity Loot Filtering:[/b] Configurable filters to selectively vacuum Equipment/Runes, Materials, Consumables, and Recipes, with a Minimum Rarity threshold (Common up to Ancient).
[*] [b]Unified In-Game Menu (F8):[/b] A completely overhauled, lag-free UI organized into categorized tabs ([b]Stats[/b], [b]Gameplay[/b], [b]Loot[/b], [b]System[/b]).
[*] [b]Live Hot-Reload Button (↻ Reload Disk CFG):[/b] Edit your configuration on disk and reload it instantly in-game without restarting the client.
[*] [b]Dual Precision Steppers (±1 and ±10):[/b] Added fast dual-speed increment buttons [[b]-10[/b]] [[b]-1[/b]] [[b]+1[/b]] [[b]+10[/b]] for [b]Spell Haste[/b], [b]Max Level Cap[/b], and [b]Max Attribute Cap[/b].
[*] [b]Loot System Overhaul & Math Fix:[/b]
    • [i]Fixed Exponential Drop Bug:[/i] Eliminated client-server RPC compounding multipliers that caused mining a single stone node to drop 1,000+ stone.
    • [i]World Containers & Starter Loot:[/i] Standalone versions failed to scale world loot boxes and starter chests. The ModPack now properly multiplies world chests and map storage items.
    • [i]Reward Duplication Safeguard:[/i] Fixed double-scaling between [font=Courier New]ClaimRewards[/font] and [font=Courier New]ApplyReward[/font] on map interaction tags.
    • [i]Dungeon Chests:[/i] Added automatic scaling for dungeon chest drop ranges.
[*] [b]Deed Unlocker Rarity Fix:[/b] Upgraded cycle controls to full enum pickers supporting all 6 rarity tiers (including [b]Ancient[/b]) and 9 stars.
[*] [b]Any Chest & Inventory Safety:[/b] Added boundary guards to prevent storage corruption on crafted chests while supporting expansions up to 216 slots.
[/list]

[line]

[size=4][b]✦ Included Modules[/b][/size]

[b][size=3]1. 🗡️ Stats[/size][/b]
[list]
[*] [b]Always Regen:[/b] Configurable out-of-combat and in-combat HP/MP regeneration.
[*] [b]Custom Stats:[/b] Modify player base stats, including [b]Spell Haste[/b] with precision [±1] and [±10] steppers.
[*] [b]Carry Weight:[/b] Scale your character's max carry capacity to stop hoarders from crawling.
[/list]

[b][size=3]2. 🎮 Gameplay[/size][/b]
[list]
[*] [b]No-Click Pickup (AutoPickup):[/b] 
    • Automatically vacuums dropped items, equipment/runes, and gold within range.
    • Toggle between continuous Auto-Vacuum or Manual Keypress trigger (Space, V, F, G, Alt, Ctrl, etc.).
    • Item Type Filters: Toggle pickup for Equipment/Runes, Materials/Resources, Consumables (Food/Potions), and Recipes/Other.
    • Minimum Rarity Filter: Filter equipment drops by rarity (Common+, Uncommon+, Rare+, Mythical+, Heroic+, Ancient).
    • Lightweight zero-allocation performance with retry cooldown guards to avoid inventory-full stutters.
[*] [b]Perfect Parry:[/b] Forgiving or guaranteed perfect parry window for satisfying combat flow.
[/list]

[b][size=3]3. 📦 Loot[/size][/b]
[list]
[*] [b]Loot Mod V2:[/b] 
    • Independent multipliers for Monster Drops, Gold, and Resource Harvesting (Wood, Ore, Stone nodes).
    • Scaled World Chests, starter loot boxes, and Dungeon Chests.
    • Toggle for 100% guaranteed monster drops & max vanilla equipment rarity.
    • Clean single-pass scaling safety guards.
[*] [b]Any Chest & Inventory:[/b] Expand chest capacity (up to 216 slots) and player backpack inventory size.
[*] [b]Deed Unlocker:[/b] Effortlessly unlock and manage progression deeds.
[/list]

[b][size=3]4. ⚙️ System[/size][/b]
[list]
[*] [b]Configurable Level Cap:[/b] Raise maximum character level and attribute caps with precision [±1] and [±10] steppers.
[*] [b]Hell Mode:[/b] Extreme difficulty modifiers for players seeking a true challenge.
[*] [b]NavMesh Fix:[/b] Smoothes out monster pathfinding, stuck states, and obstacle navigation.
[*] [b]EXP Multiplier:[/b] Custom rate modifier for character leveling speed.
[*] [b]Relationship Multiplier:[/b] Accelerate NPC friendship, romance, and affinity gain.
[*] [b]Memory & GC Optimizer:[/b] Automatic RAM cleaner and scene-change memory purger to prevent Unity stutters, memory leaks, and GC lag. Includes real-time memory monitor and manual [b]"Clean RAM"[/b] button.
[/list]

[line]

[size=4][b]✦ Controls & Configuration[/b][/size]
[list]
[*] [b]F8 Key:[/b] Toggle the In-Game Configuration UI.
[*] [b]Hot Reload:[/b] Click the [b]"↻ Reload Disk CFG"[/b] button in the F8 menu to instantly apply manual edits made to your [font=Courier New].cfg[/font] file without restarting the game.
[*] [b]Config Location:[/b] [font=Courier New]BepInEx/config/com.custom.dimraethmodpack.cfg[/font]
[/list]

[line]

[size=4][b]✦ Installation[/b][/size]
[list=1]
[*] Ensure you have [b]BepInEx 6 (IL2CPP)[/b] installed for Dimraeth ([url=https://builds.bepinex.dev/projects/bepinex_be]Bleeding Edge Build #788+[/url]).
[*] Download this mod and extract [font=Courier New]DimraethModPack.dll[/font].
[*] Place [font=Courier New]DimraethModPack.dll[/font] into your [font=Courier New]Dimraeth/BepInEx/plugins/[/font] directory.
[*] [b]Important:[/b] If you have older standalone versions of any included mods installed, please remove their DLL files to prevent duplicate hooking.
[*] Launch the game, press [b]F8[/b], and enjoy!
[/list]

[line]

[size=4][b]✦ Credits & Compatibility[/b][/size]
[list]
[*] Fully compatible with IL2CPP Dimraeth builds.
[*] Clean Harmony patches with single-pass scaling safety guards.
[/list]
```

---

## 📝 Markdown Version (For Nexus Markdown Editor or GitHub)

# **Dimraeth QoL & Progression ModPack**
*The all-in-one unified enhancement pack for Dimraeth — 14 modular tweaks in a single DLL with in-game F8 configuration.*

---

### ✦ Overview
Tired of juggling a dozen separate mods, conflicting configs, and restarting the game just to tweak values? 
**Dimraeth ModPack** merges 14 essential quality-of-life, loot, gameplay, and system mods into **one single optimized DLL** with a unified config file and a real-time in-game configuration menu.

Press **F8** at any time during gameplay to tweak settings, toggle modules, adjust precision steppers, or hot-reload your config directly!

---

### ✦ Changelog & Improvements vs. Standalone Versions

If you previously used the standalone versions of these mods, here is what is new and improved in this unified pack:

- **1 Unified DLL & Config File:** Replaced 14 separate DLLs and 14 separate config files with a single coordinated DLL (`DimraethModPack.dll`) and one central configuration file (`com.custom.dimraethmodpack.cfg`).
- **Zero Hook Conflicts:** Solved race conditions and conflicting Harmony patches on shared game systems (`MonsterUtils`, `Storage`, `HarvestClient`, `SceneHandler`, `XP`).
- **Performance & Stutter Elimination:**
  - *Zero Disk I/O Hitching:* Fixed a severe stutter bug where non-Hell worlds performed continuous synchronous `File.Exists` disk reads on the main thread during combat and monster spawns.
  - *Dual-Mode Garbage Collection:* Seamless non-blocking GC during active gameplay prevents combat freezes, while deep RAM purges run during map/scene transitions.
  - *Zero-Allocation Loot Vacuum:* Cooldown buffers and category caches are fully pre-allocated to prevent GC spikes while vacuuming ground items.
  - *Formula Short-Circuiting:* Bypassed expensive IL2CPP type casting on monsters and NPCs during combat formula checks.
- **EXP Multiplier Math Fix:** Fixed a double-dipping bug that caused monster kill EXP to square itself (e.g. 5x became 25x). Combat, quest, and deed EXP now strictly scale at your exact chosen multiplier.
- **Item Type & Rarity Loot Filtering:** Configurable filters to selectively vacuum Equipment/Runes, Materials, Consumables, and Recipes, with a Minimum Rarity threshold (Common up to Ancient).
- **Unified In-Game Menu (F8):** A completely overhauled, lag-free UI organized into categorized tabs (**Stats**, **Gameplay**, **Loot**, **System**).
- **Live Hot-Reload Button (↻ Reload Disk CFG):** Edit your configuration on disk and reload it instantly in-game without restarting the client.
- **Dual Precision Steppers (±1 and ±10):** Added fast dual-speed increment buttons `[-10]` `[-1]` `[+1]` `[+10]` for **Spell Haste**, **Max Level Cap**, and **Max Attribute Cap**.
- **Loot System Overhaul & Math Fix:**
  - *Fixed Exponential Drop Bug:* Eliminated client-server RPC compounding multipliers that caused mining a single stone node to drop 1,000+ stone.
  - *World Containers & Starter Loot:* Standalone versions failed to scale world loot boxes and starter chests. The ModPack now properly multiplies world chests and map storage items.
  - *Reward Duplication Safeguard:* Fixed double-scaling between `ClaimRewards` and `ApplyReward` on map interaction tags.
  - *Dungeon Chests:* Added automatic scaling for dungeon chest drop ranges.
- **Deed Unlocker Rarity Fix:** Upgraded cycle controls to full enum pickers supporting all 6 rarity tiers (including **Ancient**) and 9 stars.
- **Any Chest & Inventory Safety:** Added boundary guards to prevent storage corruption on crafted chests while supporting expansions up to 216 slots.

---

### ✦ Included Modules

#### **1. 🗡️ Stats**
- **Always Regen:** Configurable out-of-combat and in-combat HP/MP regeneration.
- **Custom Stats:** Modify player base stats, including **Spell Haste** with precision `[±1]` and `[±10]` steppers.
- **Carry Weight:** Scale your character's max carry capacity.

#### **2. 🎮 Gameplay**
- **No-Click Pickup (AutoPickup):**
  - Automatically vacuums dropped items, equipment/runes, and gold within range.
  - Toggle between continuous Auto-Vacuum or Manual Keypress trigger (`Space`, `V`, `F`, `G`, `Alt`, `Ctrl`, etc.).
  - **Item Type Filters:** Toggle pickup for Equipment/Runes, Materials/Resources, Consumables (Food/Potions), and Recipes/Other.
  - **Minimum Rarity Filter:** Filter equipment drops by rarity (`Common+`, `Uncommon+`, `Rare+`, `Mythical+`, `Heroic+`, `Ancient`).
  - Lightweight zero-allocation performance with retry cooldown guards to avoid inventory-full stutters.
- **Perfect Parry:** Forgiving or guaranteed perfect parry window for satisfying combat flow.

#### **3. 📦 Loot**
- **Loot Mod V2:** 
  - Independent multipliers for Monster Drops, Gold, and Resource Harvesting (Wood, Ore, Stone nodes).
  - Scaled World Chests, starter loot boxes, and Dungeon Chests.
  - Toggle for 100% guaranteed monster drops & max vanilla equipment rarity.
  - Clean single-pass scaling safety guards.
- **Any Chest & Inventory:** Expand chest capacity (up to 216 slots) and player backpack inventory size.
- **Deed Unlocker:** Effortlessly unlock and manage progression deeds.

#### **4. ⚙️ System**
- **Configurable Level Cap:** Raise maximum character level and attribute caps with precision `[±1]` and `[±10]` steppers.
- **Hell Mode:** Extreme difficulty modifiers for players seeking a true challenge.
- **NavMesh Fix:** Smoothes out monster pathfinding, stuck states, and obstacle navigation.
- **EXP Multiplier:** Custom rate modifier for character leveling speed.
- **Relationship Multiplier:** Accelerate NPC friendship, romance, and affinity gain.
- **Memory & GC Optimizer:** Automatic RAM cleaner and scene-change memory purger to prevent Unity stutters, memory leaks, and GC lag. Includes real-time memory monitor and manual **"Clean RAM"** button.

---

### ✦ Controls & Configuration
- **F8 Key:** Toggle the In-Game Configuration UI.
- **Hot Reload:** Click the **"↻ Reload Disk CFG"** button in the F8 menu to instantly apply manual edits made to your `.cfg` file without restarting the game.
- **Config Location:** `BepInEx/config/com.custom.dimraethmodpack.cfg`

---

### ✦ Installation
1. Ensure you have **BepInEx 6 (IL2CPP)** installed for Dimraeth ([Bleeding Edge Build #788+](https://builds.bepinex.dev/projects/bepinex_be)).
2. Download this mod and extract `DimraethModPack.dll`.
3. Place `DimraethModPack.dll` into your `Dimraeth/BepInEx/plugins/` directory.
4. **Important:** If you have older standalone versions of any included mods installed, please remove their DLL files to prevent duplicate hooking.
5. Launch the game, press **F8**, and enjoy!
