# 10 — Network authority surface & RPC-level validation

> Scope: where **client input enters and mutates shared multiplayer state** (the ServerRpc
> surface), and which cheat-blocking guards run on that path. This doc is the "perimeter"
> layer; the post-hoc sweeps that catch what gets past it are documented in
> [02-gear-legality](02-gear-legality.md), [03-player-anticheat](03-player-anticheat.md),
> [04-character-identity-integrity](04-character-identity-integrity.md),
> [05-character-plausibility](05-character-plausibility.md) and
> [11-obfuscated-network-values](11-obfuscated-network-values.md).

## 1. Purpose & threat model

The game runs player-hosted multiplayer on Netcode for GameObjects (NGO) with the
Facepunch Steam transport. There is **no dedicated authoritative server**: the host is a
peer. The threat model is therefore:

1. A modified client can call **any** ServerRpc with **any** payload, at any rate.
2. A modified client can edit its own replicated state (memory editors) before it syncs.
3. A modified client can refuse to run local validation entirely.

The game's answer is a **layered, mostly client-driven RPC perimeter with post-hoc
sweeps** — RPCs carry gameplay intents, light guards reject obviously-bogus requests,
and the periodic integrity systems (docs 02–05) destroy/revert/kick afterwards.
This doc maps the perimeter and its gaps.

## 2. How the RPC layer works here (evidence)

Every `...ServerRpc` in the decompiled stubs follows the standard NGO generated pattern
(verified in the IL-recovery dumps — e.g. `Bank.txt`):

- **Client send path** (`ISIL` ops 037–087 in `DepositMoneyServerRpc`):
  `NetworkBehaviour.get_NetworkManager` → `get_IsListening` → `get_IsClient`/`get_IsHost`
  → serialize args (`BytePacker.WriteValueBitPacked`) → `__endSendServerRpc`.
  If the RPC is ownership-required and the local client is not the owner, the generated
  code logs **"Only the owner can invoke a ServerRpc that requires ownership!"**
  (`Debug.LogError`) and drops the call
  *(Verified — `IsilDump/Assembly-CSharp/Bank.txt:513`, `:905`, `:1327`)*.
- **Server receive path** (ops 088+): `get_IsServer`/`get_IsHost` gate, then the actual
  handler body executes on the host.

**Ownership attribute survey** over `modding/DecompilerTool/ilrecovery_out/*.cs` (stubs):

| Attribute | Count | Meaning |
|---|---|---|
| `[ServerRpc(RequireOwnership = false)]` | ~315 | **Any** connected client may invoke |
| `[ServerRpc]` (ownership required) | ~78 | Only the object owner may invoke |

*(Verified by regex count over the stub dumps, 2026-09-29. Caveat: some stub files dump the
same class twice — e.g. the `Inventory` class appears in `Inventory.cs`,
`BackpackEntryEdge.cs` and `PaneLanding.cs` — so absolute numbers are inflated; the
**ratio** is the meaningful fact: the large majority of server mutations are callable by
any client.)*

## 3. Guard patterns observed on the RPC perimeter

| Guard | What it does | Evidence |
|---|---|---|
| NGO ownership check | Blocks non-owner invocation of owner-only RPCs | Verified — `Bank.txt:513/905/1327` (string above) |
| Host/server gate | Handler only executes on host/server | Verified — `Bank.txt` ops 090–099 |
| Existence / account check | `Bank.AccountCheck(clientId)` before bank mutations | Verified — `Bank.txt:1236`, `:1643` |
| Affordability check | Server compares stored gold (`ObfuscatedNetworkInt`) to requested amount, aborts if less | Verified — `Bank.txt:1252–1254` (`get_Value` → `Compare rax, r14` → `JumpIfLess {188}`) |
| Player lookup guard | `Utility.ReturnPlayerById(clientId)` null-checked before touch | Verified — `Bank.txt:1245–1247` |
| Amount sanity | `test r14d,r14d / jle` — reject non-positive amounts | Verified (native disasm) — `NPCShop.txt` sell handler |
| Item existence | Item looked up in DB; null → early-out | Verified (native disasm) — `NPCShop.txt` sell handler |
| Obfuscated replicated values | Gold/vitals stored via `ObfuscatedNetworkInt/Float` with integrity hash — tamper detection on read & on sync | See [11-obfuscated-network-values](11-obfuscated-network-values.md) |
| Post-hoc sweeps | GearLegality purge, rune integrity loop, identity/plausibility checks | Docs 02–05 |
| Telemetry flagging | Suspicious state uploaded & flagged (`cheat_attempted`) | See [09-telemetry-cheat-events](09-telemetry-cheat-events.md) |

## 4. Verified case studies

### 4.1 `Bank.DepositMoneyServerRpc(ulong clientId, int gold)` — a *well-guarded* RPC

Server-side chain *(all Verified, `IsilDump/Assembly-CSharp/Bank.txt`)*:

1. `Bank.AccountCheck(clientId)` → abort if no account (`:1236`, compare → `JumpIfEqual {188}`).
2. `Utility.ReturnPlayerById(clientId)` → null → abort (`:1245`).
3. Player's gold (`ObfuscatedNetworkInt` at player+992) read (`:1252`); if
   `gold < amount` → abort (`:1254` `JumpIfLess {188}`).
4. Match the account entry via `Utility.ReturnHashById(clientId)` + `Enumerable.FirstOrDefault`
   over `ServerBankData` (`:1256–1293` region).
5. Atomic move: gold -= amount via `ObfuscatedNetworkInt.set_Value` (`:1306`), then
   `ServerBankData` balance += amount (`Add [rdi+84], [rdi+84], r14`).

Note: the RPC's `clientId` parameter is *not* cross-checked against
`rpcParams.Receive.SenderClientId` in the recovered body — a client can move money for
another player's account **into** existence checks but the affordability check still binds
the named player's gold *(Inferred implication, not a proven exploit path)*.

### 4.2 `Inventory.AddSimpleItemToInventoryServerRpc(ItemType item, int amount)` — a *wide-open* RPC

Signature: `[ServerRpc(RequireOwnership = false)]` *(Verified — `ilrecovery_out/Inventory.cs:3058–3059`)*.

Server-side chain *(Verified, `IsilDump/Assembly-CSharp/Inventory.txt:69217ff`)*:

1. Standard NGO host gate.
2. `Inventory.AddSimpleItemToInventoryClientRpc(item, amount)` — broadcasts the grant
   (`:69428`).
3. `QuestManager.CheckItemPickupQuestTrigger(item, clientId)` (`:69442`).
4. Return. **No amount clamp, no ownership/possession check, no sender validation, no
   rarity/level filter.**

**Finding:** any connected client can invoke this RPC and the host will broadcast an item
grant. The compensating control is not on the RPC path at all — it is the
`GearLegality` sweep (destroying unobtainable gear, [doc 02](02-gear-legality.md)) and the
`PeriodicRuneIntegrityCheck` ([doc 03](03-player-anticheat.md)). *(Verified that the body
contains no guards; the "compensating control" reading is Inferred.)*

### 4.3 `NPCShop.SellItemToShopServerRpc(ulong sellerId, ItemType itemType, int amount, ...)` — partially guarded

From the native disasm of the server half *(Verified at disasm level,
`IsilDump/Assembly-CSharp/NPCShop.txt:12985ff`)*:

- Rejects non-positive amounts (`test r14d,r14d / jle`).
- Item must resolve in the item DB (null → early-out).
- Gold paid = `itemPrice * amount`, clamped non-negative (`cmovns` chain).
- **No possession check observed** in the visible half — whether the seller actually has
  `amount` of `itemType` removed before payout is **Unknown** from this reconstruction
  (the tail of the method calls further helpers not yet resolved).

The matching `SellItemToBefrServerRpc` / `SellRuneToBefrServerRpc` (inventory → vendor)
are also `RequireOwnership = false` *(Verified — `ilrecovery_out/Inventory.cs:4197–4205`)*.

## 5. Cheat-relevant mutation RPC inventory (perimeter map)

Grouped by subsystem. "Guards" = verified guards seen on the path (see §3 codes):
`OWN` ownership-required, `ACC` account/holder check, `AFF` affordability, `AMT` amount
sanity, `EXI` existence check, `OBF` obfuscated value touchpoint, `—` none observed.

| Subsystem | RPC (class.method) | Own? | Guards | Notes |
|---|---|---|---|---|
| Economy | `Bank.DepositMoneyServerRpc` | no | ACC, AFF, OBF | Verified chain, §4.1 |
| Economy | `Bank.WithdrawMoneyServerRpc` | no | ACC, AFF, OBF | `AccountCheck` at `Bank.txt:1643` |
| Economy | `Bank.CreateBankAccountServerRpc` / `CloseBankAccountServerRpc` | no | ACC | |
| Economy | `NPCShop.BuyItemFromShopServerRpc` | no | AMT, EXI | takes `ServerRpcParams` |
| Economy | `NPCShop.SellItemToShopServerRpc` | no | AMT, EXI | §4.3, possession check Unknown |
| Economy | `NPCShop.BuyRuneFromShopServerRpc` / `SellRuneToShopServerRpc` | no | EXI | rune passed **as full struct** — the server receives the entire `Rune` from the client; GearLegality sweep is the backstop |
| Inventory | `Inventory.AddSimpleItemToInventoryServerRpc` (×2 overloads) | no | **—** | §4.2, wide open |
| Inventory | `Inventory.DropNewItemOnGroundServerRpc` | no | **—** | item + amount decided by client |
| Inventory | `Inventory.SellItemToBefrServerRpc` / `SellRuneToBefrServerRpc` | no | ? | |
| Inventory | `NPCDatabase.SendInventorySnapshotServerRpc` | no | ? | client reports its inventory |
| Crafting | `CraftingBench.CraftSelectedServerRpc` | no | EXI | "Invalid quantity or no matching recipe." (`PersonalCrafting.txt:6634`) |
| Progression | `Party.AddXPToPartyServerRpc(int xp)` | no | ? | client-chosen XP amount — check needed |
| Progression | `DeedManager.MarkDeedCompletedServerRpc` / `RegisterAcceptedDeedQuestServerRpc` | no | ? | client-supplied `playerHash` + tier |
| Progression | `EventsManager.RegisterMonsterDeathServerRpc` | no | ? | client reports kills |
| Progression | `LifetimeAttributes.CreditPlayerWithKillServerRpc` | no | ? | |
| Combat | `Damages.SendDamageToObjectsCommonServerRpc` / `SendDeathCheckToServerRpc` | mixed | ? | **client-supplied `Damage` struct** — damage-authority question |
| Combat | `HarvestClient.DealDamageServerRpc` | no | ? | |
| World | `ActiveConversation.DestroyHarvestedItemsServerRpc` | no | ? | |
| Pets | `PetPen.AddPetToPenServerRpc` / `RemovePetFromPenServerRpc` | no | ? | client-supplied `LocalPetData` |
| World | `DungeonChest.OpenDungeonChestServerRpc` | no | ? | |
| Party | `Party.KickPlayerServerRpc` / `ChangeLeaderServerRpc` | no | ? | role checks Unknown |

`?` = not yet reconstructed; listed for follow-up in `TASKS.md` (T2.1 follow-ups).

## 6. Architectural reading (Inferred, synthesis)

1. **Client-driven by design.** The sheer count of `RequireOwnership = false` RPCs that
   grant items, gold, XP and kills shows gameplay intents are trusted from clients.
2. **Defense is post-hoc and deterrent.** The heavy machinery — GearLegality destruction,
   rune integrity kicks, identity/plausibility rejection, obfuscated value integrity,
   cheat telemetry — operates *after* the fact and mostly on the host.
3. **Obfuscated values are the tripwire.** Gold and vitals being `ObfuscatedNetwork*`
   with integrity hashes ([doc 11](11-obfuscated-network-values.md)) indicates the devs
   knew memory editing is the primary cheat vector and made it detectable rather than
   impossible.

## 7. Evidence appendix

| Claim | Evidence |
|---|---|
| Ownership error string | `IsilDump/Assembly-CSharp/Bank.txt:513`, `:905`, `:1327` |
| Bank deposit guard chain | `IsilDump/Assembly-CSharp/Bank.txt:1236`, `:1245`, `:1252`, `:1254`, `:1301`, `:1306` |
| AddSimpleItemToInventory body | `IsilDump/Assembly-CSharp/Inventory.txt:69217` (method), `:69428`, `:69442` |
| RPC attribute counts | regex over `DecompilerTool/ilrecovery_out/*.cs`, 2026-09-29 |
| SellItemToShop guards | `IsilDump/Assembly-CSharp/NPCShop.txt:12985ff` (native disasm) |

## 8. Open questions / follow-ups

- [ ] `Party.AddXPToPartyServerRpc` body — is `xp` clamped? (`IsilDump/Assembly-CSharp/Party.txt`)
- [ ] `Damages.SendDamageToObjectsCommonServerRpc` — how much of `Damage` is client-controlled?
- [ ] `NPCShop.SellItemToShopServerRpc` — confirm whether the sold item is removed from the seller's inventory server-side.
- [ ] `DeedManager` / `EventsManager` — spoofed progression reports?
- [ ] Inventory `SendInventorySnapshotServerRpc` — reconciled against what, and by whom?
