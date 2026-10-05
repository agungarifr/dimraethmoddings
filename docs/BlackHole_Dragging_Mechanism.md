# Black Hole Dragging Mechanism — Complete Reverse-Engineering Reference

> **Target game:** Dimraeth (Unity IL2CPP, x64)  
> **Spell IDs:** `SpellbookBlackHole = 47`, `Spell.Blackhole = 90`  
> **Purpose:** Sufficient detail for an AI to reimplement the enemy-drag (pull) mechanic, e.g. for a "Twister" (Vortex upgrade) BepInEx Harmony mod.  
> **Methodology:** IL disassembly (Cpp2IL ISIL dump), IL2CPP interop signatures (ilspycmd), DiffableCs field-offset dump, raw GameAssembly.dll byte reads for float constants.  
> **Language notes:** Indonesian annotations allowed; all code/signatures in English.

---

## Table of Contents

1. [Mechanical Summary](#1-mechanical-summary)
2. [Class Structure](#2-class-structure)
3. [Field Offsets](#3-field-offsets)
4. [Core Drag Pipeline — Verbatim Disassembly](#4-core-drag-pipeline--verbatim-disassembly)
5. [`BaseSpellLibrary.DashTargetToPosition` — Full Signature & Semantics](#5-basespelllibrarydashtargettoposition--full-signature--semantics)
6. [Multiplayer / Authority Notes](#6-multiplayer--authority-notes)
7. [BepInEx Harmony Reimplementation Recipe](#7-bepinex-harmony-reimplementation-recipe)
8. [Evidence, Confidence & Unknowns](#8-evidence-confidence--unknowns)

---

## 1. Mechanical Summary

| Property | Value | Source |
|---|---|---|
| **Spell type** | `Blackhole : Spells` (spellbook config) + `BlackholePrefab : AreaOfEffect` (runtime prefab) | DiffableCs, Interop |
| **AoE radius** | **5.0 units** (2D) | `Blackhole.Setup` passes `5.0f` to radius setter (constant `0x18465DC00` → `5.0f`, resolved from GameAssembly.dll `.rdata`) |
| **Lifetime (`_maxTimeAlive`)** | **0.6 seconds** | `BlackholePrefab.OnStart` sets `0x3F19999A` = `0.6f` (immediate) |
| **Drag cadence** | Every AoE strike tick per target (see §4.4) | `AreaOfEffect.UpdateTargetTimes` → virtual `AreaOfEffectStrike(target)` |
| **Drag duration per call** | **0.3 seconds** (coroutine lerp) | `AreaOfEffectStrike` passes `0.3f` as `time` (constant `0x18465DCF0` → `0.3f`, resolved) |
| **Drag distance per call** | **Full current 2D distance** from target's transform to the hole's transform | `BaseSpellLibrary.GetDistanceToSpellPrefab` = `Vector2.Distance(target.transform.position, this.transform.position)` |
| **Drag direction** | **Toward the spell prefab's transform position** (the hole center) | `this.transform.position` passed as `position` arg |
| **Displacement mechanism** | **Coroutine-based `transform.position` lerp** (NOT physics force, NOT NavMeshAgent.Move) | `ObjectsCommon.KnockbackRoutine` coroutine (see §4.5) |
| **Collision handling** | `Physics2D.RaycastAll` along path; stops at walls (`Boundary` tag, `BossCollider` tag, `BuildFormulas.BlocksTraversal`) | `KnockbackRoutine` MoveNext |
| **NavMeshAgent handling** | Stopped + disabled at coroutine start; restored at end | `KnockbackRoutine` init + cleanup |
| **Pull immunity** | `ObjectsCommon.Immune.Value` (NetworkVariable\<bool\>) — silent return if true | `DashTargetToPosition` guard |
| **DisplaceExempt** | `MonsterBehaviourLibrary.DisplaceExempt` (offset 0x106) — skip + warning | `DashTargetToPosition` guard |
| **Authority gate** | Monsters: only on server (`IsServer && IsMonster`). Players: only local player. | `DashTargetToPosition` branch logic |
| **Post-drag effect** | `Passives.TryEtherHitPassives(comp, target)` if `IsSpellOwner(this)` and owner is player | `BlackholePrefab.AreaOfEffectStrike` tail |

### Drag in plain terms

Every AoE tick, each target inside the hole's 5-unit radius gets dragged toward the hole's center. The drag is a 0.3-second smooth position lerp covering the target's **entire current distance** to the hole — so a target at the edge is pulled all the way to the center in one 0.3s burst. The lerp stops early if a wall/obstacle blocks the path (raycast-checked per frame). The NavMeshAgent is disabled during the lerp and restored afterward. Enemies with `Immune.Value == true` or `DisplaceExempt` are skipped.

---

## 2. Class Structure

### 2.1 `BlackholePrefab : AreaOfEffect`

```
BlackholePrefab : AreaOfEffect : BaseSpell : BaseSpellLibrary : NetworkBehaviour
```

**Key methods (IL2CPP interop signature, `ilspycmd -t BlackholePrefab`):**

```csharp
// BlackholePrefab
protected override void OnStart();                    // spawns cast VFX, positions spell, sets _maxTimeAlive = 0.6f
protected override void AreaOfEffectStrike(ObjectsCommon target);  // THE drag call
public GameObject cast_vfx;                            // field at offset 0x2E8
```

### 2.2 `Blackhole : Spells`

```
Blackhole : Spells : SpellLibrary, ISpellBook
```

**`Blackhole.Setup()`** configures the spell parameters:
- Calls `SpellLibrary` setters to configure cast type, animation, attributes
- Passes **5.0f** as the AoE radius (constant `0x18465DC00` = `5.0f`)
- Calls `SpellLibrary.SetCastAnimType(this, 2)`

### 2.3 `BaseSpellLibrary : NetworkBehaviour`

Static/utility base class holding shared spell fields and the `DashTargetToPosition` entry point.

### 2.4 `ObjectsCommon : NetworkBehaviour, ICommonInterface, ICombatInterface`

Base class for all combat entities (players, monsters). Holds `ApplyKnockback`, `KnockbackRoutine`, `Immune`, `PullImmuneUntil`, etc.

### 2.5 `AreaOfEffect : BaseSpell`

Manages the AoE collider, target tracking dictionary (`_targetTimes`), and per-target strike cadence.

---

## 3. Field Offsets

All offsets are **absolute from the IL2CPP object pointer** (i.e., `ptr + offset`).

### 3.1 `BaseSpellLibrary` fields (inherited by `BlackholePrefab`)

| Field | Type | Offset | Notes |
|---|---|---|---|
| `_owner` (aka `_caster`) | `ObjectsCommon` | **0xA0** | The spell caster |
| `_ownerObject` | `GameObject` | 0xA8 | |
| `_target` | `ObjectsCommon` | 0xB0 | |
| `_targets` | `List<ObjectsCommon>` | 0xC0 | |
| `_spell` | ? | 0x1B0 | |
| `_spellComponent` | `Spells` | **0x1B8** | Used to call `DashToPosition` |
| `_castTime` | `float` | 0x1C0 | |
| `_maxTimeAlive` | `float` | **0x1C8** | Spell lifetime; Blackhole sets 0.6f |
| `_timealive` | `float` | 0x1CC | |

### 3.2 `BlackholePrefab` fields

| Field | Type | Offset | Notes |
|---|---|---|---|
| `cast_vfx` | `GameObject` | **0x2E8** | Cast VFX spawned at `OnStart` |

### 3.3 `AreaOfEffect` fields

| Field | Type | Offset | Notes |
|---|---|---|---|
| `_aoeCollider` | `Collider2D` | 0x288 | |
| `_targetTimes` | `Dictionary<ObjectsCommon, float>` | **0x290** | Per-target timer for strike cadence |
| `_targetColliders` | `Dictionary<...>` | 0x2A0 | |
| `_targetSweep` | `List<...>` | 0x2A8 | Sweep list for target enumeration |
| `_strandedTargets` | `List<...>` | 0x2B0 | |

### 3.4 `ObjectsCommon` fields (relevant to drag)

| Field | Type | Offset | Notes |
|---|---|---|---|
| `IsCasting` | `NetworkVariable<bool>` | 0x110 | |
| `Immune` | `NetworkVariable<bool>` | **0x140** | Pull-immunity gate; `DashTargetToPosition` reads `.Value` |
| `ActiveEffects` | `List<EffectValues>` | 0x1E8 | Rooted effect check for player dashes |
| `IsMonster` | `bool` | **0x224** | Authority branch discriminator |
| `IsPlayer` | `bool` | **0x225** | |
| `NetworkId` | `ulong` | 0x250 | |
| `_knockbackCoroutine` | `Coroutine` | **0x268** | Active KnockbackRoutine reference |
| `PullImmuneUntil` | `float` | **0x2B8** | Time-based pull immunity (`get_IsPullImmune()` compares to `Time.time`) |
| `NavMeshAgent` ref | `NavMeshAgent` | **0x190** | Used in ApplyKnockback/KnockbackRoutine |
| `_playerAnimations` | `PlayerAnimations`-typed | 0x2A0 (via GetComponent) | Animation state during dash |

### 3.5 `MonsterBehaviourLibrary` fields

| Field | Type | Offset | Notes |
|---|---|---|---|
| `DisplaceExempt` | `bool` | **0x106** | If true, `DashTargetToPosition` skips (unless `ignoreExempt=true`) |

### 3.6 `BaseSpell` reaim fields

| Field | Offset |
|---|---|
| `_reaimTarget` | 0x270 |
| `_reaimPosition` | 0x278 |
| `_reaimDirection` | 0x280 |

---

## 4. Core Drag Pipeline — Verbatim Disassembly

### 4.1 `BlackholePrefab.OnStart()` — Spell initialization

**Source:** `BlackholePrefab.txt` lines 3–117 (ISIL dump)

```
Key operations:
1. Check _owner (0xA0) is non-null
2. Read cast_vfx (0x2E8)
3. Call BaseSpellLibrary.SpawnAndTrack(this, cast_vfx, owner.transform.position, cast_vfx.transform.rotation, destroyDelay=0)
   → call 0x18114FFC0
4. Call BaseSpellLibrary.SpawnSpellAtMousePosition(this)
   → call 0x1811524D0
5. Set _maxTimeAlive (0x1C8) = 0x3F19999A = 0.6f  [immediate value, confirmed]
```

**Annotation:** `OnStart` spawns the visual effect and positions the spell prefab at the mouse position. The `0.6f` lifetime means the hole exists for 600ms.

### 4.2 `BlackholePrefab.AreaOfEffectStrike(target)` — THE drag call

**Source:** `BlackholePrefab.txt` lines 119–311 (ISIL dump)

**Verbatim key excerpt (annotated):**

```
; === Calculate distance from target to the hole ===
  Call BaseSpellLibrary.GetDistanceToSpellPrefab, rcx=this, rdx=target
  → xmm6 = distance (float, 2D Vector2.Distance)

; === Get hole center position ===
  Call Component.get_transform, rcx=this
  Call Transform.get_position, rcx=rax, → position (Vector2 packed in xmm1)
  movq r8, xmm1                    ; r8 = Vector2 position (hole center)

; === Load dash time constant ===
  movss xmm3, dword ptr [0x18465DCF0]  ; xmm3 = 0.3f  [RESOLVED: 0.3 seconds]

; === Call DashTargetToPosition ===
  ; Stack args (all zeroed):
  ;   [rsp+0x20] = useMiddleOfSprite = false
  ;   [rsp+0x28] = ignoreImmune = false
  ;   [rsp+0x30] = ignoreExempt = false
  ;   [rsp+0x38] = ignoreBodyRoot = null
  Call BaseSpellLibrary.DashTargetToPosition
    rcx = this (spell prefab, as BaseSpellLibrary)
    rdx = target (ObjectsCommon)
    r8  = position (Vector2, hole transform position)
    xmm3 = time = 0.3f
    ; distance is in xmm6 → passed as stack arg slot 0 (see §5)
    ; Actually: distance is the 4th float param → xmm? Let me re-check
    ; Per x64 ABI: rcx=this, rdx=target, r8=position(Vector2), xmm3=time,
    ;   then stack: distance, useMiddleOfSprite, ignoreImmune, ignoreExempt, ignoreBodyRoot
    ; BUT the disasm shows distance in xmm6 loaded earlier and passed via stack slot

; === Post-drag: Ether Hit passives ===
  Call BaseSpellLibrary.IsSpellOwner, rcx=this
  if (result) {
    if (this._owner != null && this._owner.[0x225] == IsPlayer) {
      comp = GetComponent(typeof [0x185A1C128])   ; some component type
      if (comp != null)
        Passives.TryEtherHitPassives(comp, target)  ; call 0x180915230
    }
  }
```

**Critical observation:** Each call to `AreaOfEffectStrike` passes **the full current distance** as the `distance` parameter and the **hole's transform position** as the target position. The 0.3s lerp therefore moves the target all the way to (or very near) the hole center.

### 4.3 `BaseSpellLibrary.GetDistanceToSpellPrefab(target)`

**Source:** `BaseSpellLibrary.txt` lines 23337–23436

```
Signature: float GetDistanceToSpellPrefab(ObjectsCommon target)
Implementation: return Vector2.Distance(target.transform.position, this.transform.position);
  → tail-call to 0x1806BC770 (Vector2.Distance)
Returns 0 if target is null.
```

### 4.4 `AreaOfEffect` strike cadence — When does `AreaOfEffectStrike` fire?

**Source:** `AreaOfEffect.txt` lines 102–231 (Update, Strike) + 2250–2827 (UpdateTargetTimes)

**`AreaOfEffect.Update()`** (per frame):
```
1. Call BaseSpell.Update(this)          ; base spell tick (lifetime countdown, etc.)
2. If _targetTimes (0x290) has Count > 0:
   Call AreaOfEffect.UpdateTargetTimes(this)
```

**`AreaOfEffect.UpdateTargetTimes()`** (per frame, if targets exist):
```
1. Enumerate _targets dictionary (0x290) via enumerator
2. For each target:
   a. Check target still in AoE: TargetStillInAreaOfEffect(target)
      → if NOT in AoE: move to _strandedTargets (0x2B0) for cleanup
   b. Check target.IsDead() → skip if dead
   c. Get current timer: _targetTimes[target]  (Dictionary<Object, float>)
   d. timer += Time.deltaTime
   e. _targetTimes[target] = timer  (store back)
   f. Check integer boundary crossing:
      oldFloor = FloorToInt(_targetTimes[target] BEFORE update)
      newFloor = FloorToInt(timer AFTER update)
      if (newFloor > oldFloor):
        → Virtual dispatch: call [vtable+0x5C8](this, target, newFloor)
          This is AreaOfEffectStrike(target) or a per-second variant
          (for BlackholePrefab, it resolves to AreaOfEffectStrike)
3. Cleanup stranded targets from dictionaries
```

**Cadence summary:** `AreaOfEffectStrike(target)` fires **once per second per target** (when the per-target accumulated timer crosses each integer boundary). This is the standard `AreaOfEffect` cadence. Each fire triggers a 0.3s drag lerp.

> **Note:** The exact virtual method dispatched depends on the vtable slot. For `BlackholePrefab`, the override is `AreaOfEffectStrike(ObjectsCommon)`. The `newFloor` integer is passed as a "seconds in AoE" parameter in some variants (`AreaOfEffectStrikeFriendlyOn1Second`).

**`AreaOfEffect.Strike()`** (called once when the AoE activates):
```
1. Check HasCollider()
2. Enable _aoeCollider (0x288) via Behaviour.set_enabled(true)
3. Virtual call [vtable+0x3A8](this)  → OnStrike() or similar activation hook
```

### 4.5 `ObjectsCommon.ApplyKnockback` + `KnockbackRoutine` — The actual displacement

This is where the physical movement happens. Despite the "Dash" naming in the call chain, the displacement is a **coroutine-based transform.position lerp**, not a physics impulse or NavMeshAgent.Move.

#### `ObjectsCommon.ApplyKnockback(Vector2 position, float duration, float distance, bool UseMiddleOfSprite=false, bool allowOverride=false, bool playerDash=false, Transform ignoreRoot=null)`

**Source:** `ObjectsCommon.txt` lines 3010–3333

**Flow (annotated):**
```
1. If this.IsPlayer (0x225):
   a. Get NavMeshAgent at [this+0x190]
   b. NavMeshAgent.set_isStopped(true)     ; stop agent movement
   c. Get this.transform.position
   d. Compute direction = normalize(position - this.transform.position)
      → Vector2.get_normalized
   e. If useMiddleOfSprite (stack:0xF0): recalculate direction from sprite center
   f. Call ObjectsCommon.CalculateAvailableKnockback(direction, distance, ignoreRoot)
      → returns availableKnockback (float, clamped by physics)
   g. displacement = direction * availableKnockback
   h. Call ObjectsCommon.DisplacementIsFinite(displacement, "dash to position")
      → if NOT finite (NaN/Inf): skip (jump to cleanup)
   i. If !allowOverride (stack:0xF8):
      → Create EffectValues(type=26, duration, 999, ...)
      → Add to this.ActiveEffects (0x1E8)  ; marks entity as "being knocked back"
   j. Call ObjectsCommon.KnockbackRoutine(displacement, duration, playerDash, ignoreRoot)
      → returns IEnumerator (coroutine)
   k. Store coroutine ref at [this+0x268]  ; _knockbackCoroutine
   l. StartCoroutine(coroutine)

2. Cleanup: restore registers, return
```

**Key detail:** The `distance` parameter is used only in `CalculateAvailableKnockback` to clamp the displacement. The actual movement direction is `normalize(targetPosition - currentPosition)`, and the movement amount is `direction * availableKnockback` (which may be less than `distance` if walls block).

#### `ObjectsCommon.KnockbackRoutine(Vector2 force, float duration, bool playerDash=false, Transform ignoreRoot=null)` — The coroutine

**Source:** `ObjectsCommon_NestedType__KnockbackRoutine_d__133.txt` lines 43–1140 (MoveNext)

**Coroutine state machine (annotated):**

**State 0 — Initialization:**
```
1. Get NavMeshAgent at [this+0x190]
2. If NavMeshAgent is active and on NavMesh:
   a. NavMeshAgent.set_isStopped(true)
   b. Save NavMeshAgent.enabled state to [rbx+0x40] (local)
   c. NavMeshAgent.set_enabled(false)      ; DISABLE agent during lerp
3. Get this.transform.position → startPosition
4. targetPosition = startPosition + force   ; force is the displacement vector
5. elapsed = 0  ([rbx+0x5C])
6. frameCount = 0  ([rbx+0x60])
7. hasPositioned = false  ([rbx+0x70])
```

**State 1 — Per-frame lerp loop:**
```
1. If elapsed >= duration: goto END
2. dt = Time.deltaTime, clamped to max 0.05f (constant 0x18465DE18 = 0.05f)
3. elapsed += dt  (clamped so elapsed <= duration)
4. t = elapsed / duration  (normalized 0..1)
5. Easing:
   if (!playerDash):
     easedT = 1 - Pow(1 - t, 2.75f)    ; constant 0x18465DE34 = 2.75f
     ; This is an ease-out curve (fast start, slow end)
   else:
     ; playerDash uses a cubic Hermite-like curve:
     threshold = 0.6666667f  (constant 0x18465E2D4 = 2/3)
     divisor = 0.3333333f    (constant 0x18465E824 = 1/3)
     if (t >= threshold):
       t2 = (t - threshold) / divisor
       easedT = threshold + divisor * (t2 + t2*t2 - t2*t2*t2)  ; smoothstep variant
     else:
       easedT = t  (linear for first 2/3)
6. Clamp easedT to [0, 1]
7. newPos = Lerp(startPosition, targetPosition, easedT)
8. Collision check (if !hasPositioned):
   a. delta = newPos - lastPosition
   b. deltaLength = delta.magnitude
   c. layers = LayerMask("Boundary" | "Ignore Raycast")
   d. hits = Physics2D.RaycastAll(lastPosition, delta.normalized, deltaLength, layers)
   e. For each hit:
      - Skip if collider is null
      - Skip if collider.transform is child of this.transform (self-collision)
      - Skip if ObjectsCommon.BelongsTo(collider.transform, ignoreRoot)
      - Check blocking:
        * tag == "Boundary" → BLOCKS
        * tag == "BossCollider" → BLOCKS
        * BuildFormulas.BlocksTraversal(collider) → BLOCKS
      - If blocking:
        → hitDistance = RaycastHit2D.distance - 0.1f  (constant 0x18465DBF8 = 0.1f skin)
        → clampedPos = lastPosition + delta.normalized * max(0, hitDistance)
        → Transform.set_position(clampedPos)
        → hasPositioned = true
        → break
   f. If no blocking hit:
      → Transform.set_position(newPos)   ; THE ACTUAL DISPLACEMENT
9. lastPosition = newPos
10. frameCount++
11. Return true (continue coroutine next frame)
```

**END — Cleanup (duration reached):**
```
1. Clear [this+0x268]  (_knockbackCoroutine = null)
2. Call virtual completion callback on [this+0x2A0]  (some interface)
3. If this.IsPlayer (0x225):
   Call ObjectsCommon.ApplyKnockbackClientRpc(this)
4. If this.[0x25] != 0 (is local player):
   a. Get PlayerAnimations component
   b. Check animation state (lunge anim = state 4 or 16)
   c. If holding attack: PlayerAnimations.SetAnimationState(4, 2, 0, 0)
   d. If near target (squared dist < 9.999999E-11 ≈ 0): SetAnimationState(4, 2, 0, 0)
5. Restore NavMeshAgent:
   a. NavMeshAgent.set_isStopped(false)
   b. NavMeshAgent.set_enabled(savedState)
   c. If NavMeshAgent is on NavMesh: NavMeshAgent.set_isStopped(false)
6. If was player and NavMeshAgent was disabled:
   → PlayerAnimations.UpdateFacingDirection(...)
   → NavMeshAgent.Warp(this.transform.position)  ; snap agent to new position
```

**Critical displacement summary:**
- **Mechanism:** `Transform.set_position()` called every frame during the coroutine
- **NOT:** physics `AddForce`, NOT `Rigidbody2D.MovePosition`, NOT `NavMeshAgent.Move`
- **Wall handling:** `Physics2D.RaycastAll` per frame; stops at walls with 0.1-unit skin margin
- **NavMeshAgent:** Disabled during lerp; restored + warped to final position at end
- **Easing:** `1 - Pow(1-t, 2.75)` — fast start, decelerating (ease-out)

---

## 5. `BaseSpellLibrary.DashTargetToPosition` — Full Signature & Semantics

### 5.1 Exact signature (from IL2CPP interop)

```csharp
// BaseSpellLibrary (instance method, inherited by all spell prefabs)
protected void DashTargetToPosition(
    ObjectsCommon target,           // rdx  — the entity to drag
    UnityEngine.Vector2 position,   // r8   — destination (hole center)
    float time,                     // xmm3 — lerp duration in seconds (0.3f for Blackhole)
    float distance,                 // stack:0xC0 — max drag distance (full current distance)
    bool useMiddleOfSprite,         // stack:0xC8 — use sprite center vs transform origin
    bool ignoreImmune = false,      // stack:0xD0 — skip Immune.Value check
    bool ignoreExempt = false,      // stack:0xD8 — skip DisplaceExempt check
    UnityEngine.Transform ignoreBodyRoot = null  // stack:0xE0 — ignore this transform subtree in collision
);
```

**Call address:** `0x1811484E0`

### 5.2 Parameter semantics

| Parameter | Role in Black Hole drag |
|---|---|
| `target` | The enemy/player being pulled |
| `position` | The **hole prefab's `transform.position`** — drag destination |
| `time` | **0.3 seconds** — lerp duration for the coroutine |
| `distance` | **Full 2D distance** from target to hole (via `GetDistanceToSpellPrefab`) — used as max displacement |
| `useMiddleOfSprite` | `false` — uses transform origin, not sprite center |
| `ignoreImmune` | `false` — respects `Immune.Value` |
| `ignoreExempt` | `false` — respects `DisplaceExempt` |
| `ignoreBodyRoot` | `null` — no subtree exclusion |

### 5.3 Constants used in the call chain

| Constant (dump VA) | Value | Where used | Meaning |
|---|---|---|---|
| `0x18465DCF0` | **0.3f** | `AreaOfEffectStrike` → `time` param | Drag duration |
| `0x18465DC00` | **5.0f** | `Blackhole.Setup` → radius | AoE radius |
| `0x3F19999A` (imm.) | **0.6f** | `OnStart` → `_maxTimeAlive` | Spell lifetime |
| `0x18465DB98` | **1.0f** | `ApplyKnockback` EffectValues ctor | Effect magnitude |
| `0x18465DE18` | **0.05f** | `KnockbackRoutine` max timestep | Frame delta clamp |
| `0x18465DE34` | **2.75f** | `KnockbackRoutine` easing exponent | Ease-out curve |
| `0x18465E2D4` | **0.6667f** (2/3) | `KnockbackRoutine` playerDash easing | Threshold |
| `0x18465E824` | **0.3333f** (1/3) | `KnockbackRoutine` playerDash easing | Divisor |
| `0x18465DE58` | **-0.1f** | `KnockbackRoutine` raycast dot-product | Backface threshold |
| `0x18465DBF8` | **0.1f** | `KnockbackRoutine` raycast skin width | Wall margin |
| `0x18465DD70` | **~1e-10** | `KnockbackRoutine` distance² threshold | Near-zero check |

> **Constant resolution method:** The dump VAs are offset by `+0x8000` from the on-disk `GameAssembly.dll` (verified by locating the `.rdata` float constant pool). File offset = `(dumpVA + 0x8000 - 0x180000000) - 0x1400`. All values confirmed by IEEE 754 byte reads.

### 5.4 Control flow inside `DashTargetToPosition`

**Source:** `BaseSpellLibrary.txt` lines 13443–14255

```
1. if (target == null):
     Debug.LogWarning("[DashTargetToPosition] target is null for spell {0}, prefab {1}")
     return

2. if (!ignoreImmune):
     immune = target.Immune.Value          ; NetworkVariable<bool> at 0x140
     if (immune) return                     ; SILENT return — pull immunity

3. isServer = NetworkManager.IsServer
   isMonster = target.IsMonster            ; 0x224

   if (isServer && isMonster):
     // MONSTER ON SERVER — the main drag path
     if (ignoreExempt):
       BaseSpellLibrary.StopExistingDashAndRestoreMovement(target)
       → proceed to dash
     else:
       comp = GetComponent<MonsterBehaviourLibrary>(target)
       if (comp != null && comp.DisplaceExempt):    ; 0x106
         Debug.LogWarning("[DashTargetToPosition] {0} is DisplaceExempt and ignoreExempt=false, skipping dash for spell {1}")
         return
       else:
         → proceed to dash

   else if (!isMonster || isServer):
     // PLAYER ON SERVER, or any client
     → {176} block (see below)

   else if (isMonster && !isServer):
     // MONSTER ON CLIENT — skip
     Debug.LogWarning("[DashTargetToPosition] Monster {0} dash skipped - not server (IsServerInstance={1}). Spell {2}, prefab {3}")
     → falls into {176} block

4. {176} block — player/client path:
   if (DataStorage.instance != null && IsPlayerOwner(this, target)):
     // target is the local player
     optional hook via DataStorage.instance.[0xE8].[0xE0]  ; virtual call
     → proceed to dash
   else:
     return   ; non-local-player target on client → NO DASH

5. Dash tail:
   if (_spellComponent == null):            ; 0x1B8
     Debug.LogWarning("[DashTargetToPosition] _spellComponent is null for spell {0}, cannot dash monster {1}. Prefab={2}")
     return
   _spellComponent.DashToPosition(
     target, position, time, distance, useMiddleOfSprite,
     isBackward=false, dashAnim=true, playerDash=false, ignoreBodyRoot, extra=0
   )   ; call 0x18118FF80
```

### 5.5 `Spells.DashToPosition` — Further dispatch

**Source:** `Spells.txt` lines 10244–10814

```csharp
void DashToPosition(
    ObjectsCommon owner, Vector2 location, float time, float range,
    bool useMiddleOfSprite, bool isBackward,
    bool dashAnim = true, bool playerDash = false, Transform ignoreRoot = null
)
```

**Flow:**
```
1. if (owner.IsPlayer && owner.ActiveEffects.Any(effect => effect is Rooted)):
     Debug.Log("[Spells] Dash blocked for {name} due to Rooted effect.")
     return

2. if (owner.[0x25] == 0 && owner.IsPlayer):   ; not local player
     DashToPositionClientRpc(owner.NetworkId, location, time, range, ...)
     return   ; remote player dash via RPC

3. if (isBackward):
     location = 2 * owner.position - location  ; mirror through owner

4. if (!owner.IsPlayer):   ; MONSTER path
     ObjectsCommon.ApplyKnockback(owner, location, time, range, useMiddleOfSprite,
                                   allowOverride=false, playerDash, ignoreRoot)
     return

5. PLAYER path:
   a. Movement.StopCharacter(GetComponent<Movement>(owner))
   b. animComp = GetComponent<PlayerAnimations>(owner)
   c. Check owner.IsCasting (0x110 NetworkVariable)
   d. PlayerAnimations.UpdateFacingDirection(animComp, location)
   e. ObjectsCommon.ApplyKnockback(owner, location', time, range, useMiddleOfSprite=false,
                                    playerDash, ignoreRoot, 0)
   f. PlayerAnimations.SetAnimationState(animComp, 4, 2, 0, 0)  ; dash animation
```

### 5.6 Displacement mechanism — Summary

**The actual displacement is `ObjectsCommon.ApplyKnockback` → `KnockbackRoutine` coroutine**, which:
1. Disables the NavMeshAgent
2. Lerps `transform.position` from start to target over `duration` seconds
3. Checks `Physics2D.RaycastAll` per frame for wall collisions
4. Restores the NavMeshAgent at the end

**It is NOT:**
- ❌ A physics force (`AddForce` / `Rigidbody2D`)
- ❌ A direct `NavMeshAgent.Move()` call
- ❌ A teleport (`transform.position = target` in one frame)
- ❌ A knockback in the physics sense (despite the method name)

**It IS:**
- ✅ A smooth position lerp over 0.3 seconds
- ✅ Wall-aware (raycast collision)
- ✅ NavMeshAgent-safe (disabled during, restored after)
- ✅ Eased (fast start, slow end)

---

## 6. Multiplayer / Authority Notes

### 6.1 Authority gates in `DashTargetToPosition`

| Scenario | Behavior | Log message |
|---|---|---|
| `target.Immune.Value == true` | **Silent return** (no log) | None |
| Monster + `!IsServer` | Skip | `"[DashTargetToPosition] Monster {0} dash skipped - not server (IsServerInstance={1}). Spell {2}, prefab {3}"` |
| Monster + `IsServer` + `DisplaceExempt` | Skip | `"[DashTargetToPosition] {0} is DisplaceExempt and ignoreExempt=false, skipping dash for spell {1}"` |
| Non-local player on client | Skip (falls through to return) | None |
| `_spellComponent == null` | Skip | `"[DashTargetToPosition] _spellComponent is null for spell {0}, cannot dash monster {1}. Prefab={2}"` |
| Player with Rooted effect | Skip | `"[Spells] Dash blocked for {name} due to Rooted effect."` |

### 6.2 `IsSpellOwner(this)` — Passive trigger gate

**Source:** `BaseSpellLibrary.txt` lines 24768–24906

Returns `true` if:
- `DataStorage.instance.[0x20] == this._owner` (local player owns the spell), OR
- `this._owner.IsMonster && NetworkManager.IsServer` (server-side monster), OR
- `this._owner.[0x226]` is set → returns `this._owner.[0x25]`

**Used in:** `BlackholePrefab.AreaOfEffectStrike` tail to gate `Passives.TryEtherHitPassives(comp, target)`.

### 6.3 `IsPlayerOwner(target)`

**Source:** `BaseSpellLibrary.txt` lines 24906–24973

Returns `target == DataStorage.instance.[0x20]` (local player reference).

### 6.4 `IsServerInstance()`

**Source:** `BaseSpellLibrary.txt` lines 27727–27754

Returns `NetworkManager.IsServer`. Throws if no `NetworkManager` exists.

### 6.5 `get_IsPullImmune()`

**Source:** `ObjectsCommon.txt` lines 1153–1181

```csharp
bool get_IsPullImmune() {
    return this.PullImmuneUntil > Time.time;   // 0x2B8 compared to Time.time
}
```

> **Note:** `DashTargetToPosition` checks `Immune.Value` (NetworkVariable<bool> at 0x140), NOT `IsPullImmune` (time-based at 0x2B8). These are two separate immunity mechanisms. `IsPullImmune` may be used elsewhere (e.g., after being pulled recently).

### 6.6 Multiplayer drag flow

```
HOST (server):
  BlackholePrefab.AreaOfEffectStrike(target)
    → DashTargetToPosition(target, ...)
      → [IsServer && IsMonster] → Spells.DashToPosition → ApplyKnockback → KnockbackRoutine
        → transform.position lerp (server-authoritative for monsters)

CLIENT (non-host):
  BlackholePrefab.AreaOfEffectStrike(target)
    → DashTargetToPosition(target, ...)
      → [IsMonster && !IsServer] → "Monster dash skipped - not server" → NO DASH
      → [IsPlayer && IsPlayerOwner] → Spells.DashToPosition → ApplyKnockback → KnockbackRoutine
        → transform.position lerp (local player only)
```

**Implication for mods:** If you call `DashTargetToPosition` on a monster from a client, it will **silently fail** with the "not server" warning. You must run monster drags on the server (host) side.

---

## 7. BepInEx Harmony Reimplementation Recipe

### 7.1 Strategy options

| Approach | Pros | Cons |
|---|---|---|
| **A. Prefix on `BlackholePrefab.AreaOfEffectStrike`** | Reuses all vanilla guards/cadence | Only works for Blackhole spell |
| **B. Standalone per-tick `DashTargetToPosition` call** | Works for any spell (e.g., Twister) | Must replicate guards yourself |
| **C. Direct `ApplyKnockback` call** | Bypasses `DashTargetToPosition` guards | Must handle immunity/authority yourself |

**Recommended for Twister:** Option B — call `DashTargetToPosition` from your own AoE tick, replicating the Blackhole pattern.

### 7.2 Option A — Prefix on `BlackholePrefab.AreaOfEffectStrike`

```csharp
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using Il2CppInterop.Runtime;

[BepInPlugin("com.yourname.twisterdrag", "Twister Drag", "1.0.0")]
public class TwisterDragPlugin : BasePlugin
{
    internal static new BepInEx.Logging.ManualLogSource Log;
    private static Harmony _harmony;

    public override void Load()
    {
        Log = base.Log;
        _harmony = new Harmony("com.yourname.twisterdrag");
        _harmony.PatchAll(typeof(TwisterDragPlugin));
        Log.LogInfo("Twister Drag loaded.");
    }

    // Postfix: after vanilla Blackhole drag, add extra pull
    [HarmonyPatch(typeof(BlackholePrefab), nameof(BlackholePrefab.AreaOfEffectStrike))]
    [HarmonyPostfix]
    static void Postfix_AreaOfEffectStrike(BlackholePrefab __instance, ObjectsCommon target)
    {
        // Vanilla already dragged; add extra suction if needed
        // Or use Prefix to REPLACE vanilla behavior:
    }

    // Prefix: replace vanilla drag entirely
    [HarmonyPatch(typeof(BlackholePrefab), nameof(BlackholePrefab.AreaOfEffectStrike))]
    [HarmonyPrefix]
    static bool Prefix_AreaOfEffectStrike(BlackholePrefab __instance, ObjectsCommon target)
    {
        if (target == null || target.Pointer == IntPtr.Zero) return true; // fall through
        if (target.IsDead()) return true;

        // Replicate vanilla drag:
        float distance = __instance.GetDistanceToSpellPrefab(target);
        Vector2 holePos = __instance.transform.position;
        float dragTime = 0.3f; // resolved constant

        __instance.DashTargetToPosition(
            target,
            holePos,
            dragTime,
            distance,
            false,  // useMiddleOfSprite
            false,  // ignoreImmune
            false,  // ignoreExempt
            null    // ignoreBodyRoot
        );

        return false; // skip vanilla (we already did it)
    }
}
```

### 7.3 Option B — Standalone per-tick drag (for Twister/Vortex)

```csharp
// In your Twister prefab's Update() or a Harmony postfix on its AreaOfEffectStrike:
static void PullEnemiesToCenter(BaseSpellLibrary spellPrefab, Vector2 center, float radius)
{
    // Get all enemies in range (use the game's own method if available)
    var enemies = spellPrefab.GetAllEnemiesInRangeOfPosition(center, radius);
    if (enemies == null) return;

    float dragTime = 0.3f; // match vanilla Blackhole

    foreach (var enemy in enemies)
    {
        if (enemy == null || enemy.Pointer == IntPtr.Zero) continue;
        if (enemy.IsDead()) continue;

        Vector2 enemyPos = enemy.transform.position;
        float dist = Vector2.Distance(enemyPos, center);
        if (dist <= 0.05f) continue; // already at center

        // Call the vanilla drag pipeline — handles immunity, authority, NavMesh, walls
        spellPrefab.DashTargetToPosition(
            enemy,
            center,        // drag toward hole center
            dragTime,      // 0.3s lerp
            dist,          // full current distance
            false,         // useMiddleOfSprite
            false,         // ignoreImmune (respect immunity)
            false,         // ignoreExempt (respect DisplaceExempt)
            null           // ignoreBodyRoot
        );
    }
}
```

### 7.4 Option C — Direct `ApplyKnockback` (bypass `DashTargetToPosition` guards)

```csharp
// Use ONLY if you need to bypass Immune/DisplaceExempt/authority checks
// and handle them yourself.
static void DirectPull(ObjectsCommon target, Vector2 destination, float duration)
{
    // ApplyKnockback is on ObjectsCommon (the target, not the spell)
    target.ApplyKnockback(
        destination,    // position to pull toward
        duration,       // lerp time (0.3f)
        0f,             // distance (0 = calculate from direction)
        false,          // useMiddleOfSprite
        false,          // allowOverride (false = add EffectValues marker)
        false,          // playerDash (false = use ease-out curve)
        null            // ignoreRoot
    );
}
```

### 7.5 Pitfalls & gotchas

| Pitfall | Explanation | Mitigation |
|---|---|---|
| **NavMeshAgent conflict** | `KnockbackRoutine` disables NavMeshAgent during lerp. If you call `NavMeshAgent.Move()` concurrently, it fights the coroutine. | Don't use `NavMeshAgent.Move` for pull; let `ApplyKnockback` handle it. |
| **Physics colliders** | The raycast checks `Boundary`, `BossCollider`, `BuildFormulas.BlocksTraversal`. Custom colliders may not block. | Add your custom colliders to the `Boundary` layer or tag. |
| **Pull immunity silent failure** | `Immune.Value == true` → silent return (no log). You won't know why nothing happened. | Check `target.Immune.Value` yourself before calling; log it. |
| **Offline/multiplayer silent failure** | Monster drags on client → "not server" warning + no drag. | Run drags on host side only. Check `NetworkManager.IsServer`. |
| **DisplaceExempt bosses** | Some bosses have `DisplaceExempt = true` (offset 0x106). They won't be dragged. | Pass `ignoreExempt: true` to bypass (may look janky on bosses). |
| **Rooted players** | `Spells.DashToPosition` blocks player drags if they have a Rooted effect. | Check `ActiveEffects` for Rooted before calling. |
| **Coroutine stacking** | Each `ApplyKnockback` starts a new coroutine. Rapid calls stack and fight. | `CancelActiveDash()` (at offset 0x268) cancels the previous one. Or use `allowOverride: true`. |
| **`_spellComponent` null** | `DashTargetToPosition` requires `_spellComponent` (0x1B8) to be set. If you call from a non-spell object, it'll warn and skip. | Use `ApplyKnockback` directly (Option C) if not from a spell prefab. |
| **IL2CPP method visibility** | `DashTargetToPosition` is `protected` in the game. IL2CPP interop may expose it as `public` or `internal`. | Use `AccessTools.Method(typeof(BaseSpellLibrary), "DashTargetToPosition")` if needed. |
| **Float constant addresses** | The constants (0.3f, 5.0f, etc.) are resolved from GameAssembly.dll. If the game updates, addresses shift. | Hardcode the float values (0.3f, 5.0f) instead of reading from memory. |

### 7.6 Prior art: `ContagionTunerPlugin.cs` suction implementation

**File:** `modding\BepInExModsSource\ContagionTuner\ContagionTunerPlugin.cs`

The ContagionTuner mod previously implemented a Black Hole suction effect for the Contagion spell. It was **disabled as glitchy** (see comments at lines 73, 134–141, 242–244).

**Three approaches were tried:**

1. **`PullEnemiesToCenter` (lines 142–201, commented out):** Called `cp.DashTargetToPosition(enemy, center, 0f, dist, false, false, false, null)` per enemy per tick.
   - **Failed because:** `DashTargetToPosition` silently fails in multiplayer/offline non-dash contexts with "Monster dash skipped - not server". Also, Contagion is cast on an infected enemy host; the suction center must be that host, and the host itself must not be pulled into itself.

2. **`ApplySuctionFrame` (lines 251–313, commented out):** Frame-by-frame continuous suction using `NavMeshAgent.Move(moveDelta)` or `transform.position += moveDelta`.
   - **Failed because:** "The suction effect felt glitchy in Dimraeth due to NavMeshAgent pathfinding and monster physics collider resistance."

3. **`ApplySuctionPulse` (lines 319–388, commented out):** Impulse burst suction on activate/ticks using `NavMeshAgent.Move` or `transform.position += moveDelta`.
   - **Same NavMeshAgent/physics glitch issues.**

**Why vanilla Black Hole does it better:**
- Vanilla uses `ApplyKnockback` → `KnockbackRoutine` coroutine which **disables NavMeshAgent** during the lerp (avoiding pathfinding conflicts) and **raycasts for walls** (avoiding physics clipping).
- The prior art's `NavMeshAgent.Move()` approach fought with the agent's own pathfinding, causing jitter/glitches.
- The prior art's `transform.position +=` approach had no wall collision, causing entities to clip through obstacles.

**Lesson for Twister mod:** Use `DashTargetToPosition` or `ApplyKnockback` (which internally uses `KnockbackRoutine`), NOT direct `NavMeshAgent.Move` or raw transform displacement.

### 7.7 Required fields/methods for reimplementation

To call `DashTargetToPosition` from a mod, you need:

| Item | How to access |
|---|---|
| `BaseSpellLibrary` instance (spell prefab) | `__instance` in Harmony patch, or `GetComponent<BaseSpellLibrary>()` |
| `_spellComponent` (0x1B8) | Must be non-null; it's a `Spells` component. Set automatically for spell prefabs. |
| `target` (`ObjectsCommon`) | The enemy/player to drag |
| `position` (Vector2) | The hole/spell center (`transform.position`) |
| `time` (float) | 0.3f (vanilla value) |
| `distance` (float) | `Vector2.Distance(target.transform.position, holePos)` |

If `_spellComponent` is null (non-spell object), use `target.ApplyKnockback(...)` directly instead.

---

## 8. Evidence, Confidence & Unknowns

### 8.1 Evidence table

| Claim | Evidence source | Line/offset | Confidence |
|---|---|---|---|
| `BlackholePrefab` extends `AreaOfEffect` | DiffableCs `BlackholePrefab.cs` | class decl | ✅ High |
| `cast_vfx` at 0x2E8 | DiffableCs field offset dump | offset table | ✅ High |
| `_owner` at 0xA0 | DiffableCs `BaseSpellLibrary.cs` | offset table | ✅ High |
| `_maxTimeAlive` at 0x1C8 | DiffableCs + ContagionTuner runtime check | offset table, line 408 | ✅ High |
| `OnStart` sets `_maxTimeAlive = 0.6f` | `BlackholePrefab.txt` lines 3–117 | immediate `0x3F19999A` | ✅ High |
| `AreaOfEffectStrike` calls `DashTargetToPosition` | `BlackholePrefab.txt` lines 119–311 | call `0x1811484E0` | ✅ High |
| `time = 0.3f` | `BlackholePrefab.txt` + GameAssembly.dll `.rdata` | constant `0x18465DCF0` → `0.3f` | ✅ High |
| `distance = GetDistanceToSpellPrefab` | `BlackholePrefab.txt` + `BaseSpellLibrary.txt` 23337 | call `0x18114ABC0` | ✅ High |
| `DashTargetToPosition` signature | `BaseSpellLibrary.txt` line 13443 + interop | method decl | ✅ High |
| `Immune.Value` guard | `BaseSpellLibrary.txt` 13850–14255 | offset 0x140 read | ✅ High |
| `IsMonster && !IsServer` skip | `BaseSpellLibrary.txt` 13850–14255 | branch logic | ✅ High |
| `DisplaceExempt` guard | `BaseSpellLibrary.txt` 13850–14255 | offset 0x106 read | ✅ High |
| `Spells.DashToPosition` → `ApplyKnockback` | `Spells.txt` 10244–10814 | call `0x181060780` | ✅ High |
| `ApplyKnockback` → `KnockbackRoutine` | `ObjectsCommon.txt` 3010–3333 | call `0x1810612F0` | ✅ High |
| `KnockbackRoutine` = transform.position lerp | `ObjectsCommon_NestedType__KnockbackRoutine_d__133.txt` 43–1140 | `Transform.set_position` calls | ✅ High |
| NavMeshAgent disabled during lerp | `KnockbackRoutine` MoveNext state 0 | `set_enabled(false)` | ✅ High |
| Physics2D.RaycastAll wall check | `KnockbackRoutine` MoveNext state 1 | `Physics2D.RaycastAll` call | ✅ High |
| Easing = `1 - Pow(1-t, 2.75)` | `KnockbackRoutine` MoveNext + constants | `0x18465DE34` = 2.75f | ✅ High |
| AoE radius = 5.0f | `Blackhole.txt` Setup + GameAssembly.dll | constant `0x18465DC00` → `5.0f` | ✅ High |
| Strike cadence = 1/sec/target | `AreaOfEffect.txt` UpdateTargetTimes | FloorToInt boundary check | ✅ High |
| `PullImmuneUntil` at 0x2B8 | DiffableCs `ObjectsCommon.cs` | offset table | ✅ High |
| `get_IsPullImmune()` = `PullImmuneUntil > Time.time` | `ObjectsCommon.txt` 1153–1181 | `comiss` + `seta` | ✅ High |
| `IsSpellOwner` logic | `BaseSpellLibrary.txt` 24768–24906 | branch logic | ✅ High |

### 8.2 Inferences (not directly confirmed)

| Claim | Basis | Confidence |
|---|---|---|
| `Blackhole.Setup` callee `0x18118D360` is a radius setter | Called with `xmm1 = [0x18465DC00]` (5.0f) and `this` | 🟡 Medium — method name not confirmed from interop |
| `0x180976580`, `0x18118BF70`, `0x18118C5C0`, `0x1808F70E0` in `Blackhole.Setup` are SpellLibrary config setters | Called with `this` and config values (3, 0xB, 2) | 🟡 Medium — exact method names not resolved |
| `AreaOfEffectStrike` fires once per second per target | `UpdateTargetTimes` uses `FloorToInt` boundary crossing | 🟡 Medium — exact cadence depends on virtual dispatch slot |
| `EffectValues(type=26)` in `ApplyKnockback` marks "knocking back" state | Effect type 26 created with duration=999 | 🟡 Medium — enum value not confirmed |
| `KnockbackRoutine` playerDash easing is smoothstep variant | Coefficient analysis of `0x18465E2D4`/`0x18465E824` | 🟡 Medium — exact formula inferred from constants |

### 8.3 Unknowns / unresolved

| Item | Status | Impact |
|---|---|---|
| `Blackhole.Setup` callee method names | Not resolved (addresses `0x180976580`, `0x18118BF70`, `0x18118D360`, `0x18118C5C0`, `0x1808F70E0`) | Low — not needed for drag reimplementation |
| `GetStacksFromTarget` "not server" behavior | Not read (line 11162 in BaseSpellLibrary.txt) | Low — not directly in drag path |
| `EffectValues` type enum value 26 meaning | Not confirmed | Low — cosmetic |
| Exact virtual dispatch slot for `AreaOfEffectStrike` in `UpdateTargetTimes` | vtable offset `0x5C8` identified but method name not confirmed | Medium — affects cadence understanding |
| `StopExistingDashAndRestoreMovement` body | Not read (BaseSpellLibrary.txt line 17910) | Low — only called when `ignoreExempt=true` |
| `CalculateAvailableKnockback` full logic | Partially read (ObjectsCommon.txt line 3465) — does raycast clamping | Medium — affects max displacement per call |
| `ContagionPrefab._radius` base value | ContagionTuner comment says "3.0" but not confirmed from game code | Low — not needed for Blackhole |

### 8.4 Disasm quirks noted

- **ISIL pseudo-compare vs raw x64:** Cpp2IL's `Compare/Move` of bool fields sometimes misrepresents `test al,cl` patterns (e.g., `DashTargetToPosition` authority branch). The raw `Disassembly:` block is authoritative for control flow.
- **Vector2 args:** Pass in `r8` via `movq r8, xmmN` (packed 2×float).
- **x64 float arg slots:** Float param #N goes to `xmm(slot)`. With `this`=rcx, `target`=rdx, `position`=r8 (Vector2), `time`=xmm3. The `distance` param goes to stack slot 0 (`[rsp+0xC0]` in callee frame).
- **Constant pool offset:** The on-disk `GameAssembly.dll` `.rdata` constant pool is at `dumpVA + 0x8000`. File offset = `(dumpVA + 0x8000 - 0x180000000) - 0x1400`. Verified by IEEE 754 byte reads.

---

## Appendix A: Quick-reference constants

```csharp
// Black Hole drag parameters (verified from GameAssembly.dll)
const float BLACKHOLE_RADIUS = 5.0f;           // AoE radius
const float BLACKHOLE_LIFETIME = 0.6f;         // _maxTimeAlive
const float DRAG_DURATION = 0.3f;              // per-strike lerp time
const float KNOCKBACK_EASE_EXPONENT = 2.75f;   // ease-out curve
const float KNOCKBACK_MAX_TIMESTEP = 0.05f;    // frame delta clamp
const float KNOCKBACK_SKIN_WIDTH = 0.1f;       // wall raycast margin
const float KNOCKBACK_NEAR_ZERO = 1e-10f;      // squared distance threshold
```

## Appendix B: Full drag call chain

```
BlackholePrefab.AreaOfEffectStrike(target)          [BlackholePrefab.txt:119]
  └→ BaseSpellLibrary.GetDistanceToSpellPrefab(target)  [BaseSpellLibrary.txt:23337]
       └→ Vector2.Distance(target.transform.position, this.transform.position)
  └→ BaseSpellLibrary.DashTargetToPosition(              [BaseSpellLibrary.txt:13443]
       this, target, this.transform.position, 0.3f, distance, false, false, false, null)
       ├─ Guard: target.Immune.Value → silent return if true
       ├─ Guard: IsMonster && !IsServer → warning + skip
       ├─ Guard: MonsterBehaviourLibrary.DisplaceExempt → warning + skip
       ├─ Guard: non-local-player on client → skip
       └→ Spells.DashToPosition(                          [Spells.txt:10244]
            target, position, 0.3f, distance, false, false, true, false, null)
            ├─ Guard: Rooted effect on player → skip
            ├─ ClientRpc path for remote players
            └→ ObjectsCommon.ApplyKnockback(               [ObjectsCommon.txt:3010]
                 target, position, 0.3f, distance, false, false, false, null)
                 ├─ NavMeshAgent.set_isStopped(true)
                 ├─ direction = normalize(position - target.transform.position)
                 ├─ available = CalculateAvailableKnockback(direction, distance)
                 ├─ displacement = direction * available
                 ├─ DisplacementIsFinite(displacement) → skip if NaN/Inf
                 ├─ EffectValues(type=26) added to ActiveEffects
                 └→ ObjectsCommon.KnockbackRoutine(displacement, 0.3f, false, null)  [coroutine]
                      ├─ NavMeshAgent.set_enabled(false)
                      ├─ LOOP (per frame, 0.3s):
                      │    ├─ t = elapsed / 0.3
                      │    ├─ easedT = 1 - Pow(1-t, 2.75)
                      │    ├─ newPos = Lerp(start, target, easedT)
                      │    ├─ Physics2D.RaycastAll(lastPos → newPos) → stop at walls
                      │    └─ Transform.set_position(newPos)   ← THE DRAG
                      └─ END: NavMeshAgent.set_enabled(saved), .Warp(finalPos)
```

---

*Document generated: 2026-10-04*  
*GameAssembly.dll size: 103,220,736 bytes*  
*IL disassembly source: `modding\DecompilerTool\isil_out\IsilDump\Assembly-CSharp\`*  
*Interop signatures: `BepInEx\interop\Assembly-CSharp.dll` via `ilspycmd 8.2`*  
*Field offsets: `modding\cpp2il_cs_out\DiffableCs\Assembly-CSharp\`*
