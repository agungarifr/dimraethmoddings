# 04 — CharacterIdentityIntegrity (identity verdicts for save characters)

**Scope.** Static analysis of the decompiled game code only. All behavioral claims cite
`file:line` in the ISIL dumps under
`modding\cpp2il_isil_out\IsilDump\Assembly-CSharp\` (abbreviated below without the
directory). Signatures/constants are cross-checked against
`modding\DecompilerTool\ilrecovery_out\` (whole-class stubs). No runtime testing was
performed; nothing was modified.

**Confidence legend.** Every claim is marked:

- **Verified** — directly observable at the cited dump lines.
- **Inferred** — strongly suggested by the cited evidence but requires interpretation
  (field-name attribution, helper identities, enum ordinals).
- **Unknown** — the dump does not resolve it. Collected in *Open questions*; never
  guessed here.

**Sources for this document**

| Source | Role |
|---|---|
| `CharacterIdentityIntegrity.txt` (2070 lines) | real method bodies (ISIL) |
| `DecompilerTool\ilrecovery_out\CharacterIdentityIntegrity.cs` / `IdentityVerdict.cs` | signatures, enum, constants |
| `DecompilerTool\ilrecovery_out\PlayerData.cs`, `NetworkPlayerData.cs` | field names for the offset map |
| `SaveSystem.txt` | enforcement call sites (load / save) |
| `CharacterPlausibility.txt` | enforcement call sites (list filtering) |
| `SaveCodec.txt` | `ComputeIdentityTag` / `CreateIdentityHasher` used by `Stamp`/`Reconcile` (see `06-save-codec-identity.md`) |

---

## 1. Purpose & threat model

`CharacterIdentityIntegrity` is a **client-side anti-tamper layer for character identity
fields** in `PlayerData` — primarily `race` and `class` (plus, in the legacy path, `gender`,
`specialization`, `hardcore`). It works like a MAC over a small canonical projection of the
character:

1. A canonical identity string is built from the identity fields
   (`Canonical`, `LegacyFiveFieldCanonical`).
2. That string is hashed with a keyed HMAC whose key is derived through `SaveCodec`
   (see doc 06) and stored in the save as a Base64 **identity tag**
   (`PlayerData.characterIdentityTag`, native offset `+0x78`).
3. On load, the tag is recomputed and compared; on save, the tag is (re-)stamped.

**Threat model (what it defends against).** A player hand-editing a save file (or using a
generic hex/save editor) to change their race/class/specialization without knowing the
derivation key. Because the editor cannot recompute a valid tag without the key, the edit
is detected. `Reconcile` additionally *recovers* the original identity by brute-forcing the
small race × class search space against the stored tag, so a tampered-but-recoverable save
is repaired instead of rejected.

**What it explicitly does not defend against.**

- A reverse engineer with the game binary. All key material ships with the game
  (`SaveCodec.S0..S3`, salt `"Dimraeth.CharacterIdentity.v1"` — doc 06), so the tag can be
  recomputed offline. This is tamper *detection* and casual-cheat friction, not secrecy
  against a determined attacker. **Verified** (key material constants in `SaveCodec.txt`
  `.cctor` L5154–5778).
- Network/authority cheating. Every reference in the Assembly-CSharp dump is local
  (`SaveSystem`, `CharacterPlausibility`); there is no server round-trip involved.
  **Verified** (call-site table, §5).
- Failure of the check is *fail-open*: every catch block logs a warning and continues or
  returns a benign result (`FailsIdentityCheck` → `false`, `Reconcile` → `Intact`).
  **Verified** (L149, L328, L1260; behavior per method below).

The toggle `EnforceIdentityTag = true` (**Verified**, `CharacterIdentityIntegrity.cs`
public const) shows the system is compiled in as enabled; no runtime read of this const was
found in the dumps (see Open questions).

---

## 2. API surface

From `DecompilerTool\ilrecovery_out\CharacterIdentityIntegrity.cs` (identical content in
`IdentityVerdict.cs`), bodies per `CharacterIdentityIntegrity.txt`:

| Member | Kind | Signature | Body at (`CharacterIdentityIntegrity.txt`) |
|---|---|---|---|
| `IdentityVerdict` | nested enum | `Intact, Repaired, Unrecoverable` | declaration only (`CharacterIdentityIntegrity.cs`) |
| `EnforceIdentityTag` | const | `bool = true` | n/a |
| `CanonVersion` | private const | `int = 1` | n/a (value seen in `Canonical`, L554–565) |
| `HasTag` | static | `bool HasTag(PlayerData data)` | Method header L3 |
| `Stamp` | static | `void Stamp(PlayerData data)` | L33 |
| `FailsIdentityCheck` | static | `bool FailsIdentityCheck(PlayerData data, out string reason)` | L157 |
| `Canonical` | private static | `string Canonical(PlayerData d)` | L338 |
| `Canonical` | private static | `string Canonical(PlayerData d, int race, int cls)` | L362 |
| `Reconcile` | static | `IdentityVerdict Reconcile(PlayerData data, out string note)` | L668 |
| `LegacyFiveFieldCanonical` | private static | `string LegacyFiveFieldCanonical(PlayerData d)` | L1294 |
| `RaceValues` | private static | `int[] RaceValues()` | L1630 |
| `ClassValues` | private static | `int[] ClassValues()` | L1655 |
| `EnumInts<T>` | private static | `int[] EnumInts<T>() where T : struct, Enum` | L1680 (dump header shows erased `EnumInts()`) |
| `Field` | private static | `void Field(StringBuilder sb, string key, string value)` | L1876 |
| `Field` | private static | `void Field(StringBuilder sb, string key, int value)` | L1961 |

All 12 `Method:` headers listed above are the complete set in
`CharacterIdentityIntegrity.txt` (**Verified**, header scan of the 2070-line file).

---

## 3. Identity-verdict model

`IdentityVerdict` (nested public enum in `CharacterIdentityIntegrity`):

| Value | Numeric | Meaning (from producer/consumer behavior) | Evidence |
|---|---|---|---|
| `Intact` | 0 | Tag already matches, or legacy canonical matched, or check not applicable (null/empty tag), or an exception occurred | `Reconcile` returns 0 at its early-out/legacy/catch exits (**Verified** L1021–1051, L1189–1192, L1260) |
| `Repaired` | 1 | Exactly one (race, class) pair reproduces the tag; the fields were rewritten to it and `note` = `"restored {0} {1}"` | **Verified** L1160–1168 |
| `Unrecoverable` | 2 | No legal pair reproduces the tag, or more than one does | **Verified** L1126–1128, L1195–1197 |

Numeric values **Verified** from the immediate return constants in `Reconcile` (0/1/2) and
corroborated by declaration order in `IdentityVerdict.cs` (C# default 0,1,2 — **Inferred**
as to the *author's intent*, but the emitted immediates are Verified).

**How verdicts are consumed** (§5): only `Repaired` triggers user-visible logging in
`SaveSystem` (`"[CharacterIdentityIntegrity] '…': " + note`). `Intact` and `Unrecoverable`
are silently accepted on load — the game does not block play on `Unrecoverable`, it only
fails the identity *content* check inside `CharacterPlausibility` (which uses the separate
boolean API `FailsIdentityCheck`, not the verdict enum). **Verified** (§5 call-site table).

---

## 4. PlayerData field map and the `Field(...)` premise correction

**Premise correction (important).** The task brief suggested mapping "small-integer
`Field(...)` usage" to identity fields. The dump shows the opposite: `Field(sb, key,
int value)`'s third argument is the **field value** (an enum int or a 0/1 bool), not a
field index. Example: `Field(sb, "cls", cls)` passes the class enum value
(`Field` int overload renders `value.ToString(CultureInfo.InvariantCulture)` — **Verified**
L2047–2053). The identity-field mapping is therefore done by **key string** (explicit in
the dump) and by **native offset** in `PlayerData` (explicit in the dump; field *names*
attributed from `PlayerData.cs` declaration order and layout arithmetic).

| Canonical key | `PlayerData` field (name **Inferred**) | Native offset (access **Verified**) | Offset evidence |
|---|---|---|---|
| `name` | `characterName` (string) | `+0x10` (16) | read in `Canonical` (inline `\|name=` append) |
| `hash` | `characterHash` (FixedString64Bytes, 64 B spanning 28–91) | `+0x1C` (28) | read in `Canonical` (inline `\|hash=` append) |
| `race` | `characterRace` (int) | `+0xE0` (224) | L110/L275/L357/L1590 (`[reg+224]`) |
| `cls` | `characterClass` (int) | `+0xDC` (220) | L109/L274/L355/L1595 (`[reg+220]`) |
| `gen` | `characterGender` (int) | `+0xE4` (228) | L1600 (`[rdi+228]`) |
| `spec` | `characterSpecialization` (int) | `+0xEC` (236) | L1605 (`[rdi+236]`) |
| `hc` | `characterHardcore` (bool) | `+0x91` (145) | L1612 (`[rdi+145]`, bool→0/1 via `setne`) |
| *(tag storage)* | `characterIdentityTag` (string) | `+0x78` (120) | L26/L121/L269/L1021 (`[reg+120]`) |

Field-name attribution is **Inferred** from `DecompilerTool\ilrecovery_out\PlayerData.cs`
declaration order plus layout arithmetic (FixedString64Bytes = 64 B after the string at
+16 puts the next field at +92; `characterColorsVersion` at +116 corroborated by
`SaveSystem` code that writes the 64-byte GUID hash at +28 and reads colorsVersion at
+116). Offsets and accesses are **Verified** at the cited lines. All accesses listed are
the only identity-field accesses in the file (**Verified**, offset grep).

---

## 5. Canonical string formats

### 5.1 Current canonical — `Canonical(d, race, cls)`

**Verified** (L548–651, L554, L580, L611, L641, L646):

```
v1|name=<d.characterName ?? String.Empty>|hash=<helper 0x183A7E9D0(&d.hash)>|race=<int>|cls=<int>
```

- Builder: `new StringBuilder(128)` — capacity **128** **Verified** (L548–551).
- Prefix: `"v"` appended literally (L554) then `CanonVersion` (=1) rendered with
  `CultureInfo.InvariantCulture` (L558–565) → `"v1"`. No separator between `v` and `1`.
  **Verified** (append pattern) + **Inferred** (that the appended int is `CanonVersion`;
  the const's value 1 is Verified in `CharacterIdentityIntegrity.cs`).
- `|name=` and `|hash=` are appended inline (not via `Field`); `name` is null-coalesced to
  `String.Empty` (**Verified** structure at L580/L611; the empty-string fallback is via
  helper `[0x185D2CA10]+0xB8`, presumed `String.Empty` — **Inferred**).
- `hash` is rendered by helper `0x183A7E9D0(&d.hash, 0)` — most plausibly
  `FixedString64Bytes.ToString()`; exact semantics **Unknown** (Open questions).
- `Field(sb, "race", race)` then `Field(sb, "cls", cls)` (L641, L646). `Field` appends
  `'|' + key + '=' + value` (**Verified** L1876–1960 for the string overload;
  L1961–2069 for the int overload with `ToString(CultureInfo.InvariantCulture)`).

The 1-arg wrapper `Canonical(d)` just forwards `d`'s own race/class: reads `[rcx+220]`
(cls) into `r8` and `[rcx+224]` (race) into `rdx` (**Verified** L355–357) and calls the
3-arg overload (**Inferred** call target from sibling analysis; the arg setup is Verified).

### 5.2 Legacy canonical — `LegacyFiveFieldCanonical(d)`

**Verified** (L1497–1627):

```
v1|name=<...>|hash=<...>|race=<+224>|cls=<+220>|gen=<+228>|spec=<+236>|hc=<0|1>
```

- Builder: `new StringBuilder(160)` — capacity **160** **Verified** (L1497–1500).
- Same `v1` prefix (L1503–1514), same inline `|name=` / `|hash=` (L1529, L1560).
- Five `Field` calls in order: `"race"`=[rdi+224] (L1590–1594), `"cls"`=[rdi+220]
  (L1595–1599), `"gen"`=[rdi+228] (L1600–1604), `"spec"`=[rdi+236] (L1605–1609),
  `"hc"`= bool `[rdi+145]` converted to 0/1 (`setne`) (L1610–1616). **Verified**.
- Terminated by `StringBuilder.ToString()` via virtual slot 368 (L1617–1620).

**Naming discrepancy.** The method name says "FiveField" and indeed has exactly five
`Field()` calls, but the emitted string carries **seven** keyed entries (plus `name`,
`hash` and the `v1` prefix). Whether "five" refers to the five `Field` calls or to an
older format is **Unknown** (Open questions).

---

## 6. Behavior per method

Line ranges are `CharacterIdentityIntegrity.txt` unless noted. `d` = the `PlayerData`
argument; tag at `+0x78`, race at `+0xE0`, class at `+0xDC`.

### `HasTag(PlayerData data)` — L3–32
1. `data == null` → `return false`. **Verified** (null check before field read).
2. Else `return !String.IsNullOrEmpty(data.tag)` — reads `[rcx+120]` (L26), calls
   `String.IsNullOrEmpty` (L28). **Verified**.

### `Stamp(PlayerData data)` — L33–156
1. `data == null` → return (no-op). **Verified** (early-out structure).
2. Computes `SaveCodec.ComputeIdentityTag(Canonical(d, race, cls))` with race=`[rbx+224]`,
   cls=`[rbx+220]` (L109–110) and the `ComputeIdentityTag` call at L120. **Verified**.
3. Stores the result in `data.tag` (`[rbx+120]` write, L121–122, write-barrier helper
   `0x1805E4540`). **Verified**. The tag is written **unconditionally** (even when the
   computed tag is null — `ComputeIdentityTag` returns null on null canonical, see doc 06).
   Overwrites any existing tag. **Inferred** (no compare-before-write visible).
4. Any exception: `Debug.LogWarning(<literal [0x185A382C0]> + e.Message)` (L149) and
   return. **Verified** (catch-and-log); message text **Unknown**.

### `FailsIdentityCheck(PlayerData data, out string reason)` — L157–337
1. `reason = null` first. **Verified** (out-param init).
2. `data == null` or `String.IsNullOrEmpty(data.tag)` (L269–270) → `return false`
   (i.e. *does not fail*). **Verified**.
3. Recomputes `SaveCodec.ComputeIdentityTag(Canonical(d, [+224], [+220]))` (L274–275,
   call at L285). **Verified**.
4. `String.Equals(computed, data.tag, 4)` — comparison kind **4 = `StringComparison.Ordinal`**
   (**Verified** immediate `Move r8, 4` at L288 + `String.Equals` at L291; the enum
   mapping 4=Ordinal is **Inferred** from `StringComparison` ordinals).
5. Equal → `return false` (passes). **Verified**.
6. Not equal → `reason = "identity tag does not match the saved race/class"` (literal at
   L294 and L296) and `return true`. **Verified** (exact string quoted from dump).
7. Any exception: `Debug.LogWarning(<literal [0x185A384F8]> + e.Message)` (L328) and
   `return false` (**fail-open**). **Verified** (structure); log text **Unknown**.

### `Canonical(PlayerData d)` — L338–361
Wrapper. Reads `[rcx+220]`→`r8` (cls) and `[rcx+224]`→`rdx` (race) (L355–357) and forwards
to `Canonical(d, race, cls)`. **Verified** (reads) / **Inferred** (forwarding target).

### `Canonical(PlayerData d, int race, int cls)` — L362–667
Builds the v1 two-field canonical of §5.1. Key verified points:
- `StringBuilder(128)` (L548–551); `"v"` + `1` with `CultureInfo.InvariantCulture`
  (L554–565); `"name"` (L580); `"hash"` (L611); `Field "race"` (L641); `Field "cls"` (L646).
- `race`/`cls` come from the **parameters**, not from the struct — this is what lets
  `Reconcile` brute-force candidates through the same function. **Verified** (args used at
  L641/L646 are the method parameters per the header L362 and register flow).
- Dump artifact: L359 contains `Invalid "Jump target not found in method."` in the
  1-arg `Canonical` listing — decompiler noise, not runtime behavior (Open questions).

### `Reconcile(PlayerData data, out string note)` — L668–1293
1. `note = null`; `data == null` or empty tag (`[r14+120]`, L1021–1022) → `return Intact(0)`.
   **Verified**.
2. `using var hasher = SaveCodec.CreateIdentityHasher()` (call at L1030). **Verified**.
3. First try current canonical: `hasher.Compute(Canonical(d, [+224], [+220]))`
   (field reads L1037–1038), then `String.Equals(…, data.tag, 4)` (L1048–1051). Match →
   `return Intact(0)`. **Verified**.
4. Otherwise brute-force `RaceValues() × ClassValues()` (nested loops; candidate compare at
   L1110–1114 with `String.Equals(..., 4)`). **Verified** (loop + compares).
5. Count of matching candidates:
   - **≥ 2 matches** → `note = "more than one identity reproduces this tag"` (L1126,
     L1128) → `return Unrecoverable(2)`. **Verified** (exact string quoted).
   - **exactly 1 match** → writes the found race to `[r14+224]` and class to `[r14+220]`
     (L1167–1168), `note = String.Format("restored {0} {1}", (Race)…, (Class)…)`
     (format literal `"restored {0} {1}"` at L1160, `String.Format` at L1161, enum boxing
     via `il2cpp_value_box` L1151/L1156) → `return Repaired(1)`. **Verified**. Note the
     *string* arguments are the enum **names** (boxed `Race`/`Class` values formatted by
     `String.Format`) — **Inferred** (format item rendering is `ToString()` of the boxed enum).
   - **0 matches** → fallback `LegacyFiveFieldCanonical(d)` (call at L1181); if
     `String.Equals(…, data.tag, 4)` (L1189–1192) matches → `return Intact(0)` **without
     re-stamping the tag** (no write to `[r14+120]` in this path — **Verified** by absence
     in L1181–1194). Else `note = "no legal identity reproduces this tag"` (L1195, L1197)
     → `return Unrecoverable(2)`. **Verified** (exact string quoted).
6. Any exception: `Debug.LogWarning(<literal [0x185A383E0]> + e.Message)` (L1260) and
   `return Intact(0)` (**fail-open**). **Verified** (structure); log text **Unknown**.

### `LegacyFiveFieldCanonical(PlayerData d)` — L1294–1629
Builds the legacy seven-key canonical of §5.2 (see §5.2 for the exact keys/offsets;
capacity 160 at L1497–1500). **Verified**.

### `RaceValues()` — L1630–1654
Tail-calls helper `0x18174D3B0` with generic metadata static `[0x1859F7970]`
(L1636–1641 disasm; L1643–1653 ISIL). Semantics: returns the cached `int[]` of race enum
values, i.e. `EnumInts<Race>()`. **Verified** (call/return shape) / **Inferred**
(the `T = Race` attribution and the caching semantics of `0x18174D3B0`).

### `ClassValues()` — L1655–1679
Identical shape with `[0x1859F7850]` (L1661–1666 / L1668–1678) → `EnumInts<Class>()`.
**Verified** / **Inferred** as above.

### `EnumInts<T>()` — L1680–1875
`Enum.GetValues(typeof(T))` (L1815) → each value `Convert.ToInt32(v, CultureInfo.InvariantCulture)`
(L1838–1853) → `int[]`. **Verified**.

### `Field(StringBuilder sb, string key, string value)` — L1876–1960
`sb.Append('|').Append(key).Append('=').Append(value ?? String.Empty)`; the null fallback
resolves via `[0x185D2CA10]+0xB8` (**Verified** structure; `String.Empty` attribution
**Inferred**).

### `Field(StringBuilder sb, string key, int value)` — L1961–2069
Same three appends; `value` rendered `value.ToString(CultureInfo.InvariantCulture)`
(CultureInfo at L2047–2053). **Verified**.

---

## 7. Enforcement call sites

All references to `CharacterIdentityIntegrity` in the Assembly-CSharp dump
(**Verified**, exhaustive grep; only these five exist):

| # | File:line of call | Enclosing method (header line) | What happens |
|---|---|---|---|
| 1 | `SaveSystem.txt:4455` `Call CharacterIdentityIntegrity.Reconcile` | `TryLoadPlayerWithFallback(String name)` (L3712) | Verdict checked; `Repaired(1)` only → `Debug.Log("[CharacterIdentityIntegrity] '" + data.characterName + "': " + note)` (log prefix literal at L4464, concat region L4462–4472). `Intact`/`Unrecoverable` → silent. **Verified** |
| 2 | `SaveSystem.txt:9728` `Call CharacterIdentityIntegrity.Stamp` | `StampedForSave(PlayerData playerData)` (L9450) | Tag re-stamped at the end of save preparation (after rune-count repair / rune-formula migration in the same method). **Verified** (call + position) |
| 3 | `SaveSystem.txt:14249` `Call CharacterIdentityIntegrity.Reconcile` | `EnsureSettings(PlayerSaveFile sf)` (L13974) | Same `Repaired`-only logging as #1 (prefix literal at L14258, region L14256–14266). **Verified** |
| 4 | `CharacterPlausibility.txt:247` `Call CharacterIdentityIntegrity.FailsIdentityCheck` | `FilterImplausible(List<PlayerSaveFile> saves)` (L3) | On fail: `Debug.Log("[CharacterPlausibility] Hiding character '" + name-or-"<unnamed>" + "' from the character list: " + reason)` and the save is dropped from the shown list (log region L315–337). **Verified** |
| 5 | `CharacterPlausibility.txt:500` `Call CharacterIdentityIntegrity.FailsIdentityCheck` | `IsImplausible(PlayerSaveFile save, out string reason)` (L363) | On fail → `return true` (save treated as implausible). **Verified** |

Consumers of the tag itself inside the integrity class are `Stamp` (write) and
`HasTag`/`FailsIdentityCheck`/`Reconcile` (read). The tag derivation lives in
`SaveCodec` (`ComputeIdentityTag`, `CreateIdentityHasher`) — covered in
`06-save-codec-identity.md`.

---

## 8. Evidence appendix (claim → evidence → confidence)

`CharacterIdentityIntegrity.txt` = `CII.txt`; `SaveSystem.txt` = `SS.txt`;
`CharacterPlausibility.txt` = `CP.txt`; `ilrecovery_out\` = `ILR`.

| # | Claim | Evidence | Conf. |
|---|---|---|---|
| 1 | 12 methods exactly; headers at L3/33/157/338/362/668/1294/1630/1655/1680/1876/1961 | `CII.txt` header scan (2070 lines) | Verified |
| 2 | `IdentityVerdict { Intact, Repaired, Unrecoverable }` = 0/1/2 | `ILR\IdentityVerdict.cs`; return immediates in `CII.txt` Reconcile | Verified |
| 3 | `EnforceIdentityTag = true`, `CanonVersion = 1` | `ILR\CharacterIdentityIntegrity.cs` | Verified |
| 4 | `HasTag` = `!String.IsNullOrEmpty(data.tag)`; tag at +120 | `CII.txt:26,28` | Verified |
| 5 | `Stamp` computes tag from `(cls [+220], race [+224])` and writes `[+120]` | `CII.txt:109–110,120–122` | Verified |
| 6 | `Stamp` swallows exceptions with `Debug.LogWarning` | `CII.txt:149` | Verified (log text Unknown) |
| 7 | `FailsIdentityCheck` compares computed tag vs stored tag Ordinal (`4`) | `CII.txt:274–275,285,288–291` | Verified |
| 8 | Fail reason string `"identity tag does not match the saved race/class"` | `CII.txt:294,296` | Verified |
| 9 | `FailsIdentityCheck` fail-open on exception | `CII.txt:328` | Verified |
| 10 | `Canonical(d)` forwards `d`'s `cls`/`race` | `CII.txt:355–357` | Verified (target Inferred) |
| 11 | `Canonical(d,race,cls)` = `v1\|name=…\|hash=…\|race=…\|cls=…`; capacity 128 | `CII.txt:548–551,554–565,580,611,641,646` | Verified |
| 12 | `hash` rendered by helper `0x183A7E9D0(&d.hash,0)` | `CII.txt` ~L611–640 | Verified call; semantics Unknown |
| 13 | `LegacyFiveFieldCanonical` = `v1\|name=\|hash=\|race=\|cls=\|gen=\|spec=\|hc=`; capacity 160 | `CII.txt:1497–1500,1503–1514,1529,1560,1590–1616` | Verified |
| 14 | Legacy field offsets 224/220/228/236/145 with `hc` as 0/1 | `CII.txt:1590,1595,1600,1605,1610–1616` | Verified |
| 15 | `Reconcile`: match current canonical → Intact | `CII.txt:1030,1037–1038,1048–1051` | Verified |
| 16 | `Reconcile` brute force over `RaceValues()×ClassValues()` with Ordinal compares | `CII.txt:1057–1114` | Verified |
| 17 | 2nd match → `"more than one identity reproduces this tag"` → Unrecoverable | `CII.txt:1126,1128` | Verified |
| 18 | 1 match → writes `[+224]/[+220]`, `"restored {0} {1}"` via `String.Format` → Repaired | `CII.txt:1151–1168` | Verified (rendered text Inferred) |
| 19 | 0 matches → legacy fallback; legacy match → Intact **without** tag re-stamp | `CII.txt:1181,1189–1194` (absence of `[r14+120]` write) | Verified |
| 20 | 0 matches & legacy mismatch → `"no legal identity reproduces this tag"` → Unrecoverable | `CII.txt:1195,1197` | Verified |
| 21 | `Reconcile` fail-open on exception → Intact | `CII.txt:1260` | Verified (log text Unknown) |
| 22 | `RaceValues/ClassValues` tail-call `0x18174D3B0` over metadata statics `[0x1859F7970]/[0x1859F7850]` | `CII.txt:1636–1641,1661–1666` | Verified (identity Inferred) |
| 23 | `EnumInts<T>` = `Enum.GetValues` → `Convert.ToInt32(…, InvariantCulture)` | `CII.txt:1815,1838–1853` | Verified |
| 24 | `Field(string)` appends `\|key=value`, null→empty via `[0x185D2CA10]+0xB8` | `CII.txt:1876–1960` | Verified (fallback Inferred) |
| 25 | `Field(int)` renders `ToString(CultureInfo.InvariantCulture)` | `CII.txt:1961–2069 (2047–2053)` | Verified |
| 26 | `Reconcile` call in `TryLoadPlayerWithFallback`, Repaired-only log | `SS.txt:4455,4462–4472` (header 3712) | Verified |
| 27 | `Stamp` call in `StampedForSave` | `SS.txt:9728` (header 9450) | Verified |
| 28 | `Reconcile` call in `EnsureSettings`, Repaired-only log | `SS.txt:14249,14256–14266` (header 13974) | Verified |
| 29 | `FailsIdentityCheck` in `FilterImplausible` hides character + logs | `CP.txt:247,315–337` (header 3) | Verified |
| 30 | `FailsIdentityCheck` in `IsImplausible` returns true on fail | `CP.txt:500` (header 363) | Verified |
| 31 | Tag/native-field offsets +120/+224/+220/+228/+236/+145 | offset grep (`+120\|+220\|+224` etc.) | Verified (names Inferred) |

---

## 9. Open questions

1. **Log-prefix literals in catch blocks.** `Stamp` L149 (`[0x185A382C0]`),
   `FailsIdentityCheck` L328 (`[0x185A384F8]`), `Reconcile` L1260 (`[0x185A383E0]`) build
   warnings as `<literal> + e.Message`. The literal contents are not in the dump →
   exact warning text **Unknown**.
2. **`hash` rendering helper `0x183A7E9D0(&d.hash, 0)`.** Assumed
   `FixedString64Bytes.ToString()`; exact semantics (charset, length prefix handling)
   **Unknown**. This matters: any change in rendering changes every tag.
3. **`Field` null-fallback symbol `[0x185D2CA10]+0xB8`.** Presumed `String.Empty`;
   exact target **Unknown** (also used by `SaveNameVerdict.Ok()` — doc 06).
4. **"LegacyFiveFieldCanonical" name vs 7 keyed entries.** "Five" matches the five
   `Field()` calls but not the total entry count. Which historical format the name refers
   to is **Unknown**; likely there was an even older five-key canonical. Design intent
   **Unknown**.
5. **Legacy match → `Intact` without re-stamp (verified behavior).** Legacy-matching saves
   keep their old-format tag; they are only re-stamped when `StampedForSave` runs. Whether
   this is deliberate (so `Reconcile` never mutates on Intact) is **Unknown**.
6. **`EnumInts<T>` caching helper `0x18174D3B0`.** Both `RaceValues` and `ClassValues`
   tail-call the same helper with different metadata statics. Exact identity
   (generic-sharing thunk vs cached-instantiation accessor) **Unknown**.
7. **Fail-open policy.** Both boolean and verdict APIs swallow exceptions and report
   "fine". Whether this is a deliberate anti-crash decision (a broken tag must not brick a
   save) or an oversight is **Unknown**; behavior is Verified.
8. **`EnforceIdentityTag` consumers.** No read of the const was found in the
   Assembly-CSharp dump (it is compile-time inlinable). Whether external tooling/mods use
   it **Unknown**.
9. **`Canonical(PlayerData)` listing artifact.** L359 shows
   `Invalid "Jump target not found in method."` — decompiler limitation, but the exact
   control flow it obscures is **Unknown** (does not affect the visible reads at L355–357).
10. **`IdentityVerdict` numeric authorship.** Values 0/1/2 are Verified from return
    immediates; whether explicit enum values were written in source is **Unknown**
    (ilrecovery shows no explicit assignments).
