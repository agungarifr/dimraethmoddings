# Equipment Stat Editor

**Edit rune / equipment secondary stats in-game (F8) or from a config file, and save them into your character file. Changes live in the native save, so they work in multiplayer with no mod required for the other players.**

### What it does
- **F8 in-game panel** — pick any equipped item, change which stat an affix is, its value, and its upgrade tier; add or remove affixes (max **6**). Press *Apply to Item*, then *Save Character*.
- **Config-file workflow** (backup / bulk editing) — the mod writes every rune to `BepInEx/config/EquipmentStatEditor.<character>.cfg`; edit the values there and relaunch to apply. The file is written on demand via the panel's **Dump Runes to File** button (and once at first launch if it does not exist yet), so it no longer churns on every autosave.
- **Safe by design** — every edit is validated against the game's own rules and snapshotted. If the game would reject or destroy the item, the edit is rolled back and you get an on-screen notice instead of losing the item.

---

## Stat value caps (the game's real limits)

The game destroys (`GearLegality.DestroyIfUnobtainable`) any rune whose value exceeds the ceiling it derives from its own roll table. The panel reads that ceiling **directly from the game** (`GearLegality.TryStatValueCeiling`, called at runtime through the IL2CPP API), so the displayed caps are always the game's real limits - no formula is hard-coded. For reference the ceiling is a fixed cap per stat:

```
ceiling = 2 x roll(stat, stars = 2, level) + tolerance
          level = 12 when the stat is the PRIMARY stat
          level =  5 when the stat is a SECONDARY stat
roll    = baseline x (1 + 0.33 x stars) x (1 + 0.06 x (level - 1))
```

The `tolerance` term is a game build constant (it was `2` before a game update; current builds effectively use `0`). The panel never guesses it - the live value is read from the game.

<!-- [2026-09-27 15:02] Changed: this section used to state "ceiling = 2 x roll + 2" as a
     hard-coded formula. A game update changed the tolerance so the replicated cap was ~2 too
     high and the game destroyed items. The panel now calls the game's own ceiling function.
     The table below was dumped before this fix; regenerate EquipmentStatEditor.StatRolls.csv
     with the current build for the exact live caps (values will be up to 2 lower). -->

The same stat therefore has a **lower cap as a secondary stat than as a primary stat**. Keep each value at or below the cap shown in the panel for its role, or leave the game-rolled value untouched. The caps below come from the mod's `EquipmentStatEditor.StatRolls.csv` dump.

| Other hard limit | Value |
|------|-------|
| Maximum secondary stats per item | **6** (`MaxObtainableSecondaryStats`) |
| Maximum upgrade tiers | **4** (`MaxObtainableStatUpgrades`) |
| Maximum rune level | 12 |
| Maximum rune rarity | 4 |
| Maximum rune stars | 2 |

## Per-stat value caps

A `%` marks a percentage stat. "Max as secondary" / "Max as primary" are the values *above* which the game destroys the item.

| Stat | Max as secondary | Max as primary |
|------|------------------|----------------|
| Concentration % | 34.9 | 46.1 |
| Encumbrance | 10.2 | 13 |
| Health | 34.9 | 46.1 |
| AttackSpeed % | 14.4 | 18.5 |
| Stamina | 34.9 | 46.1 |
| HealthRegen | 34.9 | 46.1 |
| ConcentrationRegen % | 34.9 | 46.1 |
| StaminaRegen % | 34.9 | 46.1 |
| SpellHaste | 14.4 | 18.5 |
| CriticalChance | 14.4 | 18.5 |
| CriticalDamage | 34.9 | 46.1 |
| PhysicalPiercing | 14.4 | 18.5 |
| MagicPiercing | 14.4 | 18.5 |
| FirePiercing | 22.6 | 29.6 |
| IcePiercing | 22.6 | 29.6 |
| ElectricityPiercing | 22.6 | 29.6 |
| DecayPiercing | 22.6 | 29.6 |
| EtherPiercing | 22.6 | 29.6 |
| StrikePiercing | 22.6 | 29.6 |
| ThrustPiercing | 22.6 | 29.6 |
| BluntPiercing | 22.6 | 29.6 |
| PhysicalPower | 22.6 | 29.6 |
| MagicPower | 22.6 | 29.6 |
| StrikeDamage % | 26.7 | 35.1 |
| ThrustDamage % | 26.7 | 35.1 |
| BluntDamage % | 26.7 | 35.1 |
| FireDamage % | 26.7 | 35.1 |
| IceDamage % | 26.7 | 35.1 |
| ElectricityDamage % | 26.7 | 35.1 |
| DecayDamage % | 26.7 | 35.1 |
| EtherDamage % | 26.7 | 35.1 |
| Armor | 26.7 | 35.1 |
| Resistance | 26.7 | 35.1 |
| HPPercent % | 14.4 | 18.5 |
| ConcentrationPercent % | 14.4 | 18.5 |
| StaminaPercent % | 14.4 | 18.5 |
| ArmorPercent % | 14.4 | 18.5 |
| ResistancePenetration | 14.4 | 18.5 |
| ResistancePercent % | 14.4 | 18.5 |
| PhysicalDamage % | 18.5 | 24 |
| MagicDamage % | 18.5 | 24 |
| ArmorPenetration | 14.4 | 18.5 |
| ArmorPenetrationPercent % | 14.4 | 18.5 |
| ResistancePenetrationPercent % | 14.4 | 18.5 |
| FireResistance | 26.7 | 35.1 |
| IceResistance | 26.7 | 35.1 |
| DecayResistance | 26.7 | 35.1 |
| ElectricResistance | 26.7 | 35.1 |
| EtherResistance | 26.7 | 35.1 |
| StrikeArmor | 26.7 | 35.1 |
| ThrustArmor | 26.7 | 35.1 |
| BluntArmor | 26.7 | 35.1 |
| MovementSpeed | 18.5 | 24 |
| MaxBurning | 6.1 | 7.5 |
| MaxChill | 6.1 | 7.5 |
| MaxRage | 6.1 | 7.5 |
| MaxPoison | 6.1 | 7.5 |
| MaxShock | 6.1 | 7.5 |
| MaxBleeding | 6.1 | 7.5 |
| PoisonPiercing | 22.6 | 29.6 |
| DamageVsBleeding | 22.6 | 29.6 |
| RageDuration | 6.1 | 7.5 |
| StunBuildupIncrease | 34.9 | 46.1 |
| DamageVsFullHealth | 22.6 | 29.6 |
| DamageVsStunned | 22.6 | 29.6 |
| ConcentrationOnHit | 10.2 | 13 |
| BleedDuration | 6.1 | 7.5 |
| CriticalChanceVsBleeding | 14.4 | 18.5 |
| CriticalDamageVsBleeding | 34.9 | 46.1 |
| CriticalDamageVsStunned | 34.9 | 46.1 |
| AttackDamage | 22.6 | 29.6 |
| DamageVsLowHealth40 | 22.6 | 29.6 |
| PoisonDuration | 6.1 | 7.5 |
| BleedDamage | 51.4 | 68.1 |
| PoisonDamage | 51.4 | 68.1 |
| DamageVsPoison | 22.6 | 29.6 |
| CriticalChanceVsPoison | 14.4 | 18.5 |
| CriticalDamageVsPoison | 34.9 | 46.1 |
| CriticalChanceVsStunned | 14.4 | 18.5 |
| DamageVsBurning | 22.6 | 29.6 |
| BurningDamage | 51.4 | 68.1 |
| CriticalChanceVsBurning | 14.4 | 18.5 |
| CriticalDamageVsBurning | 34.9 | 46.1 |
| BurningDuration | 6.1 | 7.5 |
| ChilledDuration | 6.1 | 7.5 |
| DamageVsChilled | 22.6 | 29.6 |
| ChilledPower | 51.4 | 68.1 |
| CriticalChanceVsChilled | 14.4 | 18.5 |
| CriticalDamageVsChilled | 34.9 | 46.1 |
| EchoPower | 22.6 | 29.6 |
| MaxTemporalEcho | 6.1 | 7.5 |
| MaxTemporalSurge | 6.1 | 7.5 |
| TemporalEchoDuration | 6.1 | 7.5 |
| TemporalSurgeDuration | 6.1 | 7.5 |
| DamageVsEchoed | 22.6 | 29.6 |
| CriticalChanceVsEchoed | 14.4 | 18.5 |
| CriticalDamageVsEchoed | 34.9 | 46.1 |
| CriticalChanceVsLowHealth40 | 14.4 | 18.5 |
| CriticalDamageVsLowHealth40 | 34.9 | 46.1 |
| MaxKindled | 6.1 | 7.5 |
| MaxFrostbound | 6.1 | 7.5 |
| MaxInoculation | 6.1 | 7.5 |
| KindledDuration | 6.1 | 7.5 |
| FrostboundDuration | 6.1 | 7.5 |
| InoculationDuration | 6.1 | 7.5 |
| HealingPower % | 34.9 | 46.1 |
| MinionHealth | 26.7 | 35.1 |
| MinionDamage | 26.7 | 35.1 |
| MinionCriticalChance | 14.4 | 18.5 |
| GuardEfficiency | 14.4 | 18.5 |

### Stats with no roll entry in this build

These enum values have no baseline/scaling entry, so the game computes no ceiling for them and will not destroy an item over them - leave them as the game rolled them:

`MaxTreantBurning` - `MaxSwordSwap` - `MaxCrossbowSwap` - `MaxVersatileWarrior` - `MaxAlacrity` - `ShadowCloneDuration` - `MaxDoubleStrike` - `DoubleStrikeDuration` - `ShadowCloneMax`

> `None` is the game's empty-stat placeholder - not a real affix.

---

## How values scale

**Primary stat** magnitudes scale with the rune's level and stars: `value = baseline x star x level`.

| Rune level | x | Stars | x |
|-----------|----|-------|----|
| 1 | 1.00 | One (0) | 1.00 |
| 2 | 1.06 | Two (1) | 1.33 |
| 3 | 1.12 | Three (2) | 1.66 |
| 4 | 1.18 |  |  |
| 5 | 1.24 |  |  |
| 6 | 1.30 |  |  |
| 7 | 1.36 |  |  |
| 8 | 1.42 |  |  |
| 9 | 1.48 |  |  |
| 10 | 1.54 |  |  |
| 11 | 1.60 |  |  |
| 12 | 1.66 |  |  |

**Secondary stat** magnitudes do **not** scale with level - a natural secondary rolls just under its cap (the "Max as secondary" column above). Rarity does not change affix magnitudes in this build (`RarityModifiers` is not applied to rune stat values), so these figures are identical at every rarity.

Reference **primary** values at max stars (Three):

| Stat | Lv1 | Lv5 | Lv12 |
|------|-----|-----|------|
| Concentration % | 13.28 | 16.47 | 22.05 |
| Encumbrance | 3.32 | 4.12 | 5.51 |
| Health | 13.28 | 16.47 | 22.05 |
| AttackSpeed % | 4.98 | 6.18 | 8.27 |
| Stamina | 13.28 | 16.47 | 22.05 |
| HealthRegen | 13.28 | 16.47 | 22.05 |
| ConcentrationRegen % | 13.28 | 16.47 | 22.05 |
| StaminaRegen % | 13.28 | 16.47 | 22.05 |
| SpellHaste | 4.98 | 6.18 | 8.27 |
| CriticalChance | 4.98 | 6.18 | 8.27 |
| CriticalDamage | 13.28 | 16.47 | 22.05 |
| PhysicalPiercing | 4.98 | 6.18 | 8.27 |
| MagicPiercing | 4.98 | 6.18 | 8.27 |
| FirePiercing | 8.3 | 10.29 | 13.78 |
| IcePiercing | 8.3 | 10.29 | 13.78 |
| ElectricityPiercing | 8.3 | 10.29 | 13.78 |
| DecayPiercing | 8.3 | 10.29 | 13.78 |
| EtherPiercing | 8.3 | 10.29 | 13.78 |
| StrikePiercing | 8.3 | 10.29 | 13.78 |
| ThrustPiercing | 8.3 | 10.29 | 13.78 |
| BluntPiercing | 8.3 | 10.29 | 13.78 |
| PhysicalPower | 8.3 | 10.29 | 13.78 |
| MagicPower | 8.3 | 10.29 | 13.78 |
| StrikeDamage % | 9.96 | 12.35 | 16.53 |
| ThrustDamage % | 9.96 | 12.35 | 16.53 |
| BluntDamage % | 9.96 | 12.35 | 16.53 |
| FireDamage % | 9.96 | 12.35 | 16.53 |
| IceDamage % | 9.96 | 12.35 | 16.53 |
| ElectricityDamage % | 9.96 | 12.35 | 16.53 |
| DecayDamage % | 9.96 | 12.35 | 16.53 |
| EtherDamage % | 9.96 | 12.35 | 16.53 |
| Armor | 9.96 | 12.35 | 16.53 |
| Resistance | 9.96 | 12.35 | 16.53 |
| HPPercent % | 4.98 | 6.18 | 8.27 |
| ConcentrationPercent % | 4.98 | 6.18 | 8.27 |
| StaminaPercent % | 4.98 | 6.18 | 8.27 |
| ArmorPercent % | 4.98 | 6.18 | 8.27 |
| ResistancePenetration | 4.98 | 6.18 | 8.27 |
| ResistancePercent % | 4.98 | 6.18 | 8.27 |
| PhysicalDamage % | 6.64 | 8.23 | 11.02 |
| MagicDamage % | 6.64 | 8.23 | 11.02 |
| ArmorPenetration | 4.98 | 6.18 | 8.27 |
| ArmorPenetrationPercent % | 4.98 | 6.18 | 8.27 |
| ResistancePenetrationPercent % | 4.98 | 6.18 | 8.27 |
| FireResistance | 9.96 | 12.35 | 16.53 |
| IceResistance | 9.96 | 12.35 | 16.53 |
| DecayResistance | 9.96 | 12.35 | 16.53 |
| ElectricResistance | 9.96 | 12.35 | 16.53 |
| EtherResistance | 9.96 | 12.35 | 16.53 |
| StrikeArmor | 9.96 | 12.35 | 16.53 |
| ThrustArmor | 9.96 | 12.35 | 16.53 |
| BluntArmor | 9.96 | 12.35 | 16.53 |
| MovementSpeed | 6.64 | 8.23 | 11.02 |
| MaxBurning | 1.66 | 2.06 | 2.76 |
| MaxChill | 1.66 | 2.06 | 2.76 |
| MaxRage | 1.66 | 2.06 | 2.76 |
| MaxPoison | 1.66 | 2.06 | 2.76 |
| MaxShock | 1.66 | 2.06 | 2.76 |
| MaxBleeding | 1.66 | 2.06 | 2.76 |
| PoisonPiercing | 8.3 | 10.29 | 13.78 |
| DamageVsBleeding | 8.3 | 10.29 | 13.78 |
| RageDuration | 1.66 | 2.06 | 2.76 |
| StunBuildupIncrease | 13.28 | 16.47 | 22.05 |
| DamageVsFullHealth | 8.3 | 10.29 | 13.78 |
| DamageVsStunned | 8.3 | 10.29 | 13.78 |
| ConcentrationOnHit | 3.32 | 4.12 | 5.51 |
| BleedDuration | 1.66 | 2.06 | 2.76 |
| CriticalChanceVsBleeding | 4.98 | 6.18 | 8.27 |
| CriticalDamageVsBleeding | 13.28 | 16.47 | 22.05 |
| CriticalDamageVsStunned | 13.28 | 16.47 | 22.05 |
| AttackDamage | 8.3 | 10.29 | 13.78 |
| DamageVsLowHealth40 | 8.3 | 10.29 | 13.78 |
| PoisonDuration | 1.66 | 2.06 | 2.76 |
| BleedDamage | 19.92 | 24.7 | 33.07 |
| PoisonDamage | 19.92 | 24.7 | 33.07 |
| DamageVsPoison | 8.3 | 10.29 | 13.78 |
| CriticalChanceVsPoison | 4.98 | 6.18 | 8.27 |
| CriticalDamageVsPoison | 13.28 | 16.47 | 22.05 |
| CriticalChanceVsStunned | 4.98 | 6.18 | 8.27 |
| DamageVsBurning | 8.3 | 10.29 | 13.78 |
| BurningDamage | 19.92 | 24.7 | 33.07 |
| CriticalChanceVsBurning | 4.98 | 6.18 | 8.27 |
| CriticalDamageVsBurning | 13.28 | 16.47 | 22.05 |
| BurningDuration | 1.66 | 2.06 | 2.76 |
| ChilledDuration | 1.66 | 2.06 | 2.76 |
| DamageVsChilled | 8.3 | 10.29 | 13.78 |
| ChilledPower | 19.92 | 24.7 | 33.07 |
| CriticalChanceVsChilled | 4.98 | 6.18 | 8.27 |
| CriticalDamageVsChilled | 13.28 | 16.47 | 22.05 |
| EchoPower | 8.3 | 10.29 | 13.78 |
| MaxTemporalEcho | 1.66 | 2.06 | 2.76 |
| MaxTemporalSurge | 1.66 | 2.06 | 2.76 |
| TemporalEchoDuration | 1.66 | 2.06 | 2.76 |
| TemporalSurgeDuration | 1.66 | 2.06 | 2.76 |
| DamageVsEchoed | 8.3 | 10.29 | 13.78 |
| CriticalChanceVsEchoed | 4.98 | 6.18 | 8.27 |
| CriticalDamageVsEchoed | 13.28 | 16.47 | 22.05 |
| CriticalChanceVsLowHealth40 | 4.98 | 6.18 | 8.27 |
| CriticalDamageVsLowHealth40 | 13.28 | 16.47 | 22.05 |
| MaxKindled | 1.66 | 2.06 | 2.76 |
| MaxFrostbound | 1.66 | 2.06 | 2.76 |
| MaxInoculation | 1.66 | 2.06 | 2.76 |
| KindledDuration | 1.66 | 2.06 | 2.76 |
| FrostboundDuration | 1.66 | 2.06 | 2.76 |
| InoculationDuration | 1.66 | 2.06 | 2.76 |
| HealingPower % | 13.28 | 16.47 | 22.05 |
| MinionHealth | 9.96 | 12.35 | 16.53 |
| MinionDamage | 9.96 | 12.35 | 16.53 |
| MinionCriticalChance | 4.98 | 6.18 | 8.27 |
| GuardEfficiency | 4.98 | 6.18 | 8.27 |

---

### Notes
- Level, Rarity, Stars, and Set are **not editable from the in-game panel** (config file only).
- The **Primary stat is editable** (both *which* stat and its value). Its value is bounded by the **primary ceiling** (computed at level 12, higher than a secondary's level-5 ceiling) — use the **Max** button for that. Changing which stat is primary is not checked by the game's legality code, but the panel blocks any value above the new stat's primary ceiling.
- Newly added or changed affixes are **seeded with the natural roll** for the rune's stars (`CalculateStatBaseValue`), not the cap — so the item looks legitimately rolled. Use the **Max** button when you deliberately want the ceiling.
- Multiplayer-safe: other players see your edited gear because the values are written into your character's save file, not patched at runtime.