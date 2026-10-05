# Equipment Stat Editor (standalone)

<!-- [2026-09-26 01:59] Obsolete: the intro below said "Workflow (no UI)". v1.0.2 adds an
     in-game editor panel (F8) ported from the modpack's EquippedStatModifierModule UI.
     The config-file workflow stays as the backup path and still covers fields the panel
     doesn't (Level, Rarity, Stars, Set, inventory runes). -->
<!-- [2026-09-27 14:08] Updated: the panel now edits the PrimaryStat too (identity + value),
     bounded by the primary ceiling - it is no longer a config-only field. -->

**[2026-09-26 01:59] v1.0.2 — in-game editor panel (F8).** Load into your character,
press **F8** (`EditorKey` in the config), pick an equipped slot on the left, then
**add secondary stats**, **change which stat** an affix is (searchable picker), and
**edit its value** (-10 / -1 / +1 / +10 steppers or type it). Buttons: *Apply to Item*
(writes to live memory), *Save Character* (native save), *Revert*.

**[2026-09-29 10:38] v1.0.3 — Safe/Bypass mode (`BypassMode`).** The panel's title bar
now has a *Mode: SAFE / Mode: BYPASS* button. **SAFE** (default) keeps the vanilla
per-stat ceilings exactly as before. **BYPASS** raises every ceiling to **99× the
vanilla per-stat cap** (proportional: HP keeps its large scale, Crit its small one) and
disables the game's item-destruction system - `GearLegality.Inspect` is patched to "no
violation" while the mode is ON, so the inventory-load / equipped-tick / storage purge
paths never destroy your items. Stats the game leaves uncapped stay free input in both
modes; the Max button writes the current mode cap. **Warning:** items created above the
vanilla caps are destroyed by the game when the mod is removed or the mode switches
back to SAFE - they only survive while the mod runs with Bypass ON (and another
player's server enforces its own rules). The choice persists in the config
(`BypassMode`) and can be switched live from the panel.

Dump every rune (equipment) your character owns to a config file, edit it in a text
editor, and have the mod write the changes into the game on the next launch.
The result is saved by the **game's own save system**, so the edited stats persist
in your character save and work in multiplayer **with no mod installed**.

<!-- [2026-09-26 01:59] Obsolete heading: "Workflow (no UI)" - a UI now exists (F8 panel). -->
<!-- ## Workflow (no UI) -->
## Workflow (config file — backup path)

1. Run the game with the mod. Load into your character.
2. The mod automatically dumps all equipment to
   `BepInEx/config/EquipmentStatEditor.<CharacterName>.cfg`.
3. Close the game (or alt-tab - see "Live reload" below).
4. Edit the dumped config in a text editor and save it.
5. Start the game again - edited entries are applied automatically once the
   character finishes loading.
6. Trigger a normal game save (go to main menu, or just quit). Done.

After step 6 the stats live in `Characters/<name>.jrf` like any legit item.
You can uninstall the mod and everything keeps working.

## How it works

- The mod edits the live `Rune` structs (`Player.EquipmentWorn` / `Player.RuneInventory`)
  in raw il2cpp memory, then asks the game to re-sign the runes
  (`RefreshAllRuneSignatures`) so its own anti-cheat/HMAC integrity check accepts them,
  and recalculates bonuses (`Runes.UpdateRuneBonuses`).
- It never touches the save file bytes (the save is an encrypted container) - the
  game serializes the edited runes itself.
- `FormulaVersion` is always forced to `SaveSystem.CurrentRuneFormulaVersion`,
  otherwise the game's save-time migration (`MigrateRuneFormulas`) recomputes
  `StatValues` from its formula and wipes custom magnitudes.

## The config file

One `[Rune_<UUID>]` section per rune. Only the fields you change matter -
**remove or leave any key you don't want to touch**.

```ini
[Rune_a1b2c3d4e5]
Set=Momentum
SlotType=Rune
Rarity=Mythical
Stars=Nine
Level=8
PrimaryStat=AttackDamage
SecondaryStats=AttackSpeed,CriticalChance,Health
StatValues=3.5,8,120
StatUpgrades=1,0,2
Hash=1A2B3C4D
```

| Key | Meaning |
|---|---|
| `Set` | `Runes.RuneSet` name (set bonuses) |
| `SlotType` | `Runes.SlotType` (Rune, Undercoat, Belt, Charm, Ring, Amulet, Badge, Bracer) |
| `Rarity` | Common..Ancient |
| `Stars` | One..Nine |
| `Level` | 0-12 (higher allowed if another mod raises the cap) |
| `PrimaryStat` | any `Runes.Stat` name (full list in the file header) |
| `SecondaryStats` | comma-separated, max 7 (game cap is 6) |
| `StatValues` | comma-separated, same count as `SecondaryStats` |
| `StatUpgrades` | comma-separated, same count; optional (defaults to 0) |
| `Hash` | **do not edit** - detects your changes (see below) |

Rules enforced at apply time (bad entries are rejected and kept verbatim at the
bottom of the file with a `REJECTED:` note):

- a secondary stat can't equal `PrimaryStat`, no duplicates,
- `SecondaryStats` requires matching `StatValues`,
- `StatValues` / `StatUpgrades` alone are allowed and apply to the *existing*
  secondary stats (count must match) - handy for value-only edits,
- UUID / section name is the item identity and cannot be changed.

### The Hash key

`Hash=` marks the item state that was current when the file was dumped. If you
change any field of a section but leave `Hash` untouched, the mod knows *you*
edited the entry and applies it. Entries you didn't touch are skipped, so items
that changed in-game (e.g. upgraded at the Forge) are never reverted to stale
values.

- Delete a `Hash` line to force that entry to be applied again.
- The file is written **on demand**: click **Dump Runes to File** in the panel (F8), or it is
  written once at first launch when no config exists yet. Set `RewriteDumpAfterSave=true` if you
  instead want it refreshed after every native save.

## Live reload (optional)

Press **F9** (configurable) while in game: the file is re-read, edits are applied
and a native save is triggered immediately - no restart needed.

## Config (`BepInEx/config/com.dimraeth.equipmentstateditor.cfg`)

| Key | Default | Meaning |
|---|---|---|
| `ModEnabled` | true | master switch |
| `AutoApplyOnLoad` | true | apply edited entries when a character finishes loading |
| `RewriteDumpAfterSave` | false | rewrite the dump after each native save (off: use the panel's Dump button) |
| `SaveAfterApply` | false | auto-save right after applying (otherwise save manually) |
| `IncludeInventory` | true | also dump/edit unequipped runes, not only the 12 equipped slots |
| `ReloadKey` | F9 | live reload hotkey |
| `BypassMode` | false | Safe/Bypass mode: 99x per-stat ceilings + game item destruction disabled (see the v1.0.3 note at the top) |

## Multiplayer notes

- Character equipment is stored in **your local character save** - the host's world
  save contains no rune data. Edits persist and replicate as ordinary item data.
- Applying edits to `EquipmentWorn` is guaranteed to work in single-player and when
  hosting. As a client in someone else's session the equipped-rune write goes
  through a replicated network list and is best-effort - prefer editing while
  hosting/single-player.

## Build

```
modding\dotnet-sdk\dotnet.exe build modding\BepInExModsSource\EquipmentStatEditor\EquipmentStatEditor.csproj -c Release
```

The DLL is copied to `BepInEx/plugins/` automatically.
