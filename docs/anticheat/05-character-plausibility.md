# 05 — Character Plausibility (Static Implausible-Stat Rejection)

Citations: `file:line` are relative to `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/` (the cpp2il ISIL dump) unless prefixed `ilrecovery` (`modding/DecompilerTool/ilrecovery_out/`). Every behavioral claim is marked **Verified** (directly readable in the dump), **Inferred** (recovered from structure/offsets/conventions), or **Unknown** (dump insufficient; see Open questions).

## Purpose & threat model

`CharacterPlausibility` is a small static class of pure stat-sanity checks applied at two trust boundaries:

1. **Client-side, local save load** — characters whose saved stats are outside plausible bounds are *hidden* from the character-selection list (`FilterImplausible`). The save file is not deleted or modified; this is a hide, not a wipe (**Verified**, `CharacterPlausibility.txt:315-343`).
2. **Server-side, join time** — a character payload received from a joining client (`NetworkPlayerData`) is re-checked and the join is refused with a disconnect (`RejectsJoiningCharacter`, called from `SpawnManager`) (**Verified**, `SpawnManager.txt:7754`).

**Threat model.** The attacker is a player with a hex editor and full control of their own game client and local save files. They inflate `characterLevel` / `characterHighestLevel`, skill-tree points invested, or attribute totals beyond anything reachable through play, then (a) play solo or (b) bring the character into other players' sessions, trivializing content and distorting any host-side balance assumption. The checks are *plausibility* bounds only — flat ceilings of **37** (level), **84** (skill points), **126** (attribute points) (**Verified**, `CharacterPlausibility.txt:836-840`). Cryptographic save integrity is a separate system (`CharacterIdentityIntegrity`, invoked first at each check point — **Verified**, `CharacterPlausibility.txt:247, 500`); this doc covers only the stat-cap layer.

**Explicit non-goals / limits:**
- No identity or hash verification here (delegated to `CharacterIdentityIntegrity`) (**Verified**).
- No bans or kicks-with-record are issued by this system; the join rejection is a plain disconnect (cross-ref `07-ban-system.md`) (**Verified** — no ban-API call exists in `CharacterPlausibility`, `SpawnManager` rejection path, or `CharacterSelection` filtering).
- The caps are flat, **not** level-scaled, and comparisons are strict `>` (a stat exactly at the cap passes) (**Verified**, `CharacterPlausibility.txt:836-843`).
- The client-side filter runs in the client UI only; a tampered client can skip it. The authoritative gate is the server-side `RejectsJoiningCharacter` (**Verified** call sites below).

## API surface

Signatures/visibility from `ilrecovery/CharacterPlausibility.cs:3-41` (**Verified**); the class is `public static class CharacterPlausibility` (`ilrecovery/CharacterPlausibility.cs:3`).

| Signature | Visibility | Role |
|---|---|---|
| `List<PlayerSaveFile> FilterImplausible(List<PlayerSaveFile> saves)` | `public static` | Client-side. Returns a new list containing only plausible saves; drops are logged. `ilrecovery:7` |
| `bool IsImplausible(PlayerSaveFile save, out string reason)` | `public static` | Single-save predicate + reason. `ilrecovery:12` |
| `bool RejectsJoiningCharacter(NetworkPlayerData data, out string reason)` | `public static` | Server-side join gate on the wire-level character summary. `ilrecovery:17` |
| `bool ExceedsPlausibleCaps(int level, int highestLevel, long skillPointsSpent, long attributeTotal, out string reason)` | `public static` | The cap comparison + reason formatting. `ilrecovery:22` |
| `long SumPositive(List<int> values)` | `public static` | Sums only entries `> 0`; `null` → `-1`. Used by `NetworkPlayerData` serialization. `ilrecovery:27` |
| `long SumAll(List<int> values)` | `private static` | Sums all entries; `null` → `-1`. `ilrecovery:32` |
| `long SumAll(int[] values)` | `private static` | Array overload, same semantics. `ilrecovery:37` |
| `const bool RejectImplausibleJoins = true` | `public const` | Apparent feature flag. `ilrecovery:5`. No read site located (constants inline at compile time) — usage **Unknown** (Open questions). |

## Data model

Field offsets are decimal as printed by this dump (e.g. `[rbx+172]` = `+0xAC`). Layout was cross-mapped through the field-copy sequence of `NetworkPlayerData..ctor(PlayerData player, Int64 totalTimePlayed = 0)` (`NetworkPlayerData.txt:1639`) (**Verified** for all offsets below).

**`PlayerSaveFile`** (save container):
- `+0x20 playerData` — `PlayerData` reference (`CharacterPlausibility.txt:241, 243` dereferences) (**Verified**).

**`PlayerData`** (fields read by this system):
- `+0x10 characterName` (string) — used in the hide log (`CharacterPlausibility.txt:316`) (**Verified** offset; name from `ilrecovery/PlayerData.cs` field list).
- `+0x64 characterLevel` (int, 32-bit reads `mov r12d,[rbp+64h]` / `mov ecx` pattern) (`CharacterPlausibility.txt:252, 509`) (**Verified**).
- `+0x68 characterHighestLevel` (int) (`CharacterPlausibility.txt:251, 510`) (**Verified**).
- `+0xD0` (208) `characterAttributes` (`List<int>`) — summed with **SumAll** semantics (every entry) (`CharacterPlausibility.txt:283, 543`) (**Verified** offsets/semantics; field name from `ilrecovery/PlayerData.cs` **Verified**).
- `+0x328` (808) skill-tree points invested (`List<int>`) — summed with **SumPositive** semantics (only `> 0`) (`CharacterPlausibility.txt:253, 511`) (**Verified** offset/semantics). Field name **Inferred** `characterSkillTreePointsInvested` (from the reason string `"{0} skill points spent in the tree, ..."`).
- Layout caveat: in the `NetworkPlayerData..ctor` copy chain, exactly one of `characterThirst` / `characterHunger` / `characterAlcohol` occupies 8 bytes although typed `float` — which one is **Unknown** (Open questions).

**`NetworkPlayerData`** (join-time summary struct; passed by address, `LoadAddress rdx, [rbp-48]` at the `PlayerCannotJoinServer` call — `SpawnManager.txt:4046`):
- `+0x00` character name; `+0x0C NetworkPlayerHash` (`FixedString64Bytes`, 64 B) — used in the `Refused join` log (**Verified** offsets via ctor copy chain).
- `+0x4C` (76) level (int) (`CharacterPlausibility.txt:674`) (**Verified**).
- `+0x50` (80) highest level (int) (`CharacterPlausibility.txt:673`) (**Verified**).
- `+0x70` (112) `NetworkPlayerAttributes` (`int[]`) (`CharacterPlausibility.txt:654`) (**Verified**).
- `+0xAC` (172) `NetworkPlayerSkillPointsSpent` (int; read sign-extended to `long`, `movsxd r8,dword ptr [rbx+0ACh]`) (`CharacterPlausibility.txt:672`) (**Verified**).
- Produced by `NetworkPlayerData..ctor`: `NetworkPlayerSkillPointsSpent = Math.Min(SumPositive(skill list), 0x7FFFFFFF)` (`NetworkPlayerData.txt:2000, 2011-2013`) with `0xFFFFFFFF` (i.e. `-1`) when the underlying `SumPositive` is negative (the `null`-list sentinel) (**Verified**). So the `> 0` filter and the `-1` sentinel arrive pre-aggregated on the wire side.

## Behavior per method

### `FilterImplausible(List<PlayerSaveFile> saves)` — `CharacterPlausibility.txt:3`
1. Allocates `result = new List<PlayerSaveFile>()` (`:211-223`). `saves == null` → returns the empty list (`:224-225`). **Verified**
2. Iterates by index over `saves` (`List<PlayerSaveFile>.get_Item` at `:233`). Per iteration a local `reason` is reset to `null` (`:234-238`). **Verified**
3. `save == null` or `save.playerData == null` → the element is **kept** unchanged (`:239-242` → keep path `:338-343`, `List<PlayerSaveFile>.Add`). Defensive pass-through, no log. **Verified**
4. Otherwise `CharacterIdentityIntegrity.FailsIdentityCheck(playerData, out reason, 0)` (`:247`). True → drop path (step 7). **Verified**
5. Reads `characterHighestLevel` (`+0x68`, `:251`), `characterLevel` (`+0x64`, `:252`), the skill list (`+0x328`, `:253`), then:
   - `skillPointsSpent` = inlined **SumPositive** over `+0x328` — loops `List<int>.get_Item` (called twice per positive entry), adds only entries `> 0`; list `null` → `-1` (`:261-281`, sentinel at `:281`). **Verified**
   - `attributeTotal` = inlined **SumAll** over `+0xD0` — adds **every** entry (no positivity filter); list `null` → `-1` (`:282-304`, sentinel at `:304`). **Verified**
6. `ExceedsPlausibleCaps(characterLevel, characterHighestLevel, skillPointsSpent, attributeTotal, out reason)` (`:312`). False → keep (`:338-343`). **Verified**
7. Drop path (identity or caps failure): `Debug.Log(String.Concat("[CharacterPlausibility] Hiding character '", name, "' from the character list: ", reason))` where `name = playerData.characterName` (`+0x10`) or `"<unnamed>"` if `playerData` is null (`:315-333`; `"<unnamed>"` at `:320`, prefix at `:323`, `Debug.Log` at `:333`). The save is dropped from `result` only — never written back. **Verified** (the `"<unnamed>"` branch is unreachable in practice since null-`playerData` elements were kept at step 3 — branch **Verified**, unreachability **Inferred**).

### `IsImplausible(PlayerSaveFile save, out string reason)` — `CharacterPlausibility.txt:363`
1. `reason = null` first (`:485-490`). **Verified**
2. `save == null` or `save.playerData == null` → returns `false`, reason stays `null` (`:491-494` → `:593`). **Verified**
3. `CharacterIdentityIntegrity.FailsIdentityCheck(playerData, out reason, 0)` (`:500`) → `true` returned immediately (`:585-586`), reason set by callee. **Verified**
4. Else same composition as `FilterImplausible`: level `+0x64` (`:509`), highest `+0x68` (`:510`), skill list `+0x328` (`:511`); inlined SumPositive (`:518-541`, `null` → `-1` via preset at `:518` / copy at `:541`); inlined SumAll over `+0xD0` (`:542-566`, `null` → `-1` via the same preset surviving to `:566`). **Verified**
5. Returns the result of `ExceedsPlausibleCaps(level, highestLevel, skillPointsSpent, attributeTotal, out reason)` directly (`:572`). **Verified**

### `RejectsJoiningCharacter(NetworkPlayerData data, out string reason)` — `CharacterPlausibility.txt:601`
1. `reason = null` (`:648-653`). **No identity check in this method** (server side trusts the identity layer ran at serialization). **Verified**
2. `attributeTotal` = inlined **SumAll(int[])** over `NetworkPlayerAttributes` (`int[]` at `+0x70`, `:654`): array `Length` at `+0x18`, element data at `+0x20`, every element added sign-extended; `null` array → `-1` (`:654-671`). **Verified**
3. `skillPointsSpent = (long)data.NetworkPlayerSkillPointsSpent` — sign-extended read of the int at `+0xAC` (`:672`). No re-filtering; the `> 0` filter was applied at serialization (see Data model). **Verified**
4. `level` = `+0x4C` (`:674`), `highestLevel` = `+0x50` (`:673`). **Verified**
5. Returns `ExceedsPlausibleCaps(level, highestLevel, skillPointsSpent, attributeTotal, out reason)` (`:677`). **Verified**

### `ExceedsPlausibleCaps(int level, int highestLevel, long skillPointsSpent, long attributeTotal, out string reason)` — `CharacterPlausibility.txt:685`
All constants and strings exact from the dump:
1. `reason = null` (`:829-830`). `effectiveLevel = max(level, highestLevel)` via `cmov`-style select (`:831-835`). **Verified**
2. **Check order and outcomes** (first match wins; returns `true` with `reason` set):
   1. `effectiveLevel > 37` (`:836`; constant `37` boxed at `:881`) →
      `reason = String.Format("level {0} is above the plausible ceiling of {1} ", effectiveLevel, 37)` (string at `:883`; note the **trailing space**) `+ String.Format("(Level={0}, HighestLevel={1})", level, highestLevel)` (string at `:898`) joined by `String.Concat` (`:906`), assigned to `reason` (`:907-908`). **Verified**
   2. `skillPointsSpent > 84` (`:838`; constant `84` boxed at `:866`) →
      `reason = String.Format("{0} skill points spent in the tree, above the plausible ceiling of {1}", skillPointsSpent, 84)` (string at `:868`). **Verified**
   3. `attributeTotal > 126` (`:840`; constant `126` boxed at `:851`) →
      `reason = String.Format("{0} attribute points, above the plausible ceiling of {1}", attributeTotal, 126)` (string at `:853`). **Verified**
   4. else returns `false`, `reason` remains `null` (`:842-843`). **Verified**
3. Returns `true` after assigning reason (`:911`). Caps are flat (not level-scaled); all comparisons strict `>`. **Verified**
4. Negative inputs (e.g. the `-1` sentinel from a `null` list) trivially pass every check — no clamping or validation in this method. **Inferred** (pure comparison arithmetic).

### `SumPositive(List<int> values)` — `CharacterPlausibility.txt:923`
- `null` → returns `-1` (`0FFFFFFFFFFFFFFFFh`). **Verified**
- Sums only entries `> 0`; `List<int>.get_Item` (`0x1820642D0`) is invoked twice per positive entry (once to compare, once to add); non-positive entries contribute nothing. **Verified**
- Overflow: plain `long` accumulation, unchecked. **Inferred** (no overflow checks visible).

### `SumAll(List<int> values)` — `CharacterPlausibility.txt:1020`
- Sums **every** entry (no positivity filter). **Verified**
- `null` → `-1`. **Verified** for the inlined instances used by `FilterImplausible`/`IsImplausible` (`:304, :541-566`); the standalone body's null path **Inferred** to match.

### `SumAll(int[] values)` — `CharacterPlausibility.txt:1105`
- Sums every element, sign-extended to `long`; `null` → `-1`. **Verified** (inlined instance in `RejectsJoiningCharacter`, `:654-671`; standalone body consistent).

## Enforcement call sites

`CharacterPlausibility` API is called from exactly four sites (exhaustive sweep of `Call CharacterPlausibility\.` across the IsilDump tree).

### 1. `CharacterSelection.InitializePlayerSelectionScreen()` — `CharacterSelection.txt:2824`
- Call: `CharacterPlausibility.FilterImplausible` at `CharacterSelection.txt:2942`, applied to the freshly loaded sorted save list. **Verified**
- Effect: the filtered list is stored into the screen's cached-list field (`[rbx+248]`, `:2943-2944`) and the `+288` flag is set to `1` (`:2947`; pre-cleared at `:2919`). **Verified**
- **What happens to the offender:** their implausible character simply **does not appear** in the character list. No deletion, no kick, no ban. Their log gets `"[CharacterPlausibility] Hiding character '" + <name> + "' from the character list: " + <reason>` (`CharacterPlausibility.txt:323, 320, 333`) — reason being one of the three cap strings or an identity-failure string from `CharacterIdentityIntegrity`. **Verified**

### 2. `CharacterSelection.ReloadAllCharacterSlots()` — `CharacterSelection.txt:3855`
- Call: `CharacterPlausibility.FilterImplausible` at `CharacterSelection.txt:4557`; result stored to `[rbx+248]` (`:4558-4559`) and then used to populate the slot UI (`:4562`). **Verified**
- **What happens to the offender:** same hide-from-list effect as site 1 (slot refresh path). Log string identical. **Verified**

### 3. `NetworkPlayerData..ctor(PlayerData player, Int64 totalTimePlayed = 0)` — `NetworkPlayerData.txt:1639`
- Call: `CharacterPlausibility.SumPositive` at `NetworkPlayerData.txt:2000`, result clamped `Math.Min(sum, 0x7FFFFFFF)` (`:2011-2013`), negative results normalized to `0xFFFFFFFF` (`-1`). **Verified**
- Effect: shapes `NetworkPlayerSkillPointsSpent` (`+0xAC`) — the exact field later read by `RejectsJoiningCharacter`. Role in enforcement: feeds the gate. **Verified** (data flow), role classification **Inferred**.

### 4. `SpawnManager.PlayerCannotJoinServer(NetworkPlayerData data, string version, ulong clientId)` — `SpawnManager.txt:6428`
- Call: `CharacterPlausibility.RejectsJoiningCharacter` at `SpawnManager.txt:7754`. **Verified**
- Caller context: invoked from `SpawnManager.SendPlayerDataServerRpc(...)` (`SpawnManager.txt:3291`, call at `:4053`); when it returns `true`, the branch at `:4055` (`JumpIfNotEqual {541}`) skips the entire spawn sequence. **Verified**
- **What happens to the offender (all quoted strings exact):**
  1. Server log, `Debug.LogError` of `String.Concat("[SpawnManager] Refused join for '", name, "' (hash ", hash, "): ", reason)` — built as a 6-part concat (`"[SpawnManager] Refused join for '"` at `SpawnManager.txt:7765`, concat region `:7765-7840`). **Verified**
  2. Server-wide chat message: `<name>` + `" was rejected - modified character file"` via `ChatSystem.CreateServerNetworkMessage` (`SpawnManager.txt:7919`). **Verified**
  3. Disconnect: `LocalizationManager.Text(..., "UI_JOIN_CHARACTER_MODIFIED", "Server rejected modified character file", 0)` (`SpawnManager.txt:7933-7936`) produces the reason string handed to `SpawnManager.DisconnectClientDelayed(clientId, reason)` (started as a coroutine; wrapper header `SpawnManager.txt:8080`; body in `SpawnManager_NestedType__DisconnectClientDelayed_d__29.txt`: `WaitForSeconds` at `:258`, `NetworkManager.DisconnectClient(clientId, reason)` at `:276`). **Verified** (call/string lines); the `WaitForSeconds` duration is **Unknown** (Open questions).
  4. No ban is recorded — the offender is disconnected but not added to any ban list. **Verified** (no ban API in the path).
- Adjacent rejections in the same gate (**context only**, not `CharacterPlausibility`): `" was rejected - server only allows new or returning characters"` / `"Server only allows new or returning characters"` (`SpawnManager.txt:7510/7519`), `" was rejected - this character is already in the world"` / `"This character is already in the world"` (`:7687/7697`), version mismatch `" tried to join with version "` / `"Server is running a different version ("` (`:8016/8035`). **Verified** as existing strings.

## Evidence appendix

| Claim | Evidence |
|---|---|
| API signatures, visibility, `RejectImplausibleJoins = true` | `ilrecovery/CharacterPlausibility.cs:3-41`, const at `:5` |
| Cap constants `37` / `84` / `126`, check order level→skill→attribute, strict `>` | `CharacterPlausibility.txt:836, 838, 840`; boxed at `:881, :866, :851` |
| Reason strings (3 exact formats + concat) | `CharacterPlausibility.txt:883, 898, 906, 868, 853` |
| `max(level, highestLevel)` | `CharacterPlausibility.txt:831-835` |
| `reason = null` / `false` return | `CharacterPlausibility.txt:829-830, 842-843` |
| Filter keeps nulls/null-`playerData`, drops with log, hide-only | `CharacterPlausibility.txt:224-225, 239-242, 315-343`; `"<unnamed>"` `:320`; prefix `:323`; `Debug.Log` `:333` |
| Identity check runs before caps | `CharacterPlausibility.txt:247` (Filter), `:500` (IsImplausible) |
| SumPositive semantics (null → -1, sum `> 0`, double indexer) | `CharacterPlausibility.txt:923-1019`; inlined `:261-281, 518-541` |
| SumAll semantics (sum all, null → -1) | `CharacterPlausibility.txt:1020-1104, 1105+`; inlined `:282-304, 542-566, 654-671` |
| `PlayerData` offsets `+0x10/+0x64/+0x68/+0xD0/+0x328` | `CharacterPlausibility.txt:316, 252, 251, 283, 253` (Filter) and `:509-511, 543` (IsImplausible) |
| `NetworkPlayerData` offsets `+0x4C/+0x50/+0x70/+0xAC`, sign-extension | `CharacterPlausibility.txt:674, 673, 654, 672` |
| `NetworkPlayerSkillPointsSpent` clamp | `NetworkPlayerData.txt:2000, 2011-2013` |
| Struct passed by address to `PlayerCannotJoinServer` | `SpawnManager.txt:4046` (`LoadAddress`), call `:4053` |
| Skip-spawn on rejection | `SpawnManager.txt:4055` (`JumpIfNotEqual {541}`) |
| Rejection log/chat/localize strings + delayed disconnect | `SpawnManager.txt:7754, 7765-7840, 7919, 7933-7936, 8080`; `SpawnManager_NestedType__DisconnectClientDelayed_d__29.txt:258, 276` |
| Character-list filter call sites + field stores | `CharacterSelection.txt:2824, 2919, 2942-2947, 3855, 4557-4562` |
| No ban integration | exhaustive `Call BanListManager\.|BanListData\.` sweep of IsilDump (24 hits, none in `CharacterPlausibility`, `CharacterSelection`, or the `SpawnManager` rejection path) |

## Open questions

1. **`RejectImplausibleJoins` (`= true`) usage** — a `const bool`; no read site survives in the IL dump (compile-time inlining). Whether it gates `PlayerCannotJoinServer` or is dead is **Unknown**. `ilrecovery/CharacterPlausibility.cs:5`.
2. **`PlayerData` value-field layout** — in the `NetworkPlayerData..ctor` copy chain exactly one of `characterThirst` / `characterHunger` / `characterAlcohol` occupies 8 bytes despite the `float` signature. Which field (and whether it is a mis-typed recovery of a `double`/two floats) is **Unknown**. All offsets used by this system are unaffected (**Verified** separately).
3. **Field name at `PlayerData +0x328`** — **Inferred** `characterSkillTreePointsInvested`; the dump only proves it is a `List<int>` summed with `SumPositive` semantics (`CharacterPlausibility.txt:253, 511`).
4. **`DisconnectClientDelayed` delay** — `WaitForSeconds` bound to a constant at `[0x18465DB90]`; the numeric value is **Unknown** (`SpawnManager_NestedType__DisconnectClientDelayed_d__29.txt:258`).
5. **Standalone `SumAll(List<int>)` null path** — presumed `-1` to match all three inlined instances; the standalone body's exact null branch is **Inferred**.
6. **Post-hoc reachability of hidden saves** — whether a `FilterImplausible`-hidden character can still enter a world through a path that does not pass the character-selection screens is **Unknown** (out of the traced call graph); the server-side gate covers the join path.
7. **`0x1820642D0` / `0x181F80C30` bindings** — identified as `List<int>.get_Item` / `List<PlayerSaveFile>.get_Item` from call shape; the dump leaves them unlabeled. **Inferred**.
