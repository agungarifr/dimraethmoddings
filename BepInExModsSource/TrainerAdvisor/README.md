# Dimraeth Trainer Advisor

A standalone BepInEx 6 (IL2CPP) plugin that injects a **display-only** panel into the
vanilla trainer window. It reads the local player's current attributes, level, race and
class, then recommends the optimal **physical**, **magic** and **hybrid** ways to spend
the remaining XP up to the level-25 cap.

It never buys, stages, or writes a single point. It exists purely to answer
*"given where I am now, what should I buy next?"*.

## What it does

- Hooks `PlayerUIView.Bind(Player)` and filters to the trainer view (`NPCTrainView`).
- Builds a real Unity UI panel (background `Image` + `TextMeshProUGUI` rich text) parented
  under the trainer window's `_panelRoot`, so it shows and hides with the window.
- Shows:
  - Race · class and current level / 25.
  - XP spent (lifetime) and XP still required to reach the level cap.
  - Current Patk and Matk.
  - Three recommendations — **Physical**, **Magic**, **Hybrid (max total)** — each as a
    list of points to add (`Str +12, Agi +3, …`), the resulting combat power, the
    simulated end level, and XP spent to get there.
  - Discrepancy warnings when the save looks inconsistent.
- Refreshes about 4×/second, and only rebuilds the text when the state actually changes.

## Optimizer basis

The recommendation is computed from the player's **current** attributes, level and XP bar,
not from a fresh level-1 character. It:

1. Derives the XP still needed to reach level 25 from the current level + XP-bar progress
   (`Σ requirements from current level to 24 − AccumulatedXP`). This stays correct after a
   respec, which is why it does **not** trust `AllTimeXP` for the budget.
2. Runs an exact branch-and-bound search (seeded by a greedy lower bound) over the points
   remaining, respecting the cost curve, the per-attribute cap of 99, and the level-25 cap.
3. Reports the physical, magic and hybrid optima. **Hybrid = maximum `patk + matk`**
   (single target).

The engine is a port of the verified calculator in
`docs/Dimraeth_Attribute_Calculator.html`, generalised to start from the player's current
state. Against the reference archetypes in `docs/Dimraeth_Optimal_Attribute_Builds.md` and
`docs/Dimraeth_Calculated_Attribute_Builds.md` it reproduces all 27 optimal values
(9 race/class combos × patk/matk/hybrid) exactly.

### Game formulas used

| Quantity | Formula |
| --- | --- |
| Physical combat power | `STR + 0.5·(AGI + ADV)` |
| Magic combat power | `INT + 0.5·(CHA + ADV)` |
| Upgrade cost | `BaseCost·(1 + (p−1)/3)^1.1 + level·100` |
| XP to next level | `roundHalfEven(100·L^1.5 + 150)` |

where `p` is the number of points already bought above the attribute's starting value.

### Recommended Level-25 spreads (reference)

Format `[Mem, Cha, Adv, Phy, Int, Agi, Str, Ene]`.

| Combo | Physical | Magic | Hybrid (max total) |
| --- | --- | --- | --- |
| Human Magician | `[7,7,14,5,8,7,38,6]` Patk 48.5 | `[7,10,9,5,49,5,4,6]` Matk 58.5 | `[7,8,26,5,22,8,22,6]` 39.0 / 39.0 |
| Human Brawler | `[5,5,9,7,4,9,49,7]` Patk 58.0 | `[5,7,14,7,38,6,8,7]` Matk 48.5 | `[5,7,28,7,21,6,22,7]` 39.0 / 38.5 |
| Human Shadow | `[6,6,12,4,6,13,41,6]` Patk 53.5 | `[6,12,13,4,41,8,6,6]` Matk 53.5 | `[6,8,27,4,22,10,21,6]` 39.5 / 39.5 |
| Elf Magician | `[7,7,15,3,8,16,34,6]` Patk 49.5 | `[7,7,11,3,51,7,4,6]` Matk 60.0 | `[7,7,25,3,24,8,23,6]` 39.5 / 40.0 |
| Elf Brawler | `[5,5,11,5,4,15,45,7]` Patk 58.0 | `[5,8,13,5,39,8,8,7]` Matk 49.5 | `[5,6,28,5,22,9,21,7]` 39.5 / 39.0 |
| Elf Shadow | `[6,6,10,2,6,18,40,6]` Patk 54.0 | `[6,8,11,2,45,10,6,6]` Matk 54.5 | `[6,7,29,2,22,13,19,6]` 40.0 / 40.0 |
| Minotaur Magician | `[6,6,12,6,6,7,43,7]` Patk 52.5 | `[6,9,9,6,47,5,6,7]` Matk 56.0 | `[6,6,27,6,23,6,23,7]` 39.5 / 39.5 |
| Minotaur Brawler | `[4,4,9,8,2,6,56,8]` Patk 63.5 | `[4,6,14,8,36,6,10,8]` Matk 46.0 | `[4,5,26,8,23,6,23,8]` 39.0 / 38.5 |
| Minotaur Shadow | `[5,5,11,5,4,10,47,7]` Patk 57.5 | `[5,7,12,5,41,8,8,7]` Matk 50.5 | `[5,6,27,5,23,10,21,7]` 39.5 / 39.5 |

Only the three playable races (Human, Elf, Minotaur) and three playable classes
(Magician, Brawler, Shadow) are supported; other combos show an
*"optimizer unavailable"* line.

## Install

The build copies `DimraethTrainerAdvisor.dll` straight into `BepInEx\plugins\`. Requires
BepInEx 6 IL2CPP (the same runtime the other mods here use).

## Build

```powershell
& 'C:\Users\game\.dotnet\dotnet.exe' build `
  'D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource\TrainerAdvisor\DimraethTrainerAdvisor.csproj' `
  -c Release
```

Output path is `..\..\..\BepInEx\plugins\` (the game root plugin folder). Targets
`net6.0`; references the BepInEx 6 core + interop DLLs in the repo (no NuGet packages).

## Config

`BepInEx\config\dev.dimraeth.traineradvisor.cfg`

| Key | Default | Meaning |
| --- | --- | --- |
| `General.Enabled` | `true` | Inject the advisor panel into the trainer window. |
| `General.AnchorInside` | `false` | Dock the panel inside the window's right edge instead of just outside it. Enable if the outside panel is clipped off-screen. |
| `General.PanelWidth` | `330` | Width in pixels of the panel. |
| `General.PanelHeight` | `420` | Height in pixels when docked outside the window. |

## Notes / limits

- **Display only.** There are no auto-apply or stage buttons; the panel cannot change your
  build.
- The panel docks just outside the trainer window's right edge by default so it never
  covers the vanilla layout. Toggle `AnchorInside` if it goes off-screen.
- A `LayoutElement.ignoreLayout = true` is added so the panel can't disturb the trainer
  window's own layout group.
- The "XP/level mismatch" warning fires when `AllTimeXP` disagrees with the lifetime XP
  implied by the current level + XP bar (e.g. an edited save). It is informational only.
- Points spent on Memory / Physique / Energy give no combat power and are reported as a
  warning; they are already reflected in the current attributes the optimizer starts from.
