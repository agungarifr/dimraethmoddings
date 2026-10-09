# Dimraeth — Human Mage Fire / Burning Rune Build (Ember Cascade)

**Build:** Human · Magician (Elemental Ranger archetype), **matk** focus.
**Core loop:** stack **10 Burning** on the target (Fireball + Conflagration), then spam
**ranged crossbow basic attacks** — each hit on a Burning enemy triggers **Ember Cascade**,
which fires a small fireball per Burning stack, **up to 10** (Fire, `0.11×` each; nerfed
from `0.15` in build 0.107.7779).

> Testing note (community, build 0.107.7779): **Ember Cascade only triggers from normal ranged
> attacks — Rain of Arrows does not trigger it.** Stay in crossbow mode (Weapon Swap, then
> remove the swap skill from the bar).

**Requested stats — where they actually come from:**

| Stat | Real source |
|---|---|
| **matk** | Magic Power from INT + gear main "Magic Damage"; `+% Magic Damage` from Convergence / Pyreweaver set bonuses |
| **Crit Damage** | **No fire/magic set grants it as a bonus** — only **Amulet & Badge main stats** + secondary rolls |
| **Fire Damage** | Pyreweaver 2pc (+8), Burnward 2pc (+8), gear main/secondary |
| **Damage vs Burning** | **Pyreweaver 6pc — Wildfire: burning enemies take +15% increased damage from you** (the only set bonus for it); skill-tree `…vs Burning` nodes supply the rest |

## Recommended lineup — Pyreweaver 6 + Enflamed 4 + Convergence 2

| Set | Pieces | Bonus |
|---|---|---|
| **Pyreweaver** | 6 | 2pc **+8 Fire Damage, +5% Magic Damage**; 3pc +4 Spell Haste; 6pc **+7% HP + Wildfire (+15% damage to Burning enemies)** |
| **Enflamed** | 4 | 2pc **+8% Burning Damage, +3 Spell Haste**; 4pc **Everburn** — casting a Fire Spell fires a mortar shot (0.68× Fire) |
| **Convergence** | 2 | **+5% Magic Damage, +3 Spell Haste** |

**Totals: +10% Magic Damage · +8 Fire Damage · +8% Burning Damage · +10 Spell Haste · +7% HP · Wildfire (+15% vs Burning) · Everburn.**
This is the only 12-slot lineup that hits all four requested stats with no dead breakpoint.

### Alternatives

1. **Max raw stats (single-target): Pyreweaver 6 + Burnward 2 + Convergence 2 + Enflamed 2**
   → **+16 Fire Damage, +10% Magic Damage, +8% Burning Damage, +10 Spell Haste, +7% HP,
   Wildfire, +12% Fire Resistance.** More flat Fire, no Everburn.
2. **Matk ceiling (risky): Pyreweaver 6 + Convergence 6** → up to **+30% Magic Damage**
   (Overchannel +20%), +16 Concentration, +10 Concentration Regen. ⚠️ **Overchannel requires
   spending over 30 Concentration at once, and no Fire spell exceeds 30 (Inferno/Meteorite = 30),
   so it may never trigger.** Treat as a trap unless a cost modifier is found.
3. **Budget / early: Pyreweaver 2–3 + Enflamed 2 + Burnward 2** for fast breakpoints, then push
   Pyreweaver to 6 (Wildfire is the priority).

## Slot layout

Sets fill all 12 slots, so assign by the best main-stat pool per slot.

```
Rune      Pyreweaver   main: Burning / Fire / Magic Damage
Undercoat Convergence  main: Health / Armor        (low-impact slot)
Belt      Convergence  main: Health / Resistance   (low-impact slot)
Charm     Enflamed     main: Concentration
Ring 1    Pyreweaver   main: Magic / Fire / Burning / Attack Speed
Ring 2    Pyreweaver   main: Magic / Fire / Burning / Attack Speed
Amulet 1  Pyreweaver   main: CRITICAL DAMAGE
Amulet 2  Pyreweaver   main: CRITICAL DAMAGE
Badge 1   Pyreweaver   main: CRITICAL DAMAGE
Badge 2   Enflamed     main: CRITICAL DAMAGE
Bracer 1  Enflamed     main: Max Burning / Burning Duration
Bracer 2  Enflamed     main: Max Burning / Burning Duration
```

This places **4 Critical Damage mains** (Amulets + Badges are the only pieces that can roll it).

## Per-piece stat priorities

- **Main:** Amulet/Badge = **Critical Damage** (if crit chance is still low, take Crit Chance
  first). Rune/Ring = **Magic Damage** (scales Cascade + Fireball + mortar), then
  **Burning Damage** (DOT) / **Fire Damage**. Bracer = **Max Burning** (reach 10 stacks faster).
  Charm = Concentration / Concentration Regen.
- **Secondaries:** Fire Damage · Magic Damage · Burning Damage · **Crit Chance → Crit Damage** ·
  Spell Haste · Attack Speed · **Fire Piercing** · Max Burning / Burning Duration · Conc Regen.
- **Attribute:** Human·Magician, INT-first. Best matk spread: **INT +41, CHA +3, ADV +3
  (matk 58.5)** — see `Dimraeth_Optimal_Attribute_Builds.md`.

## Relevant set bonuses (reference)

- **Pyreweaver:** 2pc +8 Fire, +5% Magic; 3pc +4 Spell Haste; 6pc +7% HP + Wildfire (+15% vs Burning).
- **Enflamed:** 2pc +8% Burning Damage, +3 Spell Haste; 4pc Everburn (mortar on Fire spell cast).
- **Convergence:** 2pc +5% Magic, +3 Spell Haste; 3pc +10 Conc Regen; 6pc +16 Conc + Overchannel.
- **Burnward:** 2pc +12% Fire Res, +8 Fire; 4pc Calloused Skin (defensive).
- **Dazzler:** 3pc +4 Spell Haste, +10 Conc Regen (4pc is stun — skip for a burn build).

## Farm sources

- **Pyreweaver** — Goblin Flaskrat / Goblin Ripper (Earlwood Farmlands, Goblin Caves); best chest
  Wildwood IIA (10%). 78 chests.
- **Enflamed** — **Briarheart The Firecursed** (lvl 19), **100% per kill**; Briarheart Bounty T3.
- **Convergence** — Dryad Beastwarden/Rivercaller/Rootweaver (33%), Treant, 46 Wildwood chests.
- **Burnward** — **Gotburg The Burned** / Goblin Basher T2, **100% per kill** (easiest to complete).

*Data source: dimraethdb.com, game build 0.107.7779.*
