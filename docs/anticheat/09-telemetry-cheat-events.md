# 09 — Telemetry cheat events (`GameAnalyticsManager` cheat flags)

Scope: the cheat-telemetry chain inside `GameAnalyticsManager` — how an anti-cheat integrity violation becomes a
`"cheat_attempted"` analytics row and a `cheating = true` session flag in Supabase, what identity data rides along,
and the query/patch surface (`FetchCheatEvents`, `FetchFlaggedSessions`, `PatchIdentityFields`).

Sources (line citations):
- `IsilDump/Assembly-CSharp/GameAnalyticsManager.txt` (abbrev. `GAM.txt`) and `GameAnalyticsManager_NestedType__*.txt`
  — real bodies (x64 + ISIL pseudo-IL).
- `DecompilerTool/ilrecovery_out/GameAnalyticsManager.cs`, `ActivityEvent.cs` (contains the state-machine stubs),
  `GameConfig.cs`, `Player.cs` — signatures/field declarations (bodies `throw null`).

Claims are tagged **Verified** / **Inferred** / **Unknown** throughout.

---

## Purpose & threat model

**Purpose.** `GameAnalyticsManager` is a client-side analytics pipeline that POSTs session and event rows to a
**Supabase REST** project. The anti-cheat integration is thin but decisive:

1. `RegisterCheatListener()` hooks `ObfuscatedNetworkInt.OnIntegrityViolation` — the signal raised by the obfuscated
   stat system (see doc 03/04) when a protected value's integrity hash fails.
2. `OnCheatDetected` → `CheatAttempted(violationType)` records a `"cheat_attempted"` event carrying a full stat snapshot.
3. `FlagSessionAsCheating()` PATCHes the current `game_sessions` row to `cheating = true`.
4. `QueryCheatEvents` / `QueryFlaggedSessions` are pull-side tools that read the flags back out.

**Threat model.**
- **Client-authoritative flagging.** Both the event and the flag are written by the *client that cheated* using the
  **public Supabase anon key embedded in the binary** (`ilrecovery_out/GameAnalyticsManager.cs:980–982`). A cheater can
  suppress the flag (kill the coroutine, block the request, patch `GameConfig.AnalyticsEnabled`) or forge flags for
  other sessions subject to the project's Row Level Security (RLS) configuration, which is **not visible in the binary**.
- **PII/identity exposure.** Character name, character id/hash, derived user id, level/currency snapshots and playtime
  are uploaded per event. No raw Steam ID string appears in the cheat payloads (the world id is hashed to 8 hex
  digits); what `_customUserId` contains is derived from an auth `User` object (`GAM.txt` InitializeWhenReady path).
- **Value.** The pipeline is a *detection aid*, not enforcement: nothing in it bans, kicks, or reverts.

---

## API surface

| Signature | Visibility | Role |
|---|---|---|
| `static void RegisterCheatListener()` | `public static` | subscribes `OnCheatDetected` to `ObfuscatedNetworkInt.OnIntegrityViolation` (once) |
| `static void OnCheatDetected(string violationMessage)` | `static` | increments violation counter, forwards to `CheatAttempted` |
| `static void CheatAttempted(string violationType)` | `public static` | sends `"cheat_attempted"` real-time event + starts `FlagSessionAsCheating` coroutine |
| `IEnumerator FlagSessionAsCheating()` | `private` ([IteratorStateMachine `<FlagSessionAsCheating>d__192`]) | PATCH `game_sessions` → `cheating=true` |
| `static void QueryCheatEvents(Action<string> onResult, int limit = 50)` | `public static` | starts `FetchCheatEvents` on the manager instance |
| `IEnumerator FetchCheatEvents(Action<string> onResult, int limit)` | `private` (`<FetchCheatEvents>d__194`) | GET `analytics_events?event_type=eq.cheat_attempted…` |
| `static void QueryFlaggedSessions(Action<string> onResult, int limit = 50)` | `public static` | starts `FetchFlaggedSessions` |
| `IEnumerator FetchFlaggedSessions(Action<string> onResult, int limit)` | `private` (`<FetchFlaggedSessions>d__196`) | GET `game_sessions?cheating=eq.true…` |
| `IEnumerator PatchIdentityFields()` | `private` (`<PatchIdentityFields>d__135`) | PATCH `game_sessions` identity columns once known |
| `IEnumerator SendRealTimeEvent(string eventType, object eventData)` | `private` (`<SendRealTimeEvent>d__138`) | POST `analytics_events` row (transport for `cheat_attempted`) |
| `static int? GetCharacterPlaytime()` | `static` | playtime (minutes/seconds — see Open questions) included in payloads |

Visibility per `ilrecovery_out/GameAnalyticsManager.cs:1265–1552` and `ActivityEvent.cs:390–775`.

---

## Data model

### Backend constants (Verified, exact strings)

```
SUPABASE_URL      = "https://lkbgbfbdoittqjygjpjz.supabase.co"            (GameAnalyticsManager.cs:980)
SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxrYmdiZmJkb2l0dHFqeWdqcGp6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODA1Mjg1NDUsImV4cCI6MjA5NjEwNDU0NX0.S6-L7wBMElYj9bA3Wczn5iQrGOOKlbGvj41CfELmELI"
                                                                                     (GameAnalyticsManager.cs:982)
```
The same JWT is sent both as `apikey: <jwt>` and `Authorization: Bearer <jwt>` on every request
(e.g. `FlagSessionAsCheating_d__192.txt:584–593`). JWT claims decode to `role: anon`, `ref: lkbgbfbdoittqjygjpjz`
(Verified from the literal).

Endpoints used by the cheat surface (exact strings):

| Use | Method | URL |
|---|---|---|
| Flag session | `PATCH` | `"https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/game_sessions?session_id=eq." + sessionId` (`FlagSessionAsCheating_d__192.txt:513–514`) |
| Send cheat event | `POST` | `"https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/analytics_events"` (`SendRealTimeEvent_d__138.txt:831`) |
| Fetch cheat events | `GET` | `"https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/analytics_events?event_type=eq.cheat_attempted&select=timestamp_utc,player_id,character_id,player_level,event_data&order=timestamp_utc.desc"` + `String.Format("&limit={0}", limit)` (`FetchCheatEvents_d__194.txt:366–371`) |
| Fetch flagged sessions | `GET` | `"https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/game_sessions?cheating=eq.true&select=user_id,session_id,character_id,player_level,session_start,game_version&order=session_start.desc"` (`FetchFlaggedSessions_d__196.txt:370`) |
| Patch identity | `PATCH` | same `game_sessions?session_id=eq.` pattern (`PatchIdentityFields_d__135.txt:784–785`) |
| Session create (context) | `POST` | `"https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/game_sessions"` (`CreateInitialSessionRecord_d__131.txt:1073`) |

Headers on all of the above (Verified): `Content-Type: application/json` (write paths),
`apikey: <jwt>`, `Authorization: Bearer <jwt>`, `Prefer: return=minimal` (PATCH/POST paths)
(`FlagSessionAsCheating_d__192.txt:576–601`, `PatchIdentityFields_d__135.txt:847–872`,
`SendRealTimeEvent_d__138.txt:886–911`, `FetchCheatEvents_d__194.txt:388–398`).

### Event names (Verified, exact strings)

- `"cheat_attempted"` — the only cheat event name; used both as `event_type` value
  (`GAM.txt:17785`) and as the `event_type=eq.cheat_attempted` filter (`FetchCheatEvents_d__194.txt:370`).

### GameAnalyticsManager static layout (statics block @ `typeof+184`)

| Offset | Field (declaration `GameAnalyticsManager.cs:984–1200`) | Tag |
|---|---|---|
| 0 | `bool _initialized` (gate in `CheatAttempted`; set in `InitializeWhenReady`, `InitializeWhenReady_d__130.txt:545`) | Verified usage / Inferred name |
| 8 | `GameAnalyticsManager _instance` (MonoBehaviour that hosts coroutines; null ⇒ `"[AntiCheat] GameAnalyticsManager not found."`) | Verified |
| 24 | `string _earlyControlType` | Inferred |
| 32 | `string _customUserId` (uploaded as `player_id`) | Inferred name / Verified role |
| 40 | `string _sessionId` (URL key for `session_id=eq.`) | Verified role |
| 48 | `bool _sessionRecordCreated` | Inferred |
| 72 | `bool _cheatListenerRegistered` (`RegisterCheatListener` guard, `GAM.txt:17226`) | Verified usage |
| 76 | `int _cheatViolationCount` (incremented in `OnCheatDetected`, `GAM.txt:17275–17277`) | Verified usage |

### GameAnalyticsManager instance identity fields (verified offsets)

Anchors verified via writes in `TryCaptureGlobalFields` (`GAM.txt:3242–3528`) and `SetIsHost` (`GAM.txt:9885`):

| Offset | Field | Source value |
|---|---|---|
| 288 | `bool _wasHost` | `NetworkManager.get_IsHost()` (`GAM.txt:3398–3399`) |
| 336 | `string _worldId` | `GetHashCode().ToString("X8")` of world-id string (`GAM.txt:3509–3518`) |
| 344 | `string _characterId` | `Player.Hash` (`NetworkVariable<FixedString64Bytes>` @896, `GAM.txt:3270–3288`) |
| 352 | `bool _isWorldOwner` | string-equality over a `WorldSelection` id list (`GAM.txt:3400–3453`) |
| 376 | `string _raceId` | `Enum.ToString(typeof(Race), …)` (`GAM.txt:3319–3332`) |
| 384 | `string _classId` | `Enum.ToString(typeof(Class), …)` (`GAM.txt:3336–3349`) |
| 392 | `string _archetypeId` | `raceId + "_" + classId` (`GAM.txt:3353–3358`, literal `"_"` at `:3355`) |
| 416 | `string _characterName` | `Player.Name` (`NetworkVariable<FixedString64Bytes>` @888, `GAM.txt:3292–3310`) |
| 460 | `bool _hasUnflushedData` | set `1` with capture flag (`GAM.txt:3528`) |
| 616 | `bool _globalFieldsCaptured` | one-shot guard (`GAM.txt:3242, 3527`) |

Player field offsets referenced (Verified): `Level` @184, `Gold` @992, `XP` @1032, `AccumulatedXP` @1040,
`AllTimeXP` @1048, `SkillPoints` @1056 (`ilrecovery_out/Player.cs:400–418`); identity `Name` @888 / `Hash` @896
(`Player.cs:374–376`).

### Cheat event payload (`cheat_attempted` `event_data`)

`CheatAttempted` serializes `<>f__AnonymousType19`11<String, Int32, String, String, Nullable<Int32>×7>`
(`GAM.txt:17755–17757`). Positional reconstruction of the constructor call (`GAM.txt:17758–17774`, stack args
`:17762–17771`):

| # | Value | Source | Tag |
|---|---|---|---|
| 1 | `violationType` | `OnCheatDetected` message argument | Verified |
| 2 | violation count | statics+76 `_cheatViolationCount` (`GAM.txt:17749–17753`) | Verified |
| 3 | character name | `Player.Name` (offset 888) → string (`GAM.txt:17641–17658`) | Verified |
| 4 | character id/hash | `Player.Hash` (offset 896) → string (`GAM.txt:17659–17677`) | Verified |
| 5 | XP | `ObfuscatedNetworkInt.get_Value` @1032 (`GAM.txt:17678–17687`) | Verified |
| 6 | AccumulatedXP | @1040 (`GAM.txt:17688–17696`) | Verified |
| 7 | AllTimeXP | @1048 (`GAM.txt:17697–17705`) | Verified |
| 8 | Level | @184 (`GAM.txt:17706–17714`) | Verified |
| 9 | Gold | @992 (`GAM.txt:17715–17723`) | Verified |
| 10 | SkillPoints | @1056 (`GAM.txt:17724–17732`) | Verified |
| 11 | playtime | `GameAnalyticsManager.GetCharacterPlaytime()` (`GAM.txt:17754`) | Verified |

This object is the `eventData` argument of `SendRealTimeEvent("cheat_attempted", …)` (`GAM.txt:17785–17787`).

### Real-time event row (`analytics_events` POST body)

`SendRealTimeEvent` serializes `<>f__AnonymousType8`14<String×8, Nullable<Boolean>×2, Nullable<Int32>×2, String, Object>`
with `JsonSerializerSettings { NullValueHandling = Ignore }` (`SendRealTimeEvent_d__138.txt:778, 804–824`).
Positional reconstruction (`:781–803`):

| # | Value | Source | Tag |
|---|---|---|---|
| 1 | playtest id | `GameConfig.PlaytestId` (statics+72, `GameConfig.cs:31`) | Verified value / Inferred name |
| 2 | game version | `GameConfig.CurrentVersion` (statics+16, `GameConfig.cs:17`) | Verified value / Inferred name |
| 3 | timestamp | `DateTime.UtcNow.ToString("o")` (`:682–690`) | Verified |
| 4 | world id | instance+336 `_worldId` (`:693`) | Verified |
| 5 | user id | statics+32 `_customUserId` (`:700–702`) | Verified |
| 6 | character id | instance+344 (`:703–704`) | Verified |
| 7 | character name | instance+416 (`:706–711`) | Verified |
| 8 | session id | statics+40 `_sessionId` (`:723`) | Verified |
| 9 | is-world-owner flag | `Nullable<bool>` from instance+352 (`:724–733`) | Verified |
| 10 | was-host flag | `Nullable<bool>` from instance+288 (`:736–745`) | Verified |
| 11 | player level | `Player` Level @184 (`:754–764`) | Verified |
| 12 | playtime | `GetCharacterPlaytime()` (`:771–773`) | Verified |
| 13 | event type | `eventType` parameter (`:775`) | Verified |
| 14 | event data | `eventData` parameter (`:776–777`) | Verified |

JSON property names are the anonymous-type property names and are **not recoverable** from the dump (**Unknown**);
column names visible in the read queries are `timestamp_utc, player_id, character_id, player_level, event_data`,
`event_type` (`FetchCheatEvents_d__194.txt:370`) and `user_id, session_id, character_id, player_level, session_start,
game_version, cheating` (`FetchFlaggedSessions_d__196.txt:370`).

### Steam ID?

**No raw Steam ID is sent on the cheat path** (Verified absence in the reconstructed payloads). The world id is a
32-bit hash: `worldIdString.GetHashCode().ToString("X8")` where the source string is the `WorldSelection` world id,
falling back to a `Steam` instance string field @80 (declared among `CurrentLobby/SteamID/LobbyType/LobbyCount/
SteamHostName/SteamHostWorld/SteamWorldAge/ServerNote/Password`, exact name at 80 **Unknown**, `ilrecovery_out/Steam.cs:331–349`)
or `PlayerPrefs.GetString(Tags value 35)` (`GAM.txt:3455–3518`). `_customUserId` is built from an auth `User` object
(`User.get_displayName` → `_discordDisplayName`, `InitializeWhenReady_d__130.txt:516–520`) and an id string possibly
suffixed with `"_" + <second id>` (`:521–542`) — provenance **Inferred**.

---

## Behavior per method

### `RegisterCheatListener()` — `GAM.txt:17128–17228` (Verified)
Guards on statics+72 (`_cheatListenerRegistered`) `== 0`; creates `Action<string>` targeting `OnCheatDetected`
(`:17203–17210`), calls `ObfuscatedNetworkInt.add_OnIntegrityViolation` (`:17217`), then sets statics+72 = 1 (`:17226`).
No `AnalyticsEnabled` check here — registration happens regardless; the gate is enforced downstream.

### `OnCheatDetected(string violationMessage)` — `GAM.txt:17230–17283` (Verified)
Increments statics+76 `_cheatViolationCount` (`:17275–17277`), then tail-calls `CheatAttempted(violationMessage)`
(`:17282`).

### `CheatAttempted(string violationType)` — `GAM.txt:17285–17815` (Verified)
1. Gate: statics+0 `_initialized` must be non-zero, else return (`:17592–17594`).
2. Gate: statics+8 `_instance` must be non-null `UnityEngine.Object` (`:17602–17612`).
3. Optional player snapshot: `DataStorage` → Player (`:17620–17638`); if present, reads `Name`/`Hash` FixedStrings
   (`:17641–17677`) and six `ObfuscatedNetworkInt.get_Value` reads — XP/AccumulatedXP/AllTimeXP/Level/Gold/SkillPoints
   (`:17678–17732`).
4. Builds the 11-field anonymous payload (see Data model) including `_cheatViolationCount` (`:17749–17753`) and
   `GetCharacterPlaytime()` (`:17754`).
5. `SendRealTimeEvent("cheat_attempted", payload)` (`:17785–17787`) and `StartCoroutine(FlagSessionAsCheating())`
   (`:17797–17807`).

### `FlagSessionAsCheating()` / `<FlagSessionAsCheating>d__192.MoveNext` — `GAM.txt:17817–17859`,
`FlagSessionAsCheating_d__192.txt:111–706` (Verified)
1. Gate: `GameConfig.AnalyticsEnabled` (GameConfig statics+56) non-zero else nothing (`FlagSessionAsCheating_d__192.txt:474–477`).
2. Gate: statics+40 `_sessionId` non-empty (`:483–488`).
3. Body: `new <>f__AnonymousType20<bool>(true)` (`:489–495`, `Move rdx, 1` at `:493`) →
   `JsonConvert.SerializeObject(...)` (`:496–502`). Property name **Unknown**; the `game_sessions` column it flips is
   `cheating` (Inferred from `FetchFlaggedSessions` filter `cheating=eq.true`). Serialized body is therefore
   `{"cheating":true}` **(Inferred)**.
4. `PATCH "https://lkbgbfbdoittqjygjpjz.supabase.co/rest/v1/game_sessions?session_id=eq." + _sessionId`
   (`:510–523`) with UTF-8 `UploadHandlerRaw` (`:532–550`), `DownloadHandlerBuffer` (`:558–570`), headers
   `Content-Type` (`:576–578`), `apikey` (`:584–585`), `Authorization` (`:592–593`), `Prefer: return=minimal`
   (`:600–601`); `SendWebRequest` (`:608`).
5. On `UnityWebRequest.get_result != Success`: `Debug.Log`-style message
   `"[AntiCheat] Failed to flag session as cheating: " + get_error()` (`:634–646`).

### `QueryCheatEvents(Action<string>, int limit = 50)` — `GAM.txt:17861–18022` (Verified)
If statics+8 `_instance == null` (destroyed/absent), invokes `onResult("[AntiCheat] GameAnalyticsManager not found.")`
(`:18010–18021`, string at `:18013`). Otherwise constructs `<FetchCheatEvents>d__194`, stores `onResult` at state-machine
offset 32 (GC write barrier `:17994–17997`) and `limit` at offset 40 (`:17999`), and `StartCoroutine`s it (`:18002`).

### `FetchCheatEvents(Action<string>, int)` / `<FetchCheatEvents>d__194.MoveNext` — `GAM.txt:18024–18093`,
`FetchCheatEvents_d__194.txt:111–513` (Verified)
1. Gate: `GameConfig.AnalyticsEnabled` — if off, `onResult("[AntiCheat] Analytics disabled (GameConfig.AnalyticsEnabled).")`
   (`:349–358`, string `:420`) and stop.
2. `GET` on the base query + `String.Format("&limit={0}", limit)` (`:359–371`) via `UnityWebRequest.Get` (`:374`).
3. Headers `apikey` / `Authorization: Bearer …` (`:388–398`); `SendWebRequest` (`:404`).
4. On success (`get_result == 1`, `:434–438`): `onResult(<download handler text>)` — the raw Supabase JSON array with
   `timestamp_utc, player_id, character_id, player_level, event_data`, newest first (`:453–466`).
5. On failure: `onResult("[AntiCheat] Query failed: " + get_error())` (`:445–449`).

**What `FetchCheatEvents` retrieves:** every `analytics_events` row whose `event_type` is exactly `"cheat_attempted"`,
ordered `timestamp_utc.desc`, capped at `limit` (default 50). `event_data` carries the full stat snapshot listed above.

### `QueryFlaggedSessions` / `FetchFlaggedSessions` — `GAM.txt:18096–18329`,
`FetchFlaggedSessions_d__196.txt` (Verified)
Identical wrapper/coroutine pattern; GET selects `user_id, session_id, character_id, player_level, session_start,
game_version` where `cheating=eq.true` (`FetchFlaggedSessions_d__196.txt:370`); same error strings
(`GAM.txt:18248`). No in-dump callers of either query method (the `EventsManager_NestedType___c.txt:740` hit is the
IL2CPP shared `IDisposable.Dispose` thunk table, **not** a caller — Verified).

### `PatchIdentityFields()` / `<PatchIdentityFields>d__135.MoveNext` — `GAM.txt:3646–3708`,
`PatchIdentityFields_d__135.txt:111–986` (Verified)
Purpose: after login/character selection fills the identity fields, PATCH them onto the `game_sessions` row so the
session record is attributable.

1. Gate: `GameConfig.AnalyticsEnabled` (`:618–626`); requires `<>4__this` (state-machine field @32, `ActivityEvent.cs:719`).
2. Reads 7 strings, each guarded by `String.IsNullOrEmpty` (only non-empty values are passed):
   `_raceId` @376 (`:630–638`), `_classId` @384 (`:640–648`), `_archetypeId` @392 (`:650–657`),
   `_earlyControlType` (statics+24, `:663–680`), `_characterId` @344 (`:682–688`), `_characterName` @416
   (`:690–696`), `_worldId` @336 (`:698–704`).
3. Reads 3 nullable bools (`true` when the source byte ≠ 0, else `null`):
   `_isWorldOwner` @352 (`:705–714`), `_wasHost` @288 (`:715–724`), and **`_wasHost` @288 a second time** (`:725–734`)
   — the same field is read twice for two separate payload slots (Verified).
4. Builds `<>f__AnonymousType7`10<String×7, Nullable<Boolean>×3>` in the order
   `(_raceId, _classId, _archetypeId, _earlyControlType, _characterId, _characterName, _worldId, _isWorldOwner?, _wasHost?, _wasHost?)`
   (`:735–752`; positional — property names **Unknown**).
5. `JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = Ignore })` (`:753–773`)
   — null/false-gated fields are omitted from the PATCH body.
6. `PATCH game_sessions?session_id=eq.{_sessionId}` (`:784–794`) with the standard 4 headers (`:847–872`).
7. On failure: `"[Analytics] Identity fields patch failed: " + error` (`:913`).

### Supporting: `InitializeWhenReady` tail — `InitializeWhenReady_d__130.txt:510–556` (Verified)
Sets `_discordDisplayName` from `User.get_displayName` (`:516–520`), builds `_customUserId` = `id [+ "_" + suffix]`
(`:521–542`), sets `_initialized = 1` (`:545`), starts `CreateInitialSessionRecord` (`:550`), then calls
`RegisterCheatListener()` (`:556`). This is the single registration point for the cheat listener in the dump.

---

## Reachability & gating

- **Registration:** exactly one call site — `InitializeWhenReady` (`InitializeWhenReady_d__130.txt:556`). Registered
  regardless of `AnalyticsEnabled`; the event handler chain is what gets gated.
- **Trigger chain (Verified):**
  `ObfuscatedNetworkInt.OnIntegrityViolation` (raised by the integrity system on tamper — see docs 03/04)
  → `OnCheatDetected` (`GAM.txt:17230`) → `CheatAttempted` (`:17282`) → `SendRealTimeEvent("cheat_attempted")`
  (`:17785–17787`) + `StartCoroutine(FlagSessionAsCheating())` (`:17797–17807`).
  `QACheat` subscribes to the same event for its logging (`QACheat.txt:299`) and its tamper tests are the practical
  in-game way to fire this chain (doc 08).
- **Gates on the write path:** `_initialized` (statics+0) in `CheatAttempted` (`GAM.txt:17592–17594`);
  `GameConfig.AnalyticsEnabled` in `FlagSessionAsCheating` (`FlagSessionAsCheating_d__192.txt:474–477`), in
  `SendRealTimeEvent` (`SendRealTimeEvent_d__138.txt:648–656`) and in `PatchIdentityFields` (`:618–626`);
  `_sessionId` non-empty for the PATCH/POST URL. All gates are **client-side** (Verified).
- **Gates on the read path:** `GameConfig.AnalyticsEnabled` + `_instance` non-null (`FetchCheatEvents_d__194.txt:349–358`,
  `GAM.txt:18010–18021`).
- **Who can trigger the queries:** `QueryCheatEvents`/`QueryFlaggedSessions` are `public static` with **no callers
  inside Assembly-CSharp** (Verified). They are presumably driven by developer tooling or absent UI; external callers
  are **Unknown**. In release builds a player cannot invoke them without injected code — but anyone with the anon key
  can issue the same GETs directly against Supabase, subject to RLS (**Unknown**).
- **Release-build status:** no `Debug.isDebugBuild`/AppId gating anywhere in this chain; the telemetry runs in whatever
  build `GameConfig.AnalyticsEnabled` allows. Playtest/demo discrimination appears only as payload values
  (`GameConfig.PlaytestId`), not as gating (Inferred).

---

## Evidence appendix

Path conventions: `IsilDump/Assembly-CSharp/<file>:<line>` under `modding/cpp2il_isil_out/`, and
`ilrecovery_out/<file>:<line>` under `modding/DecompilerTool/`. Abbreviated `GAM.txt` = `GameAnalyticsManager.txt`.

| Claim | Evidence | Tag |
|---|---|---|
| Supabase URL + anon key constants | `ilrecovery_out/GameAnalyticsManager.cs:980–982` | Verified |
| JWT role `anon`, ref `lkbgbfbdoittqjygjpjz` | JWT literal, `FlagSessionAsCheating_d__192.txt:584` | Verified |
| `RegisterCheatListener` hooks `ObfuscatedNetworkInt.add_OnIntegrityViolation` | `GAM.txt:17217` | Verified |
| Registered-flag statics+72 | `GAM.txt:17226` | Verified |
| `OnCheatDetected` increments statics+76 then calls `CheatAttempted` | `GAM.txt:17275–17282` | Verified |
| `CheatAttempted` `_initialized` gate | `GAM.txt:17592–17594` | Verified |
| Stat snapshot reads (XP/Acc/AllTime/Level/Gold/SkillPoints) | `GAM.txt:17678–17732` | Verified |
| `Name`/`Hash` identity reads (player 888/896) | `GAM.txt:17641–17677` | Verified |
| Event name `"cheat_attempted"` | `GAM.txt:17785` | Verified |
| 11-field anonymous payload construction | `GAM.txt:17755–17774` | Verified |
| `FlagSessionAsCheating` started from `CheatAttempted` | `GAM.txt:17797–17807` | Verified |
| Flag PATCH URL + `PATCH` verb | `FlagSessionAsCheating_d__192.txt:513–523` | Verified |
| Flag body `AnonymousType20<bool>(true)` serialized | `FlagSessionAsCheating_d__192.txt:489–502` | Verified |
| JSON property `cheating` | inferred from `cheating=eq.true` filter | Inferred |
| `GameConfig.AnalyticsEnabled` gate (fetch/patch/send/flag) | `FetchCheatEvents_d__194.txt:349–358`; `FlagSessionAsCheating_d__192.txt:474–477`; `PatchIdentityFields_d__135.txt:618–626`; `SendRealTimeEvent_d__138.txt:648–656` | Verified |
| `FetchCheatEvents` URL + select + order + limit format | `FetchCheatEvents_d__194.txt:366–371` | Verified |
| `FetchCheatEvents` result passthrough / error strings | `FetchCheatEvents_d__194.txt:420, 445–449, 453–466` | Verified |
| `FetchFlaggedSessions` URL + select | `FetchFlaggedSessions_d__196.txt:370` | Verified |
| `"[AntiCheat] GameAnalyticsManager not found."` fallback | `GAM.txt:18013, 18248` | Verified |
| `PatchIdentityFields` 7 strings + 3 nullable bools | `PatchIdentityFields_d__135.txt:630–752` | Verified |
| `_wasHost` read twice in patch payload | `PatchIdentityFields_d__135.txt:715–734` | Verified |
| NullValueHandling.Ignore serialization | `PatchIdentityFields_d__135.txt:753–773` | Verified |
| `"[Analytics] Identity fields patch failed: "` | `PatchIdentityFields_d__135.txt:913` | Verified |
| Instance field offsets 288/336/344/352/376/384/392/416/460/616 | `GAM.txt:3242–3528`, `GAM.txt:9885` | Verified |
| `_worldId = GetHashCode().ToString("X8")` of world id string | `GAM.txt:3509–3518` (literal `"X8"` at `:3515`) | Verified |
| World-id sources: `WorldSelection` id / `Steam` field @80 / `PlayerPrefs(Tags.35)` | `GAM.txt:3400–3518` | Verified flow / Unknown field name |
| `SendRealTimeEvent` 14-field row + `NullValueHandling.Ignore` | `SendRealTimeEvent_d__138.txt:778–824` | Verified |
| `analytics_events` POST + headers | `SendRealTimeEvent_d__138.txt:830–831, 886–911` | Verified |
| `UtcNow.ToString("o")` timestamp | `SendRealTimeEvent_d__138.txt:682–690` | Verified |
| `RegisterCheatListener` call site = `InitializeWhenReady` | `InitializeWhenReady_d__130.txt:556` | Verified |
| `_customUserId` construction | `InitializeWhenReady_d__130.txt:521–542` | Verified flow / Inferred semantics |
| No in-dump callers of `QueryCheatEvents`/`QueryFlaggedSessions` | grep over `IsilDump/Assembly-CSharp` (`EventsManager_NestedType___c.txt:740` is a dispose thunk) | Verified |
| `GameConfig` field identities (`CurrentVersion`, `AnalyticsEnabled`, `PlaytestId`) | `ilrecovery_out/GameConfig.cs:17,27,31` + offset anchors 16/56/72 | Verified |
| `Player.Name`/`Player.Hash` declaration | `ilrecovery_out/Player.cs:374–376` | Verified |

---

## Open questions

1. **JSON property names** of `<>f__AnonymousType7/8/19/20` are metadata-only and unrecoverable from the dump. Column
   mapping (`player_id`, `character_id`, `player_level`, `event_data`, `cheating`, `user_id`, `session_id`, …) is
   inferred from the read queries and is not proof of the write-side property names.
2. **`_customUserId` provenance** — derived from an auth `User` id with an optional `"_" + suffix`
   (`InitializeWhenReady_d__130.txt:521–542`); whether that id is a Steam ID, Discord id, or platform account id is
   not resolvable from this dump. The `Steam` instance string at offset 80 used as a world-id source is likewise
   unnamed in the dump (`Steam.cs:331–349` lists the candidates).
3. **`GetCharacterPlaytime()` units** (seconds vs minutes) — body at `GAM.txt:18439` returns `int?`; unit semantics
   not yet read out.
4. **RLS / server-side handling** of the `cheating` flag and `analytics_events` table is not in the binary — whether
   the anon key can write arbitrary rows/flag other players' sessions depends on the Supabase project configuration.
5. **What consumes the flags** — no ban/kick/review flow appears in Assembly-CSharp; enforcement is presumably
   external (dashboard or server-side job).
6. **Exact `violationType` strings** originate in `ObfuscatedNetworkInt` raise sites (docs 03/04); the telemetry layer
   passes them through verbatim.
