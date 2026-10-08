# Build Prompt — Dimraeth Attribute / Combat-Power Calculator

Paste the entire prompt below into Google AI Studio (or any code-generating LLM).

---

## ROLE

You are a senior front-end engineer and game-systems programmer. You will build a
**complete, production-quality, single-file web calculator** for the game **Dimraeth (Early Access)**.
You must implement the exact attribute, combat-power, upgrade-cost and leveling math specified
below — no approximations, no invented stats, no external libraries.

## DELIVERABLE

- Output **one self-contained `.html` file** containing HTML + CSS + vanilla JavaScript.
- No CDNs, no frameworks, no external assets, no server calls. It must run entirely client-side
  by opening the file in a browser.
- Dark, modern, game-tool aesthetic. Responsive (works on desktop and mobile).
- Clean, commented, modular JS. All magic numbers must come from the data/constants section,
  never be hard-coded inline.

## DOMAIN BACKGROUND (verified from the game binary — treat as ground truth)

Dimraeth has 3 playable races (Human, Elf, Minotaur) and 3 playable classes (Magician, Brawler,
Shadow) → **9 archetypes**. Each archetype has a starting attribute spread = race bonus + class
bonus. Players spend XP to raise individual attributes; spending XP also raises the **character
level**. Character level is capped at **25**; each attribute is capped at **99**.

Attributes always appear in this fixed order (index 0→7):

```
Mem, Cha, Adv, Phy, Int, Agi, Str, Ene
(Memory, Charisma, Adventure, Physique, Intelligence, Agility, Strength, Energy)
```

Only 6 of the 8 attributes affect damage:
- **Physical Combat Power** = `STR + 0.5·(AGI + ADV)`
- **Magic Combat Power**    = `INT + 0.5·(CHA + ADV)`

These coefficients are **universal** — no race or class changes them. Race/class only set the
**starting attributes** and the **per-point costs** (below).

The calculator must **maximize Physical OR Magic combat power** (the user picks one) and never
mix the two damage types — every recommended build is *polarized* (only stats that feed the chosen
damage type are leveled).

---

## DATA (use exactly; embed as a constant object)

Attributes order for every array: `["Mem","Cha","Adv","Phy","Int","Agi","Str","Ene"]`.

```json
{
  "races": {
    "Human":    { "bonus": [3,3,3,3,3,3,3,3], "cost": [200,200,200,200,200,200,200,200] },
    "Elf":      { "bonus": [3,3,3,1,3,5,3,3], "cost": [200,240,200,280,160,120,240,160] },
    "Minotaur": { "bonus": [2,2,3,4,1,3,5,4], "cost": [240,240,200,160,200,280,120,160] }
  },
  "classes": {
    "Magician": { "bonus": [4,4,3,2,5,2,1,3], "cost": [160,160,200,240,120,240,280,200] },
    "Brawler":  { "bonus": [2,2,3,4,1,3,5,4], "cost": [240,240,200,160,280,200,120,160] },
    "Shadow":   { "bonus": [3,3,3,1,3,5,3,3], "cost": [160,240,200,280,200,120,240,160] }
  }
}
```

- Race/class `bonus[i]` is an **integer starting bonus**.
- Race/class `cost[i]` is the **base cost** for that attribute. If a value is `<= 0`, treat it as
  `200`.
- `startAttr[i] = races[race].bonus[i] + classes[class].bonus[i]`.

## PER-POINT DAMAGE COEFFICIENTS

The coefficients are fixed constants, identical for every archetype:

```
physical: { Str: 1.0, Agi: 0.5, Adv: 0.5 }
magic:    { Int: 1.0, Cha: 0.5, Adv: 0.5 }
```

There are **no archetype overrides**. Do not add a per-race/class coefficient table.
(Such "passives" do not exist in the game; `ClassDefinition`/`RaceDefinition` carry only
`AttributeBonus` and `AttributeBaseCosts`, and `ArchetypeEntry` carries only display data.)

All other attributes are worth **0.0** to both damage types (they only feed attack-speed,
endurance, etc. — **ignore them for this calculator**).

## UPGRADE COST FORMULA (exact)

For an attribute with base cost `B` (see averaging below), the cost of the **p-th point above the
starting value** (p = 1 for the first point bought) at **player level L** is:

```
pointCost(p, L) = B · (1 + (p-1)/3)^1.1 + L · 100
```

Where `B = round( 0.5 · (raceCost[i] + classCost[i]) )`, with each raw cost defaulting to `200`
when `<= 0`. (All real values are multiples of 20, so B is an integer; use round-half-up or
round-half-even — it does not matter for the shipped data.)

## LEVEL / XP FORMULA (exact)

```
xpForLevel(L) = roundHalfEven( 100 · L^1.5 + 150 )      // .NET Math.Round default
```

- **Use round-half-to-even** (banker's rounding), NOT JavaScript's `Math.round` (which is
  half-up). Implement `roundHalfEven()` explicitly.
- Character level starts at **1** and is capped at **25** (make the start level a constant, default 1).
- Reaching level 25 requires a cumulative spend of `Σ xpForLevel(L) for L = 1..24 = 122409 XP`
  (verify your implementation reproduces `122409`).

**Level-up mechanic (mirror the game exactly):** track a per-character `progress` counter and
`level`. After every attribute point purchased:
1. `progress += costPaid`
2. `while (level < 25 && progress >= xpForLevel(level)) { progress -= xpForLevel(level); level++; }`

You may **not** purchase an attribute point when `level >= 25` or when the attribute is already at
`99`.

---

## THE OPTIMIZATION ALGORITHM (implement exactly this)

The calculator's "Optimize" must find the allocation that **maximizes the chosen combat power at
the level-25 cap**. Because costs are convex, the optimum is *not* simply "all points in one stat" —
it pumps the primary stat hard and pours remaining XP into the cheaper same-type stats.

Candidate stats = every attribute with a coefficient `> 0` for the chosen damage type.

**Exhaustive search over count vectors (this is the reference algorithm):**

```
MAX_POINTS = 64          // upper bound; no valid build exceeds this
best = { cp: -Infinity }

for total in 0..MAX_POINTS:
    recursively enumerate every count vector c[stat] >= 0 with sum(c) == total:
        result = simulate(counts = c)
        if result.cp > best.cp: best = result
return best
```

**simulate(counts)** — buy points in ascending attribute-cost order (this minimizes the level term
and is therefore optimal):

```
limits[s]     = min(counts[s], 99 - startAttr[s])
nextCost[s]   = B[s]                       // attribute-only cost of that stat's next point
ptr[s]        = 0
level = startLevel (1); progress = 0; spent = 0; bought = {0..8: 0}

loop:
    pick the stat s with ptr[s] < limits[s] and the smallest nextCost[s]
    if none, or level >= 25: break
    cost = nextCost[s] + level * 100
    spent += cost; progress += cost
    bought[s]++; ptr[s]++
    if ptr[s] < limits[s]:
        nextCost[s] = B[s] * (1 + ptr[s]/3)^1.1     // ptr after increment == (p-1) for next p
    while (level < 25 && progress >= xpForLevel(level)) { progress -= xpForLevel(level); level++ }

cp = Σ over candidate stats s of coeff[s] * (startAttr[s] + bought[s])
return { cp, bought, spent, level }
```

Notes:
- The candidates' coefficients/`B` differ per archetype, so re-derive them from the selected race
  and class every time.
- Multiple count vectors can tie on `cp` (there are ties in the real game). Any of them is correct;
  report the `cp` as the invariant and show the allocation you found. Do not "fix" ties by rounding
  `cp`.
- `simulate` must credit only the points actually purchased (the loop stops at level 25); this is
  intentional.

**Also compute a "strict single-stat" comparison**: for each candidate stat, run `simulate` with
`counts` = only that stat (unbounded, i.e. enough) and keep the best; display it next to the
optimal so the user can see what they lose by not spreading.

---

## CALCULATOR FEATURES / UI

**1. Inputs**
- Race selector: Human / Elf / Minotaur (default Human).
- Class selector: Magician / Brawler / Shadow (default Magician).
- Focus toggle: **Physical** | **Magic** (default Physical).
- Optional "Starting level" number input (default 1, clamp 1..25).
- Optional "Level cap" number input (default 25, clamp 1..25).

**2. Optimize panel** (recomputed live on any input change)
Show, for the selected race/class/focus:
- A table per candidate attribute with columns:
  `Attribute | Start | +Points | Final | Coefficient | Contribution to CP`
  (Contribution = coefficient × Final; the CP shown is the sum of contributions across the
  candidate stats — this is why CP includes starting stats).
- Totals: total points added, total XP spent, character level reached, and the resulting
  **Physical CP** and **Magic CP** (show both, highlight the focused one).
- The **purchase order** (the exact sequence of stat names produced by `simulate`) as a compact
  "level this first → …" strip, so a player knows what to buy in order.
- The **strict single-stat** alternative (stat, final value, CP) for comparison.

**3. Manual / sandbox mode**
- Editable `+Points` input per attribute (integers ≥ 0).
- Live recalculation of total XP spent, character level (using the same level-up loop), Physical CP,
  Magic CP, and final attribute values.
- Clear validation: if a point would exceed attribute 99, block it; if the build reaches level 25
  before all entered points are spent, show "budget exhausted at level 25 — N points unbought".
- Show a small "XP remaining to level 25" / "XP overshoot" indicator.

**4. Formula reference**
A collapsible section that prints the exact formulas used (combat power, point cost, XP curve,
level-up rule, caps) so the tool is self-documenting.

**5. Extras**
- A "Copy results as Markdown" button that copies the current optimize table.
- A "Load optimal into sandbox" button that transfers the optimal allocation into manual mode.
- Number formatting: XP with thousands separators; CP with one decimal.

---

## ACCEPTANCE TESTS (the calculator MUST reproduce these)

CP values include starting stats. Arrays are `[Mem,Cha,Adv,Phy,Int,Agi,Str,Ene]` **points added**.
CP tolerance ±0.05.

| Race · Class | Focus | Optimal points added | Expected CP |
|---|---|---|---|
| Human · Magician    | Physical | Str 34, Adv 8, Agi 2                        | 48.5 |
| Human · Magician    | Magic    | Int 41, Cha 3, Adv 3                        | 58.5 |
| Human · Brawler     | Physical | Str 41, Adv 3, Agi 3                        | 58.0 |
| Human · Brawler     | Magic    | Int 34, Adv 8, Cha 2                        | 48.5 |
| Human · Shadow      | Physical | Str 35, Adv 6, Agi 5                        | 53.5 |
| Human · Shadow      | Magic    | Int 35, Adv 7, Cha 6                        | 53.5 |
| Elf · Magician      | Physical | Str 30, Adv 9, Agi 9                        | 49.5 |
| Elf · Magician      | Magic    | Int 43, Adv 5                               | 60.0 |
| Elf · Brawler       | Physical | Str 37, Adv 5, Agi 7                        | 58.0 |
| Elf · Brawler       | Magic    | Int 35, Adv 7, Cha 3                        | 49.5 |
| Elf · Shadow        | Physical | Str 34, Adv 4, Agi 8                        | 54.0 |
| Elf · Shadow        | Magic    | Int 39, Adv 5, Cha 2                        | 54.5 |
| Minotaur · Magician | Physical | Str 37, Adv 6, Agi 2                        | 52.5 |
| Minotaur · Magician | Magic    | Int 41, Cha 3, Adv 3                        | 56.0 |
| Minotaur · Brawler  | Physical | Str 46, Adv 3                               | 63.5 |
| Minotaur · Brawler  | Magic    | Int 34, Adv 8, Cha 2                        | 46.0 |
| Minotaur · Shadow   | Physical | Str 39, Adv 5, Agi 2                        | 57.5 |
| Minotaur · Shadow   | Magic    | Int 37, Adv 6, Cha 2                        | 50.5 |

Also assert:
- `xpForLevel(1) = 250`, `xpForLevel(24) = 11908`, and `Σ xpForLevel(1..24) = 122409`.
- Elf · Magician magic is **INT-primary** (CP 60.0), topping up with ADV only. AGI is worth **0.5**
  to magic (and is not a magic stat), so an evenly-split INT/AGI mage is a bug — do not do it.

If your algorithm produces a different CP than the table, it is a bug — fix the algorithm, do not
change the table.

---

## CONSTRAINTS / DO-NOT LIST

- **Do not** invent stats, symbols, races, classes, or passives beyond those listed.
- **Do not** mix physical and magic stats in one recommended build.
- **Do not** approximate the cost curve or use a simple linear/quadratic cost.
- **Do not** use `Math.round` for the XP curve; use round-half-to-even.
- **Do not** cap points at 25 — the cap is on *character level*, and builds use ~44–54 points.
- **Do not** require a network request or external file; everything is inline.

## EXAMPLE OF EXPECTED BEHAVIOR (worked)

Select **Elf / Magician / Magic**. Expected output:
- Candidate stats & coefficients: `Int×1.0, Cha×0.5, Adv×0.5` (AGI is worth 0.0 to magic — not a
  magic stat).
- Optimal points: `Int +43, Adv +5`.
- Final attributes: `Int 51, Adv 11` (plus unchanged others).
- Total points: 48; level 25.
- **Magic CP = 60.0** — the highest magic CP of all 9 archetypes.
- Strict single-stat: all-INT → CP 58.5.

---

**Start by stating your plan and the data structures, then output the complete single-file HTML.**

