# 🔥 Dimraeth - Hell Mode & Level 75 — Freebuff Edition (v2.0.0) 🔥

> Successor of **HellModeMod v1.0.0**. Same Hell, same dials — now with a **working Level 75 uncapper**.

**Plugin GUID:** `com.freebuff.hellmodelevel75`
**Assembly:** `HellModeLevel75Freebuff.dll`
**Source:** `modding/BepInExModsSource/HellModeLevel75Freebuff/`
**Old mod backup:** `modding/Backup_HellModeMod_v1_2026-09-20/`

---

## 🛠 What Was Broken in v1 (Root Cause)

v1 uncapped the level with **hardcoded RVA byte patches** in `GameAssembly.dll`:
it overwrote 8 `cmp/mov reg, 0x19` (level 25) immediates. The log happily reported
`[8/8 hooks]`, **but the cap never opened** because:

1. The engine reads the cap from **`GameConfig.MaxLevel` / `GameConfig.MaxAttributeLevel`**
   (static data read by `XP.AddXPToPlayer`'s level-up loop and the attribute path) —
   the byte patch never touched that data.
2. The byte list missed the two `cmp eax, 19h` compares inside
   **`PlayerStats.ApplyUpgradeAttributeInternal`** (attribute cap enforcement).
3. Fixed RVAs break on every game update (crash risk).

## ✅ What v2 Does Differently

- **PRIMARY — GameConfig runtime data patch (`GameConfigCapPatcher`):**
  writes `GameConfig.MaxLevel = 75` and `GameConfig.MaxAttributeLevel = 75`
  (both configurable) into the engine's live static storage while a Hell world is
  active, and restores 25/25 on standard worlds. Every write is **read back and
  verified**, with before/after values logged.
- **SECONDARY — byte-verified native patcher (optional):**
  same 25-immediate patching as v1, now including the 2 missed
  `ApplyUpgradeAttributeInternal` sites (10 sites total). Each site is only written
  when its expected byte is still present, so a game update degrades gracefully
  with a warning instead of corrupting code. Can be disabled via config.
- **Harmony patches kept** (XP blocking, attribute RPC guard, dials, slider UI,
  world lifecycle) as the third layer — they are version-resilient.
- New config keys: **`MaxAttributeCap`** (default 75) and
  **`EnableNativeBytePatch`** (default true).
- Live diagnostics: on every `Player.Awake`, the mod logs the current
  `GameConfig.MaxLevel / MaxAttributeLevel` values so you can confirm the patch
  actually took effect.

## 📊 Difficulty Dials (unchanged from your config)

| Feature | Value |
| :--- | :---: |
| Monster Health | 10.0× |
| Monster Damage | 3.0× |
| Combat Pace / Aggression | 3.5× |
| Monster Move Speed | 2.0× |
| Elite / Empowerment Chance | 5.0× |
| Monster Level | Player Level + 20 |
| **Max Level Cap** | **75** (Hell only; vanilla 25 elsewhere) |
| **Max Attribute Cap** | **75** (engine supports up to 99) |
| EXP Multiplier | 3.0× |
| Loot & Gold | 1.0× (vanilla) |

## ⚙️ Configuration

`BepInEx\config\com.freebuff.hellmodelevel75.cfg` (auto-reloads on save, same as v1):

```ini
[General]
ForceHellMode = false
EnableNativeBytePatch = true   # set false if a game update makes byte patching noisy

[DifficultyDials]
MonsterHealthMultiplier = 10
MonsterDamageDealtMultiplier = 3
CombatPaceMultiplier = 3.5
MonsterSpeedMultiplier = 2
EmpowermentChanceMultiplier = 5
MonsterBonusLevelOverPlayer = 20

[Progression]
MaxLevelCap = 75
MaxAttributeCap = 75
ExpMultiplier = 3
```

## 📦 Installation (already done)

1. `HellModeMod.dll` **disabled** (renamed `.v1.disabled`) so the two mods never run together.
2. `HellModeLevel75Freebuff.dll` deployed to `BepInEx\plugins\`.
3. New config seeded at `BepInEx\config\com.freebuff.hellmodelevel75.cfg`
   (your previous multiplier values were migrated).

## 🔬 How to Verify It Works

Launch the game, load your Hell world (`konoha99` / `vordae's World`), then check
`BepInEx\LogOutput.log` for:

```
[Info :HellModeLevel75Freebuff] HellModeLevel75Freebuff v2.0.0 (BepInEx 6 IL2CPP) Initialized!
[Info :HellModeLevel75Freebuff] [HellLevel75FB] GameConfig.MaxLevel: 25 -> 75 (Hell cap 75)
[Info :HellModeLevel75Freebuff] [HellLevel75FB] GameConfig.MaxAttributeLevel: 25 -> 75 (Hell cap 75)
[Info :HellModeLevel75Freebuff] [HellLevel75FB] GameConfig data patch applied & verified: HellMode=True LevelCap=75 AttrCap=75
[Info :HellModeLevel75Freebuff] [HellLevel75FB] Diagnostics: GameConfig live values -> MaxLevel=75, MaxAttributeLevel=75 ...
[Info :HellModeLevel75Freebuff] Native byte layer: Uncapped to 75 (Hell Mode active) [10/10 sites] ...
```

Then in-game: reach level 25 — XP must keep flowing and attributes must remain
upgradeable up to the caps. Loading a **non-Hell** world must log
`GameConfig.MaxLevel: 75 -> 25 (vanilla restored)` and behave exactly like vanilla.

## 🛡 Safety Notes

- Hell worlds are still serialized by the engine as `Difficulty.Hard` + `.hell` marker
  files (`%USERPROFILE%\AppData\LocalLow\Mudtek\Dimraeth\Worlds\<world>.hell`) —
  100% compatible with v1 worlds; markers carry over.
- If the game updates and RVAs shift, the byte layer self-disables per-site with a
  warning; the GameConfig data patch (primary) keeps working because it resolves
  values through interop metadata, not addresses.
- To roll back: delete `HellModeLevel75Freebuff.dll` + its cfg, rename the
  `.v1.disabled` files back to their original names.
