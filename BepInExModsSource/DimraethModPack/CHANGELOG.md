# Changelog

## 2026-09-29 (09:28)

### DimraethModPack (`DimraethModPack.dll`)

**Fixed** — *Configurable Level Cap: byte patch actually works now (was silently doing nothing).*
The user-reported "level stuck at 25 despite Native Byte Patch enabled" was root-caused by
decompiling the earlier standalone deploy `customlevelcap.dll` (v1.4.0, 2026-09-22) with
`modding/DecompilerTool` and diffing it against the restored module. The restored
`NativeBytePatcher` was an older variant with three fatal defects:
1. **64-byte scan window + early `ret` stop** — the cap `cmp imm8` sites live past byte 64 / past
   early returns, so **0 sites were ever patched**;
2. **`attributeCap` ignored** — `Apply(levelCap, attributeCap)` only ever wrote `levelCap`;
3. **silent `catch { }`** — failures never showed in `LogOutput.log`.
Ported the proven v1.4.0 scanner into `LevelCapModule.NativeBytePatcher`:
- 8192-byte scan window, no early stop;
- 11 targets: 9x imm `0x19` (level sites, `GameConfig.MaxLevel=25`) + 2x imm `0x63` (attribute
  sites, `GameConfig.MaxAttributeLevel=99`) replaced with `MaxAttributeCap`;
- full logging: per-site `Patched ... 0x19->0x2D @ method+0x.. [hex ctx]`, plus a summary
  `Byte patch result: X sites patched, Y updated, Z already at cap, W unresolved`;
- every failure path now logs a warning/error;
- patched sites are tracked per-address (`Dictionary<IntPtr,byte>`) so cap changes in the F8 UI
  rewrite them live without a game restart (the standalone could not — it read caps once).
Old code kept commented out with `[2026-09-29 09:28]` notes per repo rule.

Note: vanilla per-attribute cap of 50 seen in-game is `2 x level` (2x25); the imm `0x63` sites are
the 99 hard cap. Once the level cap opens, stats follow. Do NOT run `customlevelcap.dll` together
with this module (same native sites, different values).

---

## 2026-09-29

### DimraethModPack (`DimraethModPack.dll`)

**Changed**
- **Dual-Precision Steppers for every module** (`Core/ModModuleBase.cs`).
  `DrawIntSpinner` / `DrawFloatSpinner` now render 4 buttons instead of 2:
  `[-10x] [-1x] [+1x] [+10x]` where "1x" is the setting's own step and "10x" is 10x
  that step. For whole-number settings (EXP Multiplier, loot multipliers, ...) this is a
  literal `-10 / -1 / +1 / +10`. Fixes the tedium of reaching high values with `+1` clicks
  (e.g. EXP Multiplier previously stalled around 30 because clicking +1 got tiring).
- **Max limits raised (after per-module user review of all 13 modules):**
  - EXP Multiplier: max 100 -> **9999**
  - Carry Weight "Weight Multiplier": max 100 -> **9999**
  - All other modules explicitly **kept at their current limits** (LootModV2 100,
    AlwaysRegen 100, CustomStats 20/100/500/10000/10000/50000, NoClickPickup radius 50,
    PerfectParry 5s, Chest&Inventory 6x/4x with the 216/180 slot safety clamps, HellMode
    20/10/5/3/10/50, Relationship 20, AutoBackupSave 100, SkillPointMultiplier 10,
    LevelCap 99).

**Added**
- **Configurable Level Cap: startup status log.** `[LevelCap] Status: Enabled=..., MaxLevelCap=...,
  MaxAttributeCap=..., NativeBytePatch=...` is now written to `LogOutput.log` at patch time,
  so the restored module is verifiable without guessing (it previously only showed the generic
  "config bound / patches applied" lines).

---

## 2026-09-28 (20:50)

### DimraethModPack (`DimraethModPack.dll`)

**Restored**
- **Configurable Level Cap** (System category, `Modules/System/LevelCapModule.cs`) is back.
  The module silently vanished from both source and compiled DLL sometime after 2026-09-25
  (no Obsolete note; most likely lost in the backup/restore cycle for the elf crash fix),
  which left the `[System.LevelCap]` config section binding to nothing — your
  `MaxLevelCap = 45` / `MaxAttributeCap = 75` were doing nothing and the game ran at the
  vanilla cap of 25.
  - Source restored **verbatim** from `modding/Backup_2026-09-25_09-15_fix_elf_move_crash_untested`
    (the broken standalone `customlevelcap.dll` was deliberately NOT used as reference).
  - Config keys are unchanged, so existing `[System.LevelCap]` values bind as-is
    (`Enabled = true`, `MaxLevelCap = 45`, `MaxAttributeCap = 75`,
    `EnableNativeBytePatch = false`).
  - Re-registered in `DimraethModPackPlugin` with restoration notes.

**Notes**
- With `EnableNativeBytePatch = false` (your current setting), enforcement relies on the
  Harmony `XP.IsXPGainBlocked` gate only. If testing shows the cap still stuck at 25,
  try toggling **Native Byte Patch** ON in the F8 menu — that patcher removes the in-game
  `cmp 0x19` cap checks in memory (byte-verified, `GameAssembly.dll` untouched on disk).

---

## 2026-09-28

### Dimraeth ModPack (`DimraethModPack.dll`)

**Added**
- **Anti-Cheat Bypass** (new module, *System* category).
  The game hides characters from the character-select list when
  `CharacterPlausibility` considers them impossible (its ceilings are level 37, skill
  points spent in the tree 84, and total attributes 126). This module stops that filter
  so edited/boosted characters always show up again.
  Toggle: `com.custom.dimraethmodpack.cfg` → `[System.AntiCheatBypass]` → `Enabled`.
  Applies to **solo and multiplayer**: the multiplayer join gate is also bypassed by default
  (`BypassMultiplayerJoinCheck = true`), so boosted/edited characters can join again. It is a
  client-side check, so this only affects instances running the mod (e.g. when you host); set
  `BypassMultiplayerJoinCheck = false` to keep the vanilla join gate.

**Changed**
- **Skill Point Multiplier — disabled** (unregistered by request).
  The module granted extra skill points per level (sources recorded in the save as
  `SkillPointMultiplierMod_L{n}`). At 5x that is +8 per level; over a run this pushed the
  total skill points **spent in the tree** past the game's plausibility ceiling of 84,
  which made the game hide the affected characters (e.g. spent 87 / 119 vs. the 84 limit).
  Those bonus points are permanent in existing saves, so character visibility is restored
  by the new Anti-Cheat Bypass module; removing this registration only stops *new* points
  from exceeding vanilla. The cfg multiplier is left at `1` (vanilla).

**Notes**
- Nothing else about your existing characters changed. No save files were edited, deleted,
  or re-signed by these changes.

---

### Equipment Stat Editor (standalone plugin)

**Fixed**
- **Over-cap detection** now uses the game's own ceiling
  (`GearLegality.TryStatValueCeiling`) with a 0.05 tolerance, instead of a hardcoded
  constant. Correct values are no longer flagged red, and genuine over-cap values are
  still blocked on Apply.
- **Crash guard:** stat edits now only apply to the local player (pointer-identity check)
  unless **Affect all players** (`OnlyAffectLocalPlayer = false`) is explicitly enabled.
  This removes a hard crash (`AccessViolationException`) when other players were present.
- Removed dead rune pre-signing calls that always failed; the game re-signs runes itself
  after a write.

**Added**
- **On-demand "Dump Runes to File"** button in the F8 panel. Rune dumps are no longer
  written automatically on every save (`RewriteDumpAfterSave = false` by default).

---

### How to update
1. Replace `BepInEx/plugins/DimraethModPack.dll` (and `EquipmentStatEditor.dll` if used).
2. Launch the game. If a previously hidden character is still missing, open the char-select
   screen after the first in-game save, or confirm in `BepInEx/LogOutput.log`:
   `[AntiCheatBypass] Patched CharacterPlausibility.FilterImplausible.`
