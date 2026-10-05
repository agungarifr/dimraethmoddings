# Dimraeth Moddings

BepInEx 6 (IL2CPP) mods for **Dimraeth**. Four steps.

Game root: `D:\SteamLibrary\steamapps\common\Dimraeth`

---

## 1. Install opencode

Install from <https://opencode.ai>, then verify:

```bat
opencode --version
```

Give it a model with `opencode auth login` (or add a provider in `opencode.json`).

---

## 2. Open the game folder

Open `D:\SteamLibrary\steamapps\common\Dimraeth` in File Explorer, click the **address bar**,
type `cmd`, press **Enter**, then start the agent:

```bat
opencode
```

---

## 3. Set up the repo

Paste this whole prompt into opencode:

```text
Set up this Dimraeth modding workspace. The current folder is the Dimraeth game root
(Dimraeth.exe + GameAssembly.dll). Install anything missing, then clone this repo into the game
root and link it into place.

Steps:
1. Confirm Dimraeth.exe and GameAssembly.dll are in the current folder. If not, stop.
2. If `git --version` fails, install Git for Windows (winget install --id Git.Git -e) and tell me to
   reopen the terminal.
3. If `dotnet --version` fails, install the .NET SDK (winget install --id Microsoft.DotNet.SDK.6 -e).
4. If BepInEx is missing: download the newest BepInEx-Unity.IL2CPP-win-x64-*.zip from
   https://builds.bepinex.dev/projects/bepinex_be and extract it directly into this folder.
   If BepInEx\interop\Assembly-CSharp.dll is missing, ask me to run the game once and close it so
   interop gets generated.
5. Clone the repo if it is not there yet:
   git clone https://github.com/agungarifr/dimraethmoddings.git dimraethmoddings
6. Create the links (mklink is a cmd built-in):
   mkdir modding
   mklink /J "modding\BepInExModsSource" "dimraethmoddings\BepInExModsSource"
   mklink /J "modding\BepInEx" "BepInEx"
7. Verify these exist:
   modding\BepInEx\core\BepInEx.Core.dll
   modding\BepInEx\interop\Assembly-CSharp.dll
   modding\BepInExModsSource\DimraethSanctumChests\DimraethSanctumChests.csproj
8. When done, reply "setup complete".
```

---

## 4. Build a mod

Paste this into opencode (close the game first):

```text
Build modding\BepInExModsSource\DimraethSanctumChests\DimraethSanctumChests.csproj in Release with
dotnet build. Confirm DimraethSanctumChests.dll was copied into BepInEx\plugins, and show me the
build output.
```

The DLL loads the next time you start the game.

---

## Folders

The mod projects live in `dimraethmoddings\BepInExModsSource\` (the clone); you build/open them
through the link `modding\BepInExModsSource\`.

- `BepInEx\` — the loader the game runs. `core\` = BepInEx/Harmony/Il2CppInterop; `interop\` = C#
  wrappers for the game's classes, generated on first launch; `plugins\` = built mods land here;
  `config\` = per-mod settings; `LogOutput.log` = the log.
- `modding\BepInEx\` — link to the real `BepInEx`. The `.csproj` files use `..\..\BepInEx`, so the
  compiler reads `core\` + `interop\` here. Never loaded at runtime.
- `dotnet\` — BepInEx's private .NET runtime (not the build SDK).

## Mods

All under `modding\BepInExModsSource\`:

- **AlwaysRegenMod** — constant health/stamina regeneration.
- **AntiCheatBypassMod** — disables the game's anti-cheat checks.
- **AttackSpeedMod** — edits combat stats: attack speed, crit rate, spell haste, max stamina.
- **AutoPickupMod** — auto-picks up nearby loot.
- **BarrageOfArrowsTuner** — tunes the Barrage of Arrows spell.
- **CarryWeightMod** — raises/removes the carry-weight limit.
- **ConfigurableLevelCapFreebuff** — replaces the level-25 cap with a configurable one (default 99).
- **ConfigurableLevelCapFreebuff_Debug** — debug build of the same.
- **ContagionTuner** — tunes the Contagion poison spell (ticks, duration, damage).
- **DamageNumberTuner** — controls floating damage numbers (hotkey cycles modes).
- **DayNightToggleMod** — toggles/controls the day-night cycle.
- **DeedUnlockerMod** — unlocks all Deed Board tiers and raises quest loot rarity.
- **DimraethMapActionsShopPatch** — patches the DimraethMapActions mod's shop button.
- **DimraethModPack** — 13-mod QoL/progression pack with an in-game menu (Backquote / F8).
- **DimraethSanctumChests** — adds Sanctum Chest buttons to the map quick-actions.
- **EquipmentStatEditor** — in-game (F8) editor for equipped gear stats.
- **EquippedStatModifier** — live editor for equipped gear secondary stats.
- **FireballTuner** — tunes the Fireball spell.
- **HellModeLevel75Freebuff** — adds Hell Mode + a level-75 cap at world creation.
- **HellModeMod** — unlocks the Hell Mode difficulty.
- **LootAndExpMod** — 10× loot and EXP (legacy).
- **LootModV2** — 10× loot only (monsters, harvesting, interactables).
- **NavMeshFixMod** — fixes monster movement/navmesh glitches.
- **PerfectParryMod** — improves perfect parry (window, stamina, effects).
- **PlagueShardsHoming** — makes Plague Shards home in on enemies.
- **PursuingBlizzardTuner** — tunes the Pursuing Blizzard spell.
- **RahanerChestMod** — open any chest and expand backpack slots.
- **TwisterTuner** — tunes the Twister (Vortex) upgrade; optional black-hole pull.
- **UpgradeBonusStatIsNotRandom** — makes rune upgrade bonus stats fixed.
