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
