# Dimraeth ModPack — Quick Summary

**One DLL. One config. 18 modules. In-game manager (toggle key: ` by default, remappable).**

Everything is **off by default** and toggled live at runtime — no restart needed. Press the menu key in-game to
enable modules, tune values with +/-1 and +/-10 steppers, or hot-reload the `.cfg` from disk.

Config: `BepInEx/config/com.custom.dimraethmodpack.cfg` · Plugin: `BepInEx/plugins/DimraethModPack.dll`

---

## 🗡️ Stats (4)
- **Always Regen** — HP/stamina regen multipliers, infinite sprint, no post-hit regen lockout.
- **Custom Stats** — override attack speed, crit chance, spell haste, max stamina/concentration/health.
- **Carry Weight** — weight-capacity multiplier or unlimited carry.
- **Upgrade Bonus Stat Is Not Random** — pick the exact bonus stat each upgrade grants.

## 🎮 Gameplay (4)
- **No-Click Pickup** — auto-vacuum drops/gold in a radius (continuous or keypress), filters by item type and minimum rarity.
- **Perfect Parry** — forgiving or guaranteed perfect-parry window.
- **Deed Progression** — report every deed tier (normal + boss) as unlocked, with a max-tier cap.
- **Skill Point Multiplier** — bonus skill points per level-up.

## 📦 Loot (3)
- **Loot Mod V2** — independent multipliers for monster drops, gold, harvesting, and map/interactable loot; guaranteed drops, force max vanilla rarity, exact equipment drop count.
- **Any Chest & Inventory** — expand chest slots (up to 216) and backpack capacity; all chests or a chosen container only.
- **Pet Cargo** — diagnostics for pet transfer/deposit paths (all-item-types investigation).

## ⚙️ System (7)
- **Configurable Level Cap** — raise max character level and max attribute cap.
- **Hell Mode** — extreme difficulty modifiers.
- **EXP Multiplier** — scale combat, quest, and deed EXP (no double-dipping).
- **Relationship Multiplier** — faster NPC friendship/romance/affinity gain.
- **Weather Controller** — select and apply any weather for your kingdom.
- **Time Skip** — jump the world clock to a preset hour or forward by N hours.
- **Auto Backup Save** — timestamped save backups automatically on game quit.

---

## What's in the F8-style menu
- Real-time **FPS / 1% low / stutter count / RAM** diagnostics.
- Per-module enable toggles + precision steppers.
- **↻ Reload Disk CFG** button for instant config hot-reload.
- **Clean RAM** manual purge (plus automatic non-blocking GC during play).

## Notes
- Requires **BepInEx 6 (IL2CPP)** for Dimraeth.
- Remove older standalone versions of these mods to avoid duplicate hooks.
- Non-destructive: modules change what accessors report / grant, they don't corrupt saved world data.
- Level-cap and skill-point modules pair best with the standalone **AntiCheatBypassMod** so over-cap characters stay visible in character select.
