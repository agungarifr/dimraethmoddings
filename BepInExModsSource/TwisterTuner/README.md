# Twister Tuner (BepInEx 6 / IL2CPP)

Tunes the **Twister** skill-tree upgrade of the Minotaur **Vortex** spell
(`Spell.Vortex = 99`), and optionally gives it a Black-Hole-style enemy pull.

> **Twister is not a separate spell.** It is the boolean field
> `VortexPrefab._twister` (offset `0x300`), set inside `VortexPrefab.OnStart`
> from `BaseSpellLibrary.GetSpellUpgradeLevel("Twister")`.
> Everything below was read from the shipped disassembly in
> `modding/DecompilerTool/isil_out/IsilDump/Assembly-CSharp/`
> (`VortexPrefab.txt`, `AreaOfEffect.txt`, `BaseSpell.txt`, `BaseSpellLibrary.txt`),
> **not** from the signature-only dumps in `cpp2il_cs_out/`.

---

## 1. What the vanilla Twister branch actually does

`VortexPrefab.OnStart` (from `VortexPrefab.txt`, ISIL 046–187):

| Step | Behaviour |
|------|-----------|
| `GetSpellUpgradeLevel("Twister")` | `_twister = level > 0` (`0x300`) |
| `_stableVortex` (`0x301`), `_repeatingVortex` (`0x302`), `_isRecast` (`0x303`), `_harvestingVortex` (`0x304`), `_enragingVortex` (`0x305`), `_ragingVortex` (`0x306`) | other upgrade flags |
| `_damageIncrease` (`0x308`), `_bleedingIncrease` (`0x30C`), `_twisterDurationIncrease` (`0x310`), `_harvestingVortexIncrease` (`0x314`), `_enragingVortexIncrease` (`0x318`), `_ragingVortexIncrease` (`0x31C`) | per-level values from upgrade names |
| `SetFollowCaster(Middle)` ++ | `_followOwner = 1` (`0x1DC`) always |
| **Twister == true** | `_maxTimeAlive = _twisterDurationIncrease * <perLevel> + <baseDuration>`, then `_castTime = same` (`0x1C0`), `PlayVortexAnimation` (spin anim **speed 1.0**), child(3).`SetActive(true)` + `SendMessage("Follow", caster.transform)` |
| **Twister == false** | `_maxTimeAlive = 0.9f`; if `_stableVortex` → `DashCasterForwardToPosition(...)` (the forward dash the upgrade removes) |
| `DataStorage.Singleton.SpellAnimationOverride` (`+0x241`) | set `1` while spinning, reset in `HandleBeforeSpellDestroyed` (animation only, **not** move speed) |

**Move while spinning**: `_followOwner = Middle` makes `BaseSpell.FollowOwnerCheck`
(`BaseSpell.txt` ISIL 010–043) call `Transform.set_position(spell.transform, Utility.GetMiddleOfSprite(owner.transform))`
every frame, so the vortex root rides on the player. Twister also skips the
`DashCasterForwardToPosition` that non-Twister Vortex uses. **There is no explicit
movement-speed penalty in the Vortex code**; the "normal move speed" requirement is
satisfied by taking the Twister branch (no dash) and re-asserting `_followOwner = Middle`.

**Damage**: `VortexPrefab.ApplyVortexDamage` (ISIL 099–318):

```
damageStart            = GetBaseDamage(<codeValue>)
damageIncreaseMult     = _damageIncrease * <perLevel> + 1.0
damageAfterIncrease    = damageIncreaseMult * damageStart
damageAfterTwister     = damageAfterIncrease / twisterDivisor   // ONLY when _twister == 1
repeatingMultiplier    = (_repeatingVortex && _isRecast) ? <repMult> : 1.0
final                  = repeatingMultiplier * damageAfterTwister   (then harvest/bleed mods)
SendDamageToTarget(target, type=4, final, ...)
```

The `twisterDivisor` divide happens only on the Twister path, so **vanilla Twister
already deals a fraction (≈½) of base Vortex damage per tick** — that is the
"damage = half of vanilla" requirement. `SendDamageToTarget` does **not** re-read
`GetBaseDamage` (it builds a `Damage` from the passed `multiplier`), so patching
`GetBaseDamage` scales the tick linearly with no double-application.

**Ticks**: `AreaOfEffect.UpdateTargetTimes` (`AreaOfEffect.txt`, line 2250) keeps a
`Dictionary<ObjectsCommon, float> _targetTimes` (`0x290`). Per target per frame:
`stored += Time.deltaTime`; if `floor(stored) > floor(previous)` it calls the virtual
`AreaOfEffectOn1Second(target, newSec)`. `VortexPrefab.AreaOfEffectOn1Second`
(ISIL 978) calls `ApplyVortexDamage(target, isTick=true)` when `_twister` is set.
So vanilla Twister = **1 damage tick per second**.

---

## 2. What this mod changes

All levers are config entries (file: `BepInEx/config/com.custom.twistertuner.cfg`).

| Config | Default | Effect |
|--------|---------|--------|
| `General.Enabled` | `true` | master switch |
| `General.ForceTwister` | `true` | Prefix `BaseSpellLibrary.GetSpellUpgradeLevel` → reports `"Twister"` as level ≥1, so vanilla takes the Twister branch even without the skill node |
| `Twister.Duration` | `6.0` | Postfix `VortexPrefab.OnStart` → `_maxTimeAlive` (`0x1C8`) = 6s (vanilla base ≈4s) |
| `Twister.SetCastTimeEqualToDuration` | `true` | also set `_castTime` (`0x1C0`), matching vanilla Twister |
| `Twister.TicksMultiplier` | `2.0` | Postfix `AreaOfEffect.UpdateTargetTimes` → add `(mult−1)·deltaTime` to every `_targetTimes` value → tick every `1.0/mult` s (0.5s at 2×) |
| `Twister.EnsureNormalMoveSpeed` | `true` | re-assert `_followOwner = Middle` (no dash, follow player) |
| `Twister.BuiltInTwisterDivisor` | `2.0` | the game's internal Twister divisor (see caveat §4) |
| `Twister.TargetDamageFraction` | `0.5` | final tick damage as a fraction of vanilla Vortex. Applied via `GetBaseDamage` postfix `× (BuiltInTwisterDivisor · TargetDamageFraction)`. Default `2.0·0.5 = 1.0` ⇒ unchanged = half of base Vortex |
| `AreaOfEffect.RadiusMultiplier` | `1.3` | widen the Twister AoE by +30%. Postfix on `AreaOfEffect.Start` (after vanilla assigns `_aoeCollider`) → scales the spell transform for visuals **and** measures the collider's world bounds; if the collider did not follow the transform it also multiplies the collider's own geometry (`CircleCollider2D.radius` / `BoxCollider2D.size` / `CapsuleCollider2D.size` / `PolygonCollider2D.points` / `EdgeCollider2D.points`, plus offsets). Net functional area is exactly ×mult regardless of whether `Collider2D` scales with its Transform. Also multiplies `BlackHoleSuction.Radius`. **Superseded:** an earlier version only scaled `transform.localScale` in the `VortexPrefab.OnStart` postfix, which did *not* widen the trigger in-game (kept commented in the source) |
| `Movement.MoveSpeedMultiplier` | `1.5` | while a Twister cast by the player is alive, multiply that player's movement speed by this (+50%). Postfix on static `Formulas.CalculatePlayerSpeed(Player, Attributes)`; the caster is tracked from `VortexPrefab.OnStart` (`GetOwner()`), `Player : ObjectsCommon` so the pointers match. Set `1.0` to disable |
| `BlackHoleSuction.Enabled` | `true` | pull nearby enemies into the vortex center using the game's own `DashTargetToPosition` |
| `BlackHoleSuction.Radius` | `6.0` | pull radius (world units) |
| `BlackHoleSuction.DashTime` | `0.3` | dash duration (s) passed to `DashTargetToPosition`; matches vanilla Black Hole (`0.3f`, resolved in the Black Hole doc §5.3) |
| `BlackHoleSuction.OnEntry` | `true` | dash an enemy toward the center the instant it enters the AoE (matches Black Hole) |
| `BlackHoleSuction.PerTick` | `true` | also dash every enemy in radius on each damage tick (repeated pulls read as continuous suction) |
| `Tooltip.UpdateDescription` | `true` | append a summary to the Vortex spellbook description |

### Suction implementation
Uses the game's own `BaseSpellLibrary.DashTargetToPosition` — the exact primitive
`BlackholePrefab.AreaOfEffectStrike` uses (see `docs/BlackHole_Dragging_Mechanism.md`):

```
DashTargetToPosition(target,
                     position = vortex.transform.position,          // center (follows the player, §1)
                     time     = SuctionDashTime,                    // dash duration (s)
                     distance = GetDistanceToSpellPrefab(target),   // current distance => ends at the center
                     useMiddleOfSprite = false,
                     ignoreImmune = false, ignoreExempt = false, ignoreBodyRoot = null)
```

* **On entry** (`SuctionOnEntry`): postfix on `VortexPrefab.AreaOfEffectStrike`,
  mirroring Black Hole's per-entry pull.
* **Per tick** (`SuctionPerTick`): postfix on `VortexPrefab.AreaOfEffectOn1Second`;
  re-dashes every enemy in `Radius` toward the center on each damage tick, which reads
  as continuous suction (Black Hole itself only pulls on entry).
* **Exclusions are the game's**: `DashTargetToPosition` itself honors
  `ObjectsCommon.Immune`, `MonsterBehaviourLibrary.DisplaceExempt`, Rooted effects and
  server authority. Monsters are only pulled on the **host** (clients log
  `"[DashTargetToPosition] Monster ... not server"` and do nothing — positions still
  converge via Netcode). `ObjectsCommon.IsPullImmune` is **not** checked by this path
  (per the Black Hole analysis), matching Black Hole's behavior.

---

## 3. Build & deploy

```powershell
& 'C:\Users\game\.dotnet\dotnet.exe' build `
  'D:\SteamLibrary\steamapps\common\Dimraeth\modding\BepInExModsSource\TwisterTuner\TwisterTuner.csproj' `
  -c Release
```

Output is copied to both `modding\BepInEx\plugins\` and the game root
`BepInEx\plugins\`. Targets `net6.0`; references the BepInEx 6 core + interop DLLs
in the repo (no NuGet packages).

---

## 4. Caveats

* **The exact float constants could not be resolved from `GameAssembly.dll`.**
  The ISIL global addresses for the literals (e.g. `[0x18465DBE0]` = `twisterDivisor`)
  map into the `il2cpp` section at code bytes, so the values are *inferred* from the
  milestone names (`twisterDivisor`, `damageAfterTwister`, `baseDuration`, …).
  `BuiltInTwisterDivisor` / `TargetDamageFraction` are therefore exposed so the
  default can be corrected without a rebuild if a future build differs.
  The default `2.0 · 0.5 = 1.0` means "change nothing" — vanilla Twister already
  delivers half of base Vortex.
* **Multiplayer**: damage/duration/tick maths run on the spell owner; run the same
  config on host and client. The continuous NavMesh pull runs client-side; the
  vanilla-dash variant is server-gated.
* Offsets are resolved at runtime via
  `IL2CPP.il2cpp_class_get_field_from_name` with the verified hex fallbacks from this
  build.
