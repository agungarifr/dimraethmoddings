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

```
D:\SteamLibrary\steamapps\common\Dimraeth\
├─ Dimraeth.exe, GameAssembly.dll, Dimraeth_Data\   the game
├─ winhttp.dll, doorstop_config.ini, dotnet\        BepInEx loader + its .NET runtime
├─ BepInEx\                                         the loader the game runs
│  ├─ core\                                          BepInEx / Harmony / Il2CppInterop DLLs
│  ├─ interop\                                       generated C# wrappers for the game's classes
│  ├─ plugins\                                       built mods land here
│  ├─ config\                                        per-mod .cfg settings
│  └─ LogOutput.log                                  BepInEx + mod log
├─ dimraethmoddings\                                the cloned GitHub repo
│  └─ BepInExModsSource\                             the mod projects
└─ modding\                                         links the compiler builds through
   ├─ BepInEx\             → ..\BepInEx             compiler reads core\ + interop\ here
   └─ BepInExModsSource\   → ..\dimraethmoddings\BepInExModsSource
```

| Folder | What it is for |
|---|---|
| `BepInEx\` | The mod loader the game starts. |
| `BepInEx\core\` | BepInEx, Harmony and Il2CppInterop themselves — what a mod is compiled against. |
| `BepInEx\interop\` | C# wrappers for every game class, generated on first launch. Mods call the game through these. |
| `BepInEx\plugins\` | Every built mod `.dll` is copied here; the game loads them on launch. |
| `BepInEx\config\` | Per-mod settings (`.cfg`), editable. |
| `BepInEx\LogOutput.log` | BepInEx and mod log — where you check whether a mod loaded. |
| `dimraethmoddings\` | The cloned GitHub repository. |
| `dimraethmoddings\BepInExModsSource\` | The mod projects (`.csproj` + `.cs`). |
| `modding\BepInExModsSource\` | Link to the repo source — you edit and build through this path. |
| `modding\BepInEx\` | Link to the real `BepInEx`. The `.csproj` files reference `..\..\BepInEx`, so the compiler needs `core\` + `interop\` inside `modding\`. It is never loaded at runtime. |
| `dotnet\` | BepInEx's private .NET runtime (not the build SDK). |
