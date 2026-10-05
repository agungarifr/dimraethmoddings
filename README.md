# Dimraeth Moddings

A **source + documentation snapshot** of a BepInEx 6 (Unity / IL2CPP) mod suite for the
game **Dimraeth** — plus the reverse-engineering tools and research notes used to build it.

This README is written as a **step-by-step guide for a total beginner**. If you have never
modded a Unity game before, follow it top to bottom. When you finish you will have:

1. BepInEx installed and generating the game's "interop" assemblies,
2. the `.NET SDK` ready to compile C#,
3. this repository cloned in the right place,
4. at least one mod compiled and **deployed into the running game**, and
5. a repeatable edit → build → test loop you can drive from an IDE **or an AI assistant**.

> **TL;DR (if you already know BepInEx):** install BepInEx 6 IL2CPP x64 into the game root,
> run the game once so `BepInEx\interop` is generated, clone this repo into the game root, make
> `D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource` and `D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInEx` available (junction or copy),
> then `dotnet build` any `.csproj` under `BepInExModsSource`. The post-build step copies the DLL
> into `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins`. Full details below.

---

## Table of contents

- [1. How modding works here (the mental model)](#1-how-modding-works-here-the-mental-model)
- [2. Prerequisites](#2-prerequisites)
  - [2.1 Free vs paid tools](#21-free-vs-paid-tools)
- [3. Find your game folder](#3-find-your-game-folder)
- [4. Install BepInEx 6 (IL2CPP)](#4-install-bepinex-6-il2cpp)
- [5. Install the .NET SDK](#5-install-the-net-sdk)
- [6. Clone this repo and lay out the folders](#6-clone-this-repo-and-lay-out-the-folders)
- [7. Build your first mod (with an AI agent)](#7-build-your-first-mod-with-an-ai-agent)
- [8. Launch the game and verify](#8-launch-the-game-and-verify)
- [9. The edit → build → test loop](#9-the-edit--build--test-loop)
- [10. Build and code with an AI assistant (opencode)](#10-build-and-code-with-an-ai-assistant-opencode)
- [11. Troubleshooting](#11-troubleshooting)
- [12. What is inside this repository](#12-what-is-inside-this-repository)
- [13. Included mod projects](#13-included-mod-projects)
- [14. Analysis tools](#14-analysis-tools)
- [15. Notes, secrets and licensing](#15-notes-secrets-and-licensing)

---

## 1. How modding works here (the mental model)

Read this once — the rest of the guide will make much more sense.

- **Dimraeth is a Unity IL2CPP game.** The game's C# code was compiled to native machine code,
  so you cannot simply drop a `.cs` file into it.
- **BepInEx 6 (IL2CPP)** is a loader. It injects a .NET runtime into the game at launch
  (through `winhttp.dll` + `doorstop_config.ini`) and loads `.dll` plugins from
  `BepInEx\plugins`.
- **Interop assemblies.** On first launch, BepInEx generates C# "wrapper" assemblies for every
  game class (for example `Assembly-CSharp.dll`) into `BepInEx\interop`. These wrappers are what
  a mod compiles against — when your mod calls `ContagionPrefab.Activate()`, it is really calling
  the game's native method through the wrapper.
- **A mod is just a C# project** (`.csproj` + `.cs`). You compile it with `dotnet build`, and the
  resulting `YourMod.dll` is copied into `BepInEx\plugins`. The mod usually uses
  **Harmony** to "patch" (wrap/modify) a game method at runtime.
- **This repository does NOT ship the game's interop assemblies or BepInEx.** They are large and
  game-owned, so you install/generate them yourself (steps 4–6). The mod `.csproj` files point at
  them with relative paths, which is why folder placement matters.

If a build ever fails with "could not find `Assembly-CSharp.dll`" or "could not find
`BepInEx.Core.dll`", it almost always means steps 4 or 6 are not set up correctly.

---

## 2. Prerequisites

Install these before you start. Everything is free.

| Tool | Why you need it | Where to get it |
|---|---|---|
| **Dimraeth** (the game, on Steam) | The thing you are modding | Steam (App ID `2402680`) |
| **BepInEx 6 — IL2CPP, win-x64** (bleeding-edge "be" build) | Loads the mods and generates interop | <https://builds.bepinex.dev/projects/bepinex_be> — pick the newest artifact named `BepInEx-Unity.IL2CPP-win-x64-*.zip` |
| **.NET SDK 6.0 or newer** | Compiles the C# mods | <https://dotnet.microsoft.com/download> |
| **Git for Windows** | Clone/update this repository | <https://git-scm.com/download/win> |
| A text editor or IDE | To read/edit code | any tool from [section 2.1](#21-free-vs-paid-tools) — all optional but recommended |

### 2.1 Free vs paid tools

You do **not** have to pay anything to mod here. The **compiler is free** (the .NET SDK) and there
are capable **free editors and AI agents**. Paid tools are optional conveniences, not requirements.

**✅ Free to use** — the software itself costs nothing.

| Tool | Type | Notes |
|---|---|---|
| **VS Code** | Editor | Free and lightweight. Add the **C#** extension for autocomplete. <https://code.visualstudio.com/> |
| **Visual Studio Community** | IDE | Free for individuals / small teams (non-enterprise). Excellent .NET support. |
| **JetBrains Rider** | IDE | **Free for non-commercial use** (paid only if you sell your work). Best-in-class C#/Unity tooling. <https://www.jetbrains.com/rider/> |
| **Zed** | Editor | Free and fast, open source. |
| **opencode** | CLI AI agent | Open source (MIT). The software is free — use its **free included models** or plug in any provider key. Optional paid plan (OpenCode Go, $10/mo) for more. Already configured for this repo — see [section 10](#10-build-and-code-with-an-ai-assistant-opencode). <https://opencode.ai> |
| **Freebuff** | CLI + web AI agent | **Completely free**, ad-supported — no subscription, credits or API key. Uses GLM / DeepSeek / GPT backends. <https://freebuff.com/> |
| **Cline** / **Roo Code** | VS Code AI extension | Free, open source. Bring your own model key (or a local model). |
| **Aider** | CLI AI agent | Free, open source. Bring your own model key. |
| **Google Antigravity** | Agentic IDE | **Free public preview** with rate limits (Gemini 3, Claude, GPT-OSS). Paid Google AI plans exist for higher quotas. <https://antigravity.google/> |

**💰 Paid / subscription** — no real free tier, or the free tier is too limited for daily work.

| Tool | Type | Typical cost |
|---|---|---|
| **Claude Code** | CLI AI agent | No free tier — requires Claude Pro (~$20/mo) or Max (~$100–$200/mo), or pay-as-you-go API credits. <https://claude.com/pricing> |
| **GitHub Copilot** | IDE AI assistant | Free tier is very limited; **Pro ~$10/mo**, Pro+ ~$39/mo. |
| **Cursor** | AI IDE | Free "Hobby" tier is limited; **Pro ~$20/mo**. |
| **Windsurf** | AI IDE | Free tier is limited; **Pro ~$15/mo**. |
| **JetBrains Rider (commercial)** | IDE | Paid only when used to build commercial products (individual ~$14–$19/mo); free for hobby/non-commercial. |

> **Zero-budget beginner recipe:** **VS Code** + the **C#** extension to read and build the code,
> and **Freebuff** (or **opencode** with free models) as your AI helper. Everything in this guide
> can be done for **$0**. Claude Code / paid Cursor / Copilot are conveniences, not requirements.

> Pricing was correct at the time of writing and changes often — check each tool's own pricing page.

> **Not sure which BepInEx build?** You want the **IL2CPP** build, **x64**, not the Mono build.
> The archive name always contains `Unity.IL2CPP` and `win-x64`.

---

## 3. Find your game folder

1. Open **Steam**.
2. In your **Library**, right-click **Dimraeth** → **Manage** → **Browse local files**.
3. A File Explorer window opens at the game root, for example:

   ```
   D:\SteamLibrary\steamapps\common\Dimraeth
   ```

What a freshly installed game root looks like:

```
D:\SteamLibrary\steamapps\common\Dimraeth\
├─ Dimraeth.exe              the game
├─ GameAssembly.dll          the game's native (IL2CPP) code
├─ UnityPlayer.dll
├─ Dimraeth_Data\            Unity assets (scenes, bundles, ...)
└─ steam_appid.txt           contains 2402680
```

If you do **not** see `GameAssembly.dll`, you are in the wrong folder.

> **Important:** every step below places files **directly in the game root `D:\SteamLibrary\steamapps\common\Dimraeth`** — never in a
> subfolder. BepInEx must sit next to `Dimraeth.exe`.

---

## 4. Install BepInEx 6 (IL2CPP)

1. Download the newest `BepInEx-Unity.IL2CPP-win-x64-*.zip` from
   <https://builds.bepinex.dev/projects/bepinex_be>.
2. **Extract the contents of the zip directly into `D:\SteamLibrary\steamapps\common\Dimraeth`** (the folder that contains
   `Dimraeth.exe`). Do *not* extract into a subfolder inside the asset zip.
3. After extracting, `D:\SteamLibrary\steamapps\common\Dimraeth` must contain these new items:

   ```
   D:\SteamLibrary\steamapps\common\Dimraeth\
   ├─ winhttp.dll              the loader (this is how BepInEx starts)
   ├─ doorstop_config.ini      tells the loader what to run
   ├─ .doorstop_version
   ├─ BepInEx\                 BepInEx's own folder
   │  └─ core\                 BepInEx.Core.dll, BepInEx.Unity.IL2CPP.dll, 0Harmony.dll, ...
   └─ dotnet\                  a private .NET 6 runtime BepInEx uses
   ```

4. **Run the game once** (launch Dimraeth normally from Steam). On this first run BepInEx:
   - generates the **interop assemblies** into `BepInEx\interop\` (this can take a while — a
     minute or two, with a black console window), and
   - writes a log to `BepInEx\LogOutput.log`.

   Close the game when you reach a menu.

5. **Verify the install.** Open `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\LogOutput.log`. The first lines should look like:

   ```
   [Message: Preloader] BepInEx 6.0.0-be.788 - Dimraeth (...)
   [Info   :   BepInEx] Process bitness: 64-bit (x64)
   [Info   :   BepInEx] Running under Unity 6000.0.61f1
   ```

   And `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\` should contain ~180+ files, including:

   ```
   Assembly-CSharp.dll        ← the most important one: the game's own classes
   UnityEngine.CoreModule.dll
   Il2Cppmscorlib.dll
   Unity.Netcode.Runtime.dll
   ```

If `interop` is empty or missing, delete `BepInEx` and retry step 4 — make sure the game
actually launches and that the download was the **IL2CPP x64** build.

---

## 5. Install the .NET SDK

The mods target **.NET 6** (`<TargetFramework>net6.0</TargetFramework>`). .NET SDK 6.0 or
newer works.

1. Install the SDK from <https://dotnet.microsoft.com/download>.
2. **Close and reopen your terminal**, then verify:

   ```powershell
   dotnet --version
   ```

   You should see something like `6.0.428` (or `8.0.x`, `9.0.x` — newer is fine).

   > If `dotnet` is "not recognized", the SDK is not on your `PATH`. Reopen the terminal; if it
   > still fails, reinstall and tick "Add to PATH". This repository may also ship a portable SDK
   > at `D:\SteamLibrary\steamapps\common\Dimraeth\modding\dotnet-sdk\dotnet.exe`, which you can call by full path instead of `dotnet`.

---

## 6. Clone this repo and lay out the folders

### 6.1 The layout you're aiming for

```
D:\SteamLibrary\steamapps\common\Dimraeth\
├─ BepInEx\                     the game runtime (from step 4)
├─ dimraethmoddings\            the git clone
│  └─ BepInExModsSource\        the mod projects
└─ modding\                     links created in 6.4
   ├─ BepInEx\                  → ..\BepInEx
   └─ BepInExModsSource\        → ..\dimraethmoddings\BepInExModsSource
```

Two BepInEx folders, on purpose:

- `Dimraeth\BepInEx\` — the **game** runs from here.
- `Dimraeth\modding\BepInEx\` — the **compiler** reads `core\` + `interop\` here. Every `.csproj` uses `..\..\BepInEx\...`, which from `modding\BepInExModsSource\<Mod>\` points at `modding\`. It is never used at runtime; 6.4 links it to the real one so only one copy exists.

On build, a mod's DLL is copied into `Dimraeth\BepInEx\plugins\` so it loads next launch.

### 6.2 Open a terminal in the game folder

1. Open **File Explorer** and go to `D:\SteamLibrary\steamapps\common\Dimraeth` (the folder with `Dimraeth.exe`).
2. Click the **address bar** at the top, type `cmd`, and press **Enter**. A black **Command Prompt** window opens, already inside this folder.
3. Leave that window open — you use it for 6.3, 6.4 and 6.5.

### 6.3 Clone the repository

In that Command Prompt, paste this line (**right-click** to paste) and press **Enter**:

```bat
git clone https://github.com/agungarifr/dimraethmoddings.git dimraethmoddings
```

You should see `Cloning into 'dimraethmoddings'...` then `done.`, and a `dimraethmoddings` folder appears.

- **`'git' is not recognized`** → Git isn't installed. Install it from <https://git-scm.com/download/win>, close and reopen the terminal (step 6.2), try again.
- **`already exists`** → you cloned it before; just continue.

### 6.4 Link the clone into `modding\`

Still in the **same Command Prompt**, paste these three lines **one at a time**, pressing **Enter** after each:

```bat
mkdir modding
mklink /J "modding\BepInExModsSource" "dimraethmoddings\BepInExModsSource"
mklink /J "modding\BepInEx" "BepInEx"
```

Each `mklink` prints `Junction created for modding\... <<===>> ...`.

- **`You do not have sufficient privilege`** → you are in **PowerShell**, not Command Prompt. Close it and reopen with `cmd` from the address bar (step 6.2), then paste again.
- **`mkdir` says `already exists`** → fine, ignore it.
- **A junction already exists** → fine if it points to the right place.

<details><summary>PowerShell version (only if you insist on PowerShell)</summary>

```powershell
Set-Location "D:\SteamLibrary\steamapps\common\Dimraeth"
mkdir modding
New-Item -ItemType Junction -Path "modding\BepInExModsSource" -Target "dimraethmoddings\BepInExModsSource"
New-Item -ItemType Junction -Path "modding\BepInEx" -Target "BepInEx"
```
</details>

<details><summary>Option B — plain copies instead of junctions</summary>

Paste one at a time in the same Command Prompt:

```bat
mkdir "modding\BepInEx"
xcopy /E /I /Y "dimraethmoddings\BepInExModsSource" "modding\BepInExModsSource"
xcopy /E /I /Y "BepInEx\core"    "modding\BepInEx\core"
xcopy /E /I /Y "BepInEx\interop" "modding\BepInEx\interop"
```

Copy edits back into `dimraethmoddings\BepInExModsSource` before committing.
</details>

### 6.5 Confirm it worked

In the same Command Prompt, paste both lines. Each must print the file name:

```bat
dir "modding\BepInEx\core\BepInEx.Core.dll"
dir "modding\BepInEx\interop\Assembly-CSharp.dll"
```

- Shows the file → good, go to step 7.
- **`File Not Found`** → the link is wrong; delete the `modding` folder and redo 6.4.

---

## 7. Build your first mod (with an AI agent)

We will build **ContagionTuner**. You won't type build commands — you ask a coding agent to do it.
Open your agent at the **game root** `D:\SteamLibrary\steamapps\common\Dimraeth`, then paste this
prompt:

> Build `modding\BepInExModsSource\ContagionTuner\ContagionTuner.csproj` in Release with
> `dotnet build`. Then confirm `ContagionTuner.dll` was copied into `BepInEx\plugins` and show me the
> build output.

If you have not set up an agent yet, do [section 10](#10-build-and-code-with-an-ai-assistant-opencode)
(opencode) first, or use any tool below.

### 7.1 Example prompt by tool

| Tool | What to do |
|---|---|
| **opencode** (free, open source) | `cd /d "D:\SteamLibrary\steamapps\common\Dimraeth"` then `opencode`, paste the prompt. Setup: section 10 |
| **Claude Code** (paid, Anthropic) | install it, run `claude` from the game root, paste the prompt |
| **Antigravity** (Google, free preview) | open the game root as the workspace, open the **Agent** panel, paste the prompt |
| **Cursor / Windsurf / Copilot Chat / Cline / Aider** | open the game root, paste the prompt. Free vs paid: section 2.1 |

Whichever tool you use, the agent runs the build, reads the compiler errors, edits the code, and
rebuilds until it passes.

### 7.2 What success looks like

```
  ContagionTuner -> D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInEx\plugins\ContagionTuner.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The build compiles the mod, then a **post-build step copies** `ContagionTuner.dll` (and `.pdb`) into
the game's `BepInEx\plugins\`. Confirm it landed:

```bat
dir "D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins\ContagionTuner.*"
```

You should see `ContagionTuner.dll` with today's timestamp.

> **Close the game while building.** If it is running, the DLL in `BepInEx\plugins` is locked and the
> copy step fails with a "file in use" error.

<details><summary>No agent? Build manually (exactly what the agent runs)</summary>

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource\ContagionTuner"
dotnet build -c Release
```

If `dotnet` is "not recognized", this repository may ship a portable SDK — use
`"D:\SteamLibrary\steamapps\common\Dimraeth\modding\dotnet-sdk\dotnet.exe" build -c Release` instead.

To build **every** mod at once, from `modding\BepInExModsSource`:

```powershell
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object { dotnet build $_.FullName -c Release }
```
</details>

---

## 8. Launch the game and verify

1. Start Dimraeth normally (from Steam).
2. Open `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\LogOutput.log` and search for your mod:

   ```
   [Info   :   BepInEx] Loading [Contagion Tuner 1.1.0]
   ```

   If you see the line, the mod is loaded. If you see an error right after it, that is your
   Harmony patch failing — read the exception in the log.

3. Some mods print a banner to the log; some have in-game keys or config files. Check the mod's
   own `README.md` under `BepInExModsSource\<Mod>\`.

**Where configs live:** each mod writes a `.cfg` file into `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\config\`. For example
`DamageNumberTuner.cfg` (GUID `com.custom.damagenumbertuner`). You can edit the `.cfg` and, for
most mods, changes apply on the next launch (some apply live).

---

## 9. The edit → build → test loop

This is the core daily workflow. Once set up, it is three steps:

1. **Edit** a `.cs` file under `modding\BepInExModsSource\<Mod>\`.
2. **Build** (close the game first):
   ```bat
   dotnet build "D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource\<Mod>\<Mod>.csproj" -c Release
   ```
3. **Launch the game** and check `LogOutput.log`.

Tips:

- Keep a terminal open in `D:\SteamLibrary\steamapps\common\Dimraeth` so you can build quickly.
- For quick iteration, build only the one project you changed.
- If a change has no visible effect, confirm the DLL timestamp in `BepInEx\plugins` changed — a
  failed copy means the game was still running.
- Commit your source changes from inside the clone:
  ```bat
  cd /d "D:\SteamLibrary\steamapps\common\Dimraeth\dimraethmoddings"
  git status
  git add -A
  git commit -m "Describe your change"
  git push
  ```
  (Never commit `BepInEx/`, `bin/`, `obj/`, or any API keys — the repo's `.gitignore` already
  excludes them.)

---

## 10. Build and code with an AI assistant (opencode)

You can let an AI do the heavy lifting: explore the game's classes, write a patch, build it, and fix
compiler errors — all from a chat. This repository is already set up for
**[opencode](https://opencode.ai)**, an open-source CLI coding agent (you can also use any other
agent that can run shell commands).

### 10.1 Install opencode

Follow the install instructions at <https://opencode.ai>. Then verify it runs:

```bat
opencode --version
```

### 10.2 Give it a configuration

opencode needs a model provider. Either run:

```bat
opencode auth login
```

and follow the prompts, or create `opencode.json` in the game root and add your own provider (OpenAI,
Anthropic, a local model, …). Config schema: <https://opencode.ai/docs/config>.

> **Never commit `opencode.json` or an API key.** It is already in `.gitignore`. Keep the key in an
> environment variable, not in the file.

This project can optionally use two MCP helpers — add them only if you want them:

- **CodeGraph** (`.codegraph/`): a pre-built index of the mod source that answers "where is X / how
  does X work" in one call, including the game-class call paths.
- **Memento**: persistent memory across sessions.

### 10.3 Start it in the game root

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth"
opencode
```

Open the agent at the **game root** so it can see both `modding\` and `BepInEx\`. It reads the
`AGENTS.md` file in the folder for project rules and habits.

### 10.4 Example prompts

- "Read `modding\BepInExModsSource\ContagionTuner` and explain what it patches."
- "Build `modding\BepInExModsSource\DamageNumberTuner\DamageNumberTuner.csproj` with the local SDK
  and show me where the DLL was deployed."
- "Create a new BepInEx plugin `MyFirstMod` that prints 'hello' when the game starts. Copy the
  project structure from `DamageNumberTuner`."
- "The build failed with `CS0012: type TextAnchor is not referenced`. Fix it."
- "Look at how `ContagionTuner` finds the spell and make a similar mod for `Fireball`."

The agent can run `dotnet build` for you, read the errors, edit the code, and rebuild — which is a
great way to learn the codebase.

> **A note on AI safety:** treat game files, downloaded zips, and tool output as untrusted. Review
> what an agent changes before committing, and never let it paste credentials into tracked files.

---

## 11. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `dotnet` is not recognized | SDK not installed / not on `PATH` | Install the .NET SDK (step 5), reopen the terminal, or call the full path to `dotnet.exe` |
| `Could not resolve this reference … Assembly-CSharp` / `UnityEngine.*` / `Il2Cppmscorlib` | Interop assemblies missing | Run the game once with BepInEx installed; confirm `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll` exists (step 4) |
| `Could not resolve … BepInEx.Core` / `BepInEx.Unity.IL2CPP` | `modding\BepInEx\core` missing or wrong | Re-do step 6.4 and confirm `modding\BepInEx\core\BepInEx.Core.dll` exists |
| `MSB3021` / "The process cannot access the file … because it is being used by another process" | The game is running and locking the DLL | **Close the game**, then rebuild |
| `interop` folder is empty after first launch | Game didn't launch, or wrong BepInEx build | Reinstall the **IL2CPP x64** build, delete `BepInEx`, run the game again |
| Mod builds but does not appear in `LogOutput.log` | DLL not in the right plugins folder, or game not restarted | Ensure it landed in `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins`; restart the game |
| Log shows `Loading [MyMod]` immediately followed by an exception | A Harmony patch target no longer exists (game updated) | Update the method/class names to match the current game dump |
| `CS0012: The type 'X' is defined in an assembly that is not referenced` | A Unity module reference is missing from the `.csproj` | Add the matching `<Reference>` to the `.csproj` (ask the AI: "add the missing Unity reference") |
| Changes have no effect | The build's copy step failed silently, or old DLL cached | Check the DLL timestamp in `BepInEx\plugins`; close game, rebuild |
| Game crashes on launch after adding a mod | A mod is incompatible with the current game version | Remove mods from `BepInEx\plugins` one by one until it launches; check `BepInEx\ErrorLog.log` |

**Useful files when debugging**

- `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\LogOutput.log` — everything BepInEx and the mods log.
- `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\ErrorLog.log` — fatal startup errors.
- `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\config\<ModGuid>.cfg` — per-mod settings.

---

## 12. What is inside this repository

This repository is a **source + docs snapshot** intended for moving the project to another device.
Large, regenerable artifacts (runtimes, SDKs, decompiled dumps, build outputs, backups) are
**intentionally excluded** — see `.gitignore`.

```
BepInExModsSource/      Main mod projects (29 × .csproj) + per-mod README/NEXUS docs
ModsSource/             Earlier standalone mod sources (legacy MelonLoader era, 3 projects)
<tool>/                 Reverse-engineering / analysis tools (21 × .csproj)
docs/                   Research documentation
  anticheat/            Anti-cheat analysis (11 chapters + graph)
  graphify-reports/     Historical code-graph reports (markdown only)
assets/                 Art / thumbnails / mod.pdf
releases/               Published mod .zip packages
*.md, changelog.txt     Top-level reports (Hell Mode, Nexus, forensic reports, etc.)
AGENTS.md               Project agent/workflow notes (used by AI assistants)
.gitignore
README.md               ← you are here
```

---

## 13. Included mod projects

All of these live under `BepInExModsSource/` and build the same way (`dotnet build` the `.csproj`).

| Project | Project | Project |
|---|---|---|
| AlwaysRegenMod | AntiCheatBypassMod | AttackSpeedMod |
| AutoPickupMod | BarrageOfArrowsTuner | CarryWeightMod |
| ConfigurableLevelCapFreebuff | ConfigurableLevelCapFreebuff_Debug | ContagionTuner |
| DamageNumberTuner | DayNightToggleMod | DeedUnlockerMod |
| DimraethMapActionsShopPatch | DimraethModPack | DimraethSanctumChests |
| EquipmentStatEditor | EquippedStatModifier | FireballTuner |
| HellModeLevel75Freebuff | HellModeMod | LootAndExpMod |
| LootModV2 | NavMeshFixMod | PerfectParryMod |
| PlagueShardsHoming | PursuingBlizzardTuner | RahanerChestMod |
| TwisterTuner | UpgradeBonusStatIsNotRandom | |

**Notable ones for learning:**

- **DamageNumberTuner** — small, modern example of a config + hotkey + single Harmony prefix.
- **ContagionTuner** — shows patching a spell and adjusting its config values.
- **DimraethModPack** — a larger multi-module project with an in-game mod manager.
- **AutoPickupMod / NoClickPickup** — shows interacting with game objects and network calls.

`ModsSource/` contains **older MelonLoader-era** mods (`AlwaysRegenMod`, `CarryWeightMod`,
`LootAndExpMod`). They reference `MelonLoader\` and generally will **not** build with the current
BepInEx setup — kept for historical reference.

Each mod folder may contain its own `README.md` and/or NEXUS publish text; read those for what the
mod does and how to configure it.

---

## 14. Analysis tools

These are **not required to build mods**. They are the reverse-engineering utilities used to study
the game. They live at the repository root and usually need the game's assemblies or a decompiler
dump to be useful.

`BundleScan`, `ByteDump`, `CatDump`, `CheckRva`, `DirDump`, `DisasmProbe`, `DumpStrings`,
`InspectTool`, `Lz4Test`, `MetaInspect`, `MetaStrings`, `ModInspect`, `RelationshipMultiplier`,
`SaveProbe`, `ScanInterop`, `ScanMarkers`, `ScanRegion`, `ScanStrings`, `ScanTools`,
`SerDump`, `SerParse`.

> Some tools hard-code the game path. For example `RelationshipMultiplier.csproj` defines
> `<GameRoot>D:\SteamLibrary\steamapps\common\Dimraeth</GameRoot>` — if your game is installed
> elsewhere, edit that line before building, or the references will not resolve.

---

## 15. Notes, secrets and licensing

- **Secrets:** `opencode.json` is excluded because it can contain an API key. Provide the key via an
  environment variable. **Never** commit keys, tokens, or credentials.
- **Game-owned binaries are excluded:** `BepInEx/`, `dotnet/`, `MelonLoader/`, `cpp2il_*`,
  `CheatMenuDecompiled/`, `bin/`, `obj/`, and CodeGraph/graphify indexes are all in `.gitignore`.
  You regenerate them on each device.
- **Docs disclaimer:** some files under `docs/` record reverse-engineered game internals (including
  a Supabase anon key captured from the shipped client). They are kept for research/learning only.
  Do not reuse them against live services.
- **Third-party mods** are not included. If a mod is not yours, do not publish it here.
- Please respect the game's terms of service and the wishes of the original mod authors when
  sharing or publishing anything derived from this repository.

---

### Quick reference

| I want to… | Do this |
|---|---|
| See if BepInEx is installed | Check `D:\SteamLibrary\steamapps\common\Dimraeth\winhttp.dll` and `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll` |
| Build one mod | `dotnet build "D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource\<Mod>\<Mod>.csproj" -c Release` |
| Build all mods | `Get-ChildItem "D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource" -Recurse -Filter *.csproj \| % { dotnet build $_.FullName -c Release }` |
| Find where a mod deployed | Check `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins\<Mod>.dll` timestamp |
| Read the log | `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\LogOutput.log` |
| Change a mod's settings | `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\config\<ModGuid>.cfg` |
| Commit a change | From `D:\SteamLibrary\steamapps\common\Dimraeth\dimraethmoddings`: `git add -A && git commit -m "..." && git push` |
| Ask an AI for help | Run `opencode` in `D:\SteamLibrary\steamapps\common\Dimraeth` and describe what you want |
