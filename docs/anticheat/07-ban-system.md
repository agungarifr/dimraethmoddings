# 07 — Ban System (BanListManager / BanListData / BanEntry)

Citations: `file:line` are relative to `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/` (the cpp2il ISIL dump) unless prefixed `ilrecovery` (`modding/DecompilerTool/ilrecovery_out/`). Every behavioral claim is marked **Verified** (directly readable in the dump), **Inferred** (recovered from structure/offsets/conventions), or **Unknown** (dump insufficient; see Open questions).

## Purpose & threat model

The ban system is the host's **Steam-ID-keyed** exclusion list, persisted on the host machine and enforced at the two entry points a client can arrive through: Netcode connection approval (`CustomNetworkManager.ApproveConnection`) and Steam lobby join (`Steam.OnLobbyMemberJoined`). A banned player is refused with `"You are banned from this server"` and/or lobby-kicked; a host can ban and unban through the settings UI.

**Threat model.** The attacker is a disruptive or cheating remote player (e.g. the modified-character case documented in `05-character-plausibility.md` is only *disconnected*, never banned — bans here are host-initiated sanctions). Requirements: (a) a banned Steam ID cannot get back into the session across restarts → the list is persisted in an encrypted, MAC'd, atomically-written container (`SaveCodec`, `SaveFiles.AtomicWrite`); (b) a banned player cannot talk their way past approval → checks run server-side before any player object exists; (c) a banned player cannot unban themselves → the only `UnbanPlayer` call sites are host-side settings UI (**Verified** — exhaustive call-site sweep below).

**Trust boundaries and explicit limits:**
- The **host's machine is trusted**. The list lives in the host's `Application.persistentDataPath/Security/GlobalBanList.jrsf`; a banned player editing their *own* installation's file is out of scope (the authoritative copy is the host's). **Verified** (path), scope statement **Inferred**.
- There is **no central/global ban authority** — despite the `GlobalBanList` name, the file is per-installation and nothing in the traced code synchronizes it over the network. **Verified** (no network call in `BanListManager`/`SaveSystem` ban paths).
- No expiry, no appeal, no reason enforcement: `Reason` is stored and displayed, never compared. `BanDate` is informational. **Verified** (no read of `Reason`/`BanDate` outside construction/UI).
- Steam-ID resolution is trusted from the transport (`FacepunchTransport.GetSteamIdForClient`, a server-side id map). If resolution fails the ban cannot persist (explicit warning string, see call site 4). **Verified**.

## API surface

Class shapes from `ilrecovery/BanListManager.cs:6-86`, `ilrecovery/BanListData.cs:5-35`, `ilrecovery/BanEntry.cs:4-23` (**Verified**).

### `public class BanListManager : MonoBehaviour` (`ilrecovery/BanListManager.cs:6`)

| Signature | Visibility | Role |
|---|---|---|
| `static BanListManager Singleton` (field) | `public static` | Process-wide instance (`ilrecovery:8`); assigned in `Awake` (`BanListManager.txt:313`) |
| `BanListData BanList { get; }` | `public` | Exposes the in-memory list root (`ilrecovery:15-21`); body `BanListManager.txt:198` |
| `event Action OnBanListChanged { add; remove; }` | `public` | Change notification (`ilrecovery:23-35`); backing field `m_OnBanListChanged` at `+0x28` |
| `void Awake()` | `private` | Singleton bootstrap + initial load (`ilrecovery:37`) |
| `void LoadBanList()` | `public` | Reload from disk (`ilrecovery:42`) |
| `void SaveBanList()` | `public` | Persist + fire event (`ilrecovery:47`) |
| `bool IsBanned(ulong steamId)` | `public` | Enforcement query (`ilrecovery:52`) |
| `void BanPlayer(ulong steamId, string playerName, string reason = "")` | `public` | Add + persist + log (`ilrecovery:57`) |
| `bool UnbanPlayer(ulong steamId)` | `public` | Remove + persist + log (`ilrecovery:62`) |
| `int GetBanCount()` | `public` | UI counter (`ilrecovery:67`) |
| `List<BanEntry> GetAllBans()` | `public` | UI listing (`ilrecovery:72`) |
| `BanEntry GetBanEntry(ulong steamId)` | `public` | Entry lookup (`ilrecovery:77`) |
| `BanListManager()` | `public` | MonoBehaviour ctor (`ilrecovery:82`) |

### `public class BanListData` — `[Serializable]` (`ilrecovery/BanListData.cs:4-5`)

| Signature | Visibility | Role |
|---|---|---|
| `int Version` (field) | `public` | Container version; set `1` in ctor (`BanListData.txt:612`) |
| `List<BanEntry> BannedPlayers` (field) | `public` | The entries (`ilrecovery:9`) |
| `bool IsBanned(ulong steamId)` | `public` | `Exists` on `SteamId` (`ilrecovery:11`) |
| `void AddBan(ulong steamId, string playerName, string reason = "")` | `public` | Dedup + append (`ilrecovery:16`) |
| `bool RemoveBan(ulong steamId)` | `public` | `RemoveAll` + `count > 0` (`ilrecovery:21`) |
| `BanEntry GetBanEntry(ulong steamId)` | `public` | `Find` on `SteamId` (`ilrecovery:26`) |
| `BanListData()` | `public` | `Version = 1` (`ilrecovery:31`) |

### `public class BanEntry` — `[Serializable]` (`ilrecovery/BanEntry.cs:3-4`)

| Signature | Visibility | Role |
|---|---|---|
| `ulong SteamId` (field, `+0x10`) | `public` | Match key (`ilrecovery:6`) |
| `string PlayerName` (field, `+0x18`) | `public` | Display name (`ilrecovery:8`) |
| `string Reason` (field, `+0x20`) | `public` | Free-text sanction reason (`ilrecovery:10`) |
| `DateTime BanDate` (field, `+0x28`) | `public` | UTC stamp (`ilrecovery:12`) |
| `BanEntry()` | `public` | Base init only (`ilrecovery:14`; body `BanEntry.txt:3-11`) |
| `BanEntry(ulong steamId, string playerName, string reason = "")` | `public` | Stamps `BanDate` (`ilrecovery:19`; body `BanEntry.txt:14-93`) |

### Persistence helpers (supporting surface)

| Signature | Role |
|---|---|
| `SaveSystem.SaveBanList(BanListData banList)` (`SaveSystem.txt:5634`) | Atomic encrypted write |
| `SaveSystem.LoadBanList()` (`SaveSystem.txt:5754`) | Load with backup fallback |
| `static bool <LoadBanList>g__TryDeserializeBanList|43_0(String path, out BanListData list)` (`SaveSystem.txt:15239`) | Per-file decode attempt |
| `SaveFiles.BanListPaths()` (`SaveFiles.txt:1142`) | `(dst, tmp, bak1)` triple |
| `SaveFiles.AtomicWrite(String tmp, String dst, String bak1, Action<FileStream> write)` (`SaveFiles.txt:1391`) | Crash-safe replace with backup |
| `SaveCodec.EncryptAndSerialize<T>` (`SaveCodec.txt:938`) / `SaveCodec.DecryptAndDeserialize<T>` (`SaveCodec.txt:2002`) | AES + HMAC-SHA256 + GZip + JSON container |

## Data model

### In-memory layout (decimal offsets as printed by this dump)

**`BanEntry`** — field stores visible in `BanListData.AddBan`'s inline initialization and in `BanEntry..ctor(ulong,string,string)`:
- `+0x10 SteamId` (ulong), `+0x18 PlayerName` (string), `+0x20 Reason` (string), `+0x28 BanDate` (`DateTime` 8-byte value) — **Verified** (`BanListData.txt:303-317` writes all four; `BanEntry.txt:78-93` same sequence in the ctor).

**`BanListData`**:
- `+0x10 Version` (int) = `1` set in ctor (`BanListData.txt:612`) — **Verified**.
- `+0x18 BannedPlayers` (`List<BanEntry>`) — **Verified** (`BanListManager.txt:803`, `[rax+24]` deref chain in `GetBanCount`).

**`BanListManager`** (MonoBehaviour instance fields):
- `+0x20 _banList` (`BanListData`) — **Verified** (`BanListManager.txt:329, 802`).
- `+0x28 m_OnBanListChanged` (`Action`, printed `+40` decimal) — **Verified** (`BanListManager.txt:62, 156, 465`).
- `static Singleton` — assigned with write-barrier at `BanListManager.txt:313-316` — **Verified**.

### Persistence container (on-disk)

- **Path triple** from `SaveFiles.BanListPaths()` (`SaveFiles.txt:1142`): `dst` = `Path.Combine(Application.persistentDataPath, "Security")` + `"GlobalBanList.jrsf"` (dir string `"Security"` `SaveFiles.txt:1264`, file `"GlobalBanList.jrsf"` `:1273`), `tmp` = dst + `".tmp"` (`:1277`), `bak1` = dst + `".bak1"` (`:1282`). **Verified** (strings/lines); `Application.persistentDataPath` root **Inferred** from `get_SecurityDir` (`SaveFiles.txt:198-261`).
- **Write path** — `SaveSystem.SaveBanList(BanListData)` (`SaveSystem.txt:5634`) calls `SaveFiles.AtomicWrite(tmp, dst, bak1, write)` (`SaveFiles.txt:1391`) where `write` is the closure `SaveSystem+<>c__DisplayClass42_0.<SaveBanList>b__0(FileStream fs)` (`SaveSystem_NestedType___c__DisplayClass42_0.txt:14`) which tail-calls `SaveCodec.EncryptAndSerialize<BanListData>(fs, banList)` (`:68`; target `SaveCodec.txt:938` — address→name binding **Inferred**). **Verified** for the closure/arguments.
- **Read path** — `SaveSystem.LoadBanList()` (`SaveSystem.txt:5754`): try `dst` through `TryDeserializeBanList`; on failure `SafeDelete(dst)`, then try `bak1`; if `bak1` decodes, `SafeCopy` it over `dst`; if both fail, return `new BanListData()` (`SaveSystem.txt:5828-5899`, ctor at `:5880`). `TryDeserializeBanList` = `FileStream` + `SaveCodec.DecryptAndDeserialize<BanListData>` (`SaveCodec.txt:2002`). **Verified**.
- **Codec** — `SaveCodec` is *not* plain JSON: AES encryption + HMAC-SHA256 (HKDF sub-key info strings `"enc"`, `"mac"`, `"identity"`, identity info `"Dimraeth.CharacterIdentity.v1"`) + GZip + `Newtonsoft.Json.JsonConvert` for the inner serialization (`SaveCodec.txt:4332`, `JsonSerializerSettings` `:5659-5669`). **Verified** (session trace of `SaveCodec.txt`).
- **`Version`** is written (`1`) but no version check was observed in the traced load path; whether `TryDeserializeBanList` consults it is **Unknown** (Open questions).

## Behavior per method

### `BanListManager`

**`add_OnBanListChanged(Action value)`** — `BanListManager.txt:3`
- Standard thread-safe event add: loop { `Delegate.Combine(current, value)` (`:68`); CAS-compare against field `+0x28` via helper `0x1805EA350` (`:82`); retry while the field changed (`:82-84`) }. Type check that the combined delegate is `System.Action` before the store (`:71-75` region). **Verified** (structure); the CAS helper's BCL name (`Interlocked.CompareExchange`) **Inferred**.

**`remove_OnBanListChanged(Action value)`** — `BanListManager.txt:97`
- Same loop with `Delegate.Remove` (`:162`), CAS at `:176`. **Verified** (structure).

**`get_BanList()`** — `BanListManager.txt:191`
- `return _banList;` — single read of `+0x20` (`:198`). **Verified**

**`Awake()`** — `BanListManager.txt:201`
1. `if (Singleton != null)` (via `UnityEngine.Object.op_Equality`, `:291`) → `Object.Destroy(this.gameObject)` (`:308`) and return — duplicate-manager guard. **Verified**
2. else `Singleton = this` (`:313-316`, write barrier) and `_banList = SaveSystem.LoadBanList()` (`:327-335`). **Verified**

**`LoadBanList()`** — `BanListManager.txt:338`
- `_banList = SaveSystem.LoadBanList()` (`:377-384`). No event fired from this method. **Verified** (full body read)

**`SaveBanList()`** — `BanListManager.txt:387`
1. `if (_banList == null) _banList = new BanListData()` (`BanListData..ctor` call at `:452`). **Verified**
2. `SaveSystem.SaveBanList(_banList)` (`:464`). **Verified**
3. `m_OnBanListChanged?.Invoke()` — null-guarded delegate invoke over field `+0x28` (field read `:465`, invoke through delegate slot). **Verified** (asm read)

**`IsBanned(ulong steamId)`** — `BanListManager.txt:477`
- `_banList` non-null → delegates to `BanListData.IsBanned` (`:493`); `_banList` null → `false`. **Verified** (null branch visible as the zero-test before the call)

**`BanPlayer(ulong steamId, string playerName, string reason = "")`** — `BanListManager.txt:498`
1. `if (_banList == null) _banList = new BanListData()` (lazy create, `:599`). **Verified**
2. `_banList.AddBan(steamId, playerName, reason)` (`:611`). **Verified**
3. `SaveBanList()` (`:614`) → persist + `OnBanListChanged`. **Verified**
4. `Debug.Log(String.Format("[BanListManager] Banned player: {0} (Steam ID: {1})", playerName, steamId))` (`:619`). **Verified**
- Note: nothing kicks here; kicking is the caller's job (see call sites). Dedup: an existing Steam ID makes `AddBan` a no-op but this method still saves and logs. **Verified** (call sequence), log-after-no-op **Inferred**.

**`UnbanPlayer(ulong steamId)`** — `BanListManager.txt:644`
1. `_banList.RemoveBan(steamId)` (`:716`) → `bool`. **Verified**
2. `SaveBanList()` (`:721`) and `Debug.Log(String.Format("[BanListManager] Unbanned Steam ID: {0}", steamId))` (`:726`). **Verified** (presence of both calls in-method)
3. Returns `true` iff at least one entry was removed (the `RemoveBan` result). **Verified** (`setg` return path inherited from `RemoveBan`; manager-level return sequencing **Inferred**).

**`GetBanCount()`** — `BanListManager.txt:751`
- `_banList == null` (`:802-804`) or `BannedPlayers == null` (`:806-808`) → returns `0` (both paths shift the zeroed slot). **Verified**
- Else reads `List._size` (`:812`, `[rdx+24]` = `+0x18`) and passes it to helper `0x18247E770` (`:813`); the result is returned as `slot >> 32` (`:815`, `shr rax, 32`). The intended value is `BannedPlayers.Count` — **Inferred** (name, `_size` read, UI usage); the helper/transform is **Unknown** (Open questions).

**`GetAllBans()`** — `BanListManager.txt:826`
- `_banList`/`BannedPlayers` non-null → returns the **live** `BannedPlayers` reference (not a copy) (`:871-879`); otherwise `new List<BanEntry>()`. **Verified**
- Consequence: UI code holding the result can mutate the manager's in-memory list without persistence. **Inferred** (direct consequence of the reference return).

**`GetBanEntry(ulong steamId)`** — `BanListManager.txt:886`
- `_banList` non-null → `BanListData.GetBanEntry(steamId)` (`:902`); else `null`. **Verified**

**`.ctor()`** — `BanListManager.txt:907` — MonoBehaviour base construction only. **Inferred** (header only; standard pattern).

### `BanListData`

All three predicates share the same comparison closure shape: `b == null` → throw helper `0x1805E5490`; else `return b.SteamId [+0x10] == captured.steamId [+0x10]` (`BanListData_NestedType___c__DisplayClass2_0/4_0/5_0.txt:14`ff). The dump marks `sete al` as `NotImplemented` (disassembler gap); the `cmp`+`sete` sequence is the equality result. **Verified** (bodies read).

**`IsBanned(ulong steamId)`** — `BanListData.txt:3`
- `return BannedPlayers.Exists(b => b.SteamId == steamId)` — `List<BanEntry>.Exists` call `0x181F73560` at `:100`; predicate = `<IsBanned>b__0` (`BanListData_NestedType___c__DisplayClass2_0.txt:14`). **Verified** (call/closure); the `0x181F73560` = `Exists` binding **Inferred** (unlabeled generic instance).

**`AddBan(ulong steamId, string playerName, string reason = "")`** — `BanListData.txt:104`
1. Dedup: `if (BannedPlayers.Exists(<IsBanned>b__0>))` (`:287`) → **early return, the existing entry is not updated** (jump `:289` → return `:334`). **Verified**
2. Else `new BanEntry()` via the **parameterless** constructor (base init `:301`) with inline field initialization — not the 3-arg ctor: `SteamId = steamId` (`:303`), `PlayerName = playerName` (`:305`), `Reason = reason` (`:308`), `BanDate = DateTime.UtcNow` (`:316-317`). **Verified**
3. Appends with inlined `List<BanEntry>.Add`: `_version` increment (`:321`), direct slot store with write barrier (fast path `:346-354`), `List.AddWithResize` when full (`:333`). **Verified**
4. `BannedPlayers == null` → throws (null guard absent; jump to throw helper `:356` from `:282-283`). **Verified**

**`RemoveBan(ulong steamId)`** — `BanListData.txt:360`
- `return BannedPlayers.RemoveAll(<RemoveBan>b__0) > 0` — `List<BanEntry>.RemoveAll` at `:455`; **all** matches removed; return `setg` of `count > 0` (`:459`). **Verified**
- `BannedPlayers == null` → throws (`:450-451` → throw helper). **Verified**

**`GetBanEntry(ulong steamId)`** — `BanListData.txt:466`
- `return BannedPlayers.Find(<GetBanEntry>b__0)` — `List<BanEntry>.Find` call `0x181F746D0` at `:563`; first match or `null`. **Verified** (call/closure); `0x181F746D0` = `Find` binding **Inferred**.

**`.ctor()`** — `BanListData.txt:567`
- `Version = 1` (`:612`). **Verified**. `BannedPlayers` initialization (presumed `new List<BanEntry>()`) **Inferred** — not separately traced.

### `BanEntry`

**`.ctor()`** — `BanEntry.txt:3` — base object init only (`:11`); all fields default. **Verified**

**`.ctor(ulong steamId, string playerName, string reason = "")`** — `BanEntry.txt:14`
- Base init (`:78`), then `SteamId` / `PlayerName` (`:83`, write barrier) / `Reason` (`:87`, write barrier) stores, then `BanDate = DateTime.UtcNow` (`:93`). **Verified** (sequence `:78-93`)
- No caller of this constructor was found in the dump tree — `AddBan` initializes inline instead. Whether anything calls it is **Unknown** (Open questions).

## Enforcement call sites

Exhaustive sweep of `Call (BanListManager\.|BanListData\.|SaveSystem\.SaveBanList|SaveSystem\.LoadBanList)` across the IsilDump tree: 24 hits. Mutating call sites (`BanPlayer`/`UnbanPlayer`) are all host-side; **no network-reachable method mutates the ban list** (**Verified** by the sweep).

### 1. `CustomNetworkManager.ApproveConnection(NetworkManager+ConnectionApprovalRequest req, NetworkManager+ConnectionApprovalResponse resp)` — `CustomNetworkManager.txt:492` (enforcement)
- `BanListManager.Singleton.IsBanned(steamId)` at `CustomNetworkManager.txt:878` (steam id from the server-side map/`FacepunchTransport.GetSteamIdForClient`, `:815`). **Verified**
- If banned, and immediately: `resp+0x10 = 0` (Approved=false, `:883`), `resp+0x48 = "You are banned from this server"` (`:885-888`, exact string), `resp+0x11 = 0` (CreatePlayerObject=false, `:889`), `resp+0x40 = 0` (`:890`), then return. **Verified**
- **What happens to the offender:** connection refused **before any player object is created**; their client sees the reason `"You are banned from this server"`. **Verified**
- Context (non-ban path): a full lobby gets `LocalizationManager.Get(2, "UI_JOIN_LOBBY_FULL", "Server lobby is full", 0)` (`:1011-1018`) and log `"[CustomNetworkManager] Refusing client {0}: server full ({1}/{2})"` (`:968`); approval sets `resp+0x10 = 1` (`:1028`). All response paths zero `resp+0x40` (`:890` shared epilogue) — field name **Unknown**.

### 2. `Steam.OnLobbyMemberJoined(...)` — `Steam.txt:2443` (enforcement)
- `BanListManager.Singleton.IsBanned(steamId)` at `Steam.txt:2802` (steam id extracted from the `Friend` at `:2795-2796`). **Verified**
- If banned: `Debug.Log(String.Format("[Steam] Banned player {0} (Steam ID: {1}) attempted to join. Kicking...", name, steamId))` (`:2814-2826`) then `LobbyExtensionsV1.KickMemberV1(lobby, steamId, ...)` (`:2827-2836`) and return. **Verified**
- **What happens to the offender:** kicked from the Steam lobby at join; they never reach Netcode approval. Log string exact. **Verified**

### 3. `Steam.BanAndKickPlayer(UInt64 steamId, String playerName, String reason = "")` — `Steam.txt:3898` (mutation + enforcement)
- `BanListManager.Singleton == null` → `Debug.LogWarning("[Steam] BanListManager not available")` (`:4118`) and return — **no ban, no kick**. **Verified**
- Else `BanListManager.BanPlayer(steamId, playerName, reason)` (`:4064`), then `LobbyExtensionsV1.KickMemberV1` (`:4072-4084`) when a valid lobby exists, then `Debug.Log(String.Format("[Steam] Banned and kicked player: {0} (Steam ID: {1})", playerName, steamId))` (`:4089-4101`). **Verified**
- **What happens to the offender:** banned (persisted) and Steam-lobby-kicked. Log strings exact. **Verified**

### 4. `ServerList.BanPlayer(UInt64 ownerId, String playerName)` — `ServerList.txt:2234` (mutation + enforcement)
- `ownerId` is the Netcode client id; it is resolved to a Steam ID in order: server-side id map `TryGetValue` (`:2592-2598`), `CustomNetworkManager.GetSteamIdForClient(ownerId)` (static, `:2610`), `FacepunchTransport.GetSteamIdForClient` (`:2650`). **Verified**
- **Unresolved** → `Debug.LogWarning(String.Format("[ServerList] Could not resolve Steam ID for {0} (Client ID: {1}), player will be kicked but ban cannot persist without Steam ID", playerName, ownerId))` (`:2696-2708`) — ban skipped, but execution falls through to the disconnect block below. **Verified**
- **Resolved** → if the `Steam` manager exists: `Steam.BanAndKickPlayer(steamId, playerName, "")` (`:2719`); else `BanListManager.BanPlayer(steamId, playerName, "")` (`:2689`). **Verified**
- Disconnect block (always reached): guarded by `NetworkManager.Singleton != null`, `IsServer` (`:2765`) and `LocalClientId != ownerId` (`:2787-2789`, host cannot disconnect itself), then `NetworkManager.DisconnectClient(ownerId)` (`:2801`, no reason string) and `Debug.Log(String.Format("[ServerList] Banned and disconnected player: {0} (Client ID: {1}, Steam ID: {2})", playerName, ownerId, steamId))` (`:2811-2824`). **Verified** (lines/strings); `DisconnectClient` overload without reason **Inferred** (call lists `this` + client id only).
- **What happens to the offender:** banned (when an id was resolvable and a ban target existed) and disconnected from the Netcode session. When unresolvable: disconnected anyway, with the "ban cannot persist" warning server-side. **Verified**

### 5. `SettingsUI.OnUnbanClicked(UInt64 steamId)` — `SettingsUI.txt:23429` (mutation)
- `BanListManager.UnbanPlayer(steamId)` (`:23511`) then `SettingsUI.RefreshBanList()` (`:23514`). **Verified**
- **What happens to the offender:** sanction lifted — entry removed and list re-persisted. Host-initiated only. **Verified**

### 6. `SettingsUI+<>c__DisplayClass281_0.<CreateBanEntryRow>b__0()` — `SettingsUI_NestedType___c__DisplayClass281_0.txt:14` (mutation)
- Per-row unban button callback: `BanListManager.UnbanPlayer` (`:101`) then `SettingsUI.RefreshBanList` (`:104`). **Verified**
- Same effect as site 5. **Verified**

### 7. `SettingsPresenter.OnUnbanClicked(UInt64 steamId)` — `SettingsPresenter.txt:13475` (mutation)
- `BanListManager.UnbanPlayer` (`:13556`) then `SettingsPresenter.RefreshMultiplayer()` (`:13564`). **Verified**
- Same effect as site 5. **Verified**

### 8. Read-only UI call sites (no effect on the offender)
- `SettingsUI.RefreshBanList()` (`SettingsUI.txt:22525`): `BanListManager.GetAllBans` (`:22803`), `GetBanCount` (`:22878`). **Verified**
- `SettingsUI.UpdateBanCountText()` (`:23523`): `GetBanCount` (`:23637`). **Verified**
- `SettingsPresenter.RefreshMultiplayer()` (`:8506`): `BuildBanRows` (`:9330`), `GetBanCount` (`:9354`). **Verified**
- `SettingsPresenter.BuildBanRows()` → `BanRowState[]` (`:9766`): `GetAllBans` (`:9947`). **Verified**

### 9. Persistence call sites
- `BanListManager.Awake` → `SaveSystem.LoadBanList` (`BanListManager.txt:327`); `BanListManager.LoadBanList` → `SaveSystem.LoadBanList` (`:377`); `BanListManager.SaveBanList` → `SaveSystem.SaveBanList` (`:464`); `BanListManager.BanPlayer` → `SaveBanList` (`:614`); `BanListManager.UnbanPlayer` → `SaveBanList` (`:721`); `SaveSystem+<>c__DisplayClass42_0.<SaveBanList>b__0` → `SaveCodec.EncryptAndSerialize<BanListData>` (`SaveSystem_NestedType___c__DisplayClass42_0.txt:68`). **Verified**

## Evidence appendix

| Claim | Evidence |
|---|---|
| Class/field/method signatures and visibility | `ilrecovery/BanListManager.cs:6-86`, `ilrecovery/BanListData.cs:5-35`, `ilrecovery/BanEntry.cs:4-23` |
| `BanEntry` offsets `+0x10/+0x18/+0x20/+0x28` | `BanListData.txt:303-317` (inline init), `BanEntry.txt:78-93` (ctor) |
| `BanListData` `Version=1`, `BannedPlayers +0x18` | `BanListData.txt:612`; `BanListManager.txt:803` |
| `BanListManager` `_banList +0x20`, `m_OnBanListChanged +0x28`, `Singleton` | `BanListManager.txt:329, 802, 62, 156, 465, 313-316` |
| Event accessors: `Delegate.Combine`/`Delegate.Remove` + CAS retry | `BanListManager.txt:62-84, 156-176` |
| `Awake` duplicate-guard → `Object.Destroy`, else Singleton+load | `BanListManager.txt:291, 308, 313-316, 327-335` |
| `SaveBanList` lazy create + save + event invoke | `BanListManager.txt:452, 464, 465` |
| `BanPlayer` log string | `BanListManager.txt:619` |
| `UnbanPlayer` log string + save | `BanListManager.txt:721, 726` |
| `GetBanCount` null→0, `_size` read, helper+`shr` transform | `BanListManager.txt:802-808, 812-815` |
| `GetAllBans` live-reference return | `BanListManager.txt:871-879` |
| `AddBan` dedup early-return; inline init; `UtcNow`; `Add`/`AddWithResize` | `BanListData.txt:287-289, 301-317, 321, 333, 334` |
| `RemoveBan` `RemoveAll`, `count > 0` | `BanListData.txt:455, 459` |
| `GetBanEntry` `Find` (`0x181F746D0`) | `BanListData.txt:563` |
| Predicate closures compare `b.SteamId == captured`; null `b` throws | `BanListData_NestedType___c__DisplayClass2_0/4_0/5_0.txt:14`ff |
| `BanEntry` 3-arg ctor stamps `UtcNow` | `BanEntry.txt:78-93` |
| Path strings `"Security"`, `"GlobalBanList.jrsf"`, `".tmp"`, `".bak1"` | `SaveFiles.txt:1264, 1273, 1277, 1282` (method `:1142`) |
| Atomic write + encrypt lambda | `SaveFiles.txt:1391`; `SaveSystem_NestedType___c__DisplayClass42_0.txt:14, 68`; `SaveCodec.txt:938` |
| Load fallback (dst → bak1 → `new BanListData()`) | `SaveSystem.txt:5754, 5828-5899, 5880`; `TryDeserializeBanList` `:15239`; `SaveCodec.txt:2002` |
| Codec = AES + HMAC-SHA256 + GZip + `JsonConvert` (not plain JSON) | `SaveCodec.txt:938, 2002, 4332, 5659-5669`; HKDF info strings `"enc"/"mac"/"identity"`, `"Dimraeth.CharacterIdentity.v1"` |
| Approval refusal: `"You are banned from this server"`, `+0x10/+0x11/+0x48/+0x40` | `CustomNetworkManager.txt:878, 883, 885-890, 1028` |
| Lobby join kick + `"[Steam] Banned player ... attempted to join. Kicking..."` | `Steam.txt:2802, 2814-2826, 2827-2836` |
| `BanAndKickPlayer` flow + `"[Steam] Banned and kicked player: ..."` + fallback warning | `Steam.txt:4064, 4072-4101, 4118` |
| `ServerList.BanPlayer` resolution chain, warning/ban/kick, `"Could not resolve Steam ID ..."`, `"Banned and disconnected player: ..."` | `ServerList.txt:2592-2598, 2610, 2650, 2689, 2696, 2719, 2765, 2787-2789, 2801, 2811-2824` |
| UI mutation sites | `SettingsUI.txt:23429, 23511, 23514`; `SettingsUI_NestedType___c__DisplayClass281_0.txt:14, 101, 104`; `SettingsPresenter.txt:13475, 13556, 13564` |
| UI read-only sites | `SettingsUI.txt:22525, 22803, 22878, 23523, 23637`; `SettingsPresenter.txt:8506, 9330, 9354, 9766, 9947` |
| Completeness of call sites | exhaustive `Call (BanListManager\.\|BanListData\.\|SaveSystem\.SaveBanList\|SaveSystem\.LoadBanList)` sweep of the IsilDump tree (24 hits, all enumerated above or internal to `BanListManager`) |

## Open questions

1. **`GetBanCount` return transform** — the `List._size` value is routed through helper `0x18247E770` and returned as `slot >> 32` (`BanListManager.txt:812-815`). The helper's identity and why the count occupies the high dword are **Unknown**; `BannedPlayers.Count` semantics are **Inferred**.
2. **`ConnectionApprovalResponse` field names** — offsets `+0x10` / `+0x11` / `+0x40` / `+0x48` are **Verified** (`CustomNetworkManager.txt:883-890`); mapping to `Approved` / `CreatePlayerObject` / ??? / `Reason` is **Inferred** from Netcode NGO's response shape. The `+0x40` field is zeroed on *every* response path (including approval) — its meaning is **Unknown**.
3. **`BanEntry(ulong, string, string)` callers** — no call found in the dump tree (`AddBan` initializes inline). Whether it is dead code or called from undumped/reflective code is **Unknown**.
4. **`BanListData.Version` enforcement** — written as `1` (`BanListData.txt:612`) but no comparison found in the traced load path; whether `TryDeserializeBanList` (`SaveSystem.txt:15239`) validates or migrates it is **Unknown**.
5. **`SaveCodec` key material** — sub-key info strings `"enc"`, `"mac"`, `"identity"` and `"Dimraeth.CharacterIdentity.v1"` are **Verified**; the master-key source (machine binding / embedded constant / derived from install) is **Unknown** from the traced files.
6. **`NetworkManager.DisconnectClient` overload** at `ServerList.txt:2801` — the call lists only `this` + client id (no reason) — 2-arg overload vs `reason = null` is **Inferred**.
7. **Extra zeroed argument slot at call sites** — calls such as `BanListManager.BanPlayer` show one trailing `stack:0x20 = 0` slot beyond the declared parameters (also seen with `ExceedsPlausibleCaps`' `stack:0x28`, `Debug.Log`'s `rdx`, etc.). Consistent IL2CPP call-convention artifact, not a 4th parameter (the `Method:` headers list exactly 3) — pattern **Verified** across ≥6 call sites, classification **Inferred**.
8. **Steam id-map integrity** — `Dictionary<UInt64, UInt64>` on the `Steam` manager is populated during approval (`CustomNetworkManager.txt:852-857` region) and read by `ServerList.BanPlayer` (`ServerList.txt:2592-2598`). Whether its keys can be poisoned by a client is **Unknown** (depends on `FacepunchTransport` id resolution, outside the traced files).
9. **`OnBanListChanged` subscribers** — who listens (beyond UI refreshes) was not traced; Unknown whether any gameplay code reacts to ban-list changes.
