# 11 — Obfuscated network values (`ObfuscatedNetworkInt` / `ObfuscatedNetworkFloat`)

Scope: the two `NetworkVariable<T>` subclasses that wrap sensitive gameplay values with an
obfuscated shadow copy + integrity hash. All behavioral claims cite the ISIL pseudo-op dumps in
`modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/` (line numbers refer to those files; file names
are abbreviated below as `ObfuscatedNetworkInt.txt`, `ObfuscatedNetworkFloat.txt`,
`ObfuscationConfig.txt`, `Bank.txt`, `Player.txt`, `ClientDiagnostics.txt`,
`GameAnalyticsManager.txt`, `QACheat.txt`). Signature-only stubs live in
`modding/DecompilerTool/ilrecovery_out/`. Every claim is marked **Verified** (clearly visible in
the numbered pseudo-ops or the raw disassembly), **Inferred** (strongly implied but a decompiled
artifact/identity is unconfirmed), or **Unknown**.

**Type family inventory** (checked: `ObfuscatedNetworkLong`, `ObfuscatedNetworkDouble`,
`ObfuscatedNetworkBool` do **not** exist anywhere in `ilrecovery_out/`, `cpp2il_cs_out/DiffableCs/`
or the IsilDump) **[Verified — absence of any matching symbol]**:

| Type | Base | Where | Notes |
|---|---|---|---|
| `ObfuscatedNetworkInt` | `NetworkVariable<int>` | `ilrecovery_out/ObfuscatedNetworkInt.cs:6` | Full scheme + static `OnIntegrityViolation` event |
| `ObfuscatedNetworkFloat` | `NetworkVariable<float>` | `ilrecovery_out/ObfuscatedNetworkFloat.cs:5` | Same scheme on bit-cast `int` bits; shares the Int event |
| `ObfuscatedInt` | (struct) | `ilrecovery_out/ObfuscatedInt.cs:4` | Non-network sibling: `_encoded`/`_key` XOR pair + full operator set (`ObfuscatedInt.txt` methods at L3–L1740). No integrity hash, no network sync. Out of scope here. |
| `ObfuscationConfig` | (static class) | `ilrecovery_out/ObfuscationConfig.cs:1` | Key derivation service used by both network types (see below) |

---

## Purpose & threat model

These types protect values that directly gate progression and combat power — gold, essences, XP,
skill points, level, health, stamina, concentration, shield, attack speed (see
[Protected values inventory](#protected-values-inventory)) — against two attack classes:

1. **Memory editors (Cheat Engine-style).** The `NetworkVariable<T>` base stores the *plaintext*
   value at instance offset 96 **[Verified — `ObfuscatedNetworkInt.txt:950` reads `[rcx+96]` as
   `base.Value`; `ObfuscatedNetworkFloat.txt:1038` writes the raw float through
   `NetworkVariable\`1<Single>.set_Value`]**. A cheat table finds and pokes that value. The class
   keeps two additional shadow copies — `_obfuscatedCache` (XOR-encoded) and `_integrityHash` —
   plus `_lastValidValue`. Editing any one of the copies without recomputing the others breaks
   mutual consistency and is detected on the next read; the class then repairs itself from
   `_lastValidValue` and reports the incident. This is a **tripwire design, not value hiding**: the
   obfuscation protects the *consistency of the redundant copies*, not the secrecy of the value.
2. **Network tampering / setter bypass.** Every change to the base value is routed through
   `HandleNetworkValueChanged` (hooked onto the base `OnValueChanged` delegate in the constructor,
   **[Verified — `ObfuscatedNetworkInt.txt:447–489`]**). Changes that did not go through
   `set_Value` while the local process is the server are treated as tampering and rolled back to
   `_lastValidValue`; changes arriving from the server while a client (or offline) are accepted and
   re-obfuscated as the new authoritative value.

An important secondary consumer of the same event is the game's broader anti-cheat (XP invariant
checks, XP/rune HMAC checks, speedhack detection in `Player.txt`) which calls
`ObfuscatedNetworkInt.RaiseIntegrityViolation` directly to feed the same reporting pipeline
(see [Integrity violation flow](#integrity-violation-flow)).

---

## API surface

`ObfuscatedNetworkInt` (Float column notes differences; `ilrecovery_out/ObfuscatedNetworkInt.cs`,
`ilrecovery_out/ObfuscatedNetworkFloat.cs` — signatures **Verified** from stubs):

| Signature | Visibility | Role |
|---|---|---|
| `.ctor(int value = 0, NetworkVariableReadPermission readPerm = Everyone, NetworkVariableWritePermission writePerm = Server)` (Float: `float value = 0f`) | public | Calls base `NetworkVariable<T>..ctor`, `InitializeObfuscation(value)`, then `Delegate.Combine`s `HandleNetworkValueChanged` into the base `OnValueChanged` delegate (`ObfuscatedNetworkInt.txt:442–489`, `ObfuscatedNetworkFloat.txt:107–151`) **[Verified]** |
| `new int Value { get; set; }` (Float: `new float`) | public property, hides base `Value` | Verified read/write path with integrity checks (see below) **[Verified]** |
| `static event Action<string> OnIntegrityViolation` | public static, **Int only** | Single shared violation channel for both types (stored in static field `m_OnIntegrityViolation`, `ilrecovery_out/ObfuscatedNetworkInt.cs:33`) **[Verified]** |
| `static void RaiseIntegrityViolation(string message)` | public static | Null-safe multicast invoke of the static event (`ObfuscatedNetworkInt.txt:302–333`) **[Verified]** |
| `void InitializeObfuscation(int value)` | private | One-time setup of keys/cache/hash/state (`ObfuscatedNetworkInt.txt:556–612`) **[Verified]** |
| `void RepairBaseValue(int trustedValue)` | private | Writes `trustedValue` back to `base.Value` under `_isRepairing`/`_settingValue` guards (`ObfuscatedNetworkInt.txt:1453–1497`) **[Verified]** |
| `void HandleNetworkValueChanged(int previousValue, int newValue)` | private | `OnValueChanged` callback; remote-sync acceptance / setter-bypass detection (`ObfuscatedNetworkInt.txt:1658–1817`) **[Verified]** |
| `static int ComputeIntegrity(int value)` (Float: `ComputeIntegrity(int bits)`) | private static | The integrity mix function (see scheme below) (`ObfuscatedNetworkInt.txt:1850–1879`) **[Verified]** |
| `static int GenerateKey()` | private static | Randomized key generator (`ObfuscatedNetworkInt.txt:1966–2055`) **[Verified]** |
| `static implicit operator int(ObfuscatedNetworkInt o)` (Float: `→ float`) | public static | `return o.Value` with a null check (`ObfuscatedNetworkInt.txt:2068–2076`) **[Verified]** |
| `static .cctor()` | static | `_rng = new Random(...)` (see scheme) (`ObfuscatedNetworkInt.txt:2112+`) **[Verified]** |

Float has **no** `OnIntegrityViolation` event and **no** `RaiseIntegrityViolation`; it calls
`ObfuscatedNetworkInt.RaiseIntegrityViolation` for all of its violation reports
(`ObfuscatedNetworkFloat.txt:731, 786, 1399`) **[Verified]**.

---

## Data model

Field layout (declaration order from `ilrecovery_out/ObfuscatedNetworkInt.cs:8–33` and
`ObfuscatedNetworkFloat.cs:7–29`; byte offsets recovered from the instance accesses in the dumps;
offset↔field mapping **Inferred**, the values written at those offsets **Verified**):

| Offset | Field | Type (Int / Float) | What it protects / holds |
|---|---|---|---|
| 96 | *(base `NetworkVariable<T>` value slot, name not recovered)* | `int` / `float` | The plaintext, network-synced value. First read target of any cheat scan. **[Verified as `base.Value` usage — `ObfuscatedNetworkInt.txt:950`]** |
| 112 | `_obfuscatedCache` (Float: `_obfuscatedBits`) | `int` | XOR shadow: `value ^ _obfuscationKey`. Float stores the IEEE-754 **bit pattern** XORed with the key (`ObfuscatedNetworkFloat.txt:244–246`) **[Verified]** |
| 116 | `_obfuscationKey` | `int` | Per-instance XOR key for the shadow copy **[Verified — written by `GenerateKey` results, `ObfuscatedNetworkInt.txt:576, 1168]** |
| 120 | `_integrityHash` | `int` | `ComputeIntegrity(value) ^ _integrityKey` — the tamper-evident checksum **[Verified]** |
| 124 | `_integrityKey` | `int` | Second per-instance key, folded into the hash **[Verified — `ObfuscatedNetworkInt.txt:578, 1002]** |
| 128 | `_lastValidValue` | `int` / `float` | Last value that passed verification; the fallback/repair source **[Verified — `ObfuscatedNetworkInt.txt:969, 1095]** |
| 132 | `_accessCount` | `int` | Read counter driving key rotation **[Verified — incremented at `ObfuscatedNetworkInt.txt:1154–1156]** |
| 136 | `_hasSynced` | `bool` | Set once a value has been committed; enables the `base.Value`-vs-shadow tamper check **[Verified — `ObfuscatedNetworkInt.txt:971, 1097–1098]** |
| 137 | `_settingValue` | `bool` | Re-entrancy flag while *we* write `base.Value`; tells `HandleNetworkValueChanged` the change is ours **[Verified — `ObfuscatedNetworkInt.txt:1381–1383, 1393]** |
| 138 | `_isRepairing` | `bool` | Repair re-entrancy flag; while set, `get_Value` short-circuits to `_lastValidValue` **[Verified — `ObfuscatedNetworkInt.txt:946–947, 1210–1218]** |
| 139 | `_initialized` | `bool` | Lazy-init guard for `InitializeObfuscation` **[Verified — `ObfuscatedNetworkInt.txt:940–944]** |
| — | `const int KEY_ROTATION_INTERVAL = 64` | `int` | Read-count threshold for re-keying (`ilrecovery_out/ObfuscatedNetworkInt.cs:22`; `Compare rax, 64` at `ObfuscatedNetworkInt.txt:1157`) **[Verified]** |
| static 0 | `_rng` | `Random` | Shared `System.Random` for `GenerateKey`, guarded by `Monitor.Enter/Exit` **[Verified — `ObfuscatedNetworkInt.txt:2001, 2033]** |
| static | `m_OnIntegrityViolation` | `Action<string>` | The static violation event field (Int only) **[Verified — `ilrecovery_out/ObfuscatedNetworkInt.cs:33; ObfuscatedNetworkInt.txt:318–319`]** |

Supporting keys in `ObfuscationConfig` (`ObfuscationConfig.txt`; three lazily cached `int?` fields,
`ilrecovery_out/ObfuscationConfig.cs:3–7`):

| Property | Derivation | Status |
|---|---|---|
| `BuildKey` | Lazy; a virtual call (vtable slot ~352) on a `GameConfig` static member (statics offset 16), result cached in `_buildKey` (`ObfuscationConfig.txt:76–103`) | **Inferred** (exact GameConfig member not recovered) |
| `CheckKey` | `_checkKey ??= BuildKey ^ 0x5A3B7C9D` — exact constant `0x5A3B7C9D` (`ObfuscationConfig.txt:166–176`) | **Verified** (constant + call) |
| `SessionKey` | `_sessionKey ??= BuildKey ^ DateTime.UtcNow.Ticks` (`ObfuscationConfig.txt:270–288`) | **Verified** (call sequence) |

---

## Obfuscation & integrity scheme

**Stored value relation** (`InitializeObfuscation`, `ObfuscatedNetworkInt.txt:556–612`; Float
`ObfuscatedNetworkFloat.txt:219–276`, identical except the value is bit-cast via
`Move rbx, xmm6` / `movd ebx,xmm6` — `ObfuscatedNetworkFloat.txt:243, 185`) **[Verified]**:

```
_obfuscationKey = GenerateKey();          // key #1   (ObfuscatedNetworkInt.txt:574–576)
_integrityKey   = GenerateKey();          // key #2   (ObfuscatedNetworkInt.txt:577–578)
_obfuscatedCache = value ^ _obfuscationKey;                        // :581–582
_integrityHash   = ComputeIntegrity(value) ^ _integrityKey;        // :583–608
_lastValidValue  = value;                                          // :596
_initialized     = true;                                           // :600
```

So the shadow copy is a plain **XOR with a per-instance random key** (Float: XOR on the raw 32-bit
float bit pattern), and the checksum is a key-seeded avalanche mix. Note again: `base.Value` keeps
the plaintext (written through `NetworkVariable<T>.set_Value` with the raw value,
`ObfuscatedNetworkInt.txt:1390`, `ObfuscatedNetworkFloat.txt:1038`) **[Verified]**.

**`ComputeIntegrity(int v)`** — exact algorithm recovered from
`ObfuscatedNetworkInt.txt:1850–1879` (byte-identical body in
`ObfuscatedNetworkFloat.txt:1488–1517` over the float's bits) **[Verified]**:

```
m  = ObfuscationConfig.CheckKey ^ v;      // :1856–1857   k2 folded in at entry
m ^= m >> 16;                             // :1858–1860
m  = m * 0x85EBCA6B;                      // :1861        (MurmurHash3 fmix-style constant)
m ^= m >> 13;                             // :1862–1864
h  = m * 0xC2B2AE35;                      // :1865        (second fmix-style constant)
m  = h ^ (h >> 16) ^ ObfuscationConfig.CheckKey;  // :1866–1871  (CheckKey folded in again)
m  = m * 0xCC9E2D51;                      // :1872        (MurmurHash3 c1 constant)
return m ^ (m >> 15);                     // :1873–1875
```

Everywhere the hash is stored, it is additionally XORed with `_integrityKey`, i.e.
`_integrityHash = ComputeIntegrity(v) ^ _integrityKey` (e.g. `ObfuscatedNetworkInt.txt:1372–1377`
in `set_Value`, where the same mix is inlined and the final result is
`(m>>15) ^ _integrityKey ^ m`) **[Verified]**. The verification recomputes the identical
expression and compares against `_integrityHash` (`ObfuscatedNetworkInt.txt:981–1005`) **[Verified]**.

Numeric constants, quoted exactly: mix multipliers `0x85EBCA6B`, `0xC2B2AE35`, `0xCC9E2D51`;
right-shifts `16`, `13`, `16`, `15`; `ObfuscationConfig` salt `0x5A3B7C9D`; `GenerateKey` fallback
constant `0x5A5A5A5A`; Float static-RNG seed salt `0x1234`; rotation threshold `64`. No string
salts are used in the scheme itself.

**`GenerateKey()`** (`ObfuscatedNetworkInt.txt:1966–2055`; Float copy at
`ObfuscatedNetworkFloat.txt:1604–1693`) **[Verified]**:

```
lock (_rng) {                                       // Monitor.Enter/Exit on the static Random (:2001, :2033)
    int r = _rng.Next();                            // virtual call slot ~400 (:2012–2014)
    int k = ObfuscationConfig.SessionKey            // :2020
            ^ Environment.TickCount                 // :2017–2018
            ^ r;                                    // :2022–2023
    if (k == 0) k = 0x5A5A5A5A;                     // :2024–2026  (cmove after test; avoid a zero key)
    return k;
}
```

**Static constructors** **[Verified]**:
- `ObfuscatedNetworkInt..cctor`: `_rng = new Random(Environment.TickCount)` (`ObfuscatedNetworkInt.txt:2124–2127`,
  disasm `:2091–2100` — no seed mangling).
- `ObfuscatedNetworkFloat..cctor`: `_rng = new Random(Environment.TickCount ^ 0x1234)` — the raw
  disasm contains `xor ebx,1234h` between `TickCount` and the `Random..ctor` seed
  (`ObfuscatedNetworkFloat.txt:1729–1739`). Each class owns its own `_rng`.

**Key rotation** (in `get_Value` only) **[Verified]**: `_accessCount` is incremented on every
verified read; at `>= KEY_ROTATION_INTERVAL` (64) it resets to 0, both keys are regenerated, and
the cache/hash are recomputed for the current value under the new keys
(`ObfuscatedNetworkInt.txt:1154–1200`). `set_Value` does **not** rotate keys and does not touch
`_accessCount` (`ObfuscatedNetworkInt.txt:1315–1405`) **[Verified]**.

---

## Read/write path

### `get_Value` (`ObfuscatedNetworkInt.txt:915–1221`; Float `ObfuscatedNetworkFloat.txt:573–867`)

Reconstructed control flow **[Verified unless noted]**:

1. **Lazy init.** If `!_initialized` → `InitializeObfuscation(base.Value)` (`:940–944`).
2. **Repair re-entrancy short-circuit.** If `_isRepairing` → return `_lastValidValue` immediately
   (`:946–947` → `:1210–1218`). This is what makes recursive reads safe during repair and also
   forces reads during repair to report the trusted value.
3. **First-sync resync.** If `!_hasSynced` and `(_obfuscationKey ^ _obfuscatedCache) != base.Value`
   → rebuild `_obfuscatedCache = base.Value ^ _obfuscationKey`, `_integrityHash =
   ComputeIntegrity(base.Value) ^ _integrityKey` (the call to `0x180C77980` at `:964` is the
   out-of-line `ComputeIntegrity` **[Inferred]**), set `_lastValidValue = base.Value`, `_hasSynced
   = true` (`:948–971`).
4. **Decode.** `decoded = _obfuscationKey ^ _obfuscatedCache` (`:973–975`).
5. **Hash check.** Recompute `ComputeIntegrity(decoded) ^ _integrityKey` and compare with
   `_integrityHash` (`:981–1005`).
   - **Mismatch → integrity violation** (`:1006–1087`): set `_isRepairing = true`; adopt
     `_lastValidValue` as the trusted value; re-obfuscate `_obfuscatedCache` and
     `_integrityHash` for the trusted value; fire
     `"ObfuscatedNetworkInt integrity violation: " + String.Format("decoded={0}, lastValid={1}", …)`
     (`:1069–1074`) on `OnIntegrityViolation`; call `RepairBaseValue(trustedValue)` (`:1082`);
     clear `_isRepairing` in a `finally` (`:1084–1085`). The returned value is the trusted value,
     not the tampered one.
   - **Hash OK** (`:1093–1095`): `_lastValidValue = decoded`. Then a second check: if `_hasSynced`
     and `base.Value != decoded` (`:1096–1101`) → **base.Value tamper** (the raw network slot was
     edited): fire `"ObfuscatedNetworkInt base.Value tamper: " + String.Format("base={0},
     trusted={1}", base.Value, decoded)` (`:1128–1133`), `RepairBaseValue(decoded)` (`:1141`),
     `_isRepairing` cleared in `finally` (`:1143–1144`).
6. **Rotation tail.** Increment `_accessCount`; at `>= 64` rotate keys and recompute cache/hash for
   the returned value (`:1154–1200`).
7. **Return** `decoded` (post-fallback) (`:1201–1209`).

The Float variant is structurally identical; the message prefixes are
`"ObfuscatedNetworkFloat integrity violation: "` (`ObfuscatedNetworkFloat.txt:722`) and
`"ObfuscatedNetworkFloat base.Value tamper: "` (`ObfuscatedNetworkFloat.txt:777`), and the boxed
format arguments are floats (typeinfo `[0x185D2C9F8]`, `:706, 713`).

*Quirk observed as-is:* in the integrity-violation branch the value formatted under the
`decoded={0}` label is the register/stack value that was **reloaded from `_lastValidValue`**
(`ObfuscatedNetworkInt.txt:1008–1009`; `ObfuscatedNetworkFloat.txt:666–668`) before the message is
built, so both `{0}` and `{1}` box `_lastValidValue` (`ObfuscatedNetworkInt.txt:1055–1065`).
The message reports the trusted value twice; see Open questions.

### `set_Value` (`ObfuscatedNetworkInt.txt:1315–1405`; Float `ObfuscatedNetworkFloat.txt:962–1053`)

**[Verified]**

```
if (!_initialized) { _obfuscationKey = GenerateKey(); _integrityKey = GenerateKey(); _initialized = true; }
_obfuscatedCache = value ^ _obfuscationKey;            // :1347–1349
_integrityHash   = ComputeIntegrity(value) ^ _integrityKey;   // :1354–1377 (inlined mix)
_lastValidValue  = value;                              // :1378–1379
_hasSynced       = true;                               // :1380–1381
_settingValue    = true;                               // :1382–1383
try   { base.Value = value; }     // NetworkVariable<T>.set_Value (:1390)
finally { _settingValue = false; }                     // :1392–1393
```

Notes: writes go through the trusted path only — no verification is performed (a write is assumed
legitimate), and no key rotation happens here. The `try/finally` is a real IL2CPP EH frame (the
stack slots at `:1384–1386` plus the rethrow dispatch at `:1405`).

### `RepairBaseValue` (`ObfuscatedNetworkInt.txt:1453–1497`; Float `ObfuscatedNetworkFloat.txt:1101–1143`)

**[Verified]**: `_isRepairing = true; _settingValue = true;` → `base.Value = trustedValue`
(`:1474` — `NetworkVariable<T>.set_Value`), `finally { _settingValue = false; _isRepairing = false; }`.
It restores the network slot only; callers have already fixed `_obfuscatedCache`/`_integrityHash`
before calling it (see get_Value branches) — i.e. repair = "revert base slot to `_lastValidValue`
under suppression flags".

---

## `HandleNetworkValueChanged` (remote sync validation)

`void HandleNetworkValueChanged(int previousValue, int newValue)`
(`ObfuscatedNetworkInt.txt:1658–1817`; Float `ObfuscatedNetworkFloat.txt:1300–1455`) **[Verified
unless noted]**. It is combined into the base `OnValueChanged` delegate in the constructor
(`ObfuscatedNetworkInt.txt:447–489`) — i.e. it observes **every** change of the network slot, local
or remote. `previousValue` is never read in the recovered body.

```
if (_settingValue) { _hasSynced = true; return; }        // :1678–1679 → :1811–1816
                                                       // our own set_Value/RepairBaseValue → accept silently
if (NetworkManager.Singleton != null) {                // :1694–1706
    if (NetworkManager.Singleton.IsServer) {           // :1726–1728
        // A change we did not make, on the authority → setter bypass / tamper
        OnIntegrityViolation("ObfuscatedNetworkInt base.Value setter bypass: "
            + String.Format("newValue={0}, trusted={1}", newValue, _lastValidValue));   // :1750–1758
        RepairBaseValue(_lastValidValue);              // revert; tail call (:1763–1772)
        return;
    }
}
// Client (or no NetworkManager yet): the server value is authoritative — accept and re-obfuscate
_obfuscatedCache = newValue ^ _obfuscationKey;          // :1773–1775
_integrityHash   = ComputeIntegrity(newValue) ^ _integrityKey;   // :1780–1805
_lastValidValue  = newValue;                           // :1794
_hasSynced       = true;                               // :1797
```

Interpretation: on the **server**, any mutation of the network slot that bypassed `set_Value`
(e.g. a mod directly assigning `base.Value`, or a hostile write-permission path) is detected here,
reported, and rolled back. On **clients** (and offline), received values are adopted as the new
truth and re-protected — the scheme trusts the server's authority and protects the client's memory
consistency instead. The Float variant reports
`"ObfuscatedNetworkFloat base.Value setter bypass: "` (`ObfuscatedNetworkFloat.txt:1388`) and calls
`RepairBaseValue(_lastValidValue)` explicitly (`ObfuscatedNetworkFloat.txt:1400–1403`); in the Int
variant the corresponding tail call is decompiled as `Invalid "Jump target not found in method."`
(`ObfuscatedNetworkInt.txt:1772`) but the register setup (rcx = this, rdx = `_lastValidValue`,
`:1763–1765`) and the Float analogue make the target unambiguous **[Inferred: tail call to
`RepairBaseValue(_lastValidValue)`]**. A null `NetworkManager` instance at `:1724` dispatches to
helper `0x1805E5490` (`:1817`), which is the IL2CPP null-reference raise path **[Inferred]**.

---

## Integrity violation flow

`RaiseIntegrityViolation(string message)` is a plain null-safe multicast invoke of the static
`Action<string>` (`ObfuscatedNetworkInt.txt:302–333`) **[Verified]**. Three subscribers were found
(whole IsilDump grepped for `add_OnIntegrityViolation` / `RaiseIntegrityViolation`) **[Verified]**:

1. **`ClientDiagnostics.Boot()` → `ClientDiagnostics.OnIntegrityViolation`**
   (subscribe: `ClientDiagnostics.txt:1138`; handler: `ClientDiagnostics.txt:1177–1217`). The
   handler ignores the message and sets bit 0 of a static `int` flags field
   (`statics[0] |= 1`, `ClientDiagnostics.txt:1212–1215`) — the diagnostics "tamper observed" bit.
   What consumes that bit (presumably `Emit`/session telemetry, cf. `"boot"`/`"shutdown"` phases at
   `ClientDiagnostics.txt:1169, 1307`) is not confirmed **[Unknown]**.
2. **`GameAnalyticsManager.RegisterCheatListener()` → `GameAnalyticsManager.OnCheatDetected`**
   (subscribe: `GameAnalyticsManager.txt:17217`; handler: `GameAnalyticsManager.txt:17230–17283`,
   one-shot guarded by a static bool at `:17200, 17226`). The handler increments a static counter
   (`GameAnalyticsManager.txt:17274–17277`) and forwards the message to
   `CheatAttempted(violationMessage)` (`GameAnalyticsManager.txt:17282`), which builds a telemetry
   payload (reads `GetCharacterPlaytime`, `GameAnalyticsManager.txt:17754`) and sends it through
   `SendRealTimeEvent` (`GameAnalyticsManager.txt:17787`). Sibling machinery:
   `FlagSessionAsCheating` coroutine (`GameAnalyticsManager.txt:17817`), and read-back endpoints
   `QueryCheatEvents`/`FetchCheatEvents`/`QueryFlaggedSessions`/`FetchFlaggedSessions`
   (`GameAnalyticsManager.txt:17861, 18024, 18096, 18259`). Backend is Supabase REST
   (`"https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/game_sessions?session_id=eq."`,
   `GameAnalyticsManager.txt:5000`). Effect: violations flag the online session as cheating
   **[Verified for the call chain; the exact event-type string sent to Supabase is Unknown]**.
3. **`QACheat.OnEnable()` → `QACheat.OnViolation`** (subscribe: `QACheat.txt:299`; handler:
   `QACheat.txt:381–436`): `Debug.LogWarning("[AntiCheat Violation] " + message)`
   (`QACheat.txt:421–435`). `QACheat` is an in-game tamper test harness with
   `TestTamperBackingField`/`TestTamperBaseValueSetter`/`TestTamperCache`/`TestTamperHash`/
   `TestTamperLevel`/`TestTamperGold`/`TestTamperAttributes`/`RunAllTests`
   (`QACheat.txt:573, 908, 1186, 1468, 1750, 2080, 2415, 2717`) — a direct map of the attack
   surfaces the type family is expected to survive **[Verified]**.

The event is also raised directly (outside the value classes) by the Player anti-cheat — same
pipeline, messages cite the concrete invariant that failed **[Verified]**:

| Raiser | Message | Site |
|---|---|---|
| `Player.ValidateAttributeXPConsistency` | `"Attribute-XP violation: player={0}, " + "AllTimeXP={0}, minCost={1}"` | `Player.txt:6270, 6290, 6303` |
| `Player.ValidateXPState` | `"SkillPoints over grant cap: player={0}"` | `Player.txt:14116, 14125` |
| `Player.ValidateXPState` | `"HMAC violation: player={0}"` | `Player.txt:15360, 15371` |
| `Player.ReportLocalCheatServerRpc` | `"Local invariant confirmed: player={0}, reason={1}"` | `Player.txt:16753, 16765` |
| `Player.ValidateRuneState` | `"Rune HMAC violation: player={0}, slot={1}"` (+ `"  stored={0:X8} computed={1:X8}"`) | `Player.txt:18504, 18547, 18559` |
| `Player.SpeedHackDetectionRoutine` (coroutine) | `"Speedhack: player={0}, ratio={1:F2}"` | `Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:481, 493` |

Downstream consequences in `Player`: `KickForCheat()` (`Player.txt:15628`),
`DisconnectAfterDelay` (`Player.txt:15948`), `BlockSaveDueToCheatClientRpc` (`Player.txt:16014`),
and save refusal strings `"[AntiCheat] Save rejected — rune integrity failed"` /
`"[AntiCheat] Save skipped — session flagged as tampered."` (`Player.txt:23104, 23156`) and
`"[AntiCheat] Local saves disabled — server flagged this session as tampered."`
(`Player.txt:16202`) **[Verified]**. `Player` additionally keeps its own HMAC over XP state
(`ComputeXPStateHMAC` `Player.txt:8799`, `HmacMix` `Player.txt:9241`) and can read the raw slot via
`ReadBaseValue(ObfuscatedNetworkInt v)` (`Player.txt:9263`) **[Verified as signatures]**.

---

## Protected values inventory

Field declarations (from `cpp2il_cs_out/DiffableCs/Assembly-CSharp/`) **[Verified]**:

**`Player.cs:253–264, 277`** — `ObfuscatedNetworkInt`:
`Gold` (0x3E0), `CoarseEssence` (0x3E8), `ShapedEssence` (0x3F0), `RefinedEssence` (0x3F8),
`PureEssence` (0x400), `XP` (0x408), `AccumulatedXP` (0x410), `AllTimeXP` (0x418),
`SkillPoints` (0x420), `HighestLevel` (0x428), `FreeRefunds` (0x430),
`LifetimePaidRefundedPoints` (0x438); `ObfuscatedNetworkFloat`: `AttackSpeed` (0x4A0).

**`ObjectsCommon.cs:123–137`** — `ObfuscatedNetworkInt`: `Level` (0xB8); `ObfuscatedNetworkFloat`:
`Health` (0xC0, `<Health>k__BackingField`), `MaxHealth` (0xC8), `Concentration` (0xD0),
`MaxConcentration` (0xD8), `Stamina` (0xE0), `MaxStamina` (0xE8), `Shield` (0xF0).
`ICombatInterface.cs:18, 30` exposes `Health`/`MaxHealth` as `ObfuscatedNetworkFloat` properties.

Representative consumers (per-file call sites to `ObfuscatedNetworkInt/Float.get_Value`/`set_Value`
in the IsilDump):

| Game value / subsystem | Evidence |
|---|---|
| Gold (banking) | `Bank.txt:876, 881` (`CloseBankAccountServerRpc`), `Bank.txt:1252, 1301, 1306` (`DepositMoneyServerRpc`), `Bank.txt:1694, 1698` (`WithdrawMoneyServerRpc`) |
| Gold (HUD) | `BackpackStatusPresenter.txt:914` |
| Gold/XP (telemetry) | `GameAnalyticsManager.txt:2458` (`ObfuscatedNetworkInt.op_Implicit`) |
| Health / stamina / concentration / shield (combat) | `Attack.txt:1603, 1667, 1950, 5537`; `Damages.txt` (25+ sites, e.g. `:3739, 4019, 6518–6588` incl. `set_Value`); `BossClient.txt:234–290`; `BossInvulnerability.txt:1238–1244, 3571–3579`; `ArenaManager.txt:3861–3867` |
| Regen-over-time (spell coroutines) | `BaseSpell_NestedType__HealOverTimeCoroutine_d__58.txt:170, 176`; `..._HealTargetOverTimeCoroutine_d__59.txt:226, 232`; `..._ConcentrationOverTimeCoroutine_d__62.txt:170, 176`; `..._RestoreConcentrationOverTimeCoroutine_d__63.txt:226, 232`; `..._StaminaOverTimeCoroutine_d__66.txt:170, 176`; `..._RestoreStaminaOverTimeCoroutine_d__67.txt:226, 232` (get+set pairs = read-modify-write of the protected pools) |
| Spell/combat routine costs & modifiers | `BaseSpellLibrary.txt:24230, 25310`; `CombatRoutineLibrary.txt:4887, 4895`; `Consumption.txt:5284–5339, 6714–6744`; `AdrenalinePrefab.txt:188`; `BladeDancePrefab.txt:1935–2028`; `BottleOfFirePrefab.txt:327–2735`; `CorruptedForestKingEncounterRoutine.txt:784–1021` |
| XP / skill points / level (anti-cheat + init) | `Player.txt:4905–5060` (`ValidateLoadedXPData`), `:6184–6277` (`ValidateAttributeXPConsistency`), `:9034–9064` (`ComputeXPStateHMAC`), `:21020–21641` (`Initialize` — bulk `set_Value` restores incl. `AttackSpeed` at `:21575–21641`) |
| Vitals on respawn | `Player.txt:22204–22234, 22406–22440` (`SetNonZeroVitalValues` — get+set pairs) |

So: **yes, XP is obfuscated** (`XP`, `AccumulatedXP`, `AllTimeXP` — `Player.cs:258–260`), alongside
gold, essences, skill points, level, and all core combat vitals.

---

## Evidence appendix

Signature/stub evidence (`modding/DecompilerTool/ilrecovery_out/`):
- `ObfuscatedNetworkInt.cs:6` — `class ObfuscatedNetworkInt : NetworkVariable<int>`; fields `:8–33`;
  `KEY_ROTATION_INTERVAL = 64` `:22`; `Value` get/set `:35–45`; event `:47–59`;
  `RaiseIntegrityViolation` `:61`; ctor `:66`; `InitializeObfuscation` `:71`; `RepairBaseValue` `:76`;
  `HandleNetworkValueChanged` `:81`; `ComputeIntegrity` `:86`; `GenerateKey` `:91`; `op_Implicit` `:96`.
- `ObfuscatedNetworkFloat.cs:5` — same shape; `_obfuscatedBits` `:9`; `ComputeIntegrity(int bits)` `:63`;
  no event and no raise method (whole file checked).
- `ObfuscationConfig.cs:3–31` — `BuildKey`/`CheckKey`/`SessionKey` over `int?` caches.
- `ObfuscatedInt.cs:4–124` — non-network sibling (`_encoded`, `_key`, operator set).

Body evidence (`modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/`):
- Event add/remove/raise: `ObfuscatedNetworkInt.txt:70–135, 204–269, 302–333`.
- Ctor (base ctor, `InitializeObfuscation`, `OnValueChanged` hook): `ObfuscatedNetworkInt.txt:413–496`;
  Float: `ObfuscatedNetworkFloat.txt:81–158`.
- `InitializeObfuscation`: `ObfuscatedNetworkInt.txt:556–612`; `ObfuscatedNetworkFloat.txt:219–276`.
- `get_Value`: `ObfuscatedNetworkInt.txt:915–1221` (hash check `:981–1005`, violation branch
  `:1006–1087`, tamper branch `:1096–1144`, rotation `:1154–1200`, `_isRepairing` early-out
  `:1210–1218`); Float `ObfuscatedNetworkFloat.txt:573–867` (messages `:718–723, 773–778`).
- `set_Value`: `ObfuscatedNetworkInt.txt:1315–1405`; Float `ObfuscatedNetworkFloat.txt:962–1053`.
- `RepairBaseValue`: `ObfuscatedNetworkInt.txt:1453–1497` (raw disasm confirming the flag order
  `:1419–1439`); Float `ObfuscatedNetworkFloat.txt:1101–1143`.
- `HandleNetworkValueChanged`: `ObfuscatedNetworkInt.txt:1658–1817` (`_settingValue` early-out
  `:1678–1679, 1811–1816`; server branch `:1726–1758`; accept branch `:1773–1810`); Float
  `ObfuscatedNetworkFloat.txt:1300–1455` (explicit `RepairBaseValue` call `:1400–1403`).
- `ComputeIntegrity`: `ObfuscatedNetworkInt.txt:1850–1879` (raw disasm `:1821–1848` with the three
  `imul` constants `:1831, 1835, 1842`); Float `ObfuscatedNetworkFloat.txt:1488–1517`.
- `GenerateKey`: `ObfuscatedNetworkInt.txt:1966–2055` (fallback `0x5A5A5A5A` `:2024`; `Monitor`
  `:2001, 2033`); Float `ObfuscatedNetworkFloat.txt:1604–1693`.
- `op_Implicit`: `ObfuscatedNetworkInt.txt:2068–2076`; `ObfuscatedNetworkFloat.txt:1706–1714`.
- `.cctor`: `ObfuscatedNetworkInt.txt:2112–2127` (`new Random(Environment.TickCount)`);
  `ObfuscatedNetworkFloat.txt:1751–1759` + raw `xor ebx,1234h` at `:1734`.
- Key derivation: `ObfuscationConfig.txt:76–103` (`BuildKey` from `GameConfig` statics),
  `:166–176` (`CheckKey = BuildKey ^ 0x5A3B7C9D`), `:270–288` (`SessionKey = BuildKey ^ UtcNow.Ticks`).
- Subscribers: `ClientDiagnostics.txt:1138, 1177–1217`; `GameAnalyticsManager.txt:17217, 17230–17283,
  17754, 17787, 17817, 17861, 18024, 18096, 18259, 5000`; `QACheat.txt:299, 381–436`.
- Direct raisers: `Player.txt:6303, 14125, 15371, 16765, 18559`;
  `Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:481, 493`.
- Protected fields: `DiffableCs/Assembly-CSharp/Player.cs:253–264, 277, 582`;
  `ObjectsCommon.cs:123–137, 317–353`; `ICombatInterface.cs:18, 30`.
- Consumers: `Bank.txt:876, 881, 1252, 1301, 1306, 1694, 1698`; `Attack.txt:1603, 1667, 1950, 5537`;
  `BaseSpell_NestedType__*` coroutines (`:170/176` and `:226/232` pairs); `ArenaManager.txt:3861–3867`;
  `Damages.txt:3739+`; `BackpackStatusPresenter.txt:914`; `GameAnalyticsManager.txt:2458`.

---

## Open questions

1. **`decoded={0}` reports the trusted value.** In the integrity-violation message the `{0}`
   argument is boxed from the register reloaded with `_lastValidValue`
   (`ObfuscatedNetworkInt.txt:1008–1009, 1055–1065`; `ObfuscatedNetworkFloat.txt:666–668, 704–714`),
   so both placeholders print the same value. Whether the source intentionally formats
   `(trusted, _lastValidValue)` or a local was reassigned before the `String.Format` cannot be
   resolved from the dump.
2. **Tail call in `ObfuscatedNetworkInt.HandleNetworkValueChanged`.** The call after the
   "setter bypass" message decompiles as `Invalid "Jump target not found in method."`
   (`ObfuscatedNetworkInt.txt:1772`). It is read as `RepairBaseValue(_lastValidValue)` from the
   register state and the Float analogue; not directly verified.
3. **`ObfuscationConfig.BuildKey` source.** The exact `GameConfig` static member (statics offset 16)
   and the virtual method (vtable slot ~352) it calls (`ObfuscationConfig.txt:76–103`), and the
   helpers `0x18247E770` / `0x18247E9F0` (probably `Nullable<int>` construction/extraction) are not
   identified. Also: since `SessionKey` mixes `DateTime.UtcNow.Ticks`, whether ticks are truncated
   to 32 bits before the XOR (`ObfuscationConfig.txt:274–280` operates on the full 64-bit ticks in
   `rbx`) is not fully clear.
4. **`ClientDiagnostics` flag bit.** The static `int` at statics slot 0 that gets `|= 1`
   (`ClientDiagnostics.txt:1212–1215`) has no recovered name, and its consumer is not traced.
5. **`GameAnalyticsManager.CheatAttempted` payload.** The event-type string handed to
   `SendRealTimeEvent` (`GameAnalyticsManager.txt:17787`) is not a recovered string literal; the
   exact Supabase `cheat_events` row shape is unknown from this dump.
6. **Base value slot name.** Offset 96 is used as `base.Value` throughout, but the underlying
   `NetworkVariable<T>` field name (e.g. `m_InternalValue`) is not recoverable from these dumps.
7. **`previousValue` parameter** of `HandleNetworkValueChanged` is never read in either variant —
   presumably present only to match the `OnValueChangedDelegate` signature; no other use found.
8. **Floating-point NaN edge case.** Float checks compare bit patterns (`Compare rax, rdi` after
   bit-casting, `ObfuscatedNetworkFloat.txt:753–756`); whether any code path can legitimately
   produce multiple NaN bit patterns and thus false positives was not analyzed.
