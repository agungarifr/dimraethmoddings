# 00 — Anti-cheat architecture graph (Mermaid)

> Visual companion to the docs. Renders in GitHub, VS Code (Markdown Preview Mermaid
> Support), and any Mermaid live editor. Interactive version: [`graph.html`](graph.html)
> (open in a browser).

## 1. Defense pipeline (timeline: connect → kick)

```mermaid
flowchart TD
    subgraph GATES["Entry gates"]
        CONNECT["CONNECT<br/>Ban system [07]"]
        JOIN["JOIN / SPAWN<br/>Character plausibility [05]"]
    end

    subgraph LOAD["Load-time sweeps"]
        SAVELOAD["SAVE LOAD<br/>Save codec [06]<br/>+ identity integrity [04]"]
        GEARLOAD["GEAR LOAD / EQUIP<br/>GearLegality sweep [02]"]
    end

    subgraph RUNTIME["Runtime monitoring"]
        OBF["ObfuscatedNetworkInt/Float<br/>integrity hash [11]"]
        HOOKS["Player value-changed hooks<br/>XP / rune HMAC [03]"]
        LOOP1["PeriodicAntiCheatCheck<br/>every 5s / 1s [03]"]
        LOOP2["PeriodicRuneIntegrityCheck<br/>every 10s / 30s [03]"]
        SPEED["SpeedHackDetectionRoutine [03]"]
    end

    subgraph OUT["Enforcement outcomes"]
        DENY["deny connection / kick"]
        HIDE["hide character / reject join"]
        REPAIR1["repair identity fields<br/>(fail-open if unrecoverable)"]
        DESTROY["destroy contraband gear"]
        AUTOFIX["auto-repair skill points"]
        BLOCK["block save"]
        KICK["kick + disconnect 0.5s"]
    end

    TELE["Cheat telemetry [09]<br/>Supabase: cheat_attempted<br/>+ session cheating=true"]

    CONNECT -->|IsBanned| DENY
    JOIN -->|RejectsJoiningCharacter| HIDE
    SAVELOAD -->|Reconcile| REPAIR1
    GEARLOAD -->|Inspect / Purge| DESTROY
    OBF -->|OnIntegrityViolation| TELE
    HOOKS -->|ValidateXPState / ValidateRuneState| KICK
    LOOP1 -->|ValidateXPInvariants| AUTOFIX
    AUTOFIX -.->|persists| KICK
    LOOP2 -->|ValidateRuneState| KICK
    SPEED -->|ratio threshold| KICK
    KICK --> BLOCK
    KICK --> TELE
    DESTROY --> TELE
```

## 2. Who talks to whom (mechanism relationship map)

```mermaid
flowchart LR
    CLIENT["Modified client<br/>(cheater)"]

    subgraph PERIMETER["Perimeter (doc 10)"]
        RPC["ServerRpc surface<br/>~315 any-client, ~78 owner-only"]
    end

    subgraph STATE["Shared game state"]
        GOLD["Gold / vitals / XP<br/>ObfuscatedNetwork* (doc 11)"]
        RUNES["Runes & gear"]
        SAVES["Save files .jrsf<br/>SaveCodec (doc 06)"]
        IDENT["Identity tag<br/>(doc 04)"]
    end

    subgraph DETECT["Detectors"]
        GL["GearLegality (doc 02)"]
        PAC["Player anti-cheat (doc 03)"]
        CII["CharacterIdentityIntegrity (doc 04)"]
        CP["CharacterPlausibility (doc 05)"]
        BL["BanListManager (doc 07)"]
    end

    subgraph QA["QA tooling (doc 08)"]
        QAR["QARune contraband spawner"]
        QAC["QACheat tamper tests"]
    end

    GAM["GameAnalyticsManager (doc 09)"]
    SB[("Supabase<br/>analytics_events / game_sessions")]
    HOST["Host (ban / kick UI)"]

    CLIENT -->|"AddSimpleItem / Damage / Sell…"| RPC
    RPC --> STATE
    CLIENT -.->|"memory edit"| GOLD
    CLIENT -.->|"save edit"| SAVES
    GOLD -->|"integrity hash"| PAC
    RUNES -->|"Inspect / HMAC"| GL
    RUNES -->|"rune HMAC"| PAC
    SAVES --> CII
    IDENT --> CII
    SAVES --> CP
    HOST -->|"BanPlayer / UnbanPlayer"| BL
    BL -->|"ApproveConnection"| CLIENT
    QAR -->|"spawn contraband"| RUNES
    QAC -->|"tamper on purpose"| GOLD
    PAC -->|"violation events"| GAM
    GOLD -->|"OnIntegrityViolation"| GAM
    GL -->|"NoteContrabandDestroyed"| GAM
    GAM --> SB
```

## 3. Kick escalation sequence (doc 03)

```mermaid
sequenceDiagram
    participant T as Tamperer (memory/save)
    participant V as ObfuscatedNetwork* / HMAC checks
    participant P as Player anti-cheat
    participant S as SaveGame
    participant N as Network (host)

    T->>V: edit value / rune / XP
    V->>P: OnIntegrityViolation / ValidateXPState fail
    Note over P: skill-point over-grant?<br/>auto-repair first (DIM-10921 ledger)
    P->>P: _saveBlockedDueToCheat = 1
    P->>N: ReportLocalCheatServerRpc
    N->>N: KickForCheat()
    N-->>P: BlockSaveDueToCheatClientRpc
    P->>S: SaveGame() → refused (validators re-run)
    N->>N: DisconnectAfterDelay(0.5f)
    N-->>T: DisconnectClient(OwnerClientId)
    Note over N: violation also → Supabase [09]
```

## 4. Node key

| Color (in `graph.html`) | Category |
|---|---|
| Blue | Perimeter (network authority) |
| Purple | Detectors / validators |
| Gray | Data & storage |
| Red | Enforcement outcomes |
| Orange | Telemetry |
| Green | QA tooling |
