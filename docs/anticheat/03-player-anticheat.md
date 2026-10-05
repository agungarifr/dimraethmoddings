# 03 — Player anti-cheat: runtime cheat detection & the kick flow (`Player`)

> Scope: the **runtime cheat-detection subsystem embedded in `Player`** — the HMAC-seeded
> integrity layer over XP/attribute state and equipped runes, the two periodic validation
> coroutines, and the kick/disconnect enforcement flow. This is one of the "post-hoc sweeps"
> referenced by [10-network-authority-surface](10-network-authority-surface.md); the
> unobtainable-gear sweep is [02-gear-legality](02-gear-legality.md), and the value-obfuscation
> primitives it builds on are [11-obfuscated-network-values](11-obfuscated-network-values.md)
> (`ObfuscatedNetworkInt`, `ObfuscationConfig`). Telemetry consequences of violation events are
> [09-telemetry-cheat-events](09-telemetry-cheat-events.md).
>
> Evidence sources: method bodies from the ISIL pseudo-op dumps in
> `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/` (file names abbreviated below as
> `Player.txt`, `ObjectsCommon.txt`, `Player_NestedType__*.txt`, `SkillTree.txt`); signatures
> from `modding/DecompilerTool/ilrecovery_out/Player.cs` (bodies are `throw null` stubs).
> Every claim is marked **Verified** (directly observed), **Inferred** (strongly implied, not
> directly proven) or **Unknown** (not determinable from the available artifacts). Nothing is
> invented. `Player.txt` lines may fall in either the raw-disassembly or the ISIL section of a
> method; both are cited the same way.

---

## 1. Purpose & threat model

The game runs player-hosted multiplayer with no dedicated server (see
[10-network-authority-surface](10-network-authority-surface.md)), so `Player` itself carries a
client- **and** host-side integrity layer. It defends against:

1. **Memory editing of replicated progression state** — XP, accumulated/all-time XP, level,
   skill points, attributes. These are wrapped in `ObfuscatedNetworkInt` /
   `NetworkVariable<Attributes>` (see [11-obfuscated-network-values](11-obfuscated-network-values.md)),
   and `Player` adds a *state HMAC* over the whole set so a partial or inconsistent edit is
   detectable even when each individual value passes its own obfuscation check.
2. **Tampering with equipped runes** — each `EquipmentWorn` slot is HMAC-signed
   (`ComputeRuneHMAC`) and re-validated periodically on the instance that owns the list.
3. **Speed/memory cheats that break timing** — a separate routine compares Unity time against
   wall-clock time (§8.5).

Enforcement outcomes, in escalating order: the integrity-violation event
(`ObfuscatedNetworkInt.RaiseIntegrityViolation` → telemetry, [09](09-telemetry-cheat-events.md)),
save blocking (`_saveBlockedDueToCheat`), auto-repair (skill points), and finally **kick +
disconnect** (`KickForCheat` → `DisconnectAfterDelay` → `NetworkManager.DisconnectClient`) or a
local **exit to menu** (`LoadManager.ExitGame`).

Design shape (all **Verified** from the code below): the HMAC key is **seeded locally per
instance** (`Environment.TickCount ^ Object.GetInstanceID() ^ [this+0x48]`, mixed with
`ObfuscationConfig` keys), state changes **re-seed** the signature instead of clamping, and the
periodic checks compare a recomputed HMAC against the stored one. A cheat that rewrites a value
*and* re-derives the signature would evade CHECK 1 — but the invariant checks (CHECK 2–5) are
value-based and would still fire (§5.2).

---

## 2. API surface

All members below live on `class Player` unless noted. Stub line numbers refer to
`modding/DecompilerTool/ilrecovery_out/Player.cs`; body line numbers to `Player.txt`.

| Member (stub signature) | Stub | Body (`Player.txt`) | Role |
|---|---|---|---|
| `private void InitializeAntiCheat()` | `Player.cs:840` | `7413` | Seeds HMAC keys/signature, subscribes hooks, starts coroutines |
| `private void SubscribeAntiCheatValueChangedHooks()` | `Player.cs:845` | `7789` | Hooks `OnValueChanged` on 7 network fields |
| `private void OnHmacTrackedIntChanged(int prev, int curr)` | `Player.cs:850` | `8487` | Re-seeds state signature on int change |
| `private void OnHmacTrackedAttrsChanged(Attributes prev, Attributes curr)` | `Player.cs:855` | `8523` | Same, for `Attributes` |
| `private void InitializeOwnerAntiCheatClientRpc(ClientRpcParams rpcParams = default)` | `Player.cs:861` | `8559` | Owner-targeted RPC: arms anti-cheat on the owning client |
| `private void RefreshXPSignature()` | `Player.cs:866` | `8735` | Re-seed signature (no guards) |
| `public void UpdateXPStateSignature()` | `Player.cs:871` | `8763` | Re-seed signature (guarded) |
| `private int ComputeXPStateHMAC()` | `Player.cs:876` | `8799` | HMAC over the 7 watched values + key pair |
| `private static uint HmacMix(uint h, uint value)` | `Player.cs:881` | `9241` | MurmurHash3-fmix32-style mixing round |
| `private static int ReadBaseValue(ObfuscatedNetworkInt v)` | `Player.cs:886` | `9263` | Reads the *unobfuscated* base value of a watched int |
| `public bool ValidateXPInvariants()` | `Player.cs:891` | `9286` | Local invariant validation (also used at save time) |
| `public bool ValidateXPState()` | `Player.cs:896` | `11281` | The 5-check validator (periodic + save + report paths) |
| `private IEnumerator PeriodicAntiCheatCheck()` | `Player.cs:902` (+`[IteratorStateMachine]` `:901`) | `15564` (MoveNext: `Player_NestedType__PeriodicAntiCheatCheck_d__204.txt`) | XP/attribute integrity loop |
| `private void KickForCheat()` | `Player.cs:907` | `15628` | Enforcement entry point |
| `private static IEnumerator DisconnectAfterDelay(ulong clientId, float delay)` | `Player.cs:913` (+attribute `:912`) | `15948` (MoveNext: `Player_NestedType__DisconnectAfterDelay_d__207.txt`) | Delayed `DisconnectClient` |
| `private void BlockSaveDueToCheatClientRpc(ClientRpcParams rpcParams = default)` | `Player.cs:919` | `16014` | Tells the owner client to block its save |
| `private void ReportLocalCheatServerRpc(string reason, ServerRpcParams rpcParams = default)` | `Player.cs:925` | `16216` | Owner→server cheat report, server re-validates then kicks |
| `private IEnumerator SpeedHackDetectionRoutine()` | `Player.cs:931` (+attribute `:930`) | `16774` (MoveNext: `Player_NestedType__SpeedHackDetectionRoutine_d__210.txt`) | Time-ratio speedhack detection (local exit) |
| `private void OnEquipmentWornChanged(NetworkListEvent<Rune> ev)` | `Player.cs:936` | `16838` | Re-signs runes on `EquipmentWorn` list change |
| `private void RefreshAllRuneSignatures()` | `Player.cs:941` | `16859` | (Re)builds `_runeSignatureStore` |
| `private int ComputeRuneHMAC(Rune rune)` | `Player.cs:946` | `17179` | Per-rune HMAC |
| `private bool ValidateRuneState()` | `Player.cs:951` | `17959` | Rune slot HMAC validation |
| `private IEnumerator PeriodicRuneIntegrityCheck()` | `Player.cs:957` (+attribute `:956`) | `18577` (MoveNext: `Player_NestedType__PeriodicRuneIntegrityCheck_d__215.txt`) | Rune integrity loop |
| `private void ValidateLoadedXPData()` | `Player.cs:815` | `4630` | XP sanity on load (adjacent validator) |
| `private void ValidateAttributeXPConsistency()` | `Player.cs:825` | `5571` | Attribute↔XP consistency (adjacent validator) |
| `private bool TryReconcileLegacyOverGrant(int surplus)` | `Player.cs:1123` | `28026` | Legacy skill-point "amnesty" (§5.4) |
| `public void RetireLegacySkillPointReconciliation()` | `Player.cs:1128` | `28231` | Retires the amnesty via the mint ledger (§5.4) |

*(Verified — stub lines as cited; body method headers `Player.txt:4630, 5571, 7413, 7789, 8487,
8523, 8559, 8735, 8763, 8799, 9241, 9263, 9286, 11281, 15564, 15628, 15948, 16014, 16216, 16774,
16838, 16859, 17179, 17959, 18577, 28026, 28231`.)*

**Internal state fields** (names from `ilrecovery_out/Player.cs`, offsets from the raw
disassembly; the two agree):

| Field | Stub | Offset | Meaning |
|---|---|---|---|
| `_hmacKeyStore` | `Player.cs:615` | `+0x6D0` (1744) | Masked HMAC key material |
| `_hmacKeyMask` | `Player.cs:617` | `+0x6D4` (1748) | Odd (LSB-set) mask for the key |
| `_signatureStore` | `Player.cs:619` | `+0x6D8` (1752) | Stored HMAC ^ `_signatureMask` |
| `_signatureMask` | `Player.cs:621` | `+0x6DC` (1756) | Odd (LSB-set) mask for the signature |
| `_antiCheatReady` | `Player.cs:623` | `+0x6E0` (1760) | Armed flag (init runs once) |
| `_antiCheatHooksSubscribed` | `Player.cs:625` | `+0x6E1` (1761) | Hook subscription runs once |
| `_saveBlockedDueToCheat` | `Player.cs:627` | `+0x6E2` (1762) | Save-block flag (read by `SaveGame`) |
| `_suppressXPInvariantCheck` | `Player.cs:599` | `+0x6BC` (1724) | Temporarily suppresses re-seeding in hooks |
| `_antiCheatCoroutine` | `Player.cs:335` | `+0x2F8` (760) | Handle for `PeriodicAntiCheatCheck` |
| `_runeIntegrityCoroutine` | `Player.cs:337` | `+0x300` (768) | Handle for `PeriodicRuneIntegrityCheck` |
| `_runeSignatureStore` | `Player.cs:341` | `+0x310` (784) | `Dictionary<FixedString64Bytes,int>` of per-slot signatures |

*(Verified — offsets observed in raw disassembly: `+6D0h/+6D4h/+6D0h` mix at `Player.txt:7470-7484`,
`+6E0h` guard `:7438`, `+6E2h` store `:15762`/`:16097` and read `:22495`, `+1762/+1744/+1748/+1752/+1756`
in the ISIL sections, `_runeSignatureStore` at `[rbp+784]` `:7726-7727`. Stub field names as cited.)*

---

## 3. Lifecycle — when the subsystem arms

### 3.1 `InitializeAntiCheat()` (`Player.txt:7413`)

Entry guards (raw disassembly, `Player.txt:7434-7439`):

```
cmp byte ptr [rbp+26h],0   ; [this+0x26]
jne  short → continue
cmp byte ptr [rbp+25h],0   ; [this+0x25]
je   near  → return        ; both flags 0 → do nothing
cmp byte ptr [rbp+6E0h],0  ; _antiCheatReady
jne  near  → return        ; already armed
```

i.e. the body runs once per instance, only when `[this+0x26] != 0 || [this+0x25] != 0`
**[Verified — `Player.txt:7434-7439`]**. The two bools are inherited/role flags (see §10);
their exact C# identity is **Inferred** (see Open questions).

Key seeding (**Verified** — `Player.txt:7444-7484`, raw disassembly):

1. `seed = Environment.get_TickCount() ^ Object.GetInstanceID() ^ [this+0x48]`
   (`xor esi,ebx` / `xor esi,edi`, `Player.txt:7449-7456`), used to construct a
   `System.Random` (`Player.txt:7460`).
2. `_hmacKeyMask [+0x6D4] = rand | 1` (`or eax,1` / `mov [rbp+6D4h],eax`, `Player.txt:7468-7470`)
   — the `| 1` keeps the mask odd (**Verified**).
3. `_hmacKeyStore [+0x6D0] = ObfuscationConfig.CheckKey ^ _hmacKeyMask ^ ObfuscationConfig.SessionKey ^ rand`
   (the four-way `xor` chain at `Player.txt:7474-7484`; the two config keys are the statics
   loaded via the helper calls at `:7476`/`:7479` — key derivation is
   [11-obfuscated-network-values](11-obfuscated-network-values.md)).
4. `_signatureMask [+0x6DC] = rand | 1`, then
   `_signatureStore [+0x6D8] = ComputeXPStateHMAC() ^ _signatureMask`
   (`Player.txt:7678-7683`, ISIL; `Call Player.ComputeXPStateHMAC` at `:7679`).
5. `_antiCheatReady [+0x6E0] = 1` (`Player.txt:7684`).
6. `SubscribeAntiCheatValueChangedHooks()` (`Player.txt:7685`).
7. `StopCoroutine` (if running) + `StartCoroutine(<PeriodicAntiCheatCheck>d__204)`, with the
   handle stored in `_antiCheatCoroutine [+0x2F8]` (**Verified** — raw disassembly
   `Player.txt:7499-7528`: null-check `cmp qword ptr [rbp+2F8h],0`, `StopCoroutine` on the old
   handle, then `mov [rbp+2F8h],rax`; ISIL mirror `Compare [rbp+760], 0` /
   `Call MonoBehaviour.StopCoroutine` `:7686-7691`, store `:7714`).

If the `[this+0x26]` path is taken (role flag — **Inferred** as server/host-side), the method
additionally (**Verified** — `Player.txt:7726-7776`):

- allocates `_runeSignatureStore [+0x310]` (`[rbp+784]` store at `:7726-7727`),
- calls `RefreshAllRuneSignatures()` (`:7732`),
- hooks `EquipmentWorn [+0x598]` (1432) — `NetworkList<Rune>.OnListChanged += OnEquipmentWornChanged`
  (`[rbp+1432]` at `:7733-7736`),
- starts `<PeriodicRuneIntegrityCheck>d__215` into `_runeIntegrityCoroutine [+0x300]`
  (same Stop/start pattern on `+300h`, raw `Player.txt:7561-7590` **[Verified]**).

### 3.2 Call sites

| Call site | Evidence | Meaning |
|---|---|---|
| `Initialize(NetworkPlayerData data)` | `Call Player.InitializeAntiCheat` `Player.txt:22245` (method header `:18865`) | Anti-cheat arms on the peer that runs `Initialize` |
| `InitializeOwnerAntiCheatClientRpc` receive path | `Call Player.InitializeAntiCheat` `Player.txt:8723` | The **owner client** arms its own anti-cheat |

Immediately after the local arm, `Initialize` sends `InitializeOwnerAntiCheatClientRpc` to the
owner when `OwnerClientId != NetworkManager.LocalClientId` — it builds `UInt64[1]{ [rdi+72] }`
as `ClientRpcParams.TargetClientIds` (`Player.txt:22246-22285`, `get_LocalClientId` at `:22266`,
array fill `[rdi+72]` at `:22284-22285`) and calls the RPC (`Player.txt:22307`). On a listen
server the owner is local, so the RPC is skipped and only the local arm runs
(`JumpIfEqual {1706}` at `:22268`). **Verified.**

Adjacent calls inside the same `Initialize` region: `MigrateAttributesBelowBase`
(`Player.txt:22239`) and `ValidateAttributeXPConsistency` (`:22242`) — both run *before* the
anti-cheat arms. **Verified.**

---

## 4. Watched state — the HMAC-protected values

### 4.1 Hook subscription

`SubscribeAntiCheatValueChangedHooks()` (`Player.txt:7789`) is guarded by
`_antiCheatHooksSubscribed [+0x6E1]` and subscribes `OnValueChanged` delegates on exactly
seven network fields (**Verified** — hook offsets at `Player.txt:8160, 8200, 8238, 8276, 8314,
8352, 8390`; delegate type `NetworkVariable`1<Int32>+OnValueChangedDelegate<Int32>` at
`:8164-8306`):

| Field name | Offset (dec) | Declared on | Handler | Name evidence | Hook evidence |
|---|---|---|---|---|---|
| `XP` | `+0x408` (1032) | `Player` | `OnHmacTrackedIntChanged` | `Player.txt:33032-33036` | `Player.txt:8160` |
| `AccumulatedXP` | `+0x410` (1040) | `Player` | `OnHmacTrackedIntChanged` | `Player.txt:33049-33053` | `Player.txt:8200` |
| `AllTimeXP` | `+0x418` (1048) | `Player` | `OnHmacTrackedIntChanged` | `Player.txt:33066-33070` | `Player.txt:8238` |
| `Level` | `+0xB8` (184) | `ObjectsCommon` | `OnHmacTrackedIntChanged` | `ObjectsCommon.txt:24081-24085` | `Player.txt:8276` |
| `HighestLevel` | `+0x428` (1064) | `Player` | `OnHmacTrackedIntChanged` | `Player.txt:33100-33104` | `Player.txt:8314` |
| `SkillPoints` | `+0x420` (1056) | `Player` | `OnHmacTrackedIntChanged` | `Player.txt:33083-33087` | `Player.txt:8352` |
| `Attributes` | `+0x170` (368) | `ObjectsCommon` | `OnHmacTrackedAttrsChanged` | `ObjectsCommon.txt:24472-24476` | `Player.txt:8390` |

The name column is **Verified** via the NGO-generated `__initializeVariables`, which pairs each
field address with its registered name (`Move rdx, [rbx+off]` + `Move r8, "Name"` +
`Call NetworkBehaviour.__nameNetworkVariable`, e.g. `Player.txt:33032-33036`). All six ints are
`ObfuscatedNetworkInt : NetworkVariable<int>`
(`ilrecovery_out/ObfuscatedNetworkInt.cs:6` — [11](11-obfuscated-network-values.md));
`Attributes` is `NetworkVariable<Attributes>` (`ilrecovery_out/ObjectsCommon.cs:214`).

### 4.2 Re-seed handlers

Both handlers do the same thing (**Verified** — `Player.txt:8487-8557`):

- if `_antiCheatReady [+0x6E0]` and **not** `_suppressXPInvariantCheck [+0x6BC]`:
  `_signatureStore [+0x6D8] = ComputeXPStateHMAC() ^ _signatureMask [+0x6DC]`
  (`Call Player.ComputeXPStateHMAC` + `Xor rax, rax, [rbx+1756]` + `Move [rbx+1752], rax` at
  `Player.txt:8515-8517` (int handler) and `:8551-8553` (attrs handler)).

Note the semantics: **any** change to a watched value — legitimate or tampered — re-seeds the
signature. The hooks never clamp or reject; they only keep the HMAC in sync so that *later*
drift (an edit that bypasses the setter) is what `ValidateXPState` catches. **Verified**
(control flow as observed); the design intent is **Inferred**.

`get_/set_SuppressXPInvariantCheck` touch `[rcx+1724]`
(`Player.txt:236-288`, e.g. `Move rax, [rcx+1724]` at `:243`) **[Verified]**; who sets it is
outside this doc's scope (set around multi-field grant sequences so mid-update states don't
re-seed — **Inferred**).

`RefreshXPSignature()` (`Player.cs:866`, `Player.txt:8735`) and `UpdateXPStateSignature()`
(`Player.cs:871`, `Player.txt:8763`) are the manual re-seed entry points (`UpdateXPStateSignature`
carries the same `_antiCheatReady`/`_suppressXPInvariantCheck` guards as the hooks; both show the
`ComputeXPStateHMAC` → `Xor` → `[_signatureStore]` sequence at `Player.txt:8755-8757` and
`:8791-8793`). **Verified.** A full respec also re-seeds (`ForceRespec`, method `Player.txt:7065`;
re-seed at `:7394-7400`) **[Verified]**.

### 4.3 The HMAC itself

`ComputeXPStateHMAC()` (`Player.txt:8799`) reads exactly the seven watched values via
`ObfuscatedNetworkInt.get_Value` (and the `Attributes` struct), in this order: `XP` (`:9027-9034`),
`AccumulatedXP` (`:9035-9040`), `AllTimeXP` (`:9041-9046`), `Level` (`:9047-9052`), `HighestLevel`
(`:9053-9058`), `SkillPoints` (`:9059-9064`), `Attributes` (`:9065-9069`). It loads the key pair
`_hmacKeyStore [+0x6D0]` / `_hmacKeyMask [+0x6D4]` (`:9030-9032`) and finishes with a
`HmacMix`-based fold whose tail XORs both key words back in
(`Player.txt:9224-9225`: `Xor rcx, rcx, [r15+1748]` / `Xor rcx, rcx, [r15+1744]`; the raw
disassembly tail shows the fmix32 constants `0x85EBCA6B`, `0xC2B2AE35`, `0xCC9E2D51` and the same
two XORs, `Player.txt:8995-9005`). `HmacMix(uint h, uint value)` (`Player.txt:9241`) is a
MurmurHash3-fmix32-style mixing round. **Verified.**
(`ObfuscationConfig.CheckKey`/`SessionKey` semantics: [11](11-obfuscated-network-values.md).)

---

## 5. `PeriodicAntiCheatCheck` (XP / attribute integrity loop)

### 5.1 Coroutine structure & timing

Stub: `Player.cs:901-902`, `Player.txt:15564` (constructs `<PeriodicAntiCheatCheck>d__204`).
MoveNext (`Player_NestedType__PeriodicAntiCheatCheck_d__204.txt`):

1. **state 0** — construct `WaitForSeconds(5f)` and yield (initial grace period)
   (`WaitForSeconds..ctor` call at `:255-261`).
2. **state 1** — construct `WaitForSeconds(1f)`, cache it in `<wait>5__2` (the
   `private WaitForSeconds <wait>5__2` field, `ilrecovery_out/Player.cs:72-124`) and yield
   (ctor call at `:232-238`).
3. **state 2** (loop) — call `Player.ValidateXPState()`
   (`Player_NestedType__PeriodicAntiCheatCheck_d__204.txt:189`, op 030):
   - **pass** → yield the cached `<wait>5__2` (1 s) and loop;
   - **fail** → `String.Format("[AntiCheat] Kicking player {0} — state validation failed", name)`
     (`:210`, op 051) → `Debug.LogError` → `Player.KickForCheat()` (`:224`, op 065) → coroutine ends.

**Timing constants — Verified.** The float immediates are not rendered in the pseudo-ops; they
were recovered from the on-disk `GameAssembly.dll` (methodology and addresses in §9.2):
**initial wait 5 s, loop wait 1 s** (i.e. after a 5 s grace the XP state is re-validated every
second).

### 5.2 `ValidateXPState()` (`Player.txt:11281`) — the five checks

The validator runs five named checks, each with a `"[AntiCheat] CHECK n …"` log and a reason
string fed to `ObfuscatedNetworkInt.RaiseIntegrityViolation(reason)`:

| Check | Log string (exact) | Reason string (exact) | Evidence | Outcome |
|---|---|---|---|---|
| CHECK 1 — stored-signature HMAC | `"[AntiCheat] CHECK 1 FAILED — HMAC mismatch for {0}"` | `"HMAC violation: player={0}"` | log `Player.txt:14951`; reason `:15360`; `RaiseIntegrityViolation` `:15371`; HMAC compare `:13674-13681` | fail → return false (kick) |
| CHECK 2 — base value tamper | `"[AntiCheat] CHECK 2 FAILED — base.Value tamper for {0}"` | `"base.Value tamper: player={0}"` | log `:14753`; reason `:14925` | fail → return false |
| CHECK 3 — XP invariants | `"[AntiCheat] CHECK 3 FAILED — XP invariant for {0}"` | `"XP invariant violation: player={0}"` | log `:14581`; reason `:14727` | fail → return false |
| CHECK 4 — attribute↔XP consistency | `"[AntiCheat] CHECK 4 FAILED — Attribute-XP violation for {0}"` | `"Attribute-XP violation: player={0}"` | log `:14248`; reason `:14559` | fail → return false |
| CHECK 5 — skill-point grant cap | `"[AntiCheat] CHECK 5 — SkillPoints over grant cap for {0}"` (note: **no "FAILED"**) | `"SkillPoints over grant cap: player={0}"` | log `:13967`; reason `:14116` | **non-fatal** (see below) |

**Verified** (all strings and call lines observed). Mechanics per check:

- **CHECK 1** compares `ComputeXPStateHMAC()` against `_signatureStore [+0x6D8] ^ _signatureMask [+0x6DC]`
  (`Call Player.ComputeXPStateHMAC` `:13674`; the two mask/store reads and XOR follow at
  `:13674-13681`; `RaiseIntegrityViolation("HMAC violation: player={0}")` at `:15360-15371`).
  **Verified.**
- **CHECK 2** re-reads each watched `ObfuscatedNetworkInt`'s *base* value
  (`ReadBaseValue`, `Player.txt:9263`) and cross-checks it against the encoded value — the
  seven field reads at `:13528-13564` and again at `:13571-13607` are the two passes.
  Full formula **Unknown** (see §10); the check name and reason string are **Verified**.
- **CHECK 3 / CHECK 4** are value-plausibility checks over XP and attributes. Helper calls
  observed in the CHECK 5 preamble (`GetTotalBonusSkillPoints` `:13932`, `TotalGrantedSkillPoints`
  `:13938`, `SkillPointOverGrantPersists` `:13944`) belong to CHECK 5; the per-check formulas
  for 3 and 4 are **Unknown** beyond their strings.
- **CHECK 5** is **non-fatal**: when the over-grant persists it logs the CHECK 5 message, raises
  the integrity violation (`:14116-14125`), then attempts an **auto-repair** via
  `SkillTree.TryRepairOverGrantedSkillPoints` (`:14146`) and, on success,
  `Debug.LogError("[AntiCheat] Auto-repaired {0}'s skill points {1} -> {2} " + "(MaxGrantable={0}). The surplus was never earned; every node is kept.")`
  (`:14183-14205`), then returns `1` (true) — validation continues
  (`Move rax, 1` `:14206`). Skill nodes are kept, only the numbers are repaired. **Verified**
  (control flow `:14116-14206`); "CHECK 5 can never fail validation" is **Inferred** from the
  observed single return-true exit in this block.

### 5.3 Related validators (adjacent, called on the same paths)

| Method | What it does | Evidence |
|---|---|---|
| `ValidateXPInvariants()` (`Player.txt:9286`) | Local invariant variant used at save time; its own failure strings are the `"Local …"` family: `"[AntiCheat] Local SkillPoints over grant cap for {0}: "` (`:10712`), `"MaxGrantable={0} Level={1} HighestLevel={2}"` (`:10757`), `"[AntiCheat] Local XP invariant violation for {0}: "` (`:10852`), `"[AntiCheat] Local base.Value tamper for {0}"` (`:10910`); calls `GetTotalBonusSkillPoints`/`TotalGrantedSkillPoints`/`SkillPointOverGrantPersists` (`:10678-10689`) | **Verified** (strings/calls) |
| `ValidateLoadedXPData()` (`Player.txt:4630`) | XP sanity when loading saved data | header **Verified**; internals **Unknown** (not extracted) |
| `ValidateAttributeXPConsistency()` (`Player.txt:5571`) | Attribute↔XP consistency, run in `Initialize` (`:22242`); strings `"[AntiCheat] Attribute-XP violation for {0}: "` (`:6216`), `"Attribute-XP violation: player={0}, "` (`:6270`); reads `Attributes [+0x170]` and `AllTimeXP [+0x418]` (`:6032`, `:6180-6273`) | **Verified** (strings/reads) |
| `MigrateAttributesBelowBase()` (`Player.txt:6339`) | One-shot migration, called in `Initialize` (`:22239`) before the arm | **Verified** (call) |

### 5.4 Legacy skill-point reconciliation — the "mint ledger" / amnesty

(TASKS.md follow-up item; mechanism lives next to the grant-cap check.)

The bonus-skill-point accounting keeps a **ledger of named bonus sources**
(`EnsureBonusSkillPointSourcesInitialized`, `Player.txt:26598`; `HasBonusSkillPointSource`
`:26660`; `TryGrantBonusSkillPoints(source, amount)` `:26715`; `GetTotalBonusSkillPoints`
`:28499`; `TotalGrantedSkillPoints` `:27865`; `MaxGrantableSkillPoints` `:28425`;
`SkillPointOverGrantPersists` `:27960`). Two legacy-amnesty methods use a source named
**`"LegacySkillPointReconciliation"`** (`Player.txt:28164, 28173, 28361, 28373`):

- `TryReconcileLegacyOverGrant(int surplus)` (`Player.txt:28026`) — reconciles skill points
  over-granted before ticket **DIM-10921**; logs
  `"[AntiCheat] Reconciled {0} legacy over-granted skill point(s) for {1} " +
  "(pre-DIM-10921 free start-node refund). Recorded as a bonus source; no points granted."`
  (`:28198`, `:28203`). **Verified** (strings); no call site found in the dumps (§10).
- `RetireLegacySkillPointReconciliation()` (`Player.txt:28231`) — runs on a **full respec**
  (called from `SkillTree.txt:19436` and `:23948`) and logs
  `"[AntiCheat] Retired {0} legacy reconciled skill point(s) for {1} on a full respec " +
  "(pre-DIM-10921 mint). Ledger entry kept at 0 so the amnesty cannot run again."`
  (`:28399`, `:28404`) — the ledger entry is deliberately kept at **0**, so the one-time
  amnesty can never re-trigger. **Verified** (strings + call sites).

---

## 6. `PeriodicRuneIntegrityCheck` (rune slot HMAC loop)

### 6.1 Coroutine structure & timing

Stub: `Player.cs:956-957`, `Player.txt:18577` (constructs `<PeriodicRuneIntegrityCheck>d__215`).
MoveNext (`Player_NestedType__PeriodicRuneIntegrityCheck_d__215.txt`) mirrors §5.1:

1. **state 0** — `WaitForSeconds(10f)`, yield (ctor `:255-261`).
2. **state 1** — `WaitForSeconds(30f)` cached in `<wait>5__2`, yield (ctor `:232-238`).
3. **state 2** (loop) — `Player.ValidateRuneState()` (`:189`, op 030):
   - **pass** → re-yield cached 30 s wait;
   - **fail** → `"[AntiCheat] Kicking player {0} — rune integrity failed"` (`:210`, op 051) →
     `Debug.LogError` → `Player.KickForCheat()` (`:224`, op 065).

**Timing constants — Verified** (§9.2): **initial wait 10 s, loop wait 30 s**.

Unlike the XP hooks (re-seed per change), rune integrity is a **periodic comparison**: each
`EquipmentWorn` slot's rune is HMAC-signed once and re-checked every 30 s. **Verified.**

### 6.2 Rune signing & validation

- `RefreshAllRuneSignatures()` (`Player.txt:16859`) iterates `EquipmentWorn [+0x598]`
  (`[rbx+1432]` `:17048-17068`) and fills `_runeSignatureStore [+0x310]` (`[rbx+784]` `:17043-17046`)
  with `ComputeRuneHMAC(rune)` results (`Call Player.ComputeRuneHMAC` `:17144`). **Verified.**
- `OnEquipmentWornChanged(NetworkListEvent<Rune> ev)` (`Player.txt:16838`) re-signs on list
  changes (it calls `RefreshAllRuneSignatures`, `Call Player.RefreshAllRuneSignatures` `:16855`).
  **Verified.**
- `ComputeRuneHMAC(Rune rune)` (`Player.txt:17179`) mixes the rune's contents with the same key
  pair — the internal mix uses `[rbx+1748]^[rbx+1744]` (`_hmacKeyMask ^ _hmacKeyStore`) and the
  result is folded against `_signatureMask` (`:18423`, per `ValidateRuneState`'s call site).
  **Verified** (mix reads at `:17596-17597`, result XOR at `:18423`); the exact rune-field
  coverage is **Unknown** (not extracted).
- `ValidateRuneState()` (`Player.txt:17959`) re-computes per slot (`Call Player.ComputeRuneHMAC`
  `:18420`) and compares against `_runeSignatureStore`; mismatch logs
  `"Rune HMAC violation: player={0}, slot={1}"` (`:18547`) and raises the integrity violation
  (`ObfuscatedNetworkInt.RaiseIntegrityViolation` `:18559`). **Verified.**

Only the role path where `[this+0x26] != 0` builds `_runeSignatureStore` and starts this loop
(§3.1) — so rune integrity is enforced on the instance that owns the authoritative
`EquipmentWorn` list (server/host — **Inferred** role mapping, §10).

---

## 7. `InitializeOwnerAntiCheatClientRpc`

Generated NGO ClientRpc (registered as `"InitializeOwnerAntiCheatClientRpc"`,
`Player.txt:34785` in `__initializeRpcs`; hash `0x26053DB4` = handler
`__rpc_handler_637877684`, `Player.txt:34900`). Stub `Player.cs:861`, body `Player.txt:8559`.

- **Send side** (`Initialize`, server/peer): only when `OwnerClientId != LocalClientId`;
  `ClientRpcParams.TargetClientIds = UInt64[1]{ OwnerClientId }`
  (`Player.txt:22246-22285`, send `:22307`). **Verified.**
- **Receive side** (owner client): the generated exec-stage guard (`[rbx+32] == 1` for the
  execute stage, `Player.txt:8668`-region; `NetworkManager.get_IsServer` `:8672`,
  `get_IsClient` `:8710`) resets the stage and, if `[this+0x25] != 0`, calls
  `InitializeAntiCheat()` (`Player.txt:8723`). **Verified** (call + guards observed);
  the exec-stage semantics are standard NGO generated code (**Inferred** from pattern).

Purpose: on a client-hosted session the owner client must seed **its own** HMAC keys/signature
locally (the server's RNG seed can't be — and isn't — transmitted). The RPC is just a "now
initialize yourself" ping; it carries no key material. **Inferred** from the empty-parameter
signature and the local seeding in §3.1.

---

## 8. Enforcement — `KickForCheat` + `DisconnectAfterDelay`

### 8.1 `KickForCheat()` (`Player.txt:15628`)

Two branches, decided by `NetworkManager.Singleton != null && IsServer && OwnerClientId != LocalClientId`:

| Condition | Path | Evidence |
|---|---|---|
| Server-side kick of a **remote** cheater (`IsServer` and `OwnerClientId [rsi+72] != LocalClientId`) | Build `UInt64[1]{ OwnerClientId }` → `BlockSaveDueToCheatClientRpc(ClientRpcParams{ TargetClientIds })` (`Player.txt:15869-15892`), then `StartCoroutine(<DisconnectAfterDelay>d__207 { delay = 0.5f, clientId = OwnerClientId })` (`:15894-15913`) | `get_IsServer` `:15845`; `[rsi+72]` vs `get_LocalClientId` `:15848-15853`; RPC call `:15892`; coroutine field stores `Move [rbx+40], rdi` (clientId) and `Move [rbx+32], 0x3F000000` (delay = **0.5 s**, exact immediate `0x3F000000`) `:15909-15911`; `StartCoroutine` `:15913` |
| Otherwise (no `NetworkManager`, not server, or the cheater is **ourselves**) | `_saveBlockedDueToCheat [+0x6E2] = 1` (`Player.txt:15922`, raw `:15762`) → `LoadManager.ExitGame()` (`:15942`, guarded by a null check on the `LoadManager` instance via `Object.op_Inequality` `:15926-15933`) | **Verified** |

So: the host punishes remote cheaters with **save-block + delayed disconnect**, and punishes a
local cheat (or a client detecting its own tamper) with **save-block + exit to menu**.
**Verified.**

### 8.2 `DisconnectAfterDelay(ulong clientId, float delay)` (`Player.txt:15948`, static)

MoveNext (`Player_NestedType__DisconnectAfterDelay_d__207.txt`):
state 0 → `new WaitForSeconds(delay)` (`:151-157`); state 1 → if
`NetworkManager.Singleton != null && IsServer`, call
`NetworkManager.DisconnectClient(clientId)` (`:209`, op 079). No log strings inside the
coroutine. The `delay` passed by `KickForCheat` is the inline immediate `0x3F000000` =
**0.5 s** (`Player.txt:15911`); the disconnect target is the offending player's `OwnerClientId`
(`Move [rbx+40], rdi` with `rdi = [rsi+72]`, `Player.txt:15894-15909`). **Verified.**

### 8.3 The two enforcement RPCs

- `BlockSaveDueToCheatClientRpc` (`Player.txt:16014`) — sets `_saveBlockedDueToCheat [+0x6E2] = 1`
  on the receiving owner client (`Move [rbx+1762], 1` `:16197`, raw `:16097`); standard generated
  RPC send/receive guards (`get_IsServer` `:16148`, `get_IsClient` `:16186`). **Verified.**
- `ReportLocalCheatServerRpc(string reason, ServerRpcParams)` (`Player.txt:16216`) — owner→server
  self-report (also used by the save path, §8.4). Server-side body: re-runs
  `Player.ValidateXPState` (`:16642`); if the server state is clean it logs
  `"[AntiCheat] Local violation reported for {0} but server state is clean " +
  "(likely transient mid-update observation): "` and does **not** kick (`:16663-16679`); if the
  violation is confirmed it logs `"[AntiCheat] Local violation confirmed by server for {0}: {1}"`
  (`:16722`), raises `ObfuscatedNetworkInt.RaiseIntegrityViolation("Local invariant confirmed: player={0}, reason={1}")`
  (`:16753-16765`) and calls `KickForCheat()` (`:16768`). The generated ownership guard logs
  `"Only the owner can invoke a ServerRpc that requires ownership!"` (`:16701`).
  **Verified.** The server-side double-check is the anti-spoofing measure: a client cannot
  trigger a kick by reporting a violation that the server cannot reproduce. **Inferred** intent.

### 8.4 Save-path integration (cross-ref)

`SaveGame()` (`Player.txt:22444`) gates on `_saveBlockedDueToCheat` (`cmp byte ptr [rbx+6E2h],0`
`:22495`, ISIL `Compare [rbx+1762], 0` `:22853`) and re-validates with all three validators —
`ValidateXPInvariants` (`:22861`), `ValidateXPState` (`:22870`), `ValidateRuneState` (`:22875`).
Rejection strings: `"[AntiCheat] Save rejected — rune integrity failed"` (`:23104`),
`"[AntiCheat] Save rejected — XP state validation failed"` (`:23110`). On a save-time violation
it calls `LoadManager.ExitGame()` (`:23132`) and
`ReportLocalCheatServerRpc("XP/attribute invariant violation on save")`
(reason `:23141`, call `:23147`). **Verified** (calls + strings); exact branch ordering
**Inferred**.

### 8.5 Adjacent loop: `SpeedHackDetectionRoutine` (`Player.txt:16774`)

Not part of the kick flow (TASKS.md follow-up item; covered here for completeness). MoveNext
(`Player_NestedType__SpeedHackDetectionRoutine_d__210.txt`) samples `WaitForSecondsRealtime`
(`:540-565`) and compares Unity elapsed time to wall-clock time; on anomaly it logs
`"[AntiCheat] Speedhack detected for {0} — "` + `"ratio={0:F2}, unityΔ={1:F2}s, wallΔ={2:F2}s"`
(`:420`, `:439`) and `"Speedhack: player={0}, ratio={1:F2}"` (`:481`), and ends with
`LoadManager.ExitGame` (`:513`) — a **local exit**, not a kick. **Verified** (strings + call).
Ratio threshold formula **Unknown** (not extracted).

---

## 9. Evidence appendix

### 9.1 Field-offset ↔ name mapping

The dumps contain raw disassembly and ISIL; the IL2CPP field offsets are unnamed in both. The
names were recovered from NGO's generated `__initializeVariables`, which passes each field and
its registered name to `NetworkBehaviour.__nameNetworkVariable`
(`Player.txt:32725-32730` shows the canonical block: `NetworkVariableBase.Initialize` →
`Move r8, "Name"` → `__nameNetworkVariable`):

- `Player.__initializeVariables` (method `Player.txt:30638`): `1032→"XP"` (`:33026-33036`),
  `1040→"AccumulatedXP"` (`:33043-33053`), `1048→"AllTimeXP"` (`:33060-33070`),
  `1056→"SkillPoints"` (`:33077-33087`), `1064→"HighestLevel"` (`:33094-33104`).
- `ObjectsCommon.__initializeVariables` (`ObjectsCommon.txt`): `184→"Level"` (`:24075-24085`),
  `368→"Attributes"` (`:24466-24476`).

The same offsets are then matched to hook sites and HMAC reads (§4). **Verified.**

### 9.2 WaitForSeconds float constants

`WaitForSeconds` immediates are not visible in the pseudo-ops (only
`typeof(UnityEngine.WaitForSeconds)` + `WaitForSeconds..ctor`). They were read from the on-disk
`GameAssembly.dll`:

1. Each coroutine's `MoveNext` was located by the dispatch byte signature
   `8B 4F 10 48 89 9C 24 B0 00 00 00` at file offsets `0x956E5C` (d__204) and `0x9570AC` (d__215)
   (both re-verified 2026-09-29).
2. Each `movss xmm1,[rip+disp32]` feeding a `WaitForSeconds` ctor was resolved (RIP =
   instruction + 8) to four float slots, re-read 2026-09-29:
   `RVA 0x4664798 = 1`, `RVA 0x4664800 = 5` (d__204); `RVA 0x46647A4 = 10`, `RVA 0x46649F8 = 30` (d__215).
3. Pairing constants to coroutine states: d__204 initial **5 s** / loop **1 s**; d__215 initial
   **10 s** / loop **30 s** (instruction VAs map to the ISIL ops at
   `Player_NestedType__PeriodicAntiCheatCheck_d__204.txt` ops 075/098 and
   `Player_NestedType__PeriodicRuneIntegrityCheck_d__215.txt` equivalents).

**Verified** (bytes read from the binary; pairing per instruction-VA matching).

### 9.3 Address-space note

Dump virtual addresses do **not** linearly match on-disk RVAs: `RVA = (dump VA − 0x180000000) + 0x7800`
(example: dump `0x18465DB98` ↔ RVA `0x4665398`). The `0x7800` shift is unexplained (**Unknown**,
§10). String literals could not be cross-located in `GameAssembly.dll` — they live in the IL2CPP
global-metadata, which is not present in the game directory (**Verified** — absence).

### 9.4 Exact string inventory (this doc)

| String | Location |
|---|---|
| `"[AntiCheat] Kicking player {0} — state validation failed"` | `Player_NestedType__PeriodicAntiCheatCheck_d__204.txt:210` |
| `"[AntiCheat] Kicking player {0} — rune integrity failed"` | `Player_NestedType__PeriodicRuneIntegrityCheck_d__215.txt:210` |
| `"[AntiCheat] CHECK 1 FAILED — HMAC mismatch for {0}"` | `Player.txt:14951` |
| `"[AntiCheat] CHECK 2 FAILED — base.Value tamper for {0}"` | `Player.txt:14753` |
| `"[AntiCheat] CHECK 3 FAILED — XP invariant for {0}"` | `Player.txt:14581` |
| `"[AntiCheat] CHECK 4 FAILED — Attribute-XP violation for {0}"` | `Player.txt:14248` |
| `"[AntiCheat] CHECK 5 — SkillPoints over grant cap for {0}"` | `Player.txt:13967` |
| `"HMAC violation: player={0}"` | `Player.txt:15360` |
| `"base.Value tamper: player={0}"` | `Player.txt:14925` |
| `"XP invariant violation: player={0}"` | `Player.txt:14727` |
| `"Attribute-XP violation: player={0}"` | `Player.txt:14559` |
| `"SkillPoints over grant cap: player={0}"` | `Player.txt:14116` |
| `"MaxGrantable={0} Level={1} HighestLevel={2} Bonus={3}"` | `Player.txt:14081` |
| `"(MaxGrantable={0}). The surplus was never earned; every node is kept."` | `Player.txt:14192` |
| `"[AntiCheat] Auto-repaired {0}'s skill points {1} -> {2} "` | `Player.txt:14183` |
| `"Rune HMAC violation: player={0}, slot={1}"` | `Player.txt:18547` |
| `"[AntiCheat] Local violation reported for {0} but server state is clean "` | `Player.txt:16663` |
| `"(likely transient mid-update observation): "` | `Player.txt:16679` |
| `"[AntiCheat] Local violation confirmed by server for {0}: {1}"` | `Player.txt:16722` |
| `"Local invariant confirmed: player={0}, reason={1}"` | `Player.txt:16753` |
| `"Only the owner can invoke a ServerRpc that requires ownership!"` | `Player.txt:16701` |
| `"[AntiCheat] Save rejected — rune integrity failed"` | `Player.txt:23104` |
| `"[AntiCheat] Save rejected — XP state validation failed"` | `Player.txt:23110` |
| `"XP/attribute invariant violation on save"` | `Player.txt:23141` |
| `"[AntiCheat] Reconciled {0} legacy over-granted skill point(s) for {1} "` | `Player.txt:28198` |
| `"(pre-DIM-10921 free start-node refund). Recorded as a bonus source; no points granted."` | `Player.txt:28203` |
| `"[AntiCheat] Retired {0} legacy reconciled skill point(s) for {1} on a full respec "` | `Player.txt:28399` |
| `"(pre-DIM-10921 mint). Ledger entry kept at 0 so the amnesty cannot run again."` | `Player.txt:28404` |
| `"[AntiCheat] Speedhack detected for {0} — "` | `Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:420` |
| `"ratio={0:F2}, unityΔ={1:F2}s, wallΔ={2:F2}s"` | `Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:439` |
| `"Speedhack: player={0}, ratio={1:F2}"` | `Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:481` |
| `"LegacySkillPointReconciliation"` (ledger source name) | `Player.txt:28164, 28173, 28361, 28373` |

*(Verified — all strings quoted exactly from the cited lines.)*

---

## 10. Open questions

1. **Identity of the guard flags `[this+0x25]` / `[this+0x26]`** — behaviorally documented
   (`+0x26` gates the rune subsystem, either one arms the XP subsystem, §3.1), but their C#
   names are **Unknown** (they sit below the first named `Player` field and are likely
   role/ownership caches). The rune half being host-side is **Inferred** from what it validates.
2. **Identity of `[this+0x48]`** — used as an RNG-seed input (`Player.txt:7446`) and, as
   `[rsi+72]`, as the `OwnerClientId`-shaped kick target (`Player.txt:15848, 15869`). "A cached
   `OwnerClientId`-like ulong" is **Inferred**; the declared name is **Unknown**.
3. **`GameAssembly.dll` RVA vs dump-VA `0x7800` shift** — consistent across all sampled
   addresses but unexplained (**Unknown**); noted so future address work doesn't silently
   mismatch. Strings were not findable in `GameAssembly.dll` (metadata lives elsewhere).
4. **Internal formulas of `ValidateXPState` CHECK 2–4 and `ValidateXPInvariants`** — only the
   helper calls, field reads and strings were extracted; the exact per-check arithmetic is
   **Unknown**. CHECK 2's "two passes over the seven fields" (`Player.txt:13528-13607`) suggests
   a base-value vs encoded-value cross-check (**Inferred**).
5. **`TryReconcileLegacyOverGrant` call sites** — no caller exists anywhere in the IsilDump
   (**Verified** — absence); only `RetireLegacySkillPointReconciliation` has callers
   (`SkillTree.txt:19436, 23948`). The caller of the amnesty may be stripped, reflection-based,
   or in a non-dumped assembly (**Unknown**).
6. **CHECK 5 aftermath** — `RaiseIntegrityViolation` feeds the telemetry chain
   ([09-telemetry-cheat-events](09-telemetry-cheat-events.md)); whether a purely auto-repaired
   over-grant flags the whole session as cheating is **Unknown** from this doc's evidence.
7. **`ComputeRuneHMAC` rune-field coverage** — which `Rune` fields feed the HMAC is **Unknown**
   (only the key mixing and fold were extracted, `Player.txt:17596-17597`, `:18423`).
8. **`ValidateLoadedXPData` internals** (`Player.txt:4630`) — header and call context only;
   body **Unknown**.
9. **Speedhack ratio threshold** — the exact comparison and cutoff in
   `SpeedHackDetectionRoutine` are **Unknown** (strings and the `ExitGame` tail only).
