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
- [7. Build your first mod](#7-build-your-first-mod)
- [8. Launch the game and verify](#8-launch-the-game-and-verify)
- [9. The edit → build → test loop](#9-the-edit--build--test-loop)
- [10. Build with an IDE (VS Code / Visual Studio / Rider)](#10-build-with-an-ide-vs-code--visual-studio--rider)
- [11. Build and code with an AI assistant (opencode)](#11-build-and-code-with-an-ai-assistant-opencode)
- [12. Troubleshooting](#12-troubleshooting)
- [13. What is inside this repository](#13-what-is-inside-this-repository)
- [14. Included mod projects](#14-included-mod-projects)
- [15. Analysis tools](#15-analysis-tools)
- [16. Notes, secrets and licensing](#16-notes-secrets-and-licensing)

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
| **opencode** | CLI AI agent | Open source (MIT). The software is free — use its **free included models** or plug in any provider key. Optional paid plan (OpenCode Go, $10/mo) for more. Already configured for this repo — see [section 11](#11-build-and-code-with-an-ai-assistant-opencode). <https://opencode.ai> |
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
├─ BepInEx\                        the runtime the GAME loads (from step 4)
│  ├─ core\
│  ├─ interop\                     game wrappers, generated on first launch
│  └─ plugins\                     built mods are copied here
├─ dimraethmoddings\               the git clone of this repo
│  └─ BepInExModsSource\           the 29 mod projects (+ docs, tools)
└─ modding\                        created in step 6.3
   ├─ BepInEx\                     → link to ..\BepInEx
   └─ BepInExModsSource\           → link to ..\dimraethmoddings\BepInExModsSource
```

There are **two** folders named BepInEx on purpose:

| Path | Used by | What it is for |
|---|---|---|
| `Dimraeth\BepInEx\` | the **game** | BepInEx actually runs from here; the game loads mods from `plugins\` |
| `Dimraeth\modding\BepInEx\` | the **compiler** | `dotnet build` reads `core\` and `interop\` from here |

Why the second one? Every mod `.csproj` points at BepInEx with a path that goes **up two folders**:

```xml
<HintPath>..\..\BepInEx\core\BepInEx.Core.dll</HintPath>
<HintPath>..\..\BepInEx\interop\Assembly-CSharp.dll</HintPath>
```

From `modding\BepInExModsSource\ContagionTuner\`, "up two folders" is `modding\`, so the compiler
always looks for `modding\BepInEx\`. That copy is **never used at runtime** — the game ignores the
`modding\` folder completely. Step 6.3 just points it at the real `BepInEx` so there is only one
copy of the files on disk. (Only `core\` and `interop\` matter; `config\`, `plugins\` and logs do not.)

When a mod builds, it copies its DLL **up three folders** into `Dimraeth\BepInEx\plugins\` — the
game's real plugins folder — so the mod loads the next time you launch.

### 6.2 Clone the repository

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth"
git clone https://github.com/agungarifr/dimraethmoddings.git dimraethmoddings
```

You now have `Dimraeth\dimraethmoddings\BepInExModsSource\` holding the 29 projects (plus the docs
and tools). Do not build here yet — the compiler still needs the `modding\BepInEx\` folder that the
next step creates.

### 6.3 Link the clone into `modding\`

This creates the two links shown in 6.1. A **junction** is a "shortcut folder": no administrator
rights needed, and no files are duplicated.

**Option A — junctions (recommended: you edit one copy, and git tracks it directly).**

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth"
mkdir modding
mklink /J "modding\BepInExModsSource" "dimraethmoddings\BepInExModsSource"
mklink /J "modding\BepInEx" "BepInEx"
```

PowerShell equivalent:

```powershell
New-Item -ItemType Junction -Path "modding\BepInExModsSource" -Target "dimraethmoddings\BepInExModsSource"
New-Item -ItemType Junction -Path "modding\BepInEx" -Target "BepInEx"
```

Result: `modding\BepInExModsSource` **is** the clone's source (edit it and git sees the change), and
`modding\BepInEx` **is** the game's BepInEx (so `interop` is always current).

**Option B — plain copies (if you would rather not use links).** With this option you must copy your
edits back into `dimraethmoddings\BepInExModsSource` before committing them.

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth"
mkdir "modding\BepInEx"
xcopy /E /I /Y "dimraethmoddings\BepInExModsSource" "modding\BepInExModsSource"
xcopy /E /I /Y "BepInEx\core"    "modding\BepInEx\core"
xcopy /E /I /Y "BepInEx\interop" "modding\BepInEx\interop"
```

### 6.4 Confirm it worked

```powershell
Test-Path "modding\BepInEx\core\BepInEx.Core.dll"                          # True
Test-Path "modding\BepInEx\interop\Assembly-CSharp.dll"                    # True
Test-Path "modding\BepInExModsSource\ContagionTuner\ContagionTuner.csproj" # True
```

If any line is `False`, redo step 6.3.

---

## 7. Build your first mod

We will build **ContagionTuner** (a small, self-contained mod). Any other mod works the same way.

In **Command Prompt** or **PowerShell**:

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource\ContagionTuner"
dotnet build -c Release
```

Expected output:

```
  ContagionTuner -> D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInEx\plugins\ContagionTuner.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Two things just happened:

1. `dotnet` compiled the mod against the interop assemblies.
2. A **post-build step copied** `ContagionTuner.dll` (and `.pdb`) into the **game's**
   `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins\` folder. Verify it:

   ```powershell
   Get-ChildItem "D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins\ContagionTuner.*"
   ```

   You should see `ContagionTuner.dll` with today's timestamp.

> **The game must be closed while building.** If the game is running it holds the DLL in
> `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\plugins`, and the copy step fails with a "file in use" error. Close the game,
> then rebuild.

> **Building every mod at once:** from `D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource` you can loop over all
> projects:
> ```powershell
> Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object { dotnet build $_.FullName -c Release }
> ```

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

## 10. Build with an IDE (VS Code / Visual Studio / Rider)

You do **not** need an IDE — `dotnet build` is enough. But an IDE gives you autocomplete against
the game's classes, which makes modding much easier.

**The golden rule for all IDEs:** open the **game root** (`D:\SteamLibrary\steamapps\common\Dimraeth`) as your workspace/folder. All
paths (`modding\...`, `BepInEx\...`) are relative to it, and the AI/agent configs in this repo
assume that too.

### VS Code (lightweight, recommended)

1. Install VS Code: <https://code.visualstudio.com/>.
2. Install the **C#** extension (by Microsoft) from the Extensions tab.
3. Open the game root:
   ```
   Open Folder… → D:\SteamLibrary\steamapps\common\Dimraeth
   ```
4. Open any mod's `.csproj`; VS Code loads it as the project. Build with **Ctrl+Shift+B**, or run
   the same `dotnet build` command in the integrated terminal.

### Visual Studio 2022 (Windows)

1. **File → Open → Project/Solution…** and open
   `D:\SteamLibrary\steamapps\common\Dimraeth\dimraethmoddings\BepInExModsSource\<Mod>\<Mod>.csproj`.
   (You can also create a solution that contains several mod projects.)
2. Choose the **Release** configuration and **Build → Build Solution**.
3. The post-build step deploys automatically.

### JetBrains Rider

1. **Open** `D:\SteamLibrary\steamapps\common\Dimraeth\dimraethmoddings\BepInExModsSource\<Mod>\<Mod>.csproj`.
2. Build the project (Ctrl+F9). The post-build copy handles deployment.

> **Autocomplete against game types:** once the project loads, types such as `ObjectsCommon`,
> `Damage`, `Damages`, `Player`, or `ContagionPrefab` resolve because the `.csproj` references the
> generated `Assembly-CSharp.dll`. If they appear in red, the interop references are not resolving —
> see [section 12](#12-troubleshooting).

---

## 11. Build and code with an AI assistant (opencode)

You can let an AI do the heavy lifting: explore the game's classes, write a patch, build it, and fix
compiler errors — all from a chat. This repository is already set up for
**[opencode](https://opencode.ai)**, an open-source CLI coding agent (you can also use any other
agent that can run shell commands).

### 11.1 Install opencode

Follow the install instructions at <https://opencode.ai>. Then verify it runs:

```bat
opencode --version
```

### 11.2 Give it a configuration

This repo ships a **redacted sample** config, `opencode.example.json`. Copy it and edit it:

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth\dimraethmoddings"
copy opencode.example.json opencode.json
```

Open `opencode.json` and set up your model provider. The sample uses an OpenAI-compatible provider
named `midas`; you can keep it and set the API key as an **environment variable**, or replace the
`provider` block with any provider opencode supports (OpenAI, Anthropic, a local model, …). Example
of the important part:

```jsonc
{
  "$schema": "https://opencode.ai/config.json",
  "provider": {
    "midas": {
      "npm": "@ai-sdk/openai-compatible",
      "options": { "baseURL": "https://midas-stage.telkomdigital.id/v1", "apiKey": "{env:MIDAS_API_KEY}" }
    }
  },
  "model": "midas/deepseek-v4-pro"
}
```

```powershell
$env:MIDAS_API_KEY = "your-key-here"
```

> **Never commit `opencode.json` or an API key.** It is already in `.gitignore`. Use the environment
> variable (`{env:...}`) rather than pasting the key into the file.

The sample also configures two optional MCP helpers used by this project:

- **CodeGraph** (`.codegraph/`): a pre-built index of the mod source that answers "where is X / how
  does X work" in one call, including the game-class call paths.
- **Memento**: persistent memory across sessions.

They are convenient but optional — delete those blocks if you do not want them.

### 11.3 Start it in the game root

```bat
cd /d "D:\SteamLibrary\steamapps\common\Dimraeth"
opencode
```

Open the agent at the **game root** so it can see both `modding\` and `BepInEx\`. It reads the
`AGENTS.md` file in the folder for project rules and habits.

### 11.4 Example prompts

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

## 12. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `dotnet` is not recognized | SDK not installed / not on `PATH` | Install the .NET SDK (step 5), reopen the terminal, or call the full path to `dotnet.exe` |
| `Could not resolve this reference … Assembly-CSharp` / `UnityEngine.*` / `Il2Cppmscorlib` | Interop assemblies missing | Run the game once with BepInEx installed; confirm `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\interop\Assembly-CSharp.dll` exists (step 4) |
| `Could not resolve … BepInEx.Core` / `BepInEx.Unity.IL2CPP` | `modding\BepInEx\core` missing or wrong | Re-do step 6.3 and confirm `modding\BepInEx\core\BepInEx.Core.dll` exists |
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

## 13. What is inside this repository

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
opencode.example.json   Redacted sample of the local AI agent config
.gitignore
README.md               ← you are here
```

---

## 14. Included mod projects

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

## 15. Analysis tools

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

## 16. Notes, secrets and licensing

- **Secrets:** `opencode.json` is excluded because it can contain an API key. Copy
  `opencode.example.json` → `opencode.json` and provide the key via an environment variable
  (`{env:MIDAS_API_KEY}`). **Never** commit keys, tokens, or credentials.
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
