# Dimraeth — Optimal Polarized Attribute Builds (Early Access)

**Scope:** 3 playable races × 3 playable classes = 9 archetypes. For each combo: the best point
distribution to maximize **physical** damage, and the best to maximize **magic** damage.
All builds are **polarized** — they only level stats that feed the chosen damage type, never a
physical+magic hybrid.

**Method:** reverse-engineered from `GameAssembly.dll` (combat-power, upgrade-cost and level-up
routines) plus the serialized `RaceDefinition` / `ClassDefinition` assets in
`Dimraeth_Data\sharedassets1.assets`. Values are derived, not guessed. The allocations below were
produced by an exhaustive search over every point distribution under the real cost curve and the
level-25 cap.

> **Correction (2026-10-08):** an earlier revision of this document listed four invented
> "archetype passives" that changed per-point coefficients (Human·Magician ADV = 0.75 magic,
> Human·Brawler ADV = 0.75 physical, Minotaur·Magician STR = 0.5 magic, Elf·Magician AGI = 1.0
> magic). **Those passives do not exist in the game.** Re-reading `Formulas.CalculatePhysicalCombatPower`
> / `CalculateMagicCombatPower` (ISIL) shows the coefficients are compiled constants applied
> identically to every character; `ClassDefinition`/`RaceDefinition` carry only an `AttributeBonus`
> and an `AttributeBaseCosts`, and `ArchetypeEntry` carries only display data + one combat passive
> that never touches combat power. In particular **Elf·Magician AGI is worth 0.5**, not 1.0.
> Every build below is recomputed from the corrected universal formula (verified by the report's
> own calculator engine).

> **Interactive calculator:** `Dimraeth_Attribute_Calculator.html` (same folder) implements these
> exact formulas as a single-file web app — pick race/class/focus and it optimizes on the fly, with a
> manual sandbox and a purchase-order breakdown. Its engine is verified to reproduce every build in
> this document (all 18 combos). The natural-language prompt to regenerate/build it in AI Studio is
> in `AI_Studio_Dimraeth_Calculator_Prompt.md`.

---

## 1. The formulas that matter

Attribute order (fixed everywhere below):
`Mem, Cha, Adv, Phy, Int, Agi, Str, Ene`

| Damage type | Combat Power |
|---|---|
| **Physical** | `STR + 0.5·(AGI + ADV)` |
| **Magic** | `INT + 0.5·(CHA + ADV)` |

`STR` and `INT` are worth **1.0** per point; `AGI`, `ADV`, `CHA` are worth **0.5**.
**These coefficients are the same for all 9 race/class combos** — no race or class changes them.

### What race and class actually change
Only two things:
- **Starting attributes** — the race's `AttributeBonus` plus the class's `AttributeBonus`.
- **Per-point cost** — `base = ½·(raceBaseCost + classBaseCost)`.

### Cost & level model
- Cost of the p-th point above the starting value:
  `base · (1 + (p−1)/3)^1.1 + playerLevel · 100`
- Attribute cap = 99; **player level cap = 25**.
- XP to reach level 25 = `Σ round(100·L^1.5 + 150)` for `L = 1..24` = **122,409 XP**.
- Spending XP raises the player level; at level 25 upgrades stop. The convex cost curve means you
  realistically buy **44–49 points**, and it means *spreading* spare points into a cheap secondary
  is more efficient than forcing everything into one stat (but only by ~1.5–3 CP).

Because the primary stat is worth 2× a secondary, every build pumps the primary hard, then pours
spare XP into the cheapest secondary of the same damage type.

---

## 2. The builds

`delta` = points added above the starting spread. `CP` = resulting Combat Power (includes starting
stats). Physical secondaries are AGI / ADV; magic secondaries are CHA / ADV.

| Race · Class | **Physical** (delta) | Phys CP | **Magic** (delta) | Mag CP |
|---|---|---|---|---|
| **Human · Magician** | **STR +34**, ADV +8, AGI +2 | 48.5 | **INT +41**, CHA +3, ADV +3 | 58.5 |
| **Human · Brawler** | **STR +41**, ADV +3, AGI +3 | 58.0 | **INT +34**, ADV +8, CHA +2 | 48.5 |
| **Human · Shadow** | **STR +35**, ADV +6, AGI +5 | 53.5 | **INT +35**, ADV +7, CHA +6 | 53.5 |
| **Elf · Magician** | **STR +30**, ADV +9, AGI +9 | 49.5 | **INT +43**, ADV +5 | **60.0** |
| **Elf · Brawler** | **STR +37**, AGI +7, ADV +5 | 58.0 | **INT +35**, ADV +7, CHA +3 | 49.5 |
| **Elf · Shadow** | **STR +34**, AGI +8, ADV +4 | 54.0 | **INT +39**, ADV +5, CHA +2 | 54.5 |
| **Minotaur · Magician** | **STR +37**, ADV +6, AGI +2 | 52.5 | **INT +41**, CHA +3, ADV +3 | 56.0 |
| **Minotaur · Brawler** | **STR +46**, ADV +3 | **63.5** | **INT +34**, ADV +8, CHA +2 | 46.0 |
| **Minotaur · Shadow** | **STR +39**, ADV +5, AGI +2 | 57.5 | **INT +37**, ADV +6, CHA +2 | 50.5 |

### Final attribute spreads (start → final)

| Race · Class | Physical final `[M,C,A,P,I,Ag,S,E]` | Magic final `[M,C,A,P,I,Ag,S,E]` |
|---|---|---|
| Human Magician | 7,7,**14**,5,8,7,**38**,6 | 7,**10**,**9**,5,**49**,5,4,6 |
| Human Brawler | 5,5,**9**,7,4,9,**49**,7 | 5,7,**14**,7,**38**,6,8,7 |
| Human Shadow | 6,6,**12**,4,6,**13**,**41**,6 | 6,**12**,**13**,4,**41**,8,6,6 |
| Elf Magician | 7,7,**15**,3,8,**16**,**34**,6 | 7,7,**11**,3,**51**,7,4,6 |
| Elf Brawler | 5,5,**11**,5,4,**15**,**45**,7 | 5,8,**13**,5,**39**,8,8,7 |
| Elf Shadow | 6,6,**10**,2,6,**18**,**40**,6 | 6,8,**11**,2,**45**,10,6,6 |
| Minotaur Magician | 6,6,**12**,6,6,7,**43**,7 | 6,**9**,**9**,6,**47**,5,6,7 |
| Minotaur Brawler | 4,4,**9**,8,2,6,**56**,8 | 4,6,**14**,8,**36**,6,10,8 |
| Minotaur Shadow | 5,5,**11**,5,4,**10**,**47**,7 | 5,7,**12**,5,**41**,8,8,7 |

### Reference — starting spreads

| Race · Class | Start `[Mem,Cha,Adv,Phy,Int,Agi,Str,Ene]` |
|---|---|
| Human Magician | 7,7,6,5,8,5,4,6 |
| Human Brawler | 5,5,6,7,4,6,8,7 |
| Human Shadow | 6,6,6,4,6,8,6,6 |
| Elf Magician | 7,7,6,3,8,7,4,6 |
| Elf Brawler | 5,5,6,5,4,8,8,7 |
| Elf Shadow | 6,6,6,2,6,10,6,6 |
| Minotaur Magician | 6,6,6,6,6,5,6,7 |
| Minotaur Brawler | 4,4,6,8,2,6,10,8 |
| Minotaur Shadow | 5,5,6,5,4,8,8,7 |

---

## 3. Strict single stat vs. optimal split (all 18 combos)

Still 100% within-type, but slightly weaker. The gain from splitting into a cheap secondary is
small (0.5–3.0 CP) because the primary stat is worth double.

| Combo / focus | Strict single-stat | CP | Optimal CP | Gain |
|---|---|---:|---:|---:|
| Human Magician · Physical | all STR | 46.5 | **48.5** | +2.0 |
| Human Magician · Magic | all INT | 56.5 | **58.5** | +2.0 |
| Human Brawler · Physical | all STR | 56.0 | **58.0** | +2.0 |
| Human Brawler · Magic | all INT | 46.5 | **48.5** | +2.0 |
| Human Shadow · Physical | all STR | 51.0 | **53.5** | +2.5 |
| Human Shadow · Magic | all INT | 51.0 | **53.5** | +2.5 |
| Elf Magician · Physical | all STR | 46.5 | **49.5** | +3.0 |
| Elf Magician · Magic | all INT | 58.5 | **60.0** | +1.5 |
| Elf Brawler · Physical | all STR | 56.0 | **58.0** | +2.0 |
| Elf Brawler · Magic | all INT | 47.5 | **49.5** | +2.0 |
| Elf Shadow · Physical | all STR | 51.0 | **54.0** | +3.0 |
| Elf Shadow · Magic | all INT | 53.0 | **54.5** | +1.5 |
| Minotaur Magician · Physical | all STR | 50.5 | **52.5** | +2.0 |
| Minotaur Magician · Magic | all INT | 54.0 | **56.0** | +2.0 |
| Minotaur Brawler · Physical | all STR | 63.0 | **63.5** | +0.5 |
| Minotaur Brawler · Magic | all INT | 44.0 | **46.0** | +2.0 |
| Minotaur Shadow · Physical | all STR | 56.0 | **57.5** | +1.5 |
| Minotaur Shadow · Magic | all INT | 48.5 | **50.5** | +2.0 |

---

## 4. Hybrid builds (physical **and** magic together)

A hybrid spend is a build that raises both damage types. There are two sensible "best" definitions:

- **Hybrid — max total**: maximise `patk + matk` (best if you genuinely use both damage types).
  Since `patk + matk = STR + INT + ADV + 0.5·(CHA + AGI)`, the best hybrid stats are **ADV (worth
  1.0 combined), then the cheapest of STR / INT**, with AGI / CHA as cheap 0.5 filler.
- **Hybrid — balanced**: maximise the *weaker* of the two (`min(patk, matk)`) so both attacks stay
  usable. Because ADV feeds both sides equally it is the balance lever, so balanced hybrids cluster
  around **~39 / 39**.

Both use the same level-25 cap / 122,409-XP budget as the polarised builds.

### Hybrid — max total

| Race · Class | Delta | patk | matk | Total |
|---|---|---:|---:|---:|
| Human Magician | STR +14, INT +22, ADV +19 | 33.0 | 46.0 | 79.0 |
| Human Brawler | STR +21, INT +15, ADV +19 | 44.5 | 34.0 | 78.5 |
| Human Shadow | STR +13, INT +19, CHA +1, AGI +3, ADV +20 | 37.5 | 41.5 | 79.0 |
| Elf Magician | STR +10, INT +26, AGI +1, ADV +19 | 30.5 | 50.0 | **80.5** |
| Elf Brawler | STR +16, INT +16, CHA +1, AGI +2, ADV +21 | 42.5 | 36.5 | 79.0 |
| Elf Shadow | STR +13, INT +19, CHA +1, AGI +2, ADV +21 | 38.5 | 42.0 | **80.5** |
| Minotaur Magician | STR +13, INT +21, CHA +1, ADV +21 | 35.0 | 44.0 | 79.0 |
| Minotaur Brawler | STR +18, INT +17, ADV +21 | 44.5 | 34.5 | 79.0 |
| Minotaur Shadow | STR +17, INT +17, CHA +1, AGI +1, ADV +20 | 42.5 | 37.0 | 79.5 |

The max-total ceiling is almost flat (~79–80.5 for every combo) — the hybrid cap is set by the
budget, not the archetype. Elf Magician / Elf Shadow edge it with 80.5.

### Hybrid — balanced (equal-ish patk / matk)

| Race · Class | Delta | patk | matk | Total |
|---|---|---:|---:|---:|
| Human Magician | STR +18, INT +14, CHA +1, AGI +3, ADV +20 | 39.0 | 39.0 | 78.0 |
| Human Brawler | STR +14, INT +17, CHA +2, ADV +22 | 39.0 | 38.5 | 77.5 |
| Human Shadow | STR +15, INT +16, CHA +2, AGI +2, ADV +21 | 39.5 | 39.5 | 79.0 |
| Elf Magician | STR +19, INT +16, AGI +1, ADV +19 | 39.5 | 40.0 | 79.5 |
| Elf Brawler | STR +13, INT +18, CHA +1, AGI +1, ADV +22 | 39.5 | 39.0 | 78.5 |
| Elf Shadow | STR +13, INT +16, CHA +1, AGI +3, ADV +23 | 40.0 | 40.0 | **80.0** |
| Minotaur Magician | STR +17, INT +17, AGI +1, ADV +21 | 39.5 | 39.5 | 79.0 |
| Minotaur Brawler | STR +13, INT +21, CHA +1, ADV +20 | 39.0 | 38.5 | 77.5 |
| Minotaur Shadow | STR +13, INT +19, CHA +1, AGI +2, ADV +21 | 39.5 | 39.5 | 79.0 |

### Per-combo comparison

| Race · Class | Best Physical | Best Magic | Hybrid max total | Balanced |
|---|---|---|---|---|
| Human Magician | patk 48.5 | matk 58.5 | 33 / 46 (79.0) | 39 / 39 (78.0) |
| Human Brawler | patk 58.0 | matk 48.5 | 44.5 / 34 (78.5) | 39 / 38.5 (77.5) |
| Human Shadow | patk 53.5 | matk 53.5 | 37.5 / 41.5 (79.0) | 39.5 / 39.5 (79.0) |
| Elf Magician | patk 49.5 | matk 60.0 | 30.5 / 50 (80.5) | 39.5 / 40 (79.5) |
| Elf Brawler | patk 58.0 | matk 49.5 | 42.5 / 36.5 (79.0) | 39.5 / 39 (78.5) |
| Elf Shadow | patk 54.0 | matk 54.5 | 38.5 / 42 (80.5) | 40 / 40 (80.0) |
| Minotaur Magician | patk 52.5 | matk 56.0 | 35 / 44 (79.0) | 39.5 / 39.5 (79.0) |
| Minotaur Brawler | patk 63.5 | matk 46.0 | 44.5 / 34.5 (79.0) | 39 / 38.5 (77.5) |
| Minotaur Shadow | patk 57.5 | matk 50.5 | 42.5 / 37 (79.5) | 39.5 / 39.5 (79.0) |

---

## 5. Key takeaways

1. **Physical = STR for all 9 combos** (1.0 value beats everything), then top up with whichever of
   ADV / AGI is cheaper to buy. No passive biases either one.
2. **Magic = INT for all 9 combos**, then top up with CHA and/or ADV. There is **no** Elf-Magician
   AGI synergy — AGI is worth 0.5 to magic like any non-CHA/ADV stat, and it is simply not a magic
   stat.
3. **Race and class affect only starting attributes and per-point costs.** They never change a
   coefficient.
4. **Best physical archetype:** Minotaur Brawler (CP 63.5; STR base cost only 120).
   **Best magic archetype:** Elf Magician (CP 60.0; INT base cost 140).
   **Weakest magic:** Minotaur Brawler (46.0). **Weakest physical:** Human Magician (48.5).
5. **Base costs decide how deep you can go**, not just bonuses. Notable discounts: Elf AGI 120 /
   INT 140, Minotaur STR 120, Magician INT 120, Brawler STR 120, Shadow AGI 120.
6. **ADV is dual-purpose** (0.5 to both damage types) and appears in almost every build as the cheap
   secondary — that does **not** make the build hybrid, because the opposing type's primary is never
   levelled (no STR on a magic caster, no INT on a bruiser).
7. **Splitting barely beats mono-stat** (0.5–3.0 CP). If you want simplicity, dumping everything
   into STR or INT loses very little; the Elf Magician mono-INT loss is only 1.5 CP.
8. **Hybrids are dominated by budget, not archetype.** Every combo reaches a max-total hybrid of
   ~79–80.5 combined, and every combo reaches a balanced ~39/39. If you want to use both damage
   types, ADV is the star stat (1.0 combined) — pump your cheap of STR/INT, then pour the rest into
   ADV. *But* a hybrid's best single-side number (~39–50) is far below a polarised specialist
   (48.5–63.5); hybrid only pays off if you truly attack with both.

---

## Caveats

- Excludes rune / skill-tree flat & percent bonuses and equipment. (Note: the combat-power formula
  *does* add extra skill-tree terms for specific stats when the relevant node is taken — that is a
  build choice, not a race/class property, and is outside this attribute-optimization scope.)
- Assumes points are purchased cheapest-first and the level-25 cap (122,409 XP) is reached.
- These are additive **combat-power** optima, not DPS. Attack-speed / crit / endurance passives may
  trade a little combat power for speed, evasion or survivability.
- Ties: several combos have multiple allocations with the *same* maximal CP; the delta shown is one
  valid optimum.
