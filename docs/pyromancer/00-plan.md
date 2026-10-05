# Pyromancer — Magician Chill → Burn conversion (design plan)

Status: **plan only** (no implementation yet)
Date: 2026-10-05
Target game install: `D:\SteamLibrary\steamapps\common\Dimraeth`

## 1. Goal

Convert the **Magician** class's Cold/Ice identity into Fire/Burning:

- Every Magician damage source that deals **Ice** instead deals **Fire**.
- Every Magician spell that applies **Chill** instead applies **Burning**.
- Every Magician skill-tree node that grants a Chill/Ice stat grants its Burning/Fire equivalent.

Headline example: **Blizzard = Fire damage + Burning**, not Ice + Chill.

## 2. Scope (agreed)

| Decision | Choice |
|---|---|
| Breadth | **Magician only** (`Class.Magician = 1`) |
| Visuals | **Mechanical MVP first** — keep existing blue VFX until behavior is verified; art is a later phase |
| Packaging | **Standalone plugin** `BepInExModsSource/PyromancerConverter`, single-file like `FireballTuner` |
| Mod GUID | `com.custom.pyromancerconverter` |

Out of scope for MVP: non-Magician classes, enemy/monster ice attacks, VFX/art replacement.

## 3. Confirmed facts (from current-build dumps + mod RE)

Source of truth: `modding/cpp2il_cs_out/DiffableCs/Assembly-CSharp/` and the decompiled `NecroSkelly` plugin.

### 3.1 Enums

```
StackingEffect:  Burning=0  Chill=1  Rage=2  Poison=3  Shock=4  TreantBurning=5
                 Bleeding=6  ...  Kindled=15  Frostbound=16
DamageType:      None=0 Physical=1 Magic=2 Heal=3 Strike=4 Thrust=5 Blunt=6
                 Fire=7  Ice=8  Electric=9  Decay=10  Ether=11  Stun=12
Element:         None=0  Fire=1  Ice=2  Poison=3  Shock=4  Ether=5 ... Decay=10 Heal=11
Class:           None=0  Magician=1  Shaman=2  Bulwark=3  Brawler=4  Shadow=5  Builder=6
```

`Runes.Stat` (the enum used by `SkillTreeStatImprovement.Stat`, *not* `StatType`):

| Chill/Ice stat | Value | Burning/Fire stat | Value |
|---|---|---|---|
| `MaxChill` | 55 | `MaxBurning` | 54 |
| `ChilledDuration` | 90 | `BurningDuration` | 89 |
| `ChilledPower` | 92 | `BurningDamage` | 86 |
| `DamageVsChilled` | 91 | `DamageVsBurning` | 85 |
| `CriticalChanceVsChilled` | 93 | `CriticalChanceVsBurning` | 87 |
| `CriticalDamageVsChilled` | 94 | `CriticalDamageVsBurning` | 88 |
| `IceDamage` | 27 | `FireDamage` | 26 |
| `IcePiercing` | 14 | `FirePiercing` | 13 |
| `IceResistance` | 45 | `FireResistance` | 44 |

### 3.2 Spell model

- `SpellDefinition` (ScriptableObject): `SpellKey`, `Element`, `DamageType`, `BaseDamage`, costs, `CastRadius`, `CastTime`, `Charges`, `Tags`.
- `SpellLibrary` (NetworkBehaviour, on the player): `Spell` (0x88), `Element` (0x8C), `DmgType` (0xF0), `StackDatas` (0x110), `StackDatasSelf` (0x120), `RequiredStackingEffect` (0x230), `_definition` (0x260).
- `BaseSpellLibrary` (spawned prefab) — the single choke point for damage + status:
  - `Damage SendDamageToTarget(ObjectsCommon target, ref DamageType damageType, float mult, bool isDot=false, bool isThird=false, float stunBonus=0)`
  - `void AddStacksToTarget(ObjectsCommon target, StackingEffect effect, int amount, float time = 5)`
  - `void AddStacksToCaster(StackingEffect effect, int amount, float time = 5)`
  - both build a `StackData { StackingEffect Effect; int Stacks; float Duration; ulong SourceId; }` and call `Stacking.AddStacksToTargetServerRpc(StackData, ulong)`.
- Prefab classes call these directly (e.g. `BlizzardPrefab` fields/strings `chillToApply`, `chillApplied`, `isChilling`, `shatterChillStacks`).

### 3.3 Skill-tree model

- `SkillTree` (NetworkBehaviour, per player) with `List<SkillTreeType> { Race, Class, GameObject SkillTreeParent }`.
- Nodes are `SkillTreeNode` ScriptableObjects: `NodeId` (Magician nodes are named `Magician_*`), `Class`, `Race`, `ArchetypeNode`, `MaximumPoints`, `List<SkillTreeStatImprovement> StatImprovements`, `Passive Passive`, `Spell UnlockSpell`, `Spell AssociatedSpell`, `string SpellUpgradeName`, `Sprite Icon`, prerequisites.
- `SkillTreeStatImprovement = { Stat Stat; float FlatIncrease; float PercentIncrease; }` (struct).
- `SkillTree` methods: `ApplySingleNodeEffect`, `LoadNodeEffects`, `GetSkillTreeFlatBonus`, `GetSkillTreePercentBonus`, `GetBonusesForStat`, `OnUpgradesChanged`, `BindView`, `GetSpellUpgradeLevel`.
- Magician archetype donor tree name in vanilla data: **"Elemental Ranger"** (reused by NecroSkelly as a donor); nodes carry `Magician_*` ids.

### 3.4 Proven hook patterns (borrowed)

- **`FireballTuner`** (existing mod) patches `SpellLibrary.ApplyDefinition`, `BaseSpellLibrary.AddStacksToTarget`, `Spells.Cast`, `BaseSpell.CastTimeCallbackCheck`, `SpellTooltipDatabase.GetLocalizedDescription/GetLocalizedEffects`. It is the ideal single-file skeleton.
- **`NecroSkelly`** patches `BaseSpellLibrary.SendDamageToTarget` as `Prefix(BaseSpellLibrary __instance, ref DamageType __1)` — proves the damage-type override signature works — and rewrites tree node stats by **direct native-memory writes** to the `StatImprovements` list elements (`Marshal.WriteInt32(listPtr + off("Stat"), (int)newStat)`), guarded by an `IntPtr` "already retyped" set.
- **Important:** `AddStacksToTarget`'s `effect` parameter is **by value**, so it cannot be edited in a normal prefix. Use the **re-dispatch prefix**: when `effect == Chill` for a converted spell, `return false` after calling `__instance.AddStacksToTarget(target, Burning, amount, time)` (the re-entrant call sees Burning and runs the original body exactly once).

## 4. Architecture

New standalone plugin with four independent, config-gated patch groups.

### 4.1 Damage → Fire
- Prefix `BaseSpellLibrary.SendDamageToTarget`, `ref DamageType __1`.
- If the caster spell (`__instance._spell`) is in the converted Magician set and `__1 == Ice`, set `__1 = Fire`.
- **This is the single source of truth for damage school**; resistances/stun build then follow Fire.

### 4.2 Status → Burning
- Prefix `BaseSpellLibrary.AddStacksToTarget` (re-dispatch as above) for `Chill → Burning`.
- Prefix `BaseSpellLibrary.AddStacksToCaster` for self-buffs `Frostbound → Kindled`.
- Gate by `__instance._spell` ∈ converted set so non-Magician chill (enemies) is untouched.

### 4.3 Spell cosmetic/element + tooltips
- Postfix `SpellLibrary.ApplyDefinition`: `SetElement(Fire)` / `SetDamageType(Fire)`; rewrite `StackDatas` / `StackDatasSelf` entries `Chill → Burning` (defensive, in case a prefab reads its own list).
- Postfix `SpellTooltipDatabase.GetLocalizedDescription(Spell)` and `GetLocalizedEffects(Spell)`: swap Chill/Frost/Ice wording for Burning/Fire.

### 4.4 Skill tree
- One-shot after tree load (postfix `SkillTree.LoadSkillTreeInitial` / prefix `SkillTree.BindView`), guard with an `IntPtr` set for idempotency.
- Enumerate `Resources.FindObjectsOfTypeAll<SkillTreeNode>()`, select `Class == Magician(1)`.
- For each node with `StatImprovements`, apply the §3.1 stat remap table via the native-memory write technique.
- Retarget ice keystone passives where a fire analogue exists; log any that cannot map (manual decisions).
- Update node text/icons (localization keys `SKILL_MAGICIAN_*`) and perform a global tooltip string replacement (`Chill → Burn`, `Chilled → Burning`, `Frost/Ice → Fire`).
- Optionally also patch `SkillTree.ApplySingleNodeEffect` to translate at application time as a belt-and-braces fallback.

## 5. Config (BepInEx)

| Key | Section | Default | Meaning |
|---|---|---|---|
| `ConvertDamage` | General | true | Ice → Fire on converted spells |
| `ConvertStatus` | General | true | Chill → Burning, Frostbound → Kindled |
| `ConvertTree` | General | true | Remap Magician tree stats |
| `ConvertTooltips` | General | true | Text/description swap |
| `LogSwaps` | Diagnostics | true | Log every spell/node converted (first run) |
| `SpellAllowlist` | General | (empty) | Optional explicit spell list override |

## 6. Phased plan

1. **MVP — Blizzard + tree stats.**
   - Damage Fire, apply Burning, remap `*Chilled`/`MaxChill`/`ChilledDuration`/`Ice*` Magician node stats.
   - Log every swap so we learn the real set of Magician node ids and spells carrying Chill **at runtime** (no guessing asset names).
2. **Phase 2 — full Magician spell set** (Frost Javelin, and other Ice spells enumerated by the logs), self-buff `Frostbound → Kindled`.
3. **Phase 3 — polish** — passives, status icons, material tint/VFX, localization.

## 7. Authority, anticheat, risks

- **Server authority:** damage/status are server-computed. For co-op the mod must run on the **host** (same rule as `FireballTuner`/`NecroSkelly`). Single-player/host works directly.
- **Anticheat:** tree edits are server-authoritative `NetworkList` data, not the gear-legality path (`GearLegality`) patched by `AntiCheatBypassMod`. Risk is low; reuse `DimraethModPack.AntiCheatBypassModule` only if a stat-ceiling validator trips. Test on a fresh save first.
- **Idempotency:** retype nodes once (guard set of `IntPtr`s); `ApplyDefinition`/`AddStacksToTarget` fire repeatedly.
- **Unknowns to resolve via the MVP logs:**
  - exact set of `Magician_*` nodes carrying Chill stats;
  - whether any Chill node effect lives purely in a `Passive` (needs per-passive handling);
  - whether Blizzard's chill comes from a prefab field (`chillToApply`) vs a `StackData` — the re-dispatch hook covers both, but the log confirms.
- **Co-op desync** if host/client configs differ — document "same config both sides".

## 8. References

- `modding/cpp2il_cs_out/DiffableCs/Assembly-CSharp/`: `Spell.cs`, `SpellLibrary.cs`, `BaseSpellLibrary.cs`, `SpellDefinition.cs`, `StackData.cs`, `StackingEffect.cs`, `DamageType.cs`, `Element.cs`, `SkillTree.cs`, `SkillTreeNode.cs`, `SkillTreeStatImprovement.cs`, `Runes.cs`, `Blizzard.cs`, `FrostJavelin.cs`.
- `modding/cpp2il_new_isil/IsilDump/Assembly-CSharp/`: `BaseSpellLibrary.txt`, `BlizzardPrefab.txt`, `FrostJavelinPrefab.txt`, `InfernoPrefab.txt`, `FireballPrefab.txt`.
- `BepInExModsSource/FireballTuner/FireballTunerPlugin.cs` — single-file skeleton.
- Decompiled `NecroSkelly`: `Patch_OurSpellDamageType.cs`, `TreeAuthor.cs` (`RetypeStats`), `TreeContent.cs` (`Arch*Stats` remap tables), `SpellTypes.cs`.
