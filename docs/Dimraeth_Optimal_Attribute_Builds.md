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

---

## 1. The formulas that matter

Attribute order (fixed everywhere below):
`Mem, Cha, Adv, Phy, Int, Agi, Str, Ene`

| Damage type | Combat Power |
|---|---|
| **Physical** | `STR + 0.5·(AGI + ADV)` |
| **Magic** | `INT + 0.5·(CHA + ADV)` |

Formula `F`: `STR` and `INT` are worth **1.0** per point; `AGI`, `ADV`, `CHA` are worth **0.5**.

### Archetype passives that change per-point value
(All other passives — attack-speed and endurance — do **not** affect damage.)

| Combo | Passive | Effect |
|---|---|---|
| Human · Magician | +0.25 Magic / ADV | ADV = **0.75** magic |
| Human · Brawler | +0.25 Physical / ADV | ADV = **0.75** physical |
| Minotaur · Magician | +0.5 Magic / STR | STR = **0.5** magic |
| Elf · Magician | +0.5 Magic / AGI | AGI = **1.0** magic |
| Human/Elf/Minotaur · Shadow; Elf/Minotaur · Brawler | atk-speed / endurance | none |

### Cost & level model
- `base = ½·(raceBaseCost + classBaseCost)` for the attribute.
- Cost of the p-th point above the starting value:
  `base · (1 + (p−1)/3)^1.1 + playerLevel · 100`
- Attribute cap = 99; **player level cap = 25**.
- XP to reach level 25 = `Σ round(100·L^1.5 + 150)` for `L = 1..24` = **≈ 122,400 XP**.
- Spending XP raises the player level; at level 25 upgrades stop. The convex cost curve means you
  realistically buy **44–54 points**, not 25 — and it also means *spreading* spare points into a
  cheap secondary is more efficient than forcing everything into one stat.

Because a primary stat is worth 2× a secondary, every build pumps the primary hard, then pours
spare XP into the cheapest secondary of the same damage type.

---

## 2. The builds

`delta` = points added above the starting spread, in `[Mem, Cha, Adv, Phy, Int, Agi, Str, Ene]`.
`CP` = resulting Combat Power (includes starting stats).

| Race · Class | **Physical** (delta) | Phys CP | **Magic** (delta) | Mag CP |
|---|---|---|---|---|
| **Human · Magician** | **STR +34**, ADV +8, AGI +2 | 48.5 | **INT +34**, ADV +16, CHA +1 | 62.5 |
| **Human · Brawler** | **STR +34**, ADV +16, AGI +1 | 62.0 | **INT +34**, ADV +8, CHA +2 | 48.5 |
| **Human · Shadow** | **STR +35**, ADV +6, AGI +5 | 53.5 | **INT +35**, ADV +7, CHA +6 | 53.5 |
| **Elf · Magician** | **STR +30**, ADV +9, AGI +9 | 49.5 | **INT +26, AGI +26** (even), CHA +1, ADV +1 | **74.5** |
| **Elf · Brawler** | **STR +37**, ADV +5, AGI +7 | 58.0 | **INT +35**, ADV +7, CHA +3 | 49.5 |
| **Elf · Shadow** | **STR +34**, ADV +4, AGI +8 | 54.0 | **INT +39**, ADV +5, CHA +2 | 54.5 |
| **Minotaur · Magician** | **STR +37**, ADV +6, AGI +2 | 52.5 | **INT +38**, ADV +4, CHA +3, **STR +6** | 59.5 |
| **Minotaur · Brawler** | **STR +46**, ADV +3 | **63.5** | **INT +34**, ADV +8, CHA +2 | 46.0 |
| **Minotaur · Shadow** | **STR +39**, ADV +5, AGI +2 | 57.5 | **INT +37**, ADV +6, CHA +2 | 50.5 |

### Final attribute spreads (start → final)

| Race · Class | Physical final `[M,C,A,P,I,Ag,S,E]` | Magic final `[M,C,A,P,I,Ag,S,E]` |
|---|---|---|
| Human Magician | 7,7,**14**,5,8,7,**38**,6 | 7,8,**22**,5,**42**,5,4,6 |
| Human Brawler | 5,5,**22**,7,4,7,**42**,7 | 5,7,**14**,7,**38**,6,8,7 |
| Human Shadow | 6,6,**12**,4,6,**13**,**41**,6 | 6,**12**,**13**,4,**41**,8,6,6 |
| Elf Magician | 7,7,**15**,3,8,**16**,**34**,6 | 7,8,7,3,**34**,**33**,4,6 |
| Elf Brawler | 5,5,**11**,5,4,**15**,**45**,7 | 5,8,**13**,5,**39**,8,8,7 |
| Elf Shadow | 6,6,**10**,2,6,**18**,**40**,6 | 6,8,**11**,2,**45**,10,6,6 |
| Minotaur Magician | 6,6,**12**,6,6,7,**43**,7 | 6,9,**10**,6,**44**,5,**12**,7 |
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

## 3. If you insist on a strict single stat

Still 100% within-type, but slightly weaker:

| Combo / focus | Strict single-stat | CP | Optimal CP |
|---|---|---|---|
| Elf Magician · Magic | all INT | 65.5 | **74.5** |
| Human Magician · Magic | all INT | 58.0 | 62.5 |
| Minotaur Brawler · Physical | all STR | 63.0 | 63.5 |
| Elf Brawler · Physical | all STR | 56.0 | 58.0 |

The Elf Magician is the big loser here: because its passive makes **AGI a full 1.0 magic stat**, a
pure-INT Elf mage leaves ~9 CP on the table. Theorycrafting for an Elf mage should always level
**INT and AGI together, near-evenly**.

---

## 4. Key takeaways

1. **Physical = STR for all 9 combos** (1.0 value beats everything), then top up with the cheapest
   of ADV / AGI. ADV wins when it is the Human Brawler passive (0.75).
2. **Magic = INT for all 9 combos**, except **Elf Magician**, which splits **INT ≈ AGI**.
3. **Human Magician** invests heavily in ADV (0.75 magic); **Minotaur Magician** adds some STR
   (0.5 magic). Both passives turn those stats into magic stats.
4. **Best physical archetype:** Minotaur Brawler (CP 63.5; STR base cost only 120).
   **Best magic archetype:** Elf Magician (CP 74.5). **Weakest magic:** Minotaur Brawler (46.0).
   **Weakest physical:** Human Magician (48.5).
5. **Base costs decide how deep you can go**, not just bonuses. Notable discounts: Elf AGI 120 /
   INT 140, Minotaur STR 120, Magician INT 120, Brawler STR 120, Shadow AGI 120.
6. **ADV is dual-purpose** (0.5 to both damage types) and appears in almost every build as the cheap
   secondary — that does **not** make the build hybrid, because the opposing type's primary is never
   levelled (no STR on a magic caster, no INT on a bruiser).

---

## Caveats

- Excludes rune / skill-tree flat & percent bonuses and equipment.
- Assumes points are purchased cheapest-first and the level-25 cap (≈122,400 XP) is reached.
- These are additive **combat-power** optima, not DPS. Attack-speed / crit / endurance archetypes may
  trade a little combat power for speed, evasion or survivability.
