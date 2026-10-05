# Anti-Cheat / Cheat-Blocking Audit — Task Tracker

Project: Dimraeth — reverse-engineered documentation of every multiplayer cheat-blocking mechanism.
Docs location: `modding/docs/anticheat/` (this folder)
Language: English. Scope: pure game-mechanism analysis (no mod interop).

## Source-of-truth map (verified 2026-09-29)

| Source | What it gives us |
|---|---|
| `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/*.txt` | **Real method bodies** (native disasm + readable ISIL pseudo-ops). Primary evidence. |
| `modding/DecompilerTool/ilrecovery_out/*.cs` | Clean C# signatures (bodies are `throw null` stubs). Use for exact API surface. |
| `modding/DecompilerTool/gearlegality_out/*.cs` | Il2CppInterop wrapper signatures for GearLegality. |
| `modding/cpp2il_cs_out/DiffableCs/Assembly-CSharp/*.cs` | cpp2il C# skeleton (empty bodies), cross-check for signatures. |

## Task status legend
`[x]` done · `[~]` in progress · `[ ]` to do

---

## P0 — Setup & inventory

- [x] T0.1 Inventory the full anti-cheat surface (grep sweep over dumps + stubs)
- [x] T0.2 Create `modding/docs/anticheat/` + this tracker

### Inventory result (the mechanisms to document)

| # | Mechanism | Core types | Enforcement outcome |
|---|---|---|---|
| 1 | Gear legality sweep | `GearLegality`, `GearLegality.Violation` | Destroy item + log |
| 2 | Player anti-cheat loop | `Player` (InitializeAntiCheat, value-changed hooks, PeriodicAntiCheatCheck, PeriodicRuneIntegrityCheck, KickForCheat, DisconnectAfterDelay) | Kick/disconnect |
| 3 | Character identity integrity | `CharacterIdentityIntegrity`, `IdentityVerdict` (Stamp/Reconcile/FailsIdentityCheck) | Reject identity / revert fields |
| 4 | Character plausibility | `CharacterPlausibility` (ExceedsPlausibleCaps, FilterImplausible, RejectsJoiningCharacter, SumPositive) | Hide character / reject join |
| 5 | Save codec identity tags | `SaveCodec`, `SaveCodec.IdentityHasher` (HKDF-SHA256 tags) | Tamper-evident save fields |
| 6 | Ban system | `BanListManager`, `BanListData`, `BanEntry` | Deny connection / host ban |
| 7 | Cheat codes (dev/QA) | `CheatCodeManager`, `CheatCodeData`, `QACheat`, `QARune` | Legit cheats, gated |
| 8 | Cheat telemetry | `GameAnalyticsManager` (FlagSessionAsCheating, FetchCheatEvents, PatchIdentityFields) | Server-side cheat flag |

---

## P1 — Mechanism docs (1 doc per mechanism)

- [x] T1.1 `02-gear-legality.md` — GearLegality: all 10 methods reconstructed from ISIL + all call sites (Inventory, Storage, Runes) (done 2026-09-29; caps: Rarity≤4, Level≤12, Stars≤2, ≤6 secondaries, ≤4 upgrades/stat; value ceiling = 2×CalculateFinalStatValue(CalculateStatBaseValue(stat, Stars.Three), primary?12:5)+K, K=unknown float at 0x18465DE18; value check fails OPEN for unknown stats; destructive paths gated by GameConfig statics+0x198 (Inferred: DestroyUnobtainableGear))
- [x] T1.2 `03-player-anticheat.md` — Player anti-cheat: 7 methods + 3 coroutines, watched NetworkVariables, kick flow (done 2026-09-29; scope expanded 28 methods incl. XP/rune HMAC + SpeedHackDetectionRoutine; timings verified against GameAssembly.dll bytes: XP loop 5s/1s, rune loop 10s/30s; escalation: auto-repair (skill points) → save-block (_saveBlockedDueToCheat) → kick + DisconnectAfterDelay(0.5f))
- [x] T1.3 `04-character-identity-integrity.md` — CharacterIdentityIntegrity: Canonical/Field/Stamp/Reconcile/FailsIdentityCheck + SaveSystem call sites (done 2026-09-29; IdentityVerdict {Intact, Repaired, Unrecoverable}, fail-BY-DESIGN (Unrecoverable never blocks play), canonical v1 "…|name=|hash=|race=|cls=" + legacy 5-field, Reconcile brute-forces field combos against the HMAC tag: 2+ matches → ambiguous, 1 → repair fields, 0 → "no legal identity reproduces this tag")
- [x] T1.4 `05-character-plausibility.md` — CharacterPlausibility: caps, SumPositive, FilterImplausible, RejectsJoiningCharacter call sites (done 2026-09-29; flat caps, strict >, order level→skill→attributes; FilterImplausible HIDES (never deletes) from char select; SpawnManager.PlayerCannotJoinServer kicks with "modified character file" message — no ban)
- [x] T1.5 `06-save-codec-identity.md` — SaveCodec: IdentityHasher, HkdfSha256, DeriveSubkey, ComputeIdentityTag, encode/decode integrity (done 2026-09-29; envelope JSON→UTF-8→GZip→AES-CBC + HMAC-SHA256, HKDF-SHA256 subkeys labeled "enc"/"mac"/"identity", salt "Dimraeth.CharacterIdentity.v1" verified at SaveCodec.txt:5569, identity tag = Base64(32B HMAC) stored at PlayerData+0x78; master key reconstructible from compiled-in S0..S3 + LCG — obfuscation not secrecy; SaveNameVerdict has ZERO identity-tag coupling)
- [x] T1.6 `07-ban-system.md` — BanList*: storage format, enforcement in CustomNetworkManager/Steam/ServerList, UI (done 2026-09-29; storage = AES+HMAC'd .jrsf container at Security/GlobalBanList.jrsf via SaveCodec.EncryptAndSerialize, atomic write + .bak1 fallback; enforcement = connection approval "You are banned from this server" + lobby kick; mutations UI/host-only — no network-reachable mutation method exists)
- [x] T1.7 `08-cheat-codes-qa.md` — CheatCodeManager/Data, QACheat, QARune: what cheats exist, how gated (done 2026-09-29; key finding: key-sequence cheats INERT in this build — Update() is an empty ret and no caller of CheckCheatCodeInput/ActivateCheat exists in Assembly-CSharp, independently re-verified by grep; QACheat/QARune = anti-cheat self-tests + contraband spawner for the GearLegality sweep)
- [x] T1.8 `09-telemetry-cheat-events.md` — GameAnalyticsManager: FlagSessionAsCheating, FetchCheatEvents, PatchIdentityFields (done 2026-09-29; key finding: Supabase REST backend, URL const verified at ilrecovery_out/ActivityEvent.cs:980; trigger chain OnIntegrityViolation → OnCheatDetected → cheat_attempted event + session PATCH {"cheating":true}; all gates client-side)
- [x] T1.9 `11-obfuscated-network-values.md` — ObfuscatedNetworkInt/Float: obfuscation + integrity hash + violation event (added 2026-09-29: discovered during T2.1 survey; done 2026-09-29, spot-checked Murmur constants vs ObfuscatedNetworkInt.txt:531/535/546)
## Discovered during doc work (must be covered)

> **Status 2026-09-29: ALL RESOLVED** — every item below is covered in
> `03-player-anticheat.md` (verified present, strings re-checked against dumps):
> XP HMAC (`Player.txt:15360`), rune HMAC (`Player.txt:18547`), speedhack
> (`Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:481`), mint ledger =
> legacy skill-point amnesty `"LegacySkillPointReconciliation"`/DIM-10921
> (`Player.txt:28164ff`, callers `SkillTree.txt:19436/23948`), `OnCheatDetected` +
> `CheatAttempted` (in `09`), `ClientDiagnostics` subscriber (in `11`).
> Kept below for provenance.

Found via T1.9's evidence sweep — extra `Player` anti-cheat members NOT in the original
T1.2 scope. Must appear in `03-player-anticheat.md` (added by follow-up if the original
draft missed them):

- `Player.ValidateXPState` — XP HMAC check (`Player.txt:15360, 15371`), string `"HMAC violation: player={0}"`
- `Player.ValidateRuneState` — rune slot HMAC (`Player.txt:18504, 18547, 18559`), `"Rune HMAC violation: player={0}, slot={1}"`
- `Player.ComputeXPStateHMAC` (`Player.txt:8799`) + `Player.HmacMix` (`Player.txt:9241`)
- `Player.ValidateLoadedXPData` (`Player.txt:4905–5060`), `Player.ValidateAttributeXPConsistency` (`Player.txt:6184–6277`)
- `Player.SpeedHackDetectionRoutine` coroutine (`Player_NestedType__SpeedHackDetectionRoutine_d__210.txt:481, 493`), `"Speedhack: player={0}, ratio={1:F2}"`
- `GameAnalyticsManager.OnCheatDetected` + `CheatAttempted` event → Supabase telemetry (cover in `09-telemetry-cheat-events.md`)
- `ClientDiagnostics.OnIntegrityViolation` tamper-flag subscriber (cover in `11` or `09`)
- Player "mint ledger / amnesty" string: `(pre-DIM-10921 mint). Ledger entry kept at 0 so the amnesty cannot run again.` (`Player.txt:28404`) — find the rune-mint ledger mechanism and document it (likely in `03`)

## P2 — Synthesis

- [x] T2.1 `10-network-authority-surface.md` — where client input enters (ServerRpc surface) and which guards run per subsystem (done 2026-09-29; open follow-ups listed in doc §8: Party XP clamp, Damage authority, NPCShop sell possession check, Deed/Event spoofing)
- [x] T2.2 `01-overview.md` — defense layers, enforcement timeline, glossary, source index (done 2026-09-29)
- [x] T2.3 Final verification pass: every doc's claims spot-checked against ISIL line refs; cross-links valid (done 2026-09-29 — spot-checks run as each doc landed: Murmur constants, Supabase URL const, CheatCodeManager zero-caller claim, GearLegality caps vs raw disasm, ban/plausibility/identity log strings, XP/rune/speedhack strings — ALL matched; link + section + evidence-tag audit clean: 876 confidence tags across 11 docs)

**PROJECT STATUS 2026-09-29: COMPLETE.** 11 docs + tracker in `modding/docs/anticheat/`.
Note: `graphify update` was not run — no code in `BepInExModsSource` was modified (docs only);
the graphify `update` shim is separately reported broken (missing `~\.local\bin\graphify` script).

## P3 — Visualization (requested 2026-09-29)

- [x] T3.1 `GRAPH.md` — Mermaid diagrams: defense pipeline, mechanism relationship map, kick-escalation sequence, node color key (done 2026-09-29)
- [x] T3.2 `graph.html` — self-contained interactive graph (2 views, hover tooltips, click node = open its doc, no dependencies, offline-safe) (done 2026-09-29; verified programmatically: 39/39 nodes wired, 0 text overflows, 0 overlaps, 0 out-of-canvas; fixed renderer bug where node geometry rendered outside its <g> so hover/click never fired)

---

## P4 — Standalone bypass mod (requested 2026-09-29)

- [x] T4.1 Built the standalone BepInEx plugin `AntiCheatBypassMod` (2026-09-29 21:36):
  `modding/BepInExModsSource/AntiCheatBypassMod/` → `AntiCheatBypassMod.csproj`,
  `AntiCheatBypassModPlugin.cs`, `AntiCheatBypassPatches.cs`.
  Purpose: neutralise every mechanism documented in P1 so the player can cheat freely,
  including spawning contraband gear and taking over-cap characters into multiplayer.

  | Mechanism (doc) | Target(s) patched | Effect |
  |---|---|---|
  | 02 Gear legality | `GearLegality.Inspect` | returns `Violation.None`; no item destruction |
  | 05 Plausibility | `CharacterPlausibility.FilterImplausible` / `IsImplausible` / `RejectsJoiningCharacter` | nothing hidden; multiplayer join gate open |
  | 04 Character identity | `CharacterIdentityFromSkillTree.Reconcile` (current build) | no skill-point/identity recompute |
  | 03 XP/rune/attr | `Player.ValidateXPState` / `ValidateXPInvariants` / `ValidateRuneState` / `ValidateAttributeXPConsistency` | always "clean" |
  | 03 Enforcement | `Player.KickForCheat` / `BlockSaveDueToCheatClientRpc` / `ReportLocalCheatServerRpc` | no kick, no save-block, no self-report |
  | 03 Skill-point clawback | `SkillTree.TryRepairOverGrantedSkillPoints` | cheated skill points kept |
  | 05 Speedhack | `Player/<SpeedHackDetectionRoutine>d__210.MoveNext` | coroutine stops; no direct `LoadManager.ExitGame` |
  | 07 Ban | `BanListManager.IsBanned` / `BanListData.IsBanned` | banned players can connect/join (user's explicit choice) |
  | 09 Telemetry | `GameAnalyticsManager.OnCheatDetected` / `CheatAttempted` | no `cheat_attempted` / `cheating=true` sent |
  | 11 Obfuscated values | `ObfuscatedNetworkInt.RaiseIntegrityViolation` / `RepairBaseValue`, `ObfuscatedNetworkFloat.RepairBaseValue` | violation pipeline dead; no base-slot rollback |

  Key decisions:
  - All targets patched **by name** via `AccessTools.TypeByName` for update-proofness; a missing
    target logs a warning instead of aborting.
  - **Discovery (2026-09-30):** the 2026-09-26 game update removed `CharacterIdentityIntegrity`
    (with `IdentityVerdict` / `FailsIdentityCheck` / `Stamp`) and replaced it with
    `CharacterIdentityFromSkillTree` (`Reconcile(PlayerData, out string) -> bool`, private
    `Resolve` / `TryTreeOf`). Confirmed by reading `global-metadata.dat` and the interop
    `Assembly-CSharp.dll` (both 2026-09-26) and by enumerating all 20 target methods with Cecil:
    19 resolve in the current build; only the removed type was absent. Doc `04` and the pre-update
    dumps describe the old build; there is no body dump for the new type, so its `Reconcile` is
    skipped conservatively (no mutation of player data).
  - Polarity: the three validators return **true = clean**; the predicates are benign at **false**;
    `CharacterIdentityFromSkillTree.Reconcile` is skipped (returns false = "nothing reconciled").
  - `ObfuscatedNetworkInt/Float.HandleNetworkValueChanged` and `get_Value` are deliberately **left
    intact** so legitimate client sync still works; mod-based value edits use the trusted
    `set_Value` path, which was never detected.
  - `Player.InitializeAntiCheat` is **not** no-op'd: it keeps the HMAC re-seed subscriptions alive so
    legitimate saves stay consistent for vanilla peers; the loops are neutralised at the validator /
    `KickForCheat` choke points instead.
  - UI: sticky top-left `anticheat bypassed` label (IMGUI rich text, `bypassed` in `#00FF00`).
  - No config / enable flag by design (delete the DLL to revert), per "minimum code" principle.

  Verification: Release build clean (0 warnings / 0 errors, SDK `modding/dotnet-sdk/dotnet.exe`
  6.0.428); `AntiCheatBypassMod.dll` + `.pdb` + `.deps.json` deployed to **both**
  `modding/BepInEx/plugins/` and game-root `BepInEx/plugins/`. **Not yet runtime-tested in-game**
  (T4.2).

  Note (2026-09-30): the game held the game-root DLL locked during the final rebuild (after the
  identity retarget); once it was closed the updated DLL/pdb were re-copied, so both plugin
  locations now hold the rebuilt assembly.
- [ ] T4.2 Runtime test: spawn contraband gear, load an over-cap character, join multiplayer, tamper
  values, confirm no destroy / hide / kick / save-block / telemetry and that the ban bypass works.
- [x] T4.3 (2026-09-30 02:47) Reversed the earlier "fold in" plan at the user's request: keep
  `AntiCheatBypassMod` standalone and **strip** the bypass from the two mods.
  `DimraethModPack`'s `AntiCheatBypassModule` registration removed at `DimraethModPackPlugin.cs:100`
  (source preserved comment-only in `Modules/System/AntiCheatBypassModule.cs`, per repo rule;
  no other module referenced it). `EquipmentStatEditor`'s `Core/GearLegalityGuard.cs` removed
  (file preserved comment-only) and its `SetBypass` call dropped from the plugin.
  The editor's "Safe/Bypass mode" toggle was renamed to **Vanilla caps / Modded caps**:
  config `General.BypassMode` -> `General.UseModdedCaps`, `StatCapCatalog.BypassMode` ->
  `ModdedCaps` / `BypassCapMultiplier` -> `ModdedCapMultiplier`, `SetBypassMode` -> `SetCapMode`.
  The toggle now only selects the UI/validation ceiling; item destruction is owned unconditionally
  by the standalone mod, so over-cap items survive in either editor mode.
  Both mods rebuilt clean (0 errors) with SDK `modding/dotnet-sdk/dotnet.exe` and auto-deployed to
  both `modding/BepInEx/plugins/` and game-root `BepInEx/plugins/`.
  NOTE (config migration): the old `General.BypassMode` value is no longer read; the editor
  defaults to Vanilla caps. Set `General.UseModdedCaps=true` to restore the 99x ceilings.

Note: `graphify query` was run before coding (surfaced `AntiCheatBypassModule`, `GearLegalityGuard`,
`InspectPrefix`, `TryPatch` — the exact prior art reused). `graphify update .` was attempted but the
shim is still broken (`graphify.exe` → missing `~\.local\bin\graphify` python script; exits 0 without
updating), so the knowledge graph was **not** refreshed with the new project.

---

## Conventions (every doc must follow)

- English, technical terms kept as-is (ServerRpc, NetworkVariable, …).
- Every behavioral claim cites `file:line` (ISIL dump line) or `file (stub signature)`.
- Explicitly separate **Verified from dump** vs **Inferred** vs **Unknown**.
- Cover **every** method of the mechanism — private included.
