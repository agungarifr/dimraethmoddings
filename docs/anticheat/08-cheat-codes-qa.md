# 08 — Cheat codes & QA tooling (`CheatCodeManager`, `CheatCodeData`, `QACheat`, `QARune`, `QAAuto`)

Scope: `CheatCodeManager` + `CheatCodeData` (cheat-code sequences), `QACheat` (anti-cheat self-test harness),
`QARune` / `ContrabandKind` (illegal-gear generator that exercises the `GearLegality` sweep), and a brief note on
`QAAuto` (skill-tree verification harness — QA tooling, not anti-cheat).

Sources: decompiled IL2CPP output. Two flavors are cited:
- `IsilDump/Assembly-CSharp/<Type>.txt` — real method bodies (x64 disassembly + ISIL pseudo-IL). Line numbers cited as `<file>:<line>`.
- `DecompilerTool/ilrecovery_out/<Type>.cs` — signature stubs (bodies are `throw null`). Used only for names/visibility/attributes.

Every behavioral claim is tagged **Verified** (directly readable in the dump), **Inferred** (consistent with the dump but
requires a judgment call), or **Unknown** (unreadable or not present in the dump).

---

## Purpose & threat model

**Purpose.** This group is *developer/QA tooling that touches anti-cheat surfaces*, not anti-cheat enforcement itself:

1. `CheatCodeManager` — a Konami-style key-sequence unlock system (ScriptableObject-driven `KeyCode` lists) whose
   "cheats" are presentation/god-mode toggles invoked through UnityEvents.
2. `QACheat` — a self-test harness that deliberately tampers with `ObfuscatedNetworkInt` state via reflection to prove
   the integrity system detects and reverts it (XP, Gold, Level, cache, hash, attributes).
3. `QARune` — spawns *contraband runes* (illegal rarity/stars/level/stat combinations) into the world or inventory so QA
   can verify the `GearLegality` sweep destroys them (`"[GearLegality] Destroyed"`).
4. `QAAuto` — skill-tree/spell/cost/cooldown verification; included only to document that it does **not** touch anti-cheat.

**Threat model.**
- `QACheat`/`QARune` are *attack-surface documentation*: they show exactly which fields an attacker would target
  (`m_InternalValue`, `_obfuscatedCache`, `_integrityHash`) and what the defensive response is. In a release build they are
  the most dangerous kind of leftover — live MonoBehaviour code that writes game state without server validation.
- `CheatCodeManager` sequences are gated only by input + `PlayerPrefs` state; if reachable in release they unlock
  whatever UnityEvents the scene wires up.
- The QA harnesses themselves require **server/host** authority for most actions (`NetworkManager.IsServer`), which limits
  remote abuse but not local/cheat-client abuse.

---

## API surface

### CheatCodeManager (`ilrecovery_out/CheatCodeManager.cs`)

| Signature | Visibility | Role |
|---|---|---|
| `static CheatCodeManager Singleton` | static field | manager instance (set in `Awake`) |
| `List<CheatCodeData> cheatCodes` | field | configured cheat codes (offset 32) |
| `Dictionary<string, int> cheatProgress` | field | per-code key-sequence progress (offset 40) |
| `void Awake()` | Unity message | sets `Singleton`, seeds progress to 0, resets prefs, binds UI objects |
| `void Update()` | Unity message | **empty body — no per-frame input polling** |
| `void ResetCheatCodes()` | method | `PlayerPrefs.SetInt(code, 0)` + `Save()` for every code |
| `void AssignCheatCodeObjects()` | method | matches child `RectTransform`s to codes by name → `CanvasGroup` |
| `void CheckCheatCodeInput(CheatCodeData cheat)` | method | key-sequence matcher; fires UnityEvent on completion |
| `void ActivateCheat(CheatCodeData cheat)` | method | direct activation (same tail as completion path) |

### CheatCodeData (`ilrecovery_out/CheatCodeData.cs`) — ScriptableObject

| Signature | Visibility | Role |
|---|---|---|
| `string cheatCodeName` | field (offset 24) | code key + `PlayerPrefs` key |
| `List<KeyCode> keysToUnlock` | field (offset 32) | ordered key sequence |
| `CanvasGroup cheatCodeObject` | `[HideInInspector]` field (offset 40) | target object holding the trigger UnityEvent |
| `[CreateAssetMenu(fileName = "New CheatCode", menuName = "Cheat Code/Code Data")]` | attribute | asset authoring menu |

### QACheat (`ilrecovery_out/QACheat.cs`)

| Signature | Visibility | Role |
|---|---|---|
| `static QACheat Instance` | static field | singleton |
| `void Awake()` | Unity message | singleton dedupe (duplicates destroyed) |
| `void OnEnable()` / `OnDisable()` | Unity messages | subscribe/unsubscribe `ObfuscatedNetworkInt.OnIntegrityViolation` |
| `void OnViolation(string message)` | handler | logs `[AntiCheat Violation] …` |
| `Player GetPlayer()` | method | resolves `Player` from `DataStorage` |
| `void TestTamperBackingField()` | method | reflects `NetworkVariable<int>.m_InternalValue` = 999999 on XP |
| `void TestTamperBaseValueSetter()` | method | writes 999999 via `NetworkVariable<int>.Value` setter |
| `void TestTamperCache()` | method | corrupts `ObfuscatedNetworkInt._obfuscatedCache` = 12345 |
| `void TestTamperHash()` | method | corrupts `ObfuscatedNetworkInt._integrityHash` = 0xDEADBEEF |
| `void TestTamperLevel()` | method | `Level.Value = 99`; expects revert |
| `void TestTamperGold()` | method | reflects `Gold.m_InternalValue` = 999999 |
| `void TestTamperAttributes()` | method | `Memory += 50` direct write; expects periodic validation catch |
| `void RunAllTests()` | method | runs all 7 tests in order |
| `void LogResult(string testName, int expected, int actual)` | method | PASS/FAIL colored report |

### QARune (`ilrecovery_out/ContrabandKind.cs`, `ilrecovery_out/QARune.cs`)

| Signature | Visibility | Role |
|---|---|---|
| `static QARune Instance` | static field | singleton |
| `Runes.RuneSet SelectedSet` (offset 32) / `Runes.SlotType SelectedSlot` (offset 36) / `int Amount` (offset 40) | fields | spawn selection |
| `bool GenerateFullSet` | field | expand to every `SlotType` |
| `Contraband Contraband` | field | contraband category selector |
| `const float ContrabandStatMultiplier = 10f` | const | stat-inflation multiplier |
| `void SpawnRune()` | method | legal rune drop at random world position |
| `void SpawnLockedRune()` | method | loot-locked rune drop (tests locked-loot presentation) |
| `void SendRuneToInventory()` | method | legal rune straight into inventory |
| `void DeleteAllRunesInInventory()` | method | wipes rune inventory entries |
| `void SpawnContraband()` | method | corrupted rune drops into the world |
| `void SendContrabandToInventory()` | method | corrupted rune straight into inventory |
| `bool ContrabandPreflight()` | method | sanity gate + config warning |
| `Rune Corrupt(Rune rune, ContrabandKind kind)` | method | applies an illegal mutation |
| `void InflateStatValues(Rune rune)` | method | multiplies stat values by 10 |
| `IEnumerable<ContrabandKind> KindsToSpawn()` | method | expands `OneOfEach` |
| `Rune GenerateSelectedRune(Runes.SlotType slot)` | method | generates a legal rune for the selection |
| `string Describe(Rune rune)` | method | `"{0} {1} {2} {3} Lv{4} UUID={5}"` dump string |
| `enum ContrabandKind` | enum | `RedAndBogusStats=0, RedOnly=1, BogusStatValuesOnly=2, TooManyStars=3, OverLevelled=4, OverUpgraded=5, OneOfEach=6` |

### QAAuto (`ilrecovery_out/QAAuto.cs`) — *not anti-cheat*

49 methods, all skill-tree/spell verification: `VerifyBasicNodes`, `VerifyPassiveNodes`, `VerifySpellNodes`,
`VerifyCooldownNodes`, `VerifyCostNodes`, `SpawnTestDummy`, `ToggleSkillTreeVisualization`, etc. Log prefix
`"[QAAuto] "` / `"[QASkill] "` (`QAAuto.txt:445,500,607`). No anti-cheat fields, no network-integrity interaction.
**Verified** (method inventory `QAAuto.txt:3–16161`).

---

## Data model

### CheatCodeManager state (Verified)
- `Singleton` — static instance (`typeof+184` statics).
- `cheatCodes` — `List<CheatCodeData>` at instance offset 32.
- `cheatProgress` — `Dictionary<string, int>` at offset 40, keyed by `cheatCodeName`; built in `.ctor`/`Awake`.
- Persistence: `PlayerPrefs` int keyed by `cheatCodeName`; `1` = unlocked/activated, `0` = reset.
  Written with `PlayerPrefs.Save()` immediately (`CheatCodeManager.txt:887–889, 984–990`).

### CheatCodeData (Verified, stub)
`cheatCodeName` (string), `keysToUnlock` (`List<KeyCode>`), `cheatCodeObject` (`CanvasGroup`, hidden in inspector).
Created via `CreateAssetMenu(fileName = "New CheatCode", menuName = "Cheat Code/Code Data")`
(`ilrecovery_out/CheatCodeData.cs`).

### QACheat constants (Verified)
- `999999` (`0xF423F`) — XP/Gold tamper value (`QACheat.txt:854,862,2361`).
- `12345` (`0x3039`) — `_obfuscatedCache` corruption value (`QACheat.txt:1434`).
- `0xDEADBEEF` — `_integrityHash` corruption value (`QACheat.txt:1716`).
- `99` — `Level` tamper value (`QACheat.txt:2033–2034`).
- `+50` — `Memory` tamper delta (`QACheat.txt:2688–2691`).
- Reflection targets: `NetworkVariable<int>.m_InternalValue` (flags 36, `QACheat.txt:839`),
  `ObfuscatedNetworkInt._obfuscatedCache` (`QACheat.txt:1426`), `ObfuscatedNetworkInt._integrityHash` (`QACheat.txt:1708`).

### Player field offsets used by QACheat / telemetry (Verified)
`Level` @184, `Gold` @992, `XP` @1032, `AccumulatedXP` @1040, `AllTimeXP` @1048, `SkillPoints` @1056,
`Attributes` @368. Matches `ilrecovery_out/Player.cs:400–418` declaration order (`Gold … XP, AccumulatedXP,
AllTimeXP, SkillPoints`).

### Rune struct layout (Verified from `QARune.Describe`, `QARune.txt:6449–6861`)
| Offset | Field |
|---|---|
| 0 | `Runes.RuneSet` (int) |
| 4 | `Runes.SlotType` (int) |
| 8 | `Runes.Rarity` (int) |
| 12 | `Runes.Stars` (int) |
| 16–79 | `FixedString64Bytes` UUID |
| 80 | `Level` (int) |
| 0x88 (136) | stat values (`float` array, ushort count @136) |
| 0xA8 (168) | stat kinds (`int` array, ints begin @0xAA = 170) |

Legality references: `GenerateSelectedRune` uses rarity pool `{0,1,2,3,4}` (5 excluded ⇒ **Rarity 5 = illegal "red"**)
and star weights `{1,1}` (⇒ **Stars 4 = illegal**) (`QARune.txt:7434` region).

---

## Behavior per method

### CheatCodeManager

- **`Awake()`** — Verified (`CheatCodeManager.txt:3–201`). Sets `Singleton = this`; iterates `cheatCodes` writing
  `cheatProgress[cheat.cheatCodeName] = 0`; then calls `ResetCheatCodes()` and `AssignCheatCodeObjects()`.
- **`Update()`** — Verified (`CheatCodeManager.txt:203–209`). **Body is a single `Return`** — no `Input.GetKeyDown`
  polling. Nothing in Assembly-CSharp calls `CheckCheatCodeInput` or `ActivateCheat` (grep over `IsilDump/Assembly-CSharp`
  finds only the declarations). Key-sequence entry is therefore **inert in this build**.
- **`ResetCheatCodes()`** — Verified (`CheatCodeManager.txt:211–347`). Per cheat: `PlayerPrefs.SetInt(cheatCodeName, 0)`
  then `PlayerPrefs.Save()`.
- **`AssignCheatCodeObjects()`** — Verified (`CheatCodeManager.txt:349–631`). `GetComponentsInChildren` →
  `Enumerable.ToList` of `RectTransform`s (`:530–535`); per cheat builds a `<>c__DisplayClass6_0` closure and
  `Func<RectTransform,bool>`; `Enumerable.FirstOrDefault` (`:579`) matches child **by object name** (`Object.get_name`
  compared with `cheatCodeName`, `CheatCodeManager_NestedType___c__DisplayClass6_0.txt:47–58`); assigns
  `cheat.cheatCodeObject = rect.GetComponent<CanvasGroup>()` (`:596–602`).
- **`CheckCheatCodeInput(CheatCodeData)`** — Verified (`CheatCodeManager.txt:633–891`).
  `cheatProgress.TryGetValue(name)`; on miss logs `"No cheat progress entry found for " + cheatCodeName`
  (`:800`) and resets progress to 0; reads `Input.GetKeyDown(cheat.keysToUnlock[progress])`; on a wrong key while
  `Input.get_anyKeyDown` (`:835`) progress resets to 0; on the correct key progress increments; when
  `progress >= keysToUnlock.Count` it invokes `cheatCodeObject.GetComponent<T>().UnityEvent.Invoke()`
  (UnityEvent at component offset 256, `:879–883`), writes `PlayerPrefs.SetInt(name, 1)` + `Save()` (`:887–889`),
  and resets progress to 0.
- **`ActivateCheat(CheatCodeData)`** — Verified (`CheatCodeManager.txt:893–992`). Same tail: UnityEvent `Invoke()`
  (`:980`), `PlayerPrefs.SetInt(cheatCodeName, 1)` (`:984`), `PlayerPrefs.Save()` (`:990`).

### QACheat

- **`Awake()`** — Verified (`QACheat.txt:74–231`). Singleton: if `Instance != null && Instance != this` →
  `Object.Destroy(this.gameObject)` (`:213`), else `Instance = this`.
- **`OnEnable()` / `OnDisable()`** — Verified (`QACheat.txt:233–305 / 307–379`). `ObfuscatedNetworkInt.add_OnIntegrityViolation`
  (`:299`) / `remove_OnIntegrityViolation` (`:373`); sets `_listening` (offset 32) true/false.
- **`OnViolation(string)`** — Verified (`QACheat.txt:381–436`). Logs `Debug.LogWarning("[AntiCheat Violation] " + message)`
  (`:421, 435`).
- **`GetPlayer()`** — Verified (`QACheat.txt:438–571`). `DataStorage` statics → instance → field @32 (`Player`); null →
  `Debug.LogWarning("[AntiCheat Test] No player available.")` (`:563`), returns null.
- **`TestTamperBackingField()`** — Verified (`QACheat.txt:573–906`). Requires `NetworkManager.get_IsServer` (`:799`);
  reads XP (@1032) `before`; reflects `NetworkVariable<int>` field `"m_InternalValue"` (`:839`);
  `FieldInfo.SetValue(xp, 999999)` (`:854, 862`); logs
  `"[AntiCheat Test] Wrote 999999 to m_InternalValue via reflection (raw memory sim)"` (`:867`);
  `LogResult("backing field tamper", before, after)` (`:875–879`); missing-field fail string
  `"[AntiCheat Test] FAIL - Could not find m_InternalValue field. NGO version may differ."` (`:892`).
- **`TestTamperBaseValueSetter()`** — Verified (`QACheat.txt:908–1184`). `Type.GetProperty("Value")` on
  `NetworkVariable<int>` (`:1135–1147`); `PropertyInfo.SetValue(xp, 999999)` (`:1152, 1160`); logs
  `"[AntiCheat Test] Wrote 999999 via base.Value property setter"` (`:1161`); `LogResult("base.Value setter bypass", …)`
  (`:1169`).
- **`TestTamperCache()`** — Verified (`QACheat.txt:1186–1466`). Reflects `_obfuscatedCache` (`:1426`), writes
  **12345** (`:1434`), logs `"Corrupted _obfuscatedCache via reflection"` (`:1443`), `LogResult("cache tamper", …)` (`:1451`).
- **`TestTamperHash()`** — Verified (`QACheat.txt:1468–1748`). Reflects `_integrityHash` (`:1708`), writes
  **0xDEADBEEF** (`:1716`), logs `"Corrupted _integrityHash via reflection"` (`:1725`), `LogResult("hash tamper", …)` (`:1733`).
- **`TestTamperLevel()`** — Verified (`QACheat.txt:1750–2078`). Logs `"[AntiCheat Test] Level: {0}, AllTimeXP: {1}, "`
  (`:1998`) + `"AccumulatedXP: {0}"` (`:2013`); `Level.set_Value(99)` (`:2033–2034`);
  `"[AntiCheat Test] Level after tamper: {0} (expected: {1})"` (`:2050`); pass string
  `"<color=green>[AntiCheat Test] PASS - Level invariant violation detected and reverted</color>"`, fail string
  `"<color=red>[AntiCheat Test] FAIL - inconsistent Level was accepted</color>"` (`:2058–2061`).
- **`TestTamperGold()`** — Verified (`QACheat.txt:2080–2413`). Gold @992; `"[AntiCheat Test] Gold before: {0}"` (`:2322`);
  `m_InternalValue` write 999999 (`:2361`); `"[AntiCheat Test] Wrote 999999 to Gold m_InternalValue"` (`:2374`);
  `LogResult("Gold backing field tamper", …)` (`:2382`); fail `"[AntiCheat Test] FAIL - Could not find m_InternalValue field."`
  (`:2399`).
- **`TestTamperAttributes()`** — Verified (`QACheat.txt:2415–2715`). Attributes via player field @368 + virtual getter;
  `"[AntiCheat Test] Memory before: {0}, Total: {1}, "` (`:2656`) + `"AllTimeXP: {0}"` (`:2672`);
  `Attributes.set_Memory(memoryBefore + 50)` (`:2688–2691`); `"[AntiCheat Test] Set Memory += 50 via direct write"` (`:2699`);
  **`"[AntiCheat Test] Periodic validation will catch this within 30 seconds, or it will be caught on next load."`** (`:2702`)
  — documents a **30-second periodic validation** window.
- **`RunAllTests()`** — Verified (`QACheat.txt:2717–2814`). `"=== [AntiCheat Test] Running all anti-cheat tests ==="` (`:2784`)
  → 7 tests in declaration order (`:2789–2807`) → `"=== [AntiCheat Test] All tests complete ==="` (`:2808`).
- **`LogResult(testName, expected, actual)`** — Verified (`QACheat.txt:2816–2974`).
  `"[AntiCheat Test] Value after read: {0} (expected: {1})"` ({0}=actual, {1}=expected, `:2928`); on match
  `"<color=green>[AntiCheat Test] PASS - " + testName + " was detected and reverted</color>"` (`:2943–2945`); else
  `"<color=red>[AntiCheat Test] FAIL - {0} was NOT detected (got {1})</color>"` (`:2953`).

### QARune

- **`Awake()`** — Verified (`QARune.txt:74–232`). Same singleton-dedupe as QACheat (`Destroy(this.gameObject)` at `:201–213`).
- **`SpawnRune()`** — Verified (`QARune.txt:233–1542`). `GenerateSelectedRune` + `MonsterUtils.GetRandomDropPosition` +
  `ServerRPC.AddRuneToWorldServerRpc` (`:1104–1206`); `GenerateFullSet` iterates
  `Enum.GetValues(typeof(SlotType))` (`:1062`); strings: `"[QARune] Queued spawn of {0} rune(s) for set {1} slot {2}."`
  (`:1451`), `"[QARune] RuneManager.Singleton is null - cannot generate runes."` (`:1467`),
  `"[QARune] ServerRPC is null - cannot spawn rune centrally."` (`:1475`),
  `"[QARune] No player data available - cannot spawn rune."` (`:1483`),
  `"[QARune] No rune set selected to spawn."` (`:1491`).
- **`SpawnLockedRune()`** — Verified (`QARune.txt:1543–2380`). Strings:
  `"[QARune] PersonalLoot is ON - a drop locked to another player is hidden, not greyed. Turn it off to see the locked look."`
  (`:2110`), `"[QARune] Queued spawn of {0} LOCKED rune(s) for set {1} slot {2}."` (`:2323`),
  `"[QARune] No player/ServerRPC/RuneManager available - cannot spawn locked rune."` (`:2339`),
  `"[QARune] No rune set selected to spawn."` (`:2347`).
- **`SendRuneToInventory()`** — Verified (`QARune.txt:2381–3344`). `Inventory.AddRuneToInventory` (`:3062, 3181`);
  `"[QARune] Added {0} rune(s) of set {1} to player inventory."` (`:3232`),
  `"[QARune] Player Inventory component not found."` (`:3259`),
  `"[QARune] RuneManager.Singleton is null - cannot generate runes."` (`:3278`),
  `"[QARune] No player data available - cannot send rune to inventory."` (`:3297`),
  `"[QARune] No rune set selected to send."` (`:3316`).
- **`DeleteAllRunesInInventory()`** — Verified (`QARune.txt:3345–3828`). Iterates `NetworkList<InventoryEntry>` (`:3707`),
  skips `InventoryEntry.get_Empty` (`:3736`); `"[QARune] Removed all rune(s) from player inventory."` (`:3771`),
  `"[QARune] Player Inventory component not found."` (`:3804`),
  `"[QARune] No player data available - cannot delete runes."` (`:3810`).
- **`ContrabandPreflight()`** — Verified (`QARune.txt:5495–5753`). `SelectedSet == 0` →
  `"[QARune] No rune set selected."` (`:5745`) return false. Null-checks DataStorage singleton / player (@32) /
  ServerRPC (@56) / RuneManager singleton → `"[QARune] No player/ServerRPC/RuneManager available - cannot spawn contraband."`
  (`:5733`) return false. If `GameConfig.DestroyUnobtainableGear` (GameConfig field @408 bool) is **false** →
  `"[QARune] GameConfig.DestroyUnobtainableGear is OFF - contraband will NOT be destroyed. Turn it on or this test proves nothing."`
  (`:5721`) but **still returns true**.
- **`SpawnContraband()`** — Verified (`QARune.txt:3829–4743`). Preflight → player position → foreach
  `KindsToSpawn()` × `Math.Max(1, Amount)` (`:4423`): `GenerateSelectedRune(SelectedSlot)` (`:4430`) →
  `Corrupt(rune, kind)` (`:4464`) → `MonsterUtils.GetRandomDropPosition` (radius from GameConfig @296, `:4486–4505`) →
  `ServerRPC.AddRuneToWorldServerRpc` (`:4570`) → `Debug.Log("[QARune] Spawned contraband ({0}): {1}" kind, Describe(rune))`
  (`:4603–4612`). Final message: `String.Format("[QARune] Queued {0} contraband rune(s). Pick one up, then OPEN THE BACKPACK — ", count)`
  + `"that is what runs the sweep. Watch for '[GearLegality] Destroyed'."` (`:4688–4691`).
- **`SendContrabandToInventory()`** — Verified (`QARune.txt:4745–5493`). Same loop but `Inventory.AddRuneToInventory`
  (`:5326`); `"[QARune] Sent contraband to inventory ({0}): {1}"` (`:5356`),
  `"[QARune] Added {0} contraband rune(s). Re-open the backpack to run the sweep."` (`:5440`),
  `"[QARune] Player Inventory component not found."` (`:5482`).
- **`Corrupt(Rune, ContrabandKind)`** — Partially Verified (`QARune.txt:5819–6239`). Switch on kind
  (`cmp edi,5; ja` `:5844–5845`; jump table at RVA 0xE3ABE4 **not present in the dump**). Five write blocks observed:
  - A: `rarity = 5` (`dword [rbx+8] = 5`, `:5850`)
  - B: `stars = 4` (`dword [rbx+0Ch] = 4`, `:5852`)
  - C: `level = 32` (`dword [rbx+50h] = 20h`, `:5854`)
  - D: loop writing `stat-kind 14` (`0Eh`) into the kinds array at `0xAAh` (count ushort @0xA8; index arithmetic
    ilog2-derived and clamped to 6 — mangled, `:5856–5919`)
  - E: `rarity = 5` + `InflateStatValues` (`:5920–5981`)
  Inferred mapping (one block is shared for the 6 cases): `RedAndBogusStats`→E, `RedOnly`→A,
  `BogusStatValuesOnly`→D, `TooManyStars`→B, `OverLevelled`→C, `OverUpgraded`→C (shared). **Inferred** — exact
  case→block mapping is **Unknown**.
- **`InflateStatValues(Rune)`** — Verified (`QARune.txt:6241–6447`). Loops stat values (count @136); each value > 0 →
  `value * multiplier` (float constant at `0x18465DBA4`, presumed `ContrabandStatMultiplier = 10f` — **Inferred**), else
  fallback constant at `0x18465E384` (**Unknown**, unrecoverable).
- **`KindsToSpawn()`** — Verified wrapper (`QARune.txt:5755–5817`): constructs `<KindsToSpawn>d__19`. The iterator's
  `MoveNext` is **unreadable** (`Invalid "Jump target not found in method."`, `QARune_NestedType__KindsToSpawn_d__19.txt:62`);
  state switch `Compare rax, 6` (states 0..6). **Inferred**: `OneOfEach` expands to the six non-`OneOfEach` kinds,
  otherwise the selected single kind. Exact yields **Unknown**.
- **`GenerateSelectedRune(slot)`** — Verified (`QARune.txt:6863–7478`). Builds `List<RuneSet>{SelectedSet}`,
  `List<SlotType>{slot}`, `List<Rarity>{0,1,2,3,4}`, `List<Stars>{1,1}` → `Runes.ReturnRandomRuneData(...)` (`:7434`).
- **`Describe(Rune)`** — Verified (`QARune.txt:6449–6861`). Format `"{0} {1} {2} {3} Lv{4} UUID={5}"` (`:6816`).

---

## Reachability & gating

### CheatCodeManager
- **Input path is dead in this build.** `Update()` is empty (`CheatCodeManager.txt:203–209`) and no Assembly-CSharp code
  calls `CheckCheatCodeInput`/`ActivateCheat` (Verified: grep finds only declarations). A Konami sequence typed at
  runtime therefore does nothing through this manager.
- **No dev-build / AppId / achievement gate exists in this class** (Verified — no `Debug.isDebugBuild`,
  `GameConfig`, or Steam checks in `CheatCodeManager.txt`). Whatever gating exists would live in the caller of
  `ActivateCheat`, and no caller is present in the dump. Residual risk: external callers (other assemblies, scene
  UnityEvents directly wired to the `CanvasGroup`) are **Unknown**.
- Persistence survives: any code that ever wrote `PlayerPrefs[cheatCodeName] = 1` keeps that flag
  (`CheatCodeManager.txt:887–889, 984–990`).

### QACheat / QARune
- QACheat tamper tests early-return unless `NetworkManager.get_IsServer` is true (`QACheat.txt:799, 1971, 2622`) —
  host/server-only (Verified).
- QARune contraband spawns go through `ServerRPC.AddRuneToWorldServerRpc` (`QARune.txt:4570`) — server-authoritative,
  but *initiated* client-side; there is no caller-side gate in the dump (callers of `SpawnContraband` etc. are not in
  Assembly-CSharp dumps — **Unknown**, presumably a QA/dev UI panel).
- `ContrabandPreflight` warns when `GameConfig.DestroyUnobtainableGear` is off but proceeds (`QARune.txt:5721`) —
  the QA tool itself does not enforce the config.
- No `#if DEVELOPMENT_BUILD`-style guard is visible in any dump; these MonoBehaviours exist in the shipping
  Assembly-CSharp. Whether the QA panel objects exist in release scenes is **Unknown**.

### QAAuto
- Skill-tree verification only; requires no network authority; **no anti-cheat interaction** (Verified).

---

## Evidence appendix

Path conventions: `IsilDump/Assembly-CSharp/<file>:<line>` and `ilrecovery_out/<file>:<line>` under
`modding/cpp2il_isil_out/` and `modding/DecompilerTool/` respectively.

| Claim | Evidence | Tag |
|---|---|---|
| `Update()` empty (no key polling) | `CheatCodeManager.txt:203–209` | Verified |
| No callers of `CheckCheatCodeInput`/`ActivateCheat` in Assembly-CSharp | grep over `IsilDump/Assembly-CSharp` | Verified |
| Progress reset + error string `"No cheat progress entry found for "` | `CheatCodeManager.txt:800` | Verified |
| Completion → UnityEvent.Invoke + `PlayerPrefs.SetInt(name,1)` + `Save` | `CheatCodeManager.txt:879–889` | Verified |
| `ActivateCheat` tail (`Invoke`, `SetInt`, `Save`) | `CheatCodeManager.txt:980, 984, 990` | Verified |
| Object matching by `Object.get_name` vs `cheatCodeName` | `CheatCodeManager_NestedType___c__DisplayClass6_0.txt:47–58` | Verified |
| `cheatCodeObject = GetComponent<CanvasGroup>` | `CheatCodeManager.txt:596–602` | Verified |
| QACheat subscribes `OnIntegrityViolation` | `QACheat.txt:299, 373` | Verified |
| XP tamper 999999 via `m_InternalValue` | `QACheat.txt:839, 854, 862, 867` | Verified |
| Base.Value setter bypass test | `QACheat.txt:1135–1161` | Verified |
| Cache tamper 12345 | `QACheat.txt:1426, 1434, 1443` | Verified |
| Hash tamper 0xDEADBEEF | `QACheat.txt:1708, 1716, 1725` | Verified |
| Level tamper 99 + revert expectation | `QACheat.txt:2033–2034, 2050, 2058–2061` | Verified |
| Gold tamper 999999 | `QACheat.txt:2322, 2361, 2374` | Verified |
| Memory += 50; "Periodic validation … within 30 seconds" | `QACheat.txt:2688–2702` | Verified |
| RunAllTests order + banners | `QACheat.txt:2784–2808` | Verified |
| LogResult PASS/FAIL strings | `QACheat.txt:2928, 2943–2945, 2953` | Verified |
| Rune struct offsets | `QARune.txt:6449–6861` (Describe) | Verified |
| Rarity pool excludes 5; star weights max 1 | `QARune.txt:7434` region (GenerateSelectedRune) | Verified |
| Contraband preflight warnings | `QARune.txt:5721, 5733, 5745` | Verified |
| Contraband spawn flow + sweep hint string | `QARune.txt:4423–4612, 4688–4691` | Verified |
| Contraband inventory flow | `QARune.txt:5326, 5356, 5440, 5482` | Verified |
| `Corrupt` write blocks A–E | `QARune.txt:5850, 5852, 5854, 5856–5919, 5920–5981` | Verified |
| `Corrupt` case→block mapping | jump table RVA 0xE3ABE4 missing | Inferred / Unknown |
| `InflateStatValues` multiplier 10f | `QARune.txt:6241–6447`; const @`0x18465DBA4` | Inferred |
| `KindsToSpawn` yields | `QARune_NestedType__KindsToSpawn_d__19.txt:62` unreadable | Inferred / Unknown |
| `ContrabandKind` enum values 0–6 | `ilrecovery_out/ContrabandKind.cs` | Verified |
| QAAuto scope (skill tree, `[QAAuto]`/`[QASkill]`) | `QAAuto.txt:3–16161`, strings `:445,500,607` | Verified |
| Player offsets 184/992/1032/1040/1048/1056 | `QACheat.txt` + `ilrecovery_out/Player.cs:400–418` | Verified |

---

## Open questions

1. **`Corrupt` switch jump table** (RVA 0xE3ABE4) is not in the dump — exact `ContrabandKind` → write-block mapping
   (and which block is shared between `OverLevelled`/`OverUpgraded`) cannot be proven. (`QARune.txt:5844–5845`)
2. **`KindsToSpawn` iterator `MoveNext`** is unreadable (`Jump target not found in method`,
   `QARune_NestedType__KindsToSpawn_d__19.txt:62`) — exact yield order of `OneOfEach` unknown.
3. **Float constants** at `0x18465DBA4` and `0x18465E384` are referenced but not decoded; `ContrabandStatMultiplier = 10f`
   is presumed but unverified.
4. **Block D index arithmetic** in `Corrupt` (ilog2-derived, clamped to 6) is mangled — exact stat-kind write pattern unproven.
5. **Callers of the QA entry points** (`SpawnRune`, `SpawnContraband`, `RunAllTests`, `ActivateCheat`) are not in
   Assembly-CSharp dumps — likely a QA/dev UI, or dead code. Release-scene presence Unknown.
6. **`CheatCodeData` UnityEvent payloads** are scene/wiring data — what each cheat code actually does is defined per
   asset/scene, not in code.
