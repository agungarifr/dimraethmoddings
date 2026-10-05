# Dimraeth - Complete Equipment Upgrade & Enhancement Guide
### In-Depth Breakdown of the Forge, Randomness, Milestones, and Stat Affixes

---

## Overview

In Dimraeth, equipment pieces (internally known as Runes, which occupy slots like Undercoat, Belt, Charm, Rings, Amulets, Badges, Bracers, and Runes) can be enhanced and upgraded at the Forge.

Upgrading equipment not only amplifies its core primary attribute on every step, but also unlocks and upgrades secondary stat affixes at specific milestone levels.

This breakdown covers how the leveling system works, the math behind bonus stats, how randomness and seeds are calculated, and the full pool of available attributes.

---

## Core Upgrade Rules & Progression

### 1. Maximum Upgrade Level
- Equipment pieces start at Level 0 and can be enhanced up to Level 12 (+12) (MaxRuneLevel = 12).
- Each upgrade step consumes Essence currency depending on the star rating of the item:
  - Coarse Essence (Early tier)
  - Shaped Essence (Mid tier)
  - Refined Essence (High tier)
  - Pure Essence (Endgame tier)
- The cost scales progressively based on current level, star rating, and rarity modifier (CalculateTotalUpgradeCost).

### 2. Primary Stat Scaling (Every Level)
- The Primary Stat of the equipment piece increases on every single level upgrade (+1, +2, +3, through +12).
- The stat value follows a linear scaling curve:
  FinalValue = BaseValue x (1 + (Level - 1) x LevelScaling)
- Rounded cleanly to two decimal places for in-game display.

---

## The Milestone Rule: Bonus Stats Every 3 Levels

The most important feature of the enhancement system is the Every 3 Levels Rule (rune.Level % 3 == 0):

Milestones: Level +3, +6, +9, and +12

When an equipment piece reaches any of these four milestone levels during an upgrade, two events trigger simultaneously:

### A. All Existing Secondary Stats Level Up
- Every secondary stat already present on the item has its internal upgrade tier incremented:
  StatUpgrades[i] += 1
- Its value is immediately recalculated to reflect the new tier:
  Value = CalculateFinalStatValue(BaseValue, StatUpgrades[i] + 1)
- Every milestone strengthens ALL your current secondary affixes.

### B. A New Random Secondary Stat is Rolled
- If the item has not reached the maximum secondary stat cap of 6 (MaxSecondaryStats = 6):
  - The system rolls one brand-new secondary stat at Tier 1 (StatUpgrades = 0).
  - The new stat is appended to the item's affix list and calculated at level 1 base value.
- If the equipment already has 6 secondary stats (such as high-rarity items that started with multiple affixes), no new stat slot is created, but all 6 existing stats still receive their tier upgrade.

#### Secondary Stat Count by Base Rarity:
- Common:    0 at drop -> 1 (+3) -> 2 (+6) -> 3 (+9) -> 4 (+12)
- Uncommon:  1 at drop -> 2 (+3) -> 3 (+6) -> 4 (+9) -> 5 (+12)
- Rare:      2 at drop -> 3 (+3) -> 4 (+6) -> 5 (+9) -> 6 (+12, Cap reached)
- Mythical:  3 at drop -> 4 (+3) -> 5 (+6) -> 6 (+9, Cap reached) -> 6 (+12, All boosted)
- Heroic:    4 at drop -> 5 (+3) -> 6 (+6, Cap reached) -> 6 (+9, All boosted) -> 6 (+12, All boosted)
- Ancient:   5 at drop -> 6 (+3, Cap reached) -> 6 (+6, All boosted) -> 6 (+9, All boosted) -> 6 (+12, All boosted)

---

## How the Randomness (RNG) Works

Under the hood, Dimraeth uses a deterministic seeded RNG:

### 1. The Seed Formula (GetUpgradeSeed)
When rolling a new stat, the game generates a 32-bit integer seed derived directly from the item's identity:

```csharp
int seed = 17;
foreach (byte b in rune.UUID)
{
    seed = seed * 31 + b;
}
seed = (seed * 31 + rune.Level) ^ ObfuscationConfig.CheckKey;
```

- UUID-Dependent: Because the seed is tied directly to the item's permanent UUID and the target Level, reloading the game and re-upgrading the same exact item to +3 will yield the exact same stat roll.
- Level Progression: Each milestone level (+3, +6, +9, +12) produces a distinct seed, ensuring fresh outcomes at each milestone.

### 2. Candidate Filtering (GetRandomSecondaryStat)
Before rolling, the game applies strict eligibility filters:
1. Slot Pool Lookup: The game queries RuneSlotDefinition.SecondaryStats for the item's slot type and rune set.
2. Primary Stat Exclusion: The item's primary attribute is removed from the candidate pool:
   `validSecondaryStats.Remove(rune.PrimaryStat);`
   (An item can never have its primary stat appear as a secondary affix).
3. No Duplicate Affixes: Any secondary stat already on the equipment is stripped:
   ```csharp
   foreach (var existing in rune.SecondaryStats) {
       validSecondaryStats.Remove(existing);
   }
   ```
   (You will never get two copies of the same secondary affix on one item).
4. Uniform Selection: From the remaining eligible affixes, the game selects one with equal probability:
   ```csharp
   var rng = new System.Random(seed);
   int chosenIndex = rng.Next(0, validSecondaryStats.Count);
   return validSecondaryStats[chosenIndex];
   ```

---

## Complete Equipment Stat Affix Catalog (Runes.Stat)

Below is the complete database of attributes recognized by the game's equipment engine, grouped by category:

### Core Offensive & Penetration
- Attack Damage & Speed: AttackDamage, AttackSpeed, SpellHaste
- Power: PhysicalPower, PhysicalDamage, MagicPower, MagicDamage
- Critical Strikes: CriticalChance, CriticalDamage
- Physical Penetration: ArmorPenetration, ArmorPenetrationPercent
- Magic Penetration: ResistancePenetration, ResistancePenetrationPercent
- Weapon Damage Types: StrikeDamage, StrikePiercing, ThrustDamage, ThrustPiercing, BluntDamage, BluntPiercing
- Elemental & Magic Types: FireDamage, FirePiercing, IceDamage, IcePiercing, ElectricityDamage, ElectricityPiercing, DecayDamage, DecayPiercing, EtherDamage, EtherPiercing, PoisonPiercing

### Defensive, Armor & Resistances
- Health: Health, HPPercent, HealthRegen
- Stamina: Stamina, StaminaPercent, StaminaRegen
- Concentration: Concentration, ConcentrationPercent, ConcentrationRegen, ConcentrationOnHit
- Physical Armor: Armor, ArmorPercent, StrikeArmor, ThrustArmor, BluntArmor
- Elemental & Magic Resistances: Resistance, ResistancePercent, FireResistance, IceResistance, ElectricResistance, DecayResistance, EtherResistance
- Shielding: GuardEfficiency

### Mobility & Utility
- MovementSpeed
- Encumbrance (Carry Capacity)
- HealingPower
- StunBuildupIncrease

### Status Effect Modifiers & Durations
- Bleeding: BleedDamage, BleedDuration, DamageVsBleeding, CriticalChanceVsBleeding, CriticalDamageVsBleeding, MaxBleeding
- Poison: PoisonDamage, PoisonDuration, DamageVsPoison, CriticalChanceVsPoison, CriticalDamageVsPoison, MaxPoison
- Burning / Fire: BurningDamage, BurningDuration, DamageVsBurning, CriticalChanceVsBurning, CriticalDamageVsBurning, MaxBurning, MaxTreantBurning, MaxKindled, KindledDuration
- Chilled / Frost: ChilledPower, ChilledDuration, DamageVsChilled, CriticalChanceVsChilled, CriticalDamageVsChilled, MaxChill, MaxFrostbound, FrostboundDuration
- Shock: MaxShock
- Combat Conditions:
  - DamageVsStunned, CriticalChanceVsStunned, CriticalDamageVsStunned
  - DamageVsFullHealth
  - DamageVsLowHealth40, CriticalChanceVsLowHealth40, CriticalDamageVsLowHealth40

### Temporal, Stances & Special Passives
- Temporal & Echo: EchoPower, MaxTemporalEcho, TemporalEchoDuration, DamageVsEchoed, CriticalChanceVsEchoed, CriticalDamageVsEchoed, MaxTemporalSurge, TemporalSurgeDuration
- Combat Stances: MaxRage, RageDuration, MaxSwordSwap, MaxCrossbowSwap, MaxVersatileWarrior, MaxAlacrity
- Strikes & Clones: MaxDoubleStrike, DoubleStrikeDuration, ShadowCloneMax, ShadowCloneDuration, MaxInoculation, InoculationDuration

### Minions & Companions
- MinionHealth
- MinionDamage
- MinionCriticalChance

---

## Modder & Technical Reference

For fellow modders building custom plugins, Harmony patches, or tools for Dimraeth:

- Forge (NetworkBehaviour):
  Handles Forge station UI, essence transactions, client/server RPCs, and invokes Runes.UpgradeRune.

- Runes (NetworkBehaviour):
  Core stat engine. Hosts UpgradeRune, SimulateUpgradeRune, GetRandomSecondaryStat, and GetUpgradeSeed.

- Rune (struct):
  Network-serializable memory layout of an equipment instance. Fields: UUID, Level, Rarity, Stars, PrimaryStat, SecondaryStats, StatValues, StatUpgrades.

- RuneManager (MonoBehaviour):
  Singleton holding game balance constants, BaselineValues, ScalingValues, LevelScaling, and CalculateUpgradeCost.

- RuneSlotDefinition (ScriptableObject):
  Asset defining SlotType, allowed PrimaryStats, and allowed SecondaryStats pool.

- ForgePanelView (MonoBehaviour):
  UI presenter managing the forge window, upgrade animations, and essence costs.

---

Authored for the Dimraeth modding community. Verified against Dimraeth game binary and IL2CPP interop assemblies.
