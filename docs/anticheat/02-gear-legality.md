# 02 — Gear legality: the unobtainable-gear sweep (`GearLegality`)

> Scope: the **post-hoc gear/rune legality sweep** — the static class `GearLegality`, which
> detects runes whose stats are unobtainable through legitimate play and destroys them,
> leaving a one-line diagnostic log and a telemetry tick. This is the "catch what got past
> the RPC perimeter" layer referenced by [10-network-authority-surface](10-network-authority-surface.md);
> the obfuscated-value tripwires it complements are in
> [11-obfuscated-network-values](11-obfuscated-network-values.md).
>
> Every claim is tagged **Verified** (directly observed in the dumps), **Inferred**
> (concluded from names/offsets/behavior, not directly proven) or **Unknown** (not
> determinable from the available artifacts). Nothing here is invented.

**Source legend** — all line numbers are dump line numbers as shown in the files themselves.

| Short | Full path |
|---|---|
| `GearLegality.txt` | `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/GearLegality.txt` (raw `Disassembly:` + `ISIL:` pseudo-op sections; the raw operand sizes are authoritative where the two disagree) |
| `GearLegality.cs` | `modding/DecompilerTool/ilrecovery_out/GearLegality.cs` (API stubs) |
| `Inventory.txt` / `Storage.txt` / `Runes.txt` / `ClientDiagnostics.txt` | `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/<name>.txt` |
| `Rune.cs` / `Runes.cs` / `RuneManager.cs` / `GameConfig.cs` / `StorageData.cs` / `InventoryEntry.cs` | `modding/DecompilerTool/ilrecovery_out/<name>.cs` (declaration stubs) |

---

## 1. Purpose & threat model

**Purpose.** `GearLegality` is a client-side integrity sweep over `Rune` values. For each
rune it checks whether the rune's stats could ever have been obtained by legitimate play
(rarity/level/star caps, secondary-stat count, per-stat upgrade counts, and rolled stat
values against computed ceilings). Runes that fail are **destroyed** — replaced in place by
`Rune.Empty` (or removed from the source list) — logged via `Debug.Log`, and counted in
`ClientDiagnostics` telemetry.
*(Verified — full method bodies in `GearLegality.txt`; API surface `GearLegality.cs:4–67`.)*

**Threat model.** The game is peer-hosted multiplayer with no dedicated authoritative
server (see [doc 10](10-network-authority-surface.md) §1), so an attacker can:

1. **Memory-edit** a replicated rune (stats, level, stars, rarity, upgrade counts) on their
   own client, or edit the local save/inventory data before it loads.
2. **Forge item data** on the RPC path (item/rune grants are mostly `RequireOwnership =
   false` — doc 10 §3) so impossible runes enter the legitimate inventory flow.
3. Do both **before** the relevant sweep runs, i.e. the perimeter does not reject these at
   the door; rejection happens afterwards.

`GearLegality` is the **post-hoc, destructive** answer to (1) and (2): any rune whose
numbers exceed what the rune system can produce is destroyed when it is next loaded,
stored, or equipped. Because destruction happens wherever these sweeps run (inventory
load, storage set, equipment update), the same actor who created the contraband loses it,
and a log line + telemetry counter records the attempt.
*(Verified — sweep call sites, §7. The framing of intent is **Inferred** from the class
name, the log string "Destroyed unobtainable equipment", and the config flag name
`GameConfig.DestroyUnobtainableGear` (Inferred, see §5.7).)*

**Explicit non-coverage** *(Verified by absence)*:

- Only **runes** are inspected. Non-rune `InventoryEntry` items, pets, gold and other
  values are out of scope (no references to them in `GearLegality.txt`).
- No network kick or ban is issued here — destruction + log + `ClientDiagnostics` counter
  only (`GearLegality.txt:1606–1620`).
- The class is **stateless** (all methods static, no fields) and the checks are pure
  functions of the `Rune` plus `RuneManager`'s stat tables.
- `IsContraband`/`Inspect` are pure queries; only the `Destroy*`/`Purge*`/`Collect*`
  methods mutate state.

---

## 2. API surface

*(All rows **Verified** — signatures `GearLegality.cs:4–67`; IL2CPP method headers
`GearLegality.txt:3, 421, 629, 671, 751, 1663, 2283, 2611, 3380, 3679`.)*

| # | Method | Signature | Visibility | Returns |
|---|---|---|---|---|
| 1 | `Inspect` | `static Violation Inspect(Rune rune)` | public | first `Violation` found, `None` if clean |
| 2 | `TryStatValueCeiling` | `static bool TryStatValueCeiling(Runes.Stat stat, bool isPrimary, out float ceiling)` | private | `false` when no ceiling can be computed (out = `0f`) |
| 3 | `ExceedsValueCeiling` | `static bool ExceedsValueCeiling(Runes.Stat stat, bool isPrimary, float storedValue)` | private | `true` iff `storedValue > ceiling` |
| 4 | `IsContraband` | `static bool IsContraband(Rune rune)` | public | `Inspect(rune) != Violation.None` |
| 5 | `LogDestroyed` | `static void LogDestroyed(Rune rune, Violation violation, string where, string slotLabel)` | public | void (logs one line, ticks telemetry) |
| 6 | `DescribeValueBreach` | `static string DescribeValueBreach(Rune rune, bool primary)` | private | human-readable message fragment |
| 7 | `DestroyIfUnobtainable` | `static bool DestroyIfUnobtainable(ref Rune rune, string where, string slotLabel)` | public | `true` if destroyed (and `rune` replaced with `Rune.Empty`) |
| 8 | `PurgeContainer` | `static int PurgeContainer(StorageData data, string where)` | public | number of runes destroyed |
| 9 | `PurgeEquipped` | `static int PurgeEquipped(NetworkList<Rune> worn, string where)` | public | number of runes destroyed |
| 10 | `CollectContraband` | `static List<Rune> CollectContraband(List<Rune> runes, string where)` | public | new list of the contraband runes, or `null` if none |

Notes:

- `Rune` is a large struct (0xD8 = 216 bytes) passed **by value** into every query —
  `IsContraband`/`LogDestroyed`/`DescribeValueBreach`/`DestroyIfUnobtainable` all copy it
  wholesale to the stack before use.
  *(Verified — e.g. 14×16-byte `movups` + tail qword in `IsContraband`,
  `GearLegality.txt:674–705`.)*
- `CollectContraband` does **not** modify its input list; it returns the offending runes
  and lets the caller remove them (§7).
  *(Verified — no list setter calls on the input; add-only output list,
  `GearLegality.txt:3986–4018`.)*

---

## 3. The `Violation` enum

Declared at `GearLegality.cs:6–16`, values fixed by declaration order and corroborated by
the return constants in `Inspect` and the switch bounds in `LogDestroyed`. All rows
**Verified**.

| Value | Name | Meaning / trigger | Threshold (exact) | Evidence |
|---|---|---|---|---|
| 0 | `None` | Empty rune, or every check passed | — | return `rax=0` `GearLegality.txt:397` |
| 1 | `Rarity` | Rarity above obtainable cap | `dword [rune+0x08] > 4` (signed `jg`) — cap = `Rarity.Heroic` (`Runes.cs:67`; `Ancient`=5 exists but is unobtainable per this check, `Runes.cs:68`) | check `GearLegality.txt:43–44`; return `Move rax, 1` `:418` |
| 2 | `Level` | Level above obtainable cap | `dword [rune+0x50] > 12` (0Ch, signed `jg`) | check `:45–46`; return `:416` |
| 3 | `Stars` | Star rating above obtainable cap | `(qword [rune+0x08] >> 32) > 2` (signed `jg`) — cap = `Stars.Three` (`Runes.cs:103`) | check `:47–50`; return `:414` |
| 4 | `StatUpgrades` | Any secondary-stat upgrade count above obtainable | `dword [rune+0x0AC + 4*i] > 4` (signed `jg`) for any `i < word [rune+0xA8]` | length `:67`; element `:116–120`; return `mov eax,4` `:123` |
| 5 | `SecondaryStatCount` | Too many secondary stats | `word [rune+0x68] > 6` (**unsigned** `ja`) | check `:56–57`; return `:412` |
| 6 | `PrimaryStatValue` | Primary stat value exceeds its computed ceiling | `ExceedsValueCeiling(PrimaryStat, isPrimary: true, StatValues[0])` | `:130–145`; return `mov eax,r15d` (`r15d=6`, `:61`, `:144`) |
| 7 | `SecondaryStatValue` | A secondary stat value exceeds its computed ceiling | `ExceedsValueCeiling(SecondaryStats[i], isPrimary: false, StatValues[i+1])` for `i < min(secCount, StatValues.len-1)` | bound `:152–163`; loop `:388–396`; return `:410` |

Corroboration: `LogDestroyed`'s switch dispatches exactly values 1–7
(`lea eax,[rbx-1]; cmp eax,6; ja` → default) at `GearLegality.txt:810–819`, with
`"unknown"` as the out-of-range message (`:1427`). *(Verified.)*

**Check order in `Inspect` matters**: checks run in the order Rarity → Level → Stars →
SecondaryStatCount → StatUpgrades → PrimaryStatValue → SecondaryStatValue, and the **first**
failure is the reported violation. A rune that is both over-leveled and over-stared logs as
`Level`.
*(Verified — control flow `GearLegality.txt:43–197` and the return-value blocks `:397–419`.)*

---

## 4. Rune field map as read by `GearLegality`

The class reads `Rune` at fixed offsets. Offsets/sizes below are **Verified** from the raw
disassembly; field *names* are **Inferred** from `Rune.cs` declaration order and the boxed
IL2CPP types used in logging, except where the boxed type pins the meaning. Struct size
0xD8 (216 bytes) is **Verified** from the full-struct copies (`GearLegality.txt:674–705`,
`:2546–2587`).

| Offset | Size | Field (name confidence) | How it is read | Evidence |
|---|---|---|---|---|
| +0x00 | 4 | `Set` — `Runes.RuneSet` (name **Inferred**) | boxed as `Runes+RuneSet` in log | `GearLegality.txt:1467–1472` |
| +0x04 | 4 | `SlotType` — `Runes.SlotType` (**Inferred**) | boxed as `Runes+SlotType` in log | `:1490–1495` |
| +0x08 | 4 | `Rarity` — `Runes.Rarity` (**Inferred**) | `cmp dword ptr [rbx+8],4`; boxed `Runes+Rarity` | `:43`, `:1277–1284` |
| +0x0C | 4 | `Stars` — `Runes.Stars` (**Inferred**) | `mov rax,[rbx+8]; shr rax,20h`; boxed `Runes+Stars` | `:47–49`, `:1311–1314` |
| +0x10 | 64 | `UUID` — `FixedString64Bytes` (**Verified** type) | 64-byte copy; boxed `Unity.Collections.FixedString64Bytes`; `FixedString64Bytes.Equals` | `:1582–1591`, `:3224` |
| +0x50 | 4 | `Level` — `int` (**Inferred** name) | `cmp dword ptr [rbx+50h],0Ch`; boxed `System.Int32` (`0x185D2C9C8`) and printed as `Lv{4}` | `:45`, `:1292–1301`, `:1552–1556`, `:1572` |
| +0x58 | 8 | (`OriginalOwner`?) | never read by `GearLegality` | mapping **Unknown** |
| +0x60 | 4 | `PrimaryStat` — `Runes.Stat` (**Inferred** name) | passed as `stat` to `ExceedsValueCeiling`/`TryStatValueCeiling`; boxed `Runes+Stat` | `:134–141`, `:2217–2225` |
| +0x64 | 4 | gap | not read | **Unknown** |
| +0x68 | var | `SecondaryStats` — `FixedList32Bytes<Runes.Stat>` (**Inferred**) | length `word ptr [rbx+68h]`; element getter `0x180003560` over `&rune+0x68` | `:56`, `:152`, `:2038–2041` |
| +0x88 | var | `StatValues` — `FixedList32Bytes<float>` (**Inferred**) | length `word ptr [rbx+88h]`; element getter `0x180003660` over `&rune+0x88`; values boxed as `System.Single` (`0x185D2C9F8`) | `:130–136`, `:157`, `:2213–2216` |
| +0xA8 | var | `StatUpgrades` — `FixedList32Bytes<int>` (**Inferred**) | length `word ptr [rbx+0A8h]`; elements `dword ptr [rune+0xAC + 4*i]` | `:67`, `:116–119` |
| +0xC8..+0xD7 | 16 | tail (per `Rune.cs`: per-slot `…Count` ints + `FormulaVersion`?) | copied blindly only | `:700–705`; mapping **Unknown** |

**List semantics** *(Verified — index arithmetic)*:

- `StatValues[0]` is the **primary** stat's value (`Stat` taken from +0x60), and
  `StatValues[i+1]` is the value of `SecondaryStats[i]`
  (`GearLegality.txt:2042–2046` — `stat = SecondaryStats[i]`, `value = StatValues[i+1]`).
- `StatUpgrades[i]` counts upgrades of the i-th secondary stat (checked against `4`).

---

## 5. Behavior per method

### 5.1 `Inspect(Rune rune)` → `Violation` *(public)*

Header `GearLegality.txt:3`; body `:24–207` (raw) / `:208–419` (ISIL). All **Verified**.

| Step | Check (exact) | On failure returns | Evidence |
|---|---|---|---|
| 1 | `Rune.IsEmpty(rune)` true | `None` (clean *empty* rune) | `:245–247`, return `:397` |
| 2 | `dword [+0x08] > 4` (signed) | `Rarity` | `:43–44`, `:418` |
| 3 | `dword [+0x50] > 12` (signed) | `Level` | `:45–46`, `:416` |
| 4 | `(qword [+0x08] >> 32) > 2` (signed) | `Stars` | `:47–50`, `:414` |
| 5 | `word [+0x68] > 6` (unsigned) | `SecondaryStatCount` | `:56–57`, `:412` |
| 6 | any `dword [rune+0xAC+4i] > 4`, `i < word [+0xA8]` | `StatUpgrades` | `:67–68`, `:116–123` |
| 7 | `word [+0x88] > 0` and `ExceedsValueCeiling(Stat[+0x60], true, StatValues[0])` | `PrimaryStatValue` (6) | `:130–145` (`mov dl,1` `:140`) |
| 8 | `ExceedsValueCeiling(SecondaryStats[i], false, StatValues[i+1])`, `i < min(word [+0x68], word [+0x88] − 1)` | `SecondaryStatValue` (7) | `:152–163` (bound), `:388–396` (`rdx=0` `:389`) |
| 9 | everything passed | `None` | `:397` |

Exact caps: **rarity 4** (`Heroic`), **level 12**, **stars 2** (`Three`), **6** secondary
stats, **4** upgrades per secondary stat. Value caps are computed per stat (§6), not
hard-coded.

### 5.2 `TryStatValueCeiling(Runes.Stat, bool isPrimary, out float)` *(private)*

See §6 for the full derivation. Summary *(all **Verified**, `GearLegality.txt:421–628`)*:
requires a live `RuneManager` with both stat dictionaries containing `stat`; computes
`ceiling = 2 * CalculateFinalStatValue(CalculateStatBaseValue(stat, Stars.Three), isPrimary ? 12 : 5) + K`
(`K` = float constant at `0x18465DE18`, value **Unknown**); returns `false` (out `0f`) on
any missing prerequisite or non-finite/non-positive result.

### 5.3 `ExceedsValueCeiling(Runes.Stat, bool isPrimary, float storedValue)` *(private)*

See §6. Summary *(**Verified**, `GearLegality.txt:629–669`)*: delegates to
`TryStatValueCeiling`, then returns `storedValue > ceiling` **strictly** (`comiss` +
`seta`); returns `false` when no ceiling can be computed.

### 5.4 `IsContraband(Rune rune)` → `bool` *(public)*

Header `GearLegality.txt:671`; body `:674–749`. *(All **Verified**.)*

- Copies the entire 0xD8-byte rune onto the stack (14×16B `movups` + tail qword,
  `:674–705`) and returns `Inspect(copy) != 0` (`:706–708`; `setne` `:747`).
- **No `GameConfig` gate** here — `IsContraband` always answers, unlike the destructive
  paths (§5.7–5.10). *(Verified — no `GameConfig` reference in `:671–749`.)*

### 5.5 `LogDestroyed(Rune, Violation, string where, string slotLabel)` *(public)*

Header `GearLegality.txt:751`; body `:754–1660`. *(All **Verified**.)*

1. Switch on `violation` over cases 1–7 (`lea eax,[rbx-1]; cmp eax,6; ja` default,
   `:810–819`) and builds the violation-specific `message` string (§8):
   - 1 `Rarity` — boxes actual `[rune+0x08]` vs constant `4` (`:1275–1289`)
   - 2 `Level` — actual `[rune+0x50]` vs `12` (`:1292–1306`)
   - 3 `Stars` — actual `qword [+0x08]>>32` vs `2` (`:1309–1324`)
   - 4 `StatUpgrades` — constant `4` (`:1327–1334`)
   - 5 `SecondaryStatCount` — actual `word [+0x68]` vs `6` (`:1337–1356`)
   - 6 — `DescribeValueBreach(runeCopy, primary: 1)` (`:1359–1391`)
   - 7 — `DescribeValueBreach(runeCopy, primary: 0)` (`:1393–1425`)
   - default — literal `"unknown"` (`:1427`)
2. Builds a `string[7]` (`:1428–1431`) and fills it with the fixed prefix parts, the rune
   identity block and the UUID+message block (§8 has the exact strings), then
   `String.Concat` (`:1606`).
3. `Debug.Log(<one line>)` (`:1614`) — always `Debug.Log`, never `LogWarning`/`LogError`.
4. `ClientDiagnostics.NoteContrabandDestroyed()` (`:1620`; parameterless,
   `ClientDiagnostics.txt:353`).

Note: `LogDestroyed` is public and takes an arbitrary `Violation`, so its message builder
defends against states `Inspect` would never report (see §10 Q7).

### 5.6 `DescribeValueBreach(Rune rune, bool primary)` → `string` *(private)*

Header `GearLegality.txt:1663`; body `:1666–2280`. *(All **Verified**.)*

**`primary: true`** (`:2205–2258`):

- Value = `StatValues[0]` when `word [+0x88] > 0` (`:2210–2216`).
- `TryStatValueCeiling(Stat[+0x60], isPrimary: 1, out ceiling)` (`:2217–2221`):
  - success → `"primary {0} is worth {1} against a ceiling of {2:0.##}"` with
    `(stat, value, ceiling)` (`:2240–2245`);
  - failure → `"primary {0} is worth {1}, above what it could ever roll"` with
    `(stat, value)` (`:2253–2257`).
- Note: the primary branch does **not** re-test `value > ceiling`; it just reports both.

**`primary: false`** (`:2030–2204`):

- Loop `i` over `0 … min(?, ?) − 1` (`:2030–2036`, bound computed exactly as in `Inspect`:
  `min(word [+0x68], word [+0x88] − 1)`), with `stat = SecondaryStats[i]` (`:2038–2041`)
  and `value = StatValues[i+1]` (`:2042–2046`).
- First `i` where `ExceedsValueCeiling(stat, 0, value)` is true (`:2047–2053`) is the
  reported one; then `TryStatValueCeiling(stat, 0, out ceiling)` is re-run (`:2072–2087`):
  - success → `"secondary {0} ({1}) is worth {2} against a ceiling of {3:0.##}"` with
    `(i, stat, value, ceiling)` (`:2088–2178`);
  - failure → `"secondary {0} ({1}) is worth {2}, above what it could ever roll"` with
    `(i, stat, value)` (`:2180–2203`).
- If the loop finds **no** breach → fallback `"a secondary stat is worth more than it could
  ever roll"` (`:2057`).

### 5.7 `DestroyIfUnobtainable(ref Rune rune, string where, string slotLabel)` → `bool` *(public)*

Header `GearLegality.txt:2283`; body `:2286–2609`. *(All **Verified** unless noted.)*

1. **Gate**: `GameConfig`'s static bool at statics-block offset `+0x198` must be set,
   else return `false` (`typeof(GameConfig)` statics `[+0xB8]` → `[+0x198]`,
   `GearLegality.txt:2463–2471`; the same gate repeats in 5.8–5.10). This offset is the
   field declared at `GameConfig.cs:209`, `DestroyUnobtainableGear` — mapping **Inferred**
   (offset/declaration-order match + name/behavior agreement; not directly proven).
2. `Inspect(runeCopy) == None` → return `false` (`:2502–2505`).
3. Otherwise:
   - `LogDestroyed(rune, violation, where, slotLabel)` (`:2545`);
   - overwrite `*rune` with `Rune.Empty` — a full 0xD8-byte copy from the static
     `Rune.Empty` (statics base of `typeof(Rune)`), `:2546–2587`;
   - return `true` (`:2567`).

### 5.8 `PurgeContainer(StorageData data, string where)` → `int` *(public)*

Header `GearLegality.txt:2611`; body `:2614–3378`. *(All **Verified** unless noted.)*

1. Gate: `GameConfig` flag (`:3031–3034`); require `data != null` (`:3035–3036`),
   `data.Runes` list non-null (`[data+0x68]`, `:3037–3038`) and `data.Entries` list
   non-null (`[data+0x60]`, `:3039–3040`). Field names per `StorageData.cs:16,18`
   (mapping **Inferred** from offset/order).
2. For each index `i` in `data.Runes` (getter `0x182030200`, `:3057–3068`):
   - `slotLabel = String.Format("slot {0}", i)` (`:3101–3104`);
   - `DestroyIfUnobtainable(&copy, where, slotLabel)` (`:3109`).
3. If destroyed (`:3110–3111`):
   - write the emptied copy back: `data.Runes[i] = copy` (setter `0x18000A670`, `:3147`);
   - destroyed counter incremented (`:3148`).
   - **Entry cleanup** — only when the destroyed rune's UUID is non-empty (zero-check on
     the UUID's first qword, `:3144`, `:3149–3150`):
     - **Fast path**: if `i < data.Entries.Count` (`:3157–3158`), take `Entries[i]`
       (getter `0x1820CCF70`, `:3163`), pass its guard accessor `0x1808D4090`
       (name **Unknown**, boolean predicate, `:3186–3188`) and compare
       `entry.RuneUUID.Equals(rune.UUID)` via `FixedString64Bytes.Equals` (`:3224`);
     - **Full scan** on any miss (`:3236–3311`): iterate all `Entries`, skip entries
       failing the same guard `0x1808D4090` (`:3269–3271`), compare `RuneUUID`;
     - on match, write `InventoryEntry.Empty` into that slot
       (`InventoryEntry.get_Empty` `:3230` fast path / `:3315` scan; setter
       `0x18000C080` `:3342`).
4. Returns the destroyed count (`r15`).

Purpose of the entry cleanup (**Inferred**): keeps the parallel `List<InventoryEntry>`
inventory view consistent with the rune list so a destroyed rune does not linger as an
orphan entry.

### 5.9 `PurgeEquipped(NetworkList<Rune> worn, string where)` → `int` *(public)*

Header `GearLegality.txt:3380`; body `:3383–3677`. *(All **Verified** unless noted.)*

1. Gate: `GameConfig` flag (`:3550–3558`); `worn != null` (`:3559–3560`), else return `0`.
2. For each `i` in `worn.Count` (accessor `0x182439B90`, `:3569`):
   - `NetworkList<Rune>.get_Item(i)` (`:3576`);
   - `slotLabel = String.Format("worn slot {0}", i)` (`:3609–3612`);
   - `DestroyIfUnobtainable(&copy, where, slotLabel)` (`:3617`).
3. If destroyed: `worn[i] = copy` (NetworkList setter `0x18000A730`, `:3652`) and
   counter++ (`:3653`).
4. Returns the destroyed count (`:3658`).

Unlike `PurgeContainer`, there is **no** `InventoryEntry` cleanup (worn runes have no
parallel entry list). *(Verified — no such calls in `:3383–3677`.)*

### 5.10 `CollectContraband(List<Rune> runes, string where)` → `List<Rune>` *(public)*

Header `GearLegality.txt:3679`; body `:3682–4022` (ends at file end). *(All **Verified**
unless noted.)*

1. Gate: `GameConfig` flag (`:3891–3899`); `runes != null` (`:3900–3901`) and
   `Count > 0` (`:3902–3903`), else return `null`.
2. For each index `i`: `copy = runes[i]` (getter `0x182030200`, `:3929`);
   `violation = Inspect(copy)` (`:3960`).
3. If `violation != None`:
   - `LogDestroyed(copy, violation, where, "backpack")` — slot label is the literal
     `"backpack"` (`:3964`; call `:3985`);
   - lazily allocate the result `List<Rune>` on first hit (`:3986–3993`) and append the
     copy (add `0x18000A400`, `:4016`).
4. Returns the result list, `null` when nothing was contraband (`:4018–4021`).
5. The input list is untouched — removal is the caller's job (§7).

---

## 6. Stat-value ceilings: `TryStatValueCeiling` & `ExceedsValueCeiling`

These two private methods implement the only **computed** (non-constant) legality checks:
a per-stat maximum value a rune could ever legitimately roll.

### 6.1 `TryStatValueCeiling(Runes.Stat stat, bool isPrimary, out float ceiling)`

*(All **Verified** — `GearLegality.txt:421–628`; raw disasm `:424–508`, ISIL `:519–627`.)*

**Prerequisites** (all must hold; otherwise return `false` and `ceiling = 0f`
— out initialized at `:541`/raw `:441`):

| Prerequisite | Evidence |
|---|---|
| `RuneManager` singleton exists (`typeof(RuneManager)` statics → instance; `Object.op_Equality(singleton, null)` → false) | `:542–555` |
| `BaselineValues` dict non-null (`[manager+0x40]`) | `:558–559` |
| `ScalingValues` dict non-null (`[manager+0x48]`) | `:560–561` |
| `BaselineValues.ContainsKey(stat)` | `:562–567` (dict call `0x182DAF310`) |
| `ScalingValues.ContainsKey(stat)` | `:568–575` |

Field names `BaselineValues`/`ScalingValues` are **Inferred** from `RuneManager.cs:20–22`
(offsets `+0x40`/`+0x48` are Verified).

**Computation** (in order):

| Step | Exact operation | Evidence |
|---|---|---|
| 1 | `base = RuneManager.CalculateStatBaseValue(stat, starRating: 2)` — `2` = `Stars.Three` | `:576–580` (`r8d=2` raw `:477`; signature `RuneManager.cs:134`) |
| 2 | if `base <= 0` → `false` | `:581–583` |
| 3 | `final = RuneManager.CalculateFinalStatValue(base, level: isPrimary ? 12 : 5)` | `:584–593` (`r8d=0Ch` / `5`, `cmove` raw `:484–489`; signature `RuneManager.cs:139`) |
| 4 | if `final <= 0` → `false` | `:594–595` |
| 5 | if `|final| == +∞` (sign bit cleared, compared to `0x7F800000`) → `false` | `:596–599` |
| 6 | `ceiling = final * 2 + K`, `K` = float at `0x18465DE18` | `:600–603` (raw `addss` `:498`, `:500`) |
| 7 | return `true` | `:601`/`:614` |

**Ceiling formula** *(Verified)*:

```
ceiling = 2 * CalculateFinalStatValue(
                CalculateStatBaseValue(stat, Stars.Three /* 2 */),
                isPrimary ? 12 : 5      /* 12 = max obtainable level (§3, violation 2) */
            ) + K                       /* K = float const at 0x18465DE18, value Unknown */
```

The `12` in the primary branch equals the max obtainable level (§3) — so the ceiling is
"the value of a perfect `Stars.Three` primary stat at max level, doubled". The `5` used
for secondary stats has no documented rationale (**Unknown** — §10 Q2). `K` is a
rounding-tolerance constant of unknown value (**Unknown** — §10 Q1).

### 6.2 `ExceedsValueCeiling(Runes.Stat stat, bool isPrimary, float storedValue)`

*(All **Verified** — `GearLegality.txt:629–669`.)*

1. `ceiling` out var zeroed (`:635`), then `TryStatValueCeiling(stat, isPrimary, out ceiling)`
   (`:636–638`; named call `:658`).
2. If it returns `false` → `ExceedsValueCeiling` returns `false` (`:639–640`, `:660`).
   Consequence: a stat missing from `RuneManager`'s tables can carry **any** value without
   triggering violation 6/7.
3. Else return `storedValue > ceiling` — **strict**: `comiss xmm6, dword [rsp+58h]` +
   `seta al` (`:641–642`). NaN comparisons yield `false` (`seta` semantics).

---

## 7. Enforcement call sites

The class itself never triggers; four call sites do. A grep for `GearLegality.*` across
`IsilDump/Assembly-CSharp/*` returns only these call sites plus internal calls — so these
are **all** entrances *(Verified, grep 2026-09-29)*.

| Caller (method) | Sweep invoked | `where` string | `slotLabel`s | Post-action | Evidence |
|---|---|---|---|---|---|
| `Inventory.LoadInventory()` | `CollectContraband` | `String.Format("character {0}", <name>)` | `"backpack"` (fixed) | `Inventory.RemoveRuneFromInventory(this, rune)` per returned rune | method `Inventory.txt:64447`; where `:64772`; call `:64779`; removal `:64832` |
| `Inventory.PurgeUnobtainableRunes()` | `CollectContraband` | `String.Format("character {0}", <name>)` | `"backpack"` | `Inventory.RemoveRuneFromInventory(this, rune)` per returned rune | method `Inventory.txt:165006`; where `:165204`; call `:165211`; removal `:165263` |
| `Storage.SetStorageData()` | `PurgeContainer` | `String.Format("container {0}", <StorageName>)` | `"slot {0}"` | in-place blanking inside `PurgeContainer` | method `Storage.txt:1864`; where `:2417`; call `:2424` |
| `Runes.UpdateRuneBonuses()` | `PurgeEquipped` | `String.Format("character {0}", <name>)` | `"worn slot {0}"` | in-place blanking inside `PurgeEquipped` | method `Runes.txt:495`; where `:673`; call `:680` |

Details:

- **Inventory paths** *(Verified unless noted)*: both sweep the per-player rune list and,
  after `CollectContraband` returns, call `Inventory.RemoveRuneFromInventory(this, rune)`
  for each contraband rune (the sweep only *collects*; removal happens here). The swept
  list is a player-owned rune list (`Player` +0x5C0 per operand offsets — **Inferred**).
- **Storage path**: `Storage.SetStorageData()` sweeps `data = Storage`'s `StorageData`
  field (+0x240 per operands — **Inferred**) when storage data is applied/refreshed.
- **Equipment path**: `Runes.Update()` (`Runes.txt:401`) calls `UpdateRuneBonuses` when the
  equipment-dirty flag on `Runes` is set (`Runes.txt:487`), which sweeps the worn rune
  `NetworkList<Rune>` (`Player` +0x598 per operands — **Inferred**) and then continues into
  the set/bonus recomputation (`Runes.txt:680ff`).
- **Gating**: all four go through `DestroyIfUnobtainable`/the three sweeps, which all
  require the `GameConfig.DestroyUnobtainableGear` flag (**Inferred** mapping, §5.7). With
  the flag off, the sweeps are complete no-ops.
- **`Inventory.PurgeUnobtainableRunes()` has no caller in the dump** — grep for
  `Call Inventory.PurgeUnobtainableRunes` across `IsilDump/Assembly-CSharp/*` returns
  nothing *(Verified grep, 2026-09-29)*. See §10 Q5.

---

## 8. Logging format (exact strings)

One line per destroyed rune, emitted with `Debug.Log` (`GearLegality.txt:1614`) after
`String.Concat` over a `string[7]` (`:1428–1606`):

```
[GearLegality] Destroyed unobtainable equipment on {where} ({slotLabel}): {Set} {SlotType} {Rarity} {Stars} Lv{Level} UUID={UUID} — {message}.
```

Composition *(all **Verified**)*:

| Part | Exact content | Evidence |
|---|---|---|
| [0] | `"[GearLegality] Destroyed unobtainable equipment on "` | `:1436` |
| [1] | `where` (parameter) | `:1443–1445` |
| [2] | `" ("` | `:1448` |
| [3] | `slotLabel` (parameter) | `:1455–1457` |
| [4] | `"): "` | `:1460` |
| [5] | `String.Format("{0} {1} {2} {3} Lv{4} ", Set, SlotType, Rarity, Stars, Level)` — boxed enums print their **names** (`RuneSet`, `SlotType`, `Rarity`, `Stars` via `ToString`), `Level` as `Int32` | `:1572–1575`; arg boxing `:1467–1571` |
| [6] | `String.Format("UUID={0} — {1}.", UUID, message)` — `UUID` boxed as `FixedString64Bytes`; separator is an em dash (U+2014) | `:1593–1597` |

### 8.1 `message` per violation (`LogDestroyed` switch)

All `String.Format` templates, quoted verbatim *(all **Verified**)*:

| Violation | Template | Format args | Evidence |
|---|---|---|---|
| 1 `Rarity` | `rarity {0} is above the obtainable {1}` | (actual `Rarity`, `4`) | `:1285` |
| 2 `Level` | `level {0} is above the obtainable {1}` | (actual `Level`, `12`) | `:1302` |
| 3 `Stars` | `star rating {0} is above the obtainable {1}` | (actual `Stars`, `2`) | `:1320` |
| 4 `StatUpgrades` | `a secondary stat is upgraded above the obtainable {0}` | (`4`) | `:1331` |
| 5 `SecondaryStatCount` | `{0} secondary stats is above the obtainable {1}` | (actual count, `6`) | `:1352` |
| 6 `PrimaryStatValue` | `DescribeValueBreach(rune, primary: true)` → below | — | `:1390` |
| 7 `SecondaryStatValue` | `DescribeValueBreach(rune, primary: false)` → below | — | `:1424` |
| default | `unknown` (plain literal, no formatting) | — | `:1427` |

### 8.2 `DescribeValueBreach` templates

| Branch | Template | Format args | Evidence |
|---|---|---|---|
| secondary, ceiling known | `secondary {0} ({1}) is worth {2} against a ceiling of {3:0.##}` | (index `int`, `Runes.Stat`, value `float`, ceiling `float`) | `:2175` |
| secondary, no ceiling | `secondary {0} ({1}) is worth {2}, above what it could ever roll` | (index, `Runes.Stat`, value) | `:2198` |
| secondary, no breach found | `a secondary stat is worth more than it could ever roll` | (none) | `:2057` |
| primary, ceiling known | `primary {0} is worth {1} against a ceiling of {2:0.##}` | (`Runes.Stat`, value, ceiling) | `:2240` |
| primary, no ceiling | `primary {0} is worth {1}, above what it could ever roll` | (`Runes.Stat`, value) | `:2253` |

`{3:0.##}`/`{2:0.##}` format ceilings to at most two decimals *(Verified — standard
.NET custom numeric format in the templates)*.

### 8.3 `where` / `slotLabel` string constants

| Constant | Used by | Evidence |
|---|---|---|
| `String.Format("character {0}", …)` | `Inventory.LoadInventory`, `Inventory.PurgeUnobtainableRunes`, `Runes.UpdateRuneBonuses` | `Inventory.txt:64772`, `:165204`; `Runes.txt:673` |
| `String.Format("container {0}", …)` | `Storage.SetStorageData` | `Storage.txt:2417` |
| `String.Format("slot {0}", i)` | `PurgeContainer` | `GearLegality.txt:3101` |
| `String.Format("worn slot {0}", i)` | `PurgeEquipped` | `GearLegality.txt:3609` |
| `"backpack"` (literal) | `CollectContraband` | `GearLegality.txt:3964` |

### 8.4 Telemetry

After every log line: `ClientDiagnostics.NoteContrabandDestroyed()` (parameterless,
`GearLegality.txt:1620`; declaration `ClientDiagnostics.txt:353`). *(Verified.)*

---

## 9. Evidence appendix

| Claim | Evidence (file:line) |
|---|---|
| Class shape, `Violation` values, all 10 signatures | `DecompilerTool/ilrecovery_out/GearLegality.cs:4–67` |
| Method headers (file order) | `GearLegality.txt:3` (`Inspect`), `:421` (`TryStatValueCeiling`), `:629` (`ExceedsValueCeiling`), `:671` (`IsContraband`), `:751` (`LogDestroyed`), `:1663` (`DescribeValueBreach`), `:2283` (`DestroyIfUnobtainable`), `:2611` (`PurgeContainer`), `:3380` (`PurgeEquipped`), `:3679` (`CollectContraband`) |
| `Rune.IsEmpty` short-circuit → `None` | `GearLegality.txt:245–247`, `:397` |
| Thresholds (rarity 4 / level 12 / stars 2 / secCount 6 / upgrades 4) | `GearLegality.txt:43`, `:45`, `:47–50`, `:56`, `:67`, `:116–120` |
| `Inspect` return constants (0,1,2,3,4,5,6,7) | `GearLegality.txt:397`, `:418`, `:416`, `:414`, `:123`, `:412`, `:61`+`:144`, `:410` |
| Primary/secondary value-check wiring (`isPrimary` 1/0) | `GearLegality.txt:130–145` (`mov dl,1` `:140`), `:152–163`, `:388–396` |
| Ceiling prerequisites (`RuneManager`, two dicts, `ContainsKey`) | `GearLegality.txt:542–575` |
| Ceiling math (`Stars.Three`, level 12/5, `*2 + K`) | `GearLegality.txt:576–603` (raw `:476–501`) |
| `K` constant address `0x18465DE18` | `GearLegality.txt:602` (raw `:500`) |
| `ExceedsValueCeiling` strict compare | `GearLegality.txt:635–642` (`seta` `:662`) |
| `IsContraband` full-struct copy + `Inspect` | `GearLegality.txt:674–708` |
| `LogDestroyed` switch bounds 1–7, default `"unknown"` | `GearLegality.txt:810–819`, `:1427` |
| Log line composition (7 parts, `Debug.Log`, telemetry) | `GearLegality.txt:1436`, `:1448`, `:1460`, `:1572–1575`, `:1593–1597`, `:1606`, `:1614`, `:1620` |
| All message templates | `GearLegality.txt:1285`, `:1302`, `:1320`, `:1331`, `:1352`, `:2057`, `:2175`, `:2198`, `:2240`, `:2253` |
| `DescribeValueBreach` loop (`StatValues[i+1]` ↔ `SecondaryStats[i]`) | `GearLegality.txt:2030–2056` |
| `GameConfig` gate (statics `+0xB8` → `+0x198`) | `GearLegality.txt:2463–2471`, `:3031–3034`, `:3550–3558`, `:3891–3899` |
| `DestroyIfUnobtainable`: log → `Rune.Empty` overwrite → `true` | `GearLegality.txt:2502–2505`, `:2545–2587` |
| `PurgeContainer`: gates, `slot {0}`, rune write-back, entry cleanup (fast path + scan) | `GearLegality.txt:3035–3040`, `:3068`, `:3101–3109`, `:3147–3150`, `:3151–3235`, `:3236–3311` |
| `InventoryEntry` blanking (getters/setters) | `GearLegality.txt:3224` (`FixedString64Bytes.Equals`), `:3230`/`:3315` (`InventoryEntry.get_Empty`), `:3269` (guard `0x1808D4090`), `:3342` (setter `0x18000C080`) |
| `PurgeEquipped`: `worn slot {0}`, NetworkList set, count | `GearLegality.txt:3559–3560`, `:3569`, `:3576`, `:3609–3617`, `:3652–3653` |
| `CollectContraband`: gates, `"backpack"`, lazy list, add, `null` return | `GearLegality.txt:3891–3903`, `:3929`, `:3960`, `:3964`, `:3985–3993`, `:4016–4021` |
| Call sites + `where` strings + removals | `Inventory.txt:64447`, `:64772`, `:64779`, `:64832`, `:165006`, `:165204`, `:165211`, `:165263`; `Storage.txt:1864`, `:2417`, `:2424`; `Runes.txt:401`, `:487`, `:495`, `:673`, `:680` |
| `NoteContrabandDestroyed()` | `ClientDiagnostics.txt:353` (call `GearLegality.txt:1620`) |
| `Rarity` (`Common…Heroic=4`, `Ancient=5`), `Stars` (`Three=2`) | `DecompilerTool/ilrecovery_out/Runes.cs:61–69`, `:99–110` |
| `CalculateStatBaseValue(Stat, Stars)` / `CalculateFinalStatValue(float, int)` | `DecompilerTool/ilrecovery_out/RuneManager.cs:134`, `:139` (dicts `:20–22`) |
| `GameConfig.DestroyUnobtainableGear` | `DecompilerTool/ilrecovery_out/GameConfig.cs:209` |
| `StorageData.Entries`/`Runes` | `DecompilerTool/ilrecovery_out/StorageData.cs:16`, `:18` |
| No other `GearLegality` entrances; no caller of `PurgeUnobtainableRunes` | grep over `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/*` for `GearLegality.` and `Call Inventory.PurgeUnobtainableRunes` (2026-09-29) |

---

## 10. Open questions

1. **Value of the ceiling slack `K`** — the float constant at `0x18465DE18` added after
   doubling (`GearLegality.txt:602`, raw `:500`). Its bytes live in the data section, not
   the text dump, so its value is **Unknown**. Likely a small rounding tolerance so
   legitimate float rounding does not trip the check (**Inferred**).
2. **Why secondary ceilings use `level: 5`** while primary uses `12`
   (`GearLegality.txt:584–589`). `12` matches the max obtainable level; `5` matches no
   observed cap (stat upgrades cap at `4`). Rationale **Unknown**.
3. **Identity of guard accessor `0x1808D4090`** on `InventoryEntry`
   (`GearLegality.txt:3186`, `:3269`) — a boolean predicate applied to entries before
   UUID comparison; name **Unknown** (not resolved in the ISIL names). Plausibly an
   `IsEmpty`/`IsRune`-style guard (**Inferred**).
4. **Unmapped `Rune` fields** — the 4-byte gap at `+0x64`, the `+0x58` slot (likely
   `OriginalOwner`) and the 16-byte tail at `+0xC8..+0xD7` are never read by
   `GearLegality`; their mapping to `Rune.cs` names is **Unknown** (order suggests
   per-slot `…Count` ints + `FormulaVersion` — **Inferred**).
5. **`Inventory.PurgeUnobtainableRunes()` has no caller in the dump** — invoked via
   Unity event/reflection, from code not present in `IsilDump`, or a debug/unused entry
   point. **Unknown**. Worth checking `TASKS.md` follow-ups for a wider caller sweep.
6. **`GameConfig` static `+0x198` ↔ `DestroyUnobtainableGear` (GameConfig.cs:209)** is
   **Inferred** (offset/declaration order + name/behavior agreement), not directly proven.
   Confirming it would settle whether the sweep is intended to be toggleable per build.
7. **Reachability of the "no ceiling" log branches** — `Inspect` only reports violations
   6/7 when `ExceedsValueCeiling` succeeds (which requires a computable ceiling), yet
   `DescribeValueBreach` has "above what it could ever roll" branches for exactly that
   case (`GearLegality.txt:2198`, `:2253`) and a no-breach fallback (`:2057`). These are
   reachable only if `RuneManager` state changes between `Inspect` and `LogDestroyed`, or
   when `LogDestroyed` is called directly with an arbitrary `Violation` (it is public).
   Defensive code vs. real race — **Inferred** (defensive).
8. **Client vs. host execution** — the four call sites are ordinary MonoBehaviour/NGO
   lifecycle methods (`Inventory`, `Storage`, `Runes`), so the sweep appears to run on
   whichever peer executes them. Whether destruction is authoritative (host) or local-only
   per client is **Unknown** here; see [doc 10](10-network-authority-surface.md) for the
   authority model.
9. **Accessor naming** — `0x180003560` (`FixedList32Bytes<Stat>` getter),
   `0x180003660` (`FixedList32Bytes<float>` getter), `0x182030200` (`List<Rune>`
   getter), `0x1820CCF70` (`List<InventoryEntry>` getter), `0x18000A670`/`0x18000A730`
   (list/NetworkList setters), `0x18000A400` (`List<Rune>.Add`), `0x18000C080`
   (`InventoryEntry` setter) are **Inferred** from context and calling convention; none
   appear under resolved names in the ISIL sections.
