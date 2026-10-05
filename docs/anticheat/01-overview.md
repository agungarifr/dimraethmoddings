# 01 — Overview: Dimraeth's multiplayer cheat-blocking architecture

> Entry point for the anti-cheat audit. Reverse-engineered from the IL-recovery dumps
> (real method bodies) — see [Source index](#source-index). Every doc marks claims as
> **Verified / Inferred / Unknown**; this overview only aggregates those verdicts.

## 1. What is here

| # | Mechanism | Doc | When it runs | Outcome on detection |
|---|---|---|---|---|
| 1 | Gear legality sweep | [02](02-gear-legality.md) | Inventory load, storage load, equip refresh | Item **destroyed** + log |
| 2 | Player runtime anti-cheat | [03](03-player-anticheat.md) | 5s/1s (XP) + 10s/30s (rune) loops, on-change hooks, save time | Auto-repair → save-block → **kick** |
| 3 | Character identity integrity | [04](04-character-identity-integrity.md) | Save load / save write (Stamp/Reconcile) | Field **repaired** or fail-open |
| 4 | Character plausibility | [05](05-character-plausibility.md) | Character select, network spawn, join | Character **hidden** / join **rejected** |
| 5 | Save codec identity tags | [06](06-save-codec-identity.md) | Every save read/write | Tamper-evident envelope |
| 6 | Ban system | [07](07-ban-system.md) | Connection approval, lobby join | Connection **denied** / kick |
| 7 | QA cheat tooling | [08](08-cheat-codes-qa.md) | Manual (QA) | N/A — tests the above |
| 8 | Cheat telemetry | [09](09-telemetry-cheat-events.md) | On integrity violation | Session flagged `cheating=true` |
| — | Network authority perimeter | [10](10-network-authority-surface.md) | Every ServerRpc | Per-RPC guards (varies) |
| — | Obfuscated network values | [11](11-obfuscated-network-values.md) | Every value read/write/sync | Repair + violation event |

## 2. The trust model in one paragraph (Inferred synthesis)

Multiplayer is **host-authoritative in shape but client-trusted in practice**: the large
majority of state-mutating RPCs are `[ServerRpc(RequireOwnership = false)]` (~315 vs ~78
ownership-required), and some — notably `Inventory.AddSimpleItemToInventoryServerRpc` —
apply **no validation at all** ([doc 10 §4.2](10-network-authority-surface.md)). The
developers' compensating design is **deterrence and post-hoc destruction** rather than
prevention: obfuscated values with integrity hashes trip memory editors
([doc 11](11-obfuscated-network-values.md)), periodic HMAC/rune checks trip tampering and
kick ([doc 03](03-player-anticheat.md)), unobtainable gear is quietly destroyed
([doc 02](02-gear-legality.md)), implausible saves are hidden ([doc 05](05-character-plausibility.md)),
and everything feeds a Supabase cheat log ([doc 09](09-telemetry-cheat-events.md)).

## 3. Defense timeline

```
CONNECT        CustomNetworkManager.ApproveConnection ── BanListManager.IsBanned
                 └─ deny: "You are banned from this server"                     [07]
LOBBY          Steam.OnLobbyMemberJoined ── IsBanned → kick                     [07]
JOIN           SpawnManager.PlayerCannotJoinServer ── CharacterPlausibility
                 └─ reject: "Server rejected modified character file"           [05]
CHAR SELECT    CharacterSelection ── FilterImplausible (hide, never delete)     [05]
LOAD SAVE      SaveCodec decrypt+MAC ── CharacterIdentityIntegrity.Reconcile
                 └─ repair fields / "no legal identity reproduces this tag"     [04][06]
LOAD INV       Inventory.LoadInventory ── GearLegality.CollectContraband        [02]
LOAD STORAGE   Storage.SetStorageData ── GearLegality.PurgeContainer            [02]
EQUIP          Runes.UpdateRuneBonuses ── GearLegality.PurgeEquipped            [02]
RUNTIME        ObfuscatedNetwork* reads/writes/sync ── integrity hash           [11]
               Player value-changed hooks ── XP/rune HMAC (ValidateXPState /
               ValidateRuneState), KickForCheat                                 [03]
               SpeedHackDetectionRoutine (time-ratio check)                     [03]
EVERY 5s/1s    PeriodicAntiCheatCheck (XP invariants)                           [03]
EVERY 10s/30s  PeriodicRuneIntegrityCheck (rune slot HMACs)                     [03]
ON VIOLATION   OnIntegrityViolation → ClientDiagnostics flag +
               GameAnalyticsManager.OnCheatDetected → "cheat_attempted" event +
               FlagSessionAsCheating (PATCH game_sessions {"cheating":true})    [09][11]
SAVE           SaveGame re-runs all validators; _saveBlockedDueToCheat gates    [03]
               CharacterIdentityIntegrity.Stamp + SaveCodec MAC                 [04][06]
KICK           KickForCheat → BlockSaveDueToCheatClientRpc +
               DisconnectAfterDelay(0.5f) → DisconnectClient                   [03]
```

## 4. Escalation ladder (Player, [doc 03](03-player-anticheat.md))

Detection is graduated rather than instant-ban:

1. **Auto-repair** — skill-point over-grant is non-fatal: `SkillTree.TryRepairOverGrantedSkillPoints`,
   ledger retired via `"LegacySkillPointReconciliation"` (DIM-10921 amnesty).
2. **Save block** — `_saveBlockedDueToCheat = 1`, propagated to the owner via
   `BlockSaveDueToCheatClientRpc`; `SaveGame` refuses to persist.
3. **Kick** — `KickForCheat` → `DisconnectAfterDelay(0.5f)` → `DisconnectClient(OwnerClientId)`;
   if the cheater is local: `LoadManager.ExitGame()`.

## 5. Verified weaknesses & honest gaps

| Finding | Evidence level | Where |
|---|---|---|
| `AddSimpleItemToInventoryServerRpc` grants items with **zero validation** | Verified | [10 §4.2](10-network-authority-surface.md) |
| GearLegality value ceiling **fails open** for stats absent from `RuneManager` tables; NaN passes the `>` test | Verified | [02](02-gear-legality.md) |
| Identity integrity is **fail-by-design**: `Unrecoverable` never blocks play | Verified | [04](04-character-identity-integrity.md) |
| Save-codec master key **reconstructible** from compiled-in `S0..S3` + LCG | Verified | [06](06-save-codec-identity.md) |
| Telemetry gates are **client-side**; anon Supabase key ships in the binary | Verified | [09](09-telemetry-cheat-events.md) |
| Key-sequence cheats are **inert** in this build (empty `Update()`, zero callers) | Verified (2 independent checks) | [08](08-cheat-codes-qa.md) |
| `Inventory.PurgeUnobtainableRunes` is **dead code** (no callers) | Verified | [02](02-gear-legality.md) |
| `NPCShop.SellItemToShopServerRpc` possession check unresolved | Unknown | [10 §8](10-network-authority-surface.md) |

## 6. Log-string index (grep targets)

| Prefix / string | Mechanism |
|---|---|
| `[GearLegality] Destroyed unobtainable equipment on …` | [02](02-gear-legality.md) |
| `[AntiCheat]` (`HMAC violation: player={0}`, `Rune HMAC violation: …`, `CHECK 5 — SkillPoints over grant cap`) | [03](03-player-anticheat.md) |
| `Speedhack: player={0}, ratio={1:F2}` | [03](03-player-anticheat.md) |
| `[CharacterIdentityIntegrity] …` (`more than one identity reproduces this tag`, `restored {0} {1}`, `no legal identity reproduces this tag`) | [04](04-character-identity-integrity.md) |
| `[CharacterPlausibility] Hiding character '…'` | [05](05-character-plausibility.md) |
| `You are banned from this server` / `Server rejected modified character file` | [07](07-ban-system.md) / [05](05-character-plausibility.md) |
| `cheat_attempted` (Supabase `analytics_events`) | [09](09-telemetry-cheat-events.md) |
| `…integrity violation: ` / `…base.Value tamper: ` / `…base.Value setter bypass: ` | [11](11-obfuscated-network-values.md) |
| `Only the owner can invoke a ServerRpc that requires ownership!` (NGO generated) | [10](10-network-authority-surface.md) |

## 7. Glossary

- **Sweep** — post-hoc scan destroying/reverting already-loaded bad state (GearLegality).
- **Contraband** — a `Rune` that `GearLegality.Inspect` flags (`Violation != None`).
- **Identity tag** — 44-char Base64 HMAC-SHA256 over canonicalized identity fields.
- **ObfuscatedNetworkInt/Float** — `NetworkVariable` subclass with XOR shadow + integrity hash.
- **Fail-open** — a check that returns "valid" when it cannot decide (several exist here).
- **`.jrsf`** — the game's encrypted save container (magic `FILE_MAGIC`, AES-CBC + HMAC).

## 8. Source index

| Source | Content |
|---|---|
| `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/*.txt` | **Primary.** Real method bodies per type: `Method:` headers, raw x86-64 `Disassembly:`, then readable numbered pseudo-ops (`Call`, `Move`, `Compare`, `JumpIf…`, `Goto {n}`, `Return`). |
| `modding/DecompilerTool/ilrecovery_out/*.cs` | Clean C# signatures (`throw null` stubs) + preserved `const` strings/values. |
| `modding/DecompilerTool/gearlegality_out/`, `rune_out/`, `netcode_out/`, `coll_out/` | Il2CppInterop wrapper dumps (exact generated signatures). |
| `modding/cpp2il_cs_out/DiffableCs/Assembly-CSharp/*.cs` | cpp2il skeletons, cross-check for signatures. |
| `GameAssembly.dll` (game root) | Raw natives; used to verify timing constants (doc 03 §9.2–9.3). |

## 9. Doc index

02 GearLegality · 03 Player runtime anti-cheat · 04 Character identity integrity ·
05 Character plausibility · 06 Save codec identity tags · 07 Ban system ·
08 QA cheat tooling · 09 Cheat telemetry · 10 Network authority perimeter ·
11 Obfuscated network values — plus `TASKS.md` (tracker & provenance).
