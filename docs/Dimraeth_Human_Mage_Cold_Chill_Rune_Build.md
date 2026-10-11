# Dimraeth — Human Magician Cold / Chill Rune Build (Arrow of the Deep Cold)

**Build:** Human · Magician (Elemental Ranger archetype), **matk** focus.
**Core loop:** apply **Chill** to the target (Gust with the Gusting 3pc, plus Chill skills),
then spam **ranged crossbow basic attacks** — every hit on a Chilled enemy triggers
**Arrow of the Deep Cold**, which fires **one giant piercing ice arrow per Chill stack**,
up to 10, in sequence (one every 0.2 s). *Vanilla* fires a single arrow only on the **third**
ranged attack of a combo; the `ArrowOfTheDeepColdTuner` mod widens it to **every** ranged hit
and scales the count with Chill stacks — so **Chill stacks are the real multiplier**.

> Practical note: this is a **cold** build. If you have the **`PyromancerConverter`** mod
> installed it remaps the Magician's Chill/Ice stats to Burning/Fire — **disable it** for this
> build or your Chill application is gutted.

**Requested stats — where they actually come from:**

| Stat | Real source |
|---|---|
| **matk** | Magic Power from INT + gear main "Magic Damage"; `+% Magic Damage` from Gusting / Precision Fang / Convergence set bonuses |
| **Ice Damage** | **Gusting 4pc (+12)**, **Frostward 2pc (+8)**, gear main/secondary |
| **Crit Damage** | **No cold set grants it as a bonus** — only **Amulet & Badge main stats** + secondary rolls |
| **Damage vs Chilled** | **No rune set grants it** — this comes from the **skill tree** (`… vs Chilled` / `ColdScaledPower` / `WinterFocus` nodes). The only cold set passive is **Winterguard** (defensive) |
| **Chill stacks (= arrows fired)** | **Gusting 3pc** (Gust applies 6 Chill) + **Max Chill / Chill Power / Chill Duration** affixes (Rune, Ring, Bracer mains) |
| **Attack Speed** | More auto-attacks = more procs. Only set bonus is **Duskbloom 6pc (+7 AS)**; otherwise affixes |

## Recommended lineup — Gusting 4 + Precision Fang 3 + Frostward 3 + Convergence 2

| Set | Pieces | Bonus |
|---|---|---|
| **Gusting** | 4 | 3pc **Frostbiting Gust** — Gust deals Ice Damage and applies **6 Chill**; 4pc **+12 Ice Damage, +8% Magic Damage** |
| **Precision Fang** | 3 | 3pc **+10 Ether Damage, +6% Magic Damage** (5pc Guided Fang is an Aetherfang skill — skip) |
| **Frostward** | 3 | 2pc **+8 Ice Damage**; 3pc **+4% HP** |
| **Convergence** | 2 | **+5% Magic Damage, +3 Spell Haste** |

**Totals: +20 Ice Damage · +19% Magic Damage · +3 Spell Haste · +4% HP · +10 Ether · Frostbiting Gust.**
This is the highest **guaranteed** damage multiplier available to the keystone: the arrow scales
off Magic Power and Ice Damage, and this lineup maxes both set-bonus sources of `+% Magic Damage`
(Gusting 8 + Precision Fang 6 + Convergence 5 = **19%**).

### Alternatives

1. **Tanky / solo: Gusting 4 + Frostward 6 + Convergence 2** → **+20 Ice, +13% Magic, +4% HP,
   +16 Armor, Winterguard (Chilled enemies deal 10% less damage to you)**, +3 Spell Haste.
   Costs 6% Magic Damage vs the recommendation but is far safer when you are face-tanking while
   stacking Chill.
2. **Crit / speed: Gusting 4 + Lionspear 4 + Convergence 2 + Frostward 2** → +20 Ice, +13% Magic,
   **+4% Crit Rate, +5 Attack Speed**, +3 Spell Haste. Take if your crit chance is already high
   (the keystone fires on auto-attacks, so Attack Speed is a genuine DPS stat).
3. **Budget / early: Gusting 4 + Convergence 2 + Frostward 2 + Precision Fang 2** — grab the
   Gusting 4pc first (it is the whole build), then add flat Ice / Magic as pieces drop.

⚠️ **Trap sets to avoid:**
- **Chillhowl** (3pc Channeled Call, 5pc Call To The Moon) only modifies the **Alpha Howl**
  channel skill — it does **nothing** for the Arrow build despite the cold-themed name.
- **Convergence 6pc (Overchannel, +20% Magic for 5 s when you spend >30 Concentration at once)**
  never triggers here: the keystone is a passive proc off auto-attacks and spends no
  Concentration. Do **not** chase it.

## Slot layout (12 rune slots)

`Rune · Undercoat · Belt · Charm · Ring1 · Ring2 · Amulet1 · Amulet2 · Badge1 · Badge2 · Bracer1 · Bracer2`

```
Rune      Gusting      main: Ice Damage / Magic Damage / Chill Power
Undercoat Frostward    main: Health / Armor         (low-impact slot)
Belt      Convergence  main: Health / Resistance    (low-impact slot)
Charm     Convergence  main: Concentration
Ring 1    Gusting      main: Ice / Magic / Chill Power / Attack Speed
Ring 2    Gusting      main: Ice / Magic / Chill Power / Attack Speed
Amulet 1  Gusting      main: CRITICAL DAMAGE
Amulet 2  Precision Fang main: CRITICAL DAMAGE
Badge 1   Precision Fang main: Magic Damage / CRITICAL DAMAGE
Badge 2   Precision Fang main: Magic Damage / CRITICAL DAMAGE
Bracer 1  Frostward    main: MAX CHILL / Chill Power / Chill Duration
Bracer 2  Frostward    main: MAX CHILL / Chill Power / Chill Duration
```

This puts the offensive sets on the offensive slots (Rune/Ring = Ice/Magic, Amulet/Badge = Crit,
Bracer = Chill) and parks the low-impact slots (Undercoat/Belt/Charm) on Health/Concentration,
while still landing every breakpoint: Gusting 4, Precision Fang 3, Frostward 3, Convergence 2.

## Per-piece stat priorities

- **Main:** Amulet/Badge = **Critical Damage** (take Crit Chance first if crit is still low).
  Rune/Ring = **Ice Damage** (scales the arrows), then **Magic Damage**, then **Chill Power**.
  Bracer = **Max Chill** (reach 10 stacks faster = more arrows), then Chill Power / Chill Duration.
  Charm = Concentration / Concentration Regen.
- **Secondaries:** Ice Damage · Magic Damage · Ice Piercing · Magic Piercing ·
  **Crit Chance → Crit Damage** · **Attack Speed** (more procs) · Chill Power / Max Chill /
  Chill Duration · Max Frostbound / Frostbound Duration · Conc Regen.
- **Attribute:** Human·Magician, INT-first — **INT +41, CHA +3, ADV +3 (matk 58.5)**.
  See `Dimraeth_Optimal_Attribute_Builds.md`.
- **Stars:** level cap is 25, so **6★ is the max wearable** (7★+ needs level 30+). Push 6★ on the
  damage pieces (Rune / Ring / Amulet / Badge).

## How to play it

1. Open with **Gust** (Gusting 3pc makes it apply **6 Chill** instantly) and any other Chill
   skill to ramp the target to **10 Chill**.
2. Hold **ranged crossbow auto-attacks** — with the tuner every hit re-triggers the keystone,
   firing a giant piercing ice arrow **per Chill stack** (up to 10), 0.2 s apart.
3. Keep the target Chilled; a Chilled target is both the trigger condition and (via the skill
   tree) the damage-taken debuff. Re-apply Chill as it falls off.

## Relevant set bonuses (reference)

- **Gusting:** 3pc Frostbiting Gust (Gust → Ice + 6 Chill); 4pc +12 Ice Damage, +8% Magic Damage.
- **Frostward:** 2pc +8 Ice Damage; 3pc +4% HP; 6pc +16 Armor, Winterguard (Chilled enemies deal
  10% less damage to you).
- **Convergence:** 2pc +5% Magic Damage, +3 Spell Haste; 3pc +10 Conc Regen; 6pc +16 Conc +
  Overchannel (trap here).
- **Precision Fang:** 3pc +10 Ether Damage, +6% Magic Damage; 5pc Guided Fang (skill — skip).
- **Lionspear:** 3pc +10 Thrust Damage, +4% Crit Rate; 4pc +5 Attack Speed; 6pc +30% Crit Multi.
- **Dazzler:** 3pc +4 Spell Haste, +10 Conc Regen (4pc is stun — skip for a cold build).
- **Chillhowl:** 3pc/5pc modify Alpha Howl only — **not usable** for the Arrow build.

## Farm sources

- **Gusting** — Wildwood Aetherfang Wolf (lvl 10, 20% of its runes); 6 chests in **Wildwood
  Forest Vc/Vd**; *Return to the Forest Throne* bounty T3.
- **Precision Fang** — 6 chests in **Wildwood Forest Vc/Vd**; Wildwood Aetherfang Wolf (same
  bounty). No dedicated creature — it is a chest/bounty set.
- **Frostward** — **Goblin Basher** (lvl 4, 33.3% per kill); best chest **Corrupted Thicket**
  (20%); 46 chests total.
- **Convergence** — Dryad Beastwarden / Rivercaller / Rootweaver (33.3%), Treant, Rotting Treant;
  best chest **Wildwood Forest IIA** (10%); 46 chests total.
- **Lionspear** — Wildwood Aetherfang Wolf; Wildwood Forest Vc/Vd chests (crit alternative).

*Data source: set bonuses from dimraethdb.com (game build 0.107.7822, 25684114); vanilla keystone
behaviour verified in `modding/cpp2il_new_isil` (`Damages.PassiveChecks`); cold set passives
verified in `modding/…/Passive.cs` / `Effect.cs` (Winterguard, Frostbiting Gust, Overchannel).*
