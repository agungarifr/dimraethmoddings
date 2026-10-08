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

### 1.1 Optional agent helpers

Not needed to build mods. Once the agent is running (step 2), paste this and let it install and
configure everything for you:

```text
Set up the optional agent helpers for this repo — install what's missing and configure it:
- CodeGraph (code-graph index, the successor to graphify): install @colbymchenry/codegraph
  globally, index modding\BepInExModsSource, and register it as an MCP server in opencode.json
  (command: codegraph serve --mcp, cwd: modding/BepInExModsSource).
- Memento (persistent memory): install @iachilles/memento globally and register it as an MCP
  server (command: memento) with MEMORY_DB_PATH pointing at a database file.
- Karpathy guidelines: clone https://github.com/multica-ai/andrej-karpathy-skills and add its
  skills folder to the skills list in the opencode config.
Restart opencode if the config changed.
```

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

## 4. Build a mod (just an example)

Close the game first. Then talk to the agent like normal — no special syntax:

```text
hey build me the mod that opens the sanctum chest from anywhere
```

The agent works out that's `DimraethSanctumChests`, builds it in Release, and copies the DLL into
`BepInEx\plugins`. The DLL loads the next time you start the game.

<details><summary>More explicit version (if the agent is unsure which project)</summary>

```text
Build modding\BepInExModsSource\DimraethSanctumChests\DimraethSanctumChests.csproj in Release with
dotnet build. Confirm DimraethSanctumChests.dll was copied into BepInEx\plugins, and show me the
build output.
```
</details>

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

- **AimedShotChargeTuner** — shortens the Aimed Shot charge/draw time (default: half) and extends its effective range (default x1.5), with a tuned-stats tooltip.
- **AntiCheatBypassMod** — disables the game's anti-cheat checks.
- **BarrageOfArrowsTuner** — tunes the Barrage of Arrows spell.
- **ContagionTuner** — tunes the Contagion poison spell (ticks, duration, damage).
- **DamageNumberTuner** — controls floating damage numbers (hotkey cycles modes).
- **DayNightToggleMod** — toggles/controls the day-night cycle.
- **DimraethMapActionsShopPatch** — patches the DimraethMapActions mod's shop button.
- **DimraethModPack** — 13-mod QoL/progression pack with an in-game menu (Backquote / F8).
- **DimraethSanctumChests** — adds Sanctum Chest buttons to the map quick-actions.
- **EquipmentStatEditor** — in-game (F8) editor for equipped gear stats.
- **FireballTuner** — tunes the Fireball spell.
- **NavMeshFixMod** — fixes monster movement/navmesh glitches.
- ~~**PlagueShardsHoming** — makes Plague Shards home in on enemies.~~ **broken**
- **PursuingBlizzardTuner** — tunes the Pursuing Blizzard spell.
- **TrainerAdvisor** — injects a display-only optimal attribute-build panel (physical / magic / hybrid) into the vanilla trainer window.
- **TwisterTuner** — tunes the Twister (Vortex) upgrade; optional black-hole pull.
