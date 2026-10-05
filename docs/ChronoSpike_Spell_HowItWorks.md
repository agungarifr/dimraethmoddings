# Chrono Spike ("Chrono Spear") — How the Spell Works

> A detailed reverse-engineering write-up based on the IL2CPP decompilation dumps in
> `modding/DecompilerTool/`. Primary sources:
> `ilrecovery_out/ChronoSpikePrefab.cs`, `ilrecovery_out/ChronoSpikeVolley.cs`,
> `ilrecovery_out/SkillShot.cs`, and the disassembly/ISIL under
> `isil_out/IsilDump/Assembly-CSharp/ChronoSpikePrefab*.txt` and `SkillShot.txt`.
>
> Values marked **(inferred)** come from unnamed float constants in the ISIL and match
> the named C# constants; values marked **(unverified)** could not be pinned down exactly
> from the dumps alone. Everything else is read directly from the recovered source.

---

## 1. TL;DR

Chrono Spike is a **caster dash + 3-projectile volley**, not a rocket/true-homing spell.

1. The caster selects an aim direction (movement stick if the owner is player-controlled, else
   facing direction) and **dashes ~3 units** in that direction over ~0.2 s.
2. During a **0.2 s wind-up** it chooses targets among enemies within **10 units** and spawns a
   **3-shard volley**:
   - **slot 0** = the original cast (the root projectile),
   - **slots 1 and 2** = two clones of `_splitProjectilePrefab`.
3. Each shard is **aimed at a target position captured at spawn** (or a fanned "ground spot" if
   there aren't enough live targets) and travels there with a **fixed lateral arc ("bow")** —
   giving the curved, auto-targeting *look*. It is **not** re-aimed every frame.
4. On arrival (or a mid-flight collider hit) the shard resolves damage. **Repeat hits on the
   same target in the same volley deal 50 % damage.**
5. Launch timing is staggered by `_volleySlot` (0 / 1 / 2), so the three shards leave at
   ~0.20 s / 0.26 s / 0.32 s.

The "3-way split" only happens when the **Splitting Spike** skill-tree node is unlocked
(`_splittingSpike == true`). Without it, the root cast just fires a single aimed shard at the
single highest-priority target.

---

## 2. Class hierarchy & files

```
SkillShot : BaseSpell : BaseSpellLibrary
   └── ChronoSpikePrefab            (the spell / projectile; root cast AND split children)
ChronoSpikeVolley                   (plain class: per-target hit counter for one volley)
```

The **split projectile is the same `ChronoSpikePrefab` type**, re-entered with
`_isSplitProjectile = true`. There is no separate child class.

---

## 3. Constants (from `ChronoSpikePrefab.cs`)

| Constant | Value | Meaning |
|---|---:|---|
| `_baseStacksConsumable` | `3` | Base number of DoT stacks the spell can consume |
| `_baseStackBonusPct` | `0.2` | +20 % damage per consumed/stack-increase point |
| `_seekRange` | `10` | Enemy search radius (world units) |
| `_stepDistance` | `3` | Dash distance |
| `_stepTime` | `0.2` | Dash duration |
| `_windUpTime` | `0.2` | Base wind-up before launch |
| `_projectileSpeed` | `16` | Shard travel speed (units/s) |
| `_splitProjectileCount` | `3` | Volley size (root + 2 children) |
| `_lateralBowStep` | `2` | Lateral-arc spacing between shards sharing a target |
| `_volleyLaunchStagger` | `0.06` | Extra per-slot launch delay (seconds) |
| `_repeatHitDamageScale` | `0.5` | Damage multiplier for 2nd+ hit on same target |
| `_groundFanAngle` | `16` | Ground-spot fan angle step (degrees) |
| `_groundFanJitter` | `4` | Ground-spot random angle jitter (degrees) |

---

## 4. Serialized VFX / prefab fields

In declaration order (offset shown so it can be cross-referenced with the dumps):

| Field | Offset | Notes |
|---|---:|---|
| `_projectileVfx` | `0x2F8` | Enabled on launch (`SetActive(true)`) |
| `_impactVfx` | `0x300` | Spawned at impact |
| `_aetherCastIdleVfx` | `0x308` | Wind-up VFX |
| `_aetherCastCompleteVfx` | `0x310` | Spawned when wind-up VFX finishes |
| `_aetherBurstSeconds` | `0x318` | How long the wind-up VFX runs |
| `_splitProjectilePrefab` | `0x320` | Clone source for the 2 child shards |

---

## 5. Skill-tree upgrade fields

Read by `ReadSkillTreeUpgrades()` and defaulted/overridden at cast time.

| Field | Offset | Kind | Effect |
|---|---:|---|---|
| `_stacksConsumeIncrease` | `0x328` | int | More DoT stacks consumable |
| `_damageIncrease` | `0x32C` | int | Damage bonus (×0.2 each) |
| `_echoingSpike` | `0x330` | bool | Echoing upgrade |
| `_rendingSpike` | `0x331` | bool | Rending upgrade |
| `_surgingSpike` | `0x332` | bool | Surging upgrade |
| `_echoingSpikeIncrease` | `0x334` | int | Echoing magnitude |
| `_rendingSpikeIncrease` | `0x338` | int | Rending magnitude |
| `_surgingSpikeIncrease` | `0x33C` | int | Surging magnitude |
| `_reversingSpike` | `0x340` | bool | Reversing upgrade |
| **`_splittingSpike`** | `0x341` | bool | **Enables the 3-shard volley** |
| `_cripplingSpike` | `0x342` | bool | Prefer nearest instead of highest-DoT |
| `_reversingSpikeIncrease` | `0x344` | int | Reversing magnitude |
| `_cripplingSpikeIncrease` | `0x348` | int | Crippling magnitude |

`_dotEffects` (static `StackingEffect[]`) is built in the static constructor from a `Stacking`
array; it defines the **three** DoT stack types the spell counts (burn / poison / bleed) and is
iterated with a hard `for i in 0..2` loop.

---

## 6. Private runtime state (per instance)

| Field | Offset | Meaning |
|---|---:|---|
| `_selectedTarget` | `0x350` | Target this shard will resolve against |
| `_resolvedTargets` | `0x358` | `HashSet` — dedupes impacts per shard |
| `_dashDestination` | `0x360` | Caster dash endpoint (computed in OnStart) |
| **`_isSplitProjectile`** | `0x368` | `true` for the 2 child shards |
| `_overrideTarget` | `0x370` | Target forced onto a child by the volley |
| `_groundTargetOverride` | `0x378` | Ground spot for a child with no live target |
| `_hasGroundTargetOverride` | `0x380` | Flag for the above |
| `_volley` | `0x388` | Shared `ChronoSpikeVolley` (per-volley hit counter) |
| `_volleySlot` | `0x390` | This shard's slot 0/1/2 (also drives launch stagger) |
| `_volleyLateralBow` | `0x394` | Lateral arc offset copied from the bow array |

Relevant inherited `SkillShot` offsets used above:

| SkillShot field | Offset | Notes |
|---|---:|---|
| owner component | `0xA0` | `GetOwnerTransform` / owner lookups |
| target position | `0xF0` | **The point the projectile travels to** |
| `_maximumTravelDistance` | `0x2A8` | Chrono sets `13.0` at cast |
| trajectory mode | `0x2B0` | Chrono sets `1` |
| `_launchProjectile` | `0x2BB` | Flipped to 0 once launched |
| `_useCurvedTrajectory` | `0x2BA` | **Chrono sets `1`** |
| `_projectileSpeed` | `0x2C4` | Chrono sets `16` |
| `_arcHeight` | `0x2C8` | Vertical arc magnitude |
| `_lateralArc` | `0x2D4` | **Set to `_volleyLateralBow`** |
| `throwTime` | `0x2D8` | Curve animation timer |
| `_targetPositionReached` | `0x2BE` | Set on arrival |
| `_maxDistanceReached` | `0x2BF` | Set on distance exhaustion |

---

## 7. High-level cast timeline

```
OnStart()
 ├─ ReadSkillTreeUpgrades()
 ├─ useCurvedTrajectory = true; speed = 16; maxTravel = 13; trajectory mode = 1
 ├─ RegisterLinger(...)
 ├─ if (!_isSplitProjectile)  _dashDestination = ComputeDashDestination()
 ├─ if (!_isSplitProjectile)
 │     if (_splittingSpike)   AssignSplittingVolley(report)   // spawns children
 │     else                   _selectedTarget = SelectPriorityTarget(report)
 │  else (split child)
 │     _selectedTarget = _overrideTarget, or ground override → target 0xF0 = ground point
 ├─ if (_selectedTarget != null) target 0xF0 = _selectedTarget.transform.position
 ├─ _lateralArc (0x2D4) = _volleyLateralBow (0x394)
 ├─ spawn _aetherCastIdleVfx; start FinishAetherCastBurst coroutine
 ├─ if (_surgingSpike) AddStacksToCaster(...)
 └─ start WindUpAndLaunch coroutine

WindUpAndLaunch()
 ├─ wait  _windUpTime + _volleySlot * _volleyLaunchStagger   (0.20 / 0.26 / 0.32 s)
 ├─ SkillShot.LaunchProjectile(true)   → _projectileVfx.SetActive(true)
 ├─ if (!_isSplitProjectile)  DashTargetToPosition(owner, _dashDestination, ...)
 └─ wait a short beat, end

SkillShot.Update()  (every frame while alive)
 ├─ if curved: MoveProjectileAlongCurve()   else MoveProjectileTowardsTarget()
 └─ SweepStepForHits(previousPosition) → ApplyTargetHit on overlap

OnDestinationReached() / HandleTriggerEnter2D()
 └─ ResolveImpact(target, "destination reached" | "mid-flight collision")
```

---

## 8. OnStart details

`OnStart` (ISIL at line ~621 of `ChronoSpikePrefab.txt`) does, in order:

1. `ReadSkillTreeUpgrades()`.
2. Forces `useCurvedTrajectory = true` (`0x2BA = 1`).
3. If `_splittingSpike`, calls `SetArcHeightRange(min, max)` (arc height for the curved shot).
4. Writes movement defaults: `speed = 16` (`0x2C4`), `maxTravel = 13` (`0x2A8`),
   `trajectoryMode = 1` (`0x2B0`), plus two small flag fields.
5. `RegisterLinger(...)`.
6. Root cast only: `_dashDestination = ComputeDashDestination()`.
7. Root cast only:
   - `_splittingSpike` → `AssignSplittingVolley(report)`
   - else → `_selectedTarget = SelectPriorityTarget(report)`
8. Split child only:
   - `_overrideTarget != null` → `_selectedTarget = _overrideTarget`
   - else if `_hasGroundTargetOverride` → write `_groundTargetOverride` into target `0xF0`
9. If `_selectedTarget != null` → write its `transform.position` into target `0xF0`.
10. Copy `_volleyLateralBow` → `_lateralArc` (`0x2D4`) so the curve bends by the bow.
11. Spawn `_aetherCastIdleVfx`, start `FinishAetherCastBurst`.
12. Optional Surging stacks on the caster.
13. Start `WindUpAndLaunch`.

`FinishAetherCastBurst` simply waits `_aetherBurstSeconds`, stops every `ParticleSystem` on the
idle object, destroys it, and spawns `_aetherCastCompleteVfx` at the target position.

---

## 9. Dash direction — `ComputeDashDestination()`

- `ownerPos = [0xA0].transform.position`.
- If the owner has the controller component `0x185A1C8B8` **and** `[component+0x4F8] == 2`
  (player-controlled state):
  - read `Input.GetAxis("LeftStickHorizontal")` and `"LeftStickVertical"`;
  - if the stick magnitude² exceeds the deadzone constant, `dir = Normalize(stick)`;
  - otherwise fall through to facing.
- Otherwise (no controller / not in that state): return the current target position `0xF0`
  (AI fallback).
- Facing branch: `owner.GetComponent(0x185A1C9C8)` → an object whose vtable slot `+0x608`
  returns a `Direction`, converted by `FacingDirectionToVector(dir)`.
- Final result: `ownerPos + dir * dashDistanceConstant` (the dash length; matches the ~`_stepDistance = 3` sizing).

The dash itself is executed during `WindUpAndLaunch` via
`BaseSpellLibrary.DashTargetToPosition(owner, _dashDestination, ...)` and lasts roughly
`_stepTime` (0.2 s).

`FacingDirectionToVector(Direction)` is a jump table over the 8 compass directions returning unit
vectors (`(+1,0)`, `(+1,+1).normalized`, `(0,+1)`, `(-1,+1).normalized`, `(-1,0)`, `(-1,-1)`,
`(0,-1)`, `(+1,-1)`), defaulting to `Vector2.up`.

---

## 10. Target selection

### 10.1 Pool
`BaseSpellLibrary.GetAllEnemiesInRange(position, _seekRange = 10)` — same enemy query helper used
by other spells (this is the "30 world units" analogue: `seek range 10` × the family's 3.0 scale
factor; the spell itself passes `10`).

### 10.2 `SelectPriorityTarget(report)` — single-target (no Splitting Spike)
- `dotPriority = (_cripplingSpike == false)`.
- If the enemy list is non-empty:
  - if `dotPriority`: scan all enemies, compute `CountDotStacks(enemy)`, and keep the one with the
    **most DoT stacks** (ties → nearest to owner);
  - else (`_cripplingSpike`): use `GetNearest(enemies)`.
- Else return `null`.

`CountDotStacks(Stacking)` sums `Stacking.GetStackCount` over the three `_dotEffects` types.

`GetNearest(enemies)` returns the enemy with the smallest distance from the owner's position.

### 10.3 `SelectTopPriorityTargets(count, report)` — multi-target (Splitting Spike)
- `enemies = GetAllEnemiesInRange(owner, 10)`.
- If empty → empty list.
- Sorts the list with a `Comparison<ObjectsCommon>` that captures the owner position and
  `dotPriority`:
  - `dotPriority == true` → **descending total DoT stacks** (highest first),
  - `dotPriority == false` (`_cripplingSpike`) → **ascending distance** (nearest first).
- Returns the first `count` entries.
- Logs `enemiesInSeekRange`, `dotPriority`, `splitDistinctTargets`, and each rank as
  `"{name} (dot {n})"`.

---

## 11. The splitting volley

### 11.1 `ChronoSpikeVolley`
A tiny per-cast object holding `Dictionary<ObjectsCommon,int> _hitsPerTarget`.
`RegisterHit(target)` returns (and increments) how many times that target has been hit by the
volley. This is how the 2nd+ hit is detected for the 50 % repeat-hit penalty.

### 11.2 `AssignSplittingVolley(report)` — run by the root cast
1. Create a `ChronoSpikeVolley`, store in `_volley` (`0x388`).
2. `targets = SelectTopPriorityTargets(3, report)`.
3. Log `splitDistinctTargets`.
4. `bows = ComputeLateralBows(targets)`.
5. `_volleySlot = 0`.
6. `_volleyLateralBow = bows[0]`.
7. If no targets: `target 0xF0 = GroundSpotForSlot(0)` (log "no enemies - ground point").
   Else: `_selectedTarget = targets[0]` (log "highest DoT priority…").
8. `SpawnSplitProjectiles(targets, bows)`.

### 11.3 `ComputeLateralBows(targets)` → `float[3]`
- Group the 3 slots by target index using round-robin `slot % targets.Count`.
  - 3 distinct targets → each group has 1 slot.
  - 2 targets → groups {0,2} and {1}.
  - 1 target → group {0,1,2}.
  - 0 targets → keys `-1`.
- Within each group of size `k`, assign bows centered on 0 with step `_lateralBowStep = 2`:
  `bow[j] = (j - (k-1)/2) * 2`.
  - `k = 1` → `[0]`
  - `k = 2` → `[-1, +1]`
  - `k = 3` → `[-2, 0, +2]`
- Result is indexed by slot.

So shards sharing a target fan apart laterally by 2 units of arc each; shards on distinct targets
each fly bow `0`.

### 11.4 `SpawnSplitProjectiles(targets, bows)`
- Aborts if `_splitProjectilePrefab` is null.
- `startPos = Utility.GetMiddleOfSprite(owner.transform)`.
- For `i = 1..2` (slot 0 is the root cast itself):
  - `target = targets[i % targets.Count]` (or `null` if no targets); if the target `IsDead()`,
    treat as `null`.
  - `aimPos = target != null ? target.transform.position : GroundSpotForSlot(i)`.
  - Instantiate a clone of `_splitProjectilePrefab` and get its `ChronoSpikePrefab` component.
  - Set on the child:
    - `_isSplitProjectile = true`
    - `_volley = parent._volley`
    - `_volleySlot = i`
    - `_volleyLateralBow = bows[i]` (bounds-checked)
    - if live target: `_overrideTarget = target`
    - else: `_groundTargetOverride = groundPos; _hasGroundTargetOverride = true`
  - Combat-log `slot{i}Target` and `slot{i}Bow`.
- Logs `spikesInVolley = 3` and `distinctTargets`.

Because the child's `OnStart` then runs the "split child" branch, each child independently aims at
its assigned target/ground point and curves out with its bow.

### 11.5 `GroundSpotForSlot(slot)`
Used when there are fewer live targets than slots (or none):
- base direction = `_dashDestination − ownerPos`, normalized; if the dash was negligible, use the
  owner's facing direction.
- rotate that direction by `slot * _groundFanAngle` (plus `± _groundFanJitter` random jitter),
  converted from degrees to radians and applied as a Z rotation.
- distance = `Random.Range(a,b) * _seekRange`.
- return `ownerPos + rotatedDir * distance`.

Result: extra shards fan to ground points around the caster's aim, rather than flying off in
random directions.

---

## 12. Projectile travel (`SkillShot`) — the key "auto-target" question

Chrono sets `_useCurvedTrajectory = true`, so `SkillShot.Update` calls
**`MoveProjectileAlongCurve()`** every frame (else it would call `MoveProjectileTowardsTarget()`).

### 12.1 `MoveProjectileAlongCurve()`
- `dir = Normalize(target(0xF0) − _originalStartingPosition)`.
- Distance travelled this frame derives from `throwTime * _projectileSpeed`, clamped to
  `_maximumTravelDistance`.
- Uses `sin(progress * π)` to add:
  - an **arc-height** offset (`_arcHeight`, `0x2C8`), and
  - a **lateral-arc** offset (`_lateralArc`, `0x2D4`) perpendicular to `dir`.
- Writes the resulting world position to `transform.position`.

Because `_lateralArc` was set from `_volleyLateralBow`, this is exactly what produces the visible
sideways-curving flight and the fan between shards.

### 12.2 `MoveProjectileTowardsTarget()`
Straight-line version: `position += Normalize(target − position) * (deltaTime * _projectileSpeed)`,
snapping to the exact target and firing the arrival callback (`_targetPositionReached`) when within
the per-frame step.

### 12.3 Is it homing?
**No.** The target point `0xF0` is written **once, in `OnStart`**, from the target's position at
spawn time. `Update` moves toward that fixed point (or along a fixed arc). There is a
`CheckForTargetPositionReached` and an owner-reaim path (`UpdateAimFromOwnerInput`,
`OnRetargetedTargetUpdated`, `SupportsOwnerReaim`) but for the Chrono split children nothing
re-writes `0xF0` per frame. The apparent "auto-targeting" is therefore:

> **target assignment at cast time + a curved (arc + lateral-bow) trajectory — not continuous
> turn-rate homing.**

This is the crucial distinction versus a true homing implementation (e.g. `AetherHowlProjectilePrefab`),
which rotates its heading toward the target over time.

Arrival detection in `Update`:
- If the distance to `0xF0` is within epsilon → mark reached and invoke the arrival callback.
- If travelled distance ≥ `_maximumTravelDistance` → mark max-distance reached and invoke the
  matching callback (destroys/detonates as appropriate).

---

## 13. Impact resolution — `ResolveImpact(target, path)`

Called from:
- `ApplyTargetHit(target)` (override) with path **`"mid-flight collision"`** — via
  `SweepStepForHits` (Physics2D overlap along the path).
- `OnDestinationReached()` with path **`"destination reached"`**.

Algorithm (ISIL ~line 7488+):

1. Open a combat-log report `"Chrono Spike (impact)"`, log `path`, `target`, `role`
   (`split child` if `_isSplitProjectile`, else `root cast`).
2. If `target == null` → jump to the no-target/ground branch (spawn impact VFX + camera shake).
3. **Dedupe:** if `_resolvedTargets` already contains `target`, return.
4. **Volley hit index:**
   - if `_volley != null`: `hitIndex = _volley.RegisterHit(target)`;
   - else `hitIndex = 1`.
   - log `volleyHitIndex`.
5. **Damage:**
   ```
   base       = GetBaseDamage(...)
   dmg        = base * (1 + _damageIncrease * 0.2)     // 0.2 = _baseStackBonusPct
   if (_splittingSpike) dmg *= splittingScale          // constant (unverified)
   if (hitIndex > 1)    dmg *= 0.5                     // _repeatHitDamageScale
   SendDamageToTarget(target, 11, dmg, ...)            // damage-type id 11
   ```
   Logs `baseDamage`, `splittingScale`, and `repeatHitReduced` when the 50 % penalty applies.
6. The remainder of the method handles the DoT-stack bookkeeping
   (`CountDotStacks`, `SplitConsumptionBudget(budget, burnAvail, poisonAvail, bleedAvail, ...)`)
   and the impact VFX / camera shake.

`OnDestinationReached()` specifically:
- if `_selectedTarget` exists and is not dead → `ResolveImpact(_selectedTarget, "destination reached")`;
- else open `"Chrono Spike (no impact)"` and record a `reason`:
  - `"ground spike - no enemy for this volley slot, expired on arrival"` when a ground-fanned
    shard with no target arrives,
  - `"no target was selected at cast"`,
  - `"selected target died mid-flight"`;
- then spawn `_impactVfx` + `CameraShake`.

---

## 14. Damage / DoT model summary

- Base damage comes from `GetBaseDamage`, scaled by `_damageIncrease` at `+20 %` per point.
- The spell's identity is **DoT-stack interaction**: `_baseStacksConsumable = 3`,
  `_stacksConsumeIncrease` raises the budget, and `_dotEffects` (burn/poison/bleed) are counted by
  `CountDotStacks`. `SplitConsumptionBudget` divides a stack budget across whatever DoT types are
  present.
- Default targeting favors the enemy with the **most DoT stacks**; the **Crippling Spike**
  upgrade switches targeting to **nearest**.
- **Repeat hits in one volley deal 50 % damage** (tracked per target by `ChronoSpikeVolley`).

---

## 15. VFX & feedback

- Wind-up: `_aetherCastIdleVfx` spawned at cast; after `_aetherBurstSeconds` its particle systems
  stop and it is replaced by `_aetherCastCompleteVfx`.
- Launch: `_projectileVfx.SetActive(true)`.
- Impact: `_impactVfx` spawned at the hit point plus `BaseSpellLibrary.CameraShake(...)`.

---

## 16. Why it looks like "auto-targeting" (and why true homing differs)

| Aspect | Chrono Spike (vanilla) | A true homing shard |
|---|---|---|
| Aim point | Captured **once at spawn** (`0xF0`) | Updated every frame |
| Steering | Fixed arc: `sin(π·progress)` height + fixed lateral bow | Turn-rate rotation toward current nearest enemy |
| "Curve" | Ballistic + lateral arc | Continuous heading change |
| Target spread | 3 slots assigned to top-3 DoT/nearest targets | Per-shard nearest lookup |
| Repeat hits | 50 % damage via volley counter | — |

If you want a *homing* variant, you must (a) continuously rewrite the travel target `0xF0`
**and** re-derive the heading each frame, or (b) use the turn-rate steering pattern from
`AetherHowlProjectilePrefab`, because the base `MoveProjectileAlongCurve` only follows a static
arc to a fixed point.

---

## 17. Method reference (ISIL line numbers in `ChronoSpikePrefab.txt`)

| Method | Starts at |
|---|---:|
| `get__baseDamage` | 3 |
| `OnStart` | 16 |
| `ReadSkillTreeUpgrades` | 1234 |
| `SelectPriorityTarget` | 1550 |
| `CountDotStacks` | 2078 |
| `GetNearest` | 2219 |
| `SpawnAetherCastBurst` | 2503 |
| `FinishAetherCastBurst` | 2712 |
| `WindUpAndLaunch` | 2804 |
| `ComputeDashDestination` | 2862 |
| `FacingDirectionToVector` | 3106 |
| `AssignSplittingVolley` | 3256 |
| `ComputeLateralBows` | 3525 |
| `GroundSpotForSlot` | 3920 |
| `SpawnSplitProjectiles` | 4220 |
| `SelectTopPriorityTargets` | 5085 |
| `ApplyTargetHit` | 5708 |
| `OnDestroyLingerStarted` | 5885 |
| `OnDestinationReached` | 6081 |
| `SplitConsumptionBudget` | 6454 |
| `ResolveImpact` | ~6534 (ISIL 7361) |
| `.ctor` | 8224 |

Nested coroutine bodies:
`ChronoSpikePrefab_NestedType__WindUpAndLaunch_d__52.txt`,
`ChronoSpikePrefab_NestedType__FinishAetherCastBurst_d__51.txt`.

Base movement: `SkillShot.txt` — `Update` 531 (ISIL 778),
`MoveProjectileAlongCurve` 2186 (ISIL 2313),
`MoveProjectileTowardsTarget` 2443 (ISIL 2570),
`AimSpellAtTarget` 1313, `OnRetargetedTargetUpdated` 1352, `SupportsOwnerReaim` 1554.

---

## 18. Open / unverified items

- Exact numeric values of the unnamed float constants (dash distance `0x18465DBE0`, splitting
  damage scale `0x18465DCF4`, stick deadzone `0x18465E1C8`, ground-fan ranges, etc.) were not
  resolved; they are named where the C# constants make the intent obvious.
- The exact damage-type id `11` passed to `SendDamageToTarget` is a raw enum value.
- `_echoingSpike` / `_rendingSpike` / `_reversingSpike` per-upgrade effects live in
  `ReadSkillTreeUpgrades` and the impact/DoT path; only their storage and damage contribution
  could be confirmed from these dumps.
