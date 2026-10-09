# Dimraeth — Human Shadow Hybrid Rune Build (Thrust / Magic / Decay)

**Build:** Human · Shadow hybrid — physical + magic.
**Core skills:** Hail of Arrows (Thrust, poison applicator), Contagion (Decay, poison
applicator + burst), Plague Marksmanship (Gloom Stalker keystone — consumes 5 Poison on a
ranged/crossbow hit for a Decay burst).
**Damage types:** Thrust, Decay, Magic. **Pathweaver is a required 6-piece core.**

> Data source: set bonuses from dimraethdb.com, game build 0.107.7779. Rune slot/set enums
> verified in `modding/DecompilerTool/rune_out/Runes.cs`.

## Slot layout (12 rune slots)

`Rune · Undercoat · Belt · Charm · Ring1 · Ring2 · Amulet1 · Amulet2 · Badge1 · Badge2 · Bracer1 · Bracer2`

The optimal split is **6 Pathweaver + two 3-piece sets** (6 + 3 + 3 = 12). Pathweaver's value
is concentrated at 6pc, while the flat +Thrust / +Decay sets peak at 2–3 pieces — so nothing is
wasted and no un-reachable 5/6-piece bonus is left on the table.

## Recommended lineup — Pathweaver 6 + Forestlinked 3 + Graveveil 3

| Set | Pieces | Bonus used |
|---|---|---|
| **Pathweaver** | 6 | *Dualpath* (3pc): after Physical Damage, gain +3%/stack Magic Damage; after Magic Damage, gain +3%/stack Physical Damage, 4s, max 5. *Pathsurge* (6pc): raises both to **7%/stack → 35% Physical + 35% Magic at cap**. |
| **Forestlinked** | 3 | **+10 Thrust Damage, +10 Decay Damage** (5pc Extra Heart is not reached). |
| **Graveveil** | 3 | 2pc **+8 Decay Damage**; 3pc **+10 Thrust Damage** (5pc MS/Gravecross is not reached). |

**Flat totals: +20 Thrust Damage, +18 Decay Damage** — exactly the two damage types the build
deals, so nothing is wasted.

### Why this wins

- Pathweaver 6pc already grants both Physical% and Magic%, so the best complement is **flat
  Thrust** (scales the physical side and Hail of Arrows direct hits) plus **flat Decay** (scales
  Contagion and the Plague Marksmanship burst). Forestlinked + Graveveil is the only pairing
  that delivers both without committing to a third set.
- The rotation alternates naturally — Hail/auto (physical) → Contagion (magic) → Hail — which
  keeps Dualpath/Pathsurge stacks at maximum uptime.
- Every one of the 6 "other" slots contributes a used bonus.

### Alternatives (ranked)

1. **Pathweaver 6 + Forestlinked 3 + Putrid 3** — **+20 Decay, +10 Thrust** plus *Putrid
   Eruption* (Decay AoE; water → Poison). Best when Decay is >60% of your damage and you fight
   groups.
2. **Pathweaver 6 + Forestlinked 3 + Convergence 3** — +10 Thrust/+10 Decay, **+5% Magic
   Damage, +3 Spell Haste, +10 Concentration Regen**. Best sustain; helps afford Contagion
   casts. Slightly lower raw burst.
3. **Pathweaver 6 + Forestlinked 3 + Lionspear 3** — +10 Thrust, **+4% Crit Rate**. Take only
   if crit chance is already high; crit multiplies Hail hits and the Plague Marksmanship burst.

## Per-piece stat priorities

**Primary (main stat):**
1. **Decay Damage** — highest DPS weight (Contagion + Plague Marksmanship).
2. **Thrust Damage** — drives Hail of Arrows.
3. **Physical Damage / Magic Damage** — feeds Pathweaver's Dualpath/Pathsurge multiplier.

**Secondary (affixes), roughly in order:**
- Thrust Piercing / Decay Piercing
- Critical Chance → Critical Damage
- Attack Speed & Spell Haste (more pulses, faster poison ramp)
- Concentration & Concentration Regen (Charm naturally rolls Stamina, not Conc — park a
  Pathweaver Charm there and take Conc from the set bonus)
- Poison Damage / Max Poison / Poison Duration (poison is the enabler for Plague Marksmanship)
- Movement Speed (Graveveil 5pc is not reached, so source MS from affixes)

## Practical notes

- **Level cap 25 → max wearable rune is 6★** (7★+ requires level 30+). Prioritize 6★ on
  damage pieces (Rune / Ring / Amulet), 5–6★ elsewhere.
- **Attribute spread (Human Shadow):** balanced **39.5 / 39.5**, or max-total **37.5 / 41.5**.
  See `Dimraeth_Optimal_Attribute_Builds.md`.
- **Farm sources:**
  - Pathweaver + Blightseed → *Corrupted Treant* (Corrupted Thicket)
  - Forestlinked + Thornborn → *Aelwynor, The Wildroot Father*
  - Graveveil → Goblin Caves / Wildwood IV–V
  - Convergence → Dryad Beastwarden / Treant / Wildwood
  - Putrid → *Corrupted Monstrous Droop*

## Relevant set bonuses (reference)

- **Pathweaver:** 3pc Dualpath (as above); 6pc Pathsurge (7%/stack).
- **Forestlinked:** 3pc +10 Thrust, +10 Decay; 5pc Extra Heart.
- **Graveveil:** 2pc +8 Decay; 3pc +10 Thrust; 5pc +7% MS + Gravecross.
- **Putrid:** 3pc +10 Decay + Putrid Eruption; 5pc Expansive Eruption.
- **Gloomfang:** 2pc +8 Decay +8 Conc; 4pc +12 Conc Regen; 6pc +16 HP + Rotwound.
- **Convergence:** 2pc +5% Magic Damage, +3 Spell Haste; 3pc +10 Conc Regen; 6pc +16 Conc +
  Overchannel.
- **Lionspear:** 3pc +10 Thrust +4% Crit Rate; 4pc +5 AS; 6pc +30% Crit Multi + Lionsurge.
