# 🔥 Dimraeth - Hell Mode Difficulty & Level 75 Uncapper 🔥

> *Think Hard Mode is too easy? Welcome to Hell.*

**Hell Mode** is a complete difficulty overhaul and progression expansion for *Dimraeth*. It introduces an ultra-challenging 4th difficulty tier designed to test your mastery of combat, builds, and survival. 

To give you the power needed to survive this onslaught, Hell Mode uncaps character progression from Level 25 all the way to **Level 75**, complete with dynamic enemy scaling, accelerated EXP, and seamless in-game UI integration!

---

## ⚡ Key Features

- 💀 **Brutal Combat Overhaul:** Monsters have **10× Health**, deal **5× Damage**, move **50% faster**, and attack with **2× combat aggression** (halved squad turn cooldowns).
- 📈 **Dynamic Enemy Scaling (+20 Levels):** Monsters will never fall behind your gear or build. They dynamically spawn at **Player Level + 20**!
  - At Level 1, you immediately face Level 21 threats.
  - At Level 25, monsters are Level 45.
  - At Level 75, endgame horrors reach Level 95!
- 🌟 **Level 75 Progression Uncapper (Hell Mode Exclusive):** Vanilla hard-caps characters at Level 25. Hell Mode uncaps this ceiling to Level 75, granting up to **74 attribute points** to forge a true endgame build. Standard worlds remain untouched at the vanilla Level 25 cap.
- 🚀 **3× EXP Progression Multiplier:** Pacing is smoothly tuned so the journey to Level 75 feels engaging and rewarding rather than a tedious grind.
- 🛡️ **100% Save Safe:** Under the hood, the engine serializes Hell worlds as "Hard". Your saves remain completely safe from corruption, crashes, or unmodded play.

---

## 📊 Difficulty Comparison

| Feature | Easy | Moderate | Hard | 🔥 Hell Mode 🔥 |
| :--- | :---: | :---: | :---: | :---: |
| **Monster Health** | 0.85× | 1.00× | 1.30× | **10.0×** |
| **Monster Damage** | 0.70× | 1.00× | 1.30× | **5.0×** |
| **Combat Pace & Aggression** | 0.85× | 1.00× | 1.15× | **2.0×** (2× faster attacks) |
| **Monster Move Speed** | 1.00× | 1.00× | 1.00× | **1.5×** (+50% speed) |
| **Elite / Empowerment Chance** | 1.00× | 1.00× | 1.00× | **3.0×** |
| **Monster Level** | Base | Base | Base | **Player Level + 20** |
| **Max Character Level** | Level 25 | Level 25 | Level 25 | **Level 75** |
| **EXP Multiplier** | 1.00× | 1.00× | 1.00× | **3.0×** |
| **Loot & Gold Drops** | 1.00× | 1.00× | 1.00× | **1.00×** (Vanilla intact) |

---

## 🎮 How to Play

Hell Mode seamlessly integrates into the game's menus with a custom 4-tier difficulty slider:

### 1. Creating a World Directly (Worlds Menu)
1. From the Main Menu, click **Worlds** -> **Create New World**.
2. Slide the difficulty slider all the way to the right to **HELL** (glows crimson).
3. Name your world and press **Create World**.

### 2. Creating a World with a New Character
1. Create and customize your character in the Character Creator.
2. On the **Final Selection** screen (where the character's default world is set up), drag the difficulty slider to **HELL**.
3. Press **Start Game** — your character and Hell Mode world are generated and ready to play!

### 3. Converting Existing Worlds
1. On the Character & World selection screen, highlight your existing world.
2. Drag the difficulty slider to **HELL**.
3. Click **Start Game**. The world is instantly converted to Hell Mode!

*(Worlds in Hell Mode display a distinct red **`<color=#FF2222>Hell</color>`** badge in the world list.)*

---

## ⚙️ Configuration File Support

**Yes! Hell Mode fully supports customizable configuration.**

After running the game once with the mod installed, a config file is automatically generated at:
`BepInEx\config\com.custom.hellmodemod.cfg`

You can open this file in any text editor to customize multipliers, progression, or difficulty dials to your personal preference:

```ini
[DifficultyDials]
## Hell Mode monster health multiplier (Default: 10.0x)
MonsterHealthMultiplier = 10.0

## Hell Mode monster damage dealt multiplier (Default: 5.0x)
MonsterDamageDealtMultiplier = 5.0

## Hell Mode combat pace and squad aggression multiplier (Default: 2.0x)
CombatPaceMultiplier = 2.0

## Hell Mode monster move speed multiplier (Default: 1.5x)
MonsterSpeedMultiplier = 1.5

## Hell Mode monster empowerment / elite chance multiplier (Default: 3.0x)
EmpowermentChanceMultiplier = 3.0

## Dynamic bonus levels above player level for monsters in Hell Mode (Default: 20)
MonsterBonusLevelOverPlayer = 20

[Progression]
## Max level cap in Hell Mode (Default: 75)
MaxLevelCap = 75

## Bonus EXP multiplier while playing in Hell Mode (Default: 3.0x)
ExpMultiplier = 3.0

[General]
## Force Hell Mode active on all worlds regardless of slider selection (Default: false)
ForceHellMode = false
```

---

## 📦 Installation

1. **Prerequisite:** Install **BepInEx 6 (IL2CPP)** for *Dimraeth*.
2. Download `HellModeMod.dll`.
3. Drop `HellModeMod.dll` into your `Dimraeth\BepInEx\plugins\` folder.
4. Launch the game and enter Hell!

---

## 🛠️ Technical Details (For Nerds & Modders)

<details>
<summary>Click to view native IL2CPP & memory patch architecture</summary>

- **Native IL2CPP Byte Patching:** Safely patches native level-check instructions in `GameAssembly.dll` using runtime `VirtualProtect` without modifying game files on disk. Vanilla byte signatures are cleanly restored when switching back to non-Hell saves.
- **Engine Serialization Safety:** Hell Mode state is tracked via `<WorldName>.hell` marker files in `%USERPROFILE%\AppData\LocalLow\Mudtek\Dimraeth\Worlds\`. The base engine saves the world as `Difficulty.Hard` internally, avoiding enum index crashes in unmodded environments.
- **Attribute Allocation:** Dimraeth's engine natively supports up to 99 points per attribute, smoothly accommodating all 74 attribute points earned on the path to Level 75.
</details>
