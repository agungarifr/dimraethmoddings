# Dimraeth — Character Loss Forensic Report (2026-09-26 → 2026-09-27)

**Investigation window:** 2026-09-25 → 2026-09-27 (report written 2026-09-27 ~14:50 local)
**Time convention:** all timestamps are LOCAL (UTC+7). Game/Unity logs stamp events in UTC ("Z"); where quoted, the raw UTC text is preserved and the local equivalent given.
**Scope note:** read-only investigation. No game, save, mod, or log file was modified, moved, or deleted.

---

## 1. Executive summary

**The seven characters were not lost to corruption, mods, or Steam Cloud. They were deleted through the game's
in-game character-select DELETE flow**, in two deliberate episodes:

1. **2026-09-26 15:05:31–15:05:43 (12 seconds):** Bonkraeth → RINDAMAN → Morgen → getrekt → cumiterbang (the "old Cumi") → DOOMraeth.
   ~2 minutes later (15:07:09) a fresh character **"bonk"** was created.
2. **2026-09-27 00:01:53:** chonks (its last save was 10 seconds earlier, at 00:01:43).
   36 seconds later (00:02:29) a fresh character **"Cumi"** (the currently played character) was created.

Every deletion left an identical forensic fingerprint (Section 4) that matches exactly one code path:
`CharacterSelection.DeleteCurrentlySelectedPlayer` → `SaveSystem.DeletePlayer` (archive to Recovery + delete)
→ `CharacterSelection.RemovePlayerFromAllWorlds` (rewrite each world file, removing the 128-byte player record).
This is the **character-select screen delete button** path (`SoundManager.PlayMenuButtonClick` immediately precedes
the call at CharacterSelection.txt:2600–2603). No mod calls any delete API (exhaustive grep of mod sources found none).

**All seven characters are fully recoverable** from `Recovery\Characters\` (Section 9). Do this soon; the game
prunes that folder after a retention window.

The user's two suspected causes — **(a) skill-point multiplier changed 5→1, (b) heavy Equipment Stat Editor use** —
are **not supported by any evidence** (Section 6). The errors those two systems do produce are harmless
(slot-edit rejections with rollback; skill-node sanitization) and never touch character files.

---

## 2. Surviving evidence inventory

| Source | Path | Coverage |
|---|---|---|
| Save files | `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Characters\` | live chars: bonk, Yoink, Cumi |
| Archived deletes | `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Recovery\Characters\` | 7 full copies, timestamps = deletion seconds |
| Worlds | `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Worlds\` | 9 worlds + .bak1/.bak2 (pre-scrub backups) |
| Unity log (prev session) | `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Player-prev.log` | session A (9/27 11:31–~12:20) |
| Unity log (live) | `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Player.log` | session C (9/27 14:10→) |
| BepInEx log | `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\LogOutput.log` | **session C only** (overwritten per launch) |
| Mod configs | `D:\SteamLibrary\steamapps\common\Dimraeth\BepInEx\config\` | incl. per-character ESE dumps with save-time stamps |
| Steam Cloud log | `C:\Program Files (x86)\Steam\logs\cloud_log.txt` | continuous 9/26–9/27 sync history |
| Windows Event Log | Application log | all crashes 9/26–9/27 |
| Game code (decompiled IL) | `D:\SteamLibrary\steamapps\common\Dimraeth\modding\DecompilerTool\isil_out\IsilDump\Assembly-CSharp\` | SaveSystem.txt, CharacterSelection.txt |

---

## 3. What happened to each character

| Character | Last save (local) | Deleted (local) | Archive (Recovery\Characters\_) | World side-effect |
|---|---|---|---|---|
| DOOMraeth | 9/25 13:41 | **9/26 15:05:43** | DOOMraeth.jrf (9862 B) | DOOMraeth's World.jrwf rewritten 15:05:43, 10358→10230 B (−128) |
| getrekt | 9/26 10:47:56 | **9/26 15:05:38** | getrekt.jrf (11398 B) | getrekt's World.jrwf rewritten 15:05:38, 10294→10166 B (−128) |
| Morgen | 9/26 13:58:45 | **9/26 15:05:36** | Morgen.jrf (11558 B) | Morgen's World.jrwf rewritten 15:05:36, 10806→10678 B (−128) |
| RINDAMAN | 9/26 14:59:48 | **9/26 15:05:33** | RINDAMAN.jrf (8502 B) | RINDAMAN's World.jrwf rewritten 15:05:33, 10006→9878 B (−128) |
| Bonkraeth | 9/26 15:05:21 | **9/26 15:05:31** | Bonkraeth.jrf (7366 B) | Bonkraeth's World.jrwf rewritten 15:05:31, 10134→10006 B (−128) |
| cumiterbang ("old Cumi") | 9/26 (dump file present) | **9/26 15:05:41** | cumiterbang.jrf (4390 B) | its world file was already gone long before (see §7) |
| chonks | 9/27 00:01:43 | **9/27 00:01:53** | chonks.jrf (10150 B) | chonks's World.jrwf rewritten 00:01:54, 16854→16726 B (−128) |
| **bonk** (alive) | created 9/26 15:07:09 | — | — | created 2 min after the batch |
| **Yoink** (alive) | created 9/26 21:41:18 | — | — | — |
| **Cumi** (alive, current) | created 9/27 00:02:29 | — | — | created 36 s after chonks' deletion |

Evidence for the deletion seconds: `Recovery\Characters\*.jrf` file timestamps (Created == Modified == event
second — see the fingerprint in §4), re-verified 2026-09-27 14:37 local.

**Note on the name "Cumi":** the *current* `Cumi.jrf` (created 9/27 00:02:29) is alive and is the character being
played right now. The "old Cumi" is `cumiterbang.jrf`, archived at 9/26 15:05:41. The ancient files
`Worlds/cumiterbang's World.jrwf` and an older `Worlds/Cumi's World.jrwf` disappeared long before this incident —
they are already in Steam Cloud's "forget" backlog at the 9/26 14:16 sync together with dozens of other old worlds
(konoha, zumi, vedora, …) — unrelated to the 9/26–27 deletions.

---

## 4. The deletion fingerprint (mechanism proof)

Each deletion produced three simultaneous artifacts at the same wall-clock second:

1. **`Recovery\Characters\<name>.jrf` with Created == Modified == deletion second.**
   The game's `SaveSystem.CopyToRecovery` (SaveSystem.txt:12288) does `File.Copy` followed by
   `File.SetLastWriteTimeUtc(now)` (SaveSystem.txt:12423). NTFS `File.Copy` sets the destination's Created to
   "now" (Modified is preserved), and `SetLastWriteTimeUtc` then stamps Modified to "now" as well — exactly the
   observed Created == Modified == event-second pattern (empirically re-verified on this machine).
   This rules out `SaveSystem.QuarantineUnreadable` (SaveSystem.txt:3308), the load-corruption path, which uses
   `File.Move` and would preserve Created ≠ Modified.
2. **The character's `Characters\<name>.jrf` is removed** (SafeDelete inside `SaveSystem.DeletePlayer`,
   SaveSystem.txt:11337).
3. **Every world file containing that player is rewritten in the same second, shrinking by exactly 128 bytes**
   (the serialized `ServerPlayerData` record), e.g. `Bonkraeth's World.jrwf` 10134 → 10006 B at 15:05:31.
   That rewrite is `CharacterSelection.RemovePlayerFromAllWorlds` (CharacterSelection.txt:11573), called only
   from `DeleteCurrentlySelectedPlayer` (call at :11552) — i.e. the **UI delete wrapper**.

The IL call chain (all in `modding\DecompilerTool\isil_out\IsilDump\Assembly-CSharp\`):

- UI menu-button handler: `SoundManager.PlayMenuButtonClick` then `CharacterSelection.DeleteCurrentlySelectedPlayer`
  — CharacterSelection.txt:2600–2603 (inside the `UIActionContext` handler; after the call the screen refreshes
  `CharacterCreation.PageRefresh`, :2555 — matching "delete, then create a new character")
- `DeleteCurrentlySelectedPlayer()` method — CharacterSelection.txt:11347
- → `SaveSystem.DeletePlayer` — CharacterSelection.txt:11519; method SaveSystem.txt:11337
- → `SaveSystem.CopyToRecovery` — SaveSystem.txt:11333 (inside DeletePlayer), method at :12288
- → `CharacterSelection.RemovePlayerFromAllWorlds` — CharacterSelection.txt:11552, method at :11573

**Contrast with the two automatic delete paths (both ruled out):**

- `LoadPlayerAll` auto-delete (version mismatch / created-before-cutoff): logs
  `"[SaveSystem] Player '{0}' has version {1}, expected {2} - deleting."` (SaveSystem.txt:6486) or
  `"before the {0:u} cutoff - deleting."` (:6451) and calls only `SaveSystem.DeletePlayer` (:6506) — it does
  **not** rewrite world files. Our fingerprint includes same-second world rewrites → not this path. The encrypted
  JRSF containers prevent reading the version fields of the archived saves, so this hypothesis is ruled out on
  the fingerprint, not on file contents.
- `QuarantineUnreadable` (load failure): `File.Move` timestamp semantics differ (see above) → not this path.

**Behavioral signature of the two episodes** (in addition to the code fingerprint):

- 6 characters deleted at ~2-second intervals (15:05:31, :33, :36, :38, :41, :43) — the rhythm of selecting a
  character, pressing delete and confirming, repeatedly.
- Bonkraeth's final save is at 15:05:21 — **10 seconds before** its deletion; chonks' final save at 00:01:43 —
  also **10 seconds before** its deletion at 00:01:53. Save-then-drop-out-then-delete at the character screen.
- Each episode is followed within ~2 minutes by the creation of a fresh character (bonk 15:07:09; Cumi 00:02:29).

---

## 5. Timeline (local time, UTC+7)

### 2026-09-25
| Time | Event | Evidence |
|---|---|---|
| 13:41:15 | DOOMraeth's last save (ESE equipment dump written) | `BepInEx\config\EquipmentStatEditor.DOOMraeth.cfg:3` + file mtime |

### 2026-09-26
| Time | Event | Evidence |
|---|---|---|
| 10:47:55–56 | getrekt's last save | `Worlds\getrekt's World.jrwf.bak2/.bak1` mtimes; `EquipmentStatEditor.getrekt.cfg:3` |
| 13:58:45 | Morgen's last save | `Worlds\Morgen's World.jrwf.bak1/.bak2`; `EquipmentStatEditor.Morgen.cfg:3` |
| 14:16:28 | Game session D1 launches. Steam Cloud launch-sync confirms all six doomed characters present ("Need to upload" DOOMraeth :5823, Bonkraeth :5825, getrekt :5827, cumiterbang :5828, Morgen :5830; "Watching file" entries :5832–5834) | `cloud_log.txt:5775–5834` |
| 14:18:18 | RINDAMAN's world created (RINDAMAN joins a fresh world) | `Worlds\RINDAMAN's World.jrwf` created |
| 14:36:25 | python.exe (uv cpython tooling) crashes 0xc0000005 — unrelated to game | Event Log: Application Error 1000 |
| 14:59:41–48 | RINDAMAN's last save | `EquipmentStatEditor.RINDAMAN.cfg:3`; `Worlds\RINDAMAN's World.jrwf.bak1` |
| 15:03:29 | python.exe crashes 0xc0000005 (2 min before deletions; unrelated tooling) | Event Log: Application Error 1000 |
| 15:05:19–21 | Bonkraeth's final save (world baks written; ESE dump) | `Worlds\Bonkraeth's World.jrwf.bak2/.bak1`; `EquipmentStatEditor.Bonkraeth.cfg:3` |
| **15:05:31** | **Bonkraeth deleted** (recovery archived same second; world scrubbed −128 B) | `Recovery\Characters\Bonkraeth.jrf`; `Worlds\Bonkraeth's World.jrwf` 15:05:31 |
| **15:05:33** | **RINDAMAN deleted** | `Recovery\Characters\RINDAMAN.jrf`; `Worlds\RINDAMAN's World.jrwf` 15:05:33 |
| **15:05:36** | **Morgen deleted** | `Recovery\Characters\Morgen.jrf`; `Worlds\Morgen's World.jrwf` 15:05:36 |
| **15:05:38** | **getrekt deleted** | `Recovery\Characters\getrekt.jrf`; `Worlds\getrekt's World.jrwf` 15:05:38 |
| **15:05:41** | **cumiterbang (old Cumi) deleted** | `Recovery\Characters\cumiterbang.jrf` 15:05:41 |
| **15:05:43** | **DOOMraeth deleted** | `Recovery\Characters\DOOMraeth.jrf`; `Worlds\DOOMraeth's World.jrwf` 15:05:43 |
| 15:07:09–12 | New character **bonk** created (+ world) | `Characters\bonk.jrf` created 15:07:09; `Worlds\bonk's World.jrwf` created 15:07:12 |
| (between 15:07–19:45) | Session D1 exits cleanly (no crash record) | Event Log gap |
| 19:45:23 | **Steam updates the game** (buildid 25546572) — i.e. the game was closed at this moment | `steamapps\appmanifest_2402680.acf` lastupdated=1790426723 |
| 19:45:24 | Session D2 starts (1 second after the update) | decoded faulting-process start time of the 23:44 crash (Event Log 1000, `0x1DD4DB4E6449F99`) |
| 20:00:00–05 | bonk's saves | `Characters\bonk.jrf` modified 20:00:05; `EquipmentStatEditor.bonk.cfg:3` |
| 21:41:18–21 | New character **Yoink** created (+ world) | `Characters\Yoink.jrf` created 21:41:18 |
| 23:44:46–58 | **Session D2 CRASHES** — .NET Runtime ID 1023 (CoreCLR internal error) + Application Error ID 1000 (coreclr.dll 0xc0000005) + WER 1001 | Event Log (Application) |
| 23:45:01–02 | Steam exit-sync: persists bonk/chonks/Yoink (:7134–7139); world rewrites uploaded (:7193,7195,7198,7200); forget-list for the deleted characters — `Characters\getrekt.jrf` (:7196), `Bonkraeth.jrf` (:7194), `cumiterbang.jrf` (:7197), `Morgen.jrf` (:7199), `RINDAMAN.jrf` (:7201), `DOOMraeth.jrf` (:7192) | `cloud_log.txt:7123–7206` |
| 23:59:44 | Session E launches (Cloud launch-sync "Watching file … chonks.jrf" :7288) | `cloud_log.txt:7222–7296` |

### 2026-09-27
| Time | Event | Evidence |
|---|---|---|
| 00:01:41–43 | chonks' final save (world baks; ESE dump 00:01) | `Worlds\chonks's World.jrwf.bak2/.bak1`; `EquipmentStatEditor.chonks.cfg:3` |
| **00:01:53** | **chonks deleted** (recovery archived 00:01:53; world scrubbed −128 B at 00:01:54) | `Recovery\Characters\chonks.jrf`; `Worlds\chonks's World.jrwf` 00:01:54 |
| 00:02:29–33 | New character **Cumi** (the current one) created (+ world) | `Characters\Cumi.jrf` created 00:02:29 |
| 00:08:13 | Session E exit-sync detects the deletion: `"...Characters\chonks.jrf was removed while the game was running. Deleting from cloud and remote machines"` | `cloud_log.txt:7308` |
| 00:08:14 | `Need to delete file Mudtek/Dimraeth/Characters/chonks.jrf` | `cloud_log.txt:7444` |
| 08:32:57 | Session B starts (playing Yoink) | decoded process start `0x1DD4E201FE25AE4` (Event Log, 10:10 crash) |
| 10:10:06–09 | Yoink's last saves (rolling .jrf/.bak) | `Characters\Yoink.jrf` modified 10:10:09 |
| 10:10:10–30 | **Session B CRASHES** — .NET Runtime ID 1026 (unhandled 0xc0000005) + Application Error 1000 ×2: GameAssembly.dll 0xc0000005 and 0xc000041d at +0x24417e5 + WER 1001 ×2 | Event Log (Application) |
| 10:10:33 | Steam exit-sync (Yoink persisted) | `cloud_log.txt:7750–7830` |
| 11:31:12 | Session A starts (playing Cumi) | `Player-prev.log:26` (`started=2026-09-27T04:31:12Z`) |
| ~11:31–12:20 | Session A: two `SkillTree` LogErrors dropping cross-tree free start nodes from Cumi's save (:72, :82); later `LoadManager: Loading cancelled: Lost connection` (:115) | `Player-prev.log:72,82,115` |
| 12:21:44 | python.exe crashes 0xc0000005 (unrelated tooling) | Event Log |
| 14:10:38–41 | Session C starts (playing Cumi; still running at report time) | process start; `Player.log:26` (`started=2026-09-27T07:10:41Z`) |
| 14:18:06 | `com.custom.dimraethmodpack.cfg` rewritten (mod settings persisted; `SkillPointMultiplier = 2` is the current value) | `BepInEx\config\com.custom.dimraethmodpack.cfg:127` + mtime |
| 14:41:21–44 | Cumi's rolling saves | `Characters\Cumi.jrf` modified 14:41:41 |

---

## 6. The two suspected causes — verdict

### (a) Skill-point multiplier changed 5→1 → "broke" saves? **No evidence — and mechanism is impossible.**

- Current value is **`SkillPointMultiplier = 2`** (`com.custom.dimraethmodpack.cfg:127`; the file comment at :124:
  "Skill points per level-up = 2 x this multiplier (Default: 3, Vanilla: 1)"). The file was rewritten **today
  14:18:06**, so the setting's *history* (the alleged 5→1 change) cannot be recovered from disk — no `.bak` of
  this config exists. That is a documented gap, not evidence of a change on 9/26.
- What the mod actually does: grants skill points at level-up. Live proof in `BepInEx\LogOutput.log:128–132`
  (`[SkillPointMod] L2: granted +2 (skill points 2 -> 4)` … L6). It patches the level-up grant only.
- The `SkillTree` messages (`Player-prev.log:72,82`: "Dropped free start node 'Minotaur_Start1'/'Elf_Start1' …")
  are **load-time skill-tree sanitization** — they remove unpaid cross-tree nodes from the in-memory tree. They
  are LogError-styled but harmless to save files and are unrelated to file deletion.
- Most decisively: the deletion fingerprint (§4) is the UI delete path. A point-multiplier problem could at worst
  cause a *load* failure (which would quarantine via `File.Move`), never a clean `CopyToRecovery` + world scrub.

### (b) Equipment Stat Editor (ESE) heavy use triggering saves? **No evidence of harm.**

- Every ESE error captured in the live session is a **live slot-edit rejection with automatic rollback**:
  `[Error:Equipment Stat Editor] [RuneMemory] pre-sign failed: Object reference not set to an instance of an object.`
  (66×, first at `LogOutput.log:69`) and `[Warning …] UI edit to slot 0 was rejected by the game - rolling back.`
  (23×, first at `:73`). "Rolling back" means the edit is discarded and the save left untouched.
- ESE's save cycles show `0 rune(s) modified` throughout (`:58` onward) — it is observing, not rewriting, during
  these sessions. ESE dumps (`EquipmentStatEditor.<name>.cfg:3`) coincide with the game's own save moments
  (e.g. Bonkraeth dump 15:05:21 = its final save 10 s before deletion).
- ESE forces `FormulaVersion` to `SaveSystem.CurrentRuneFormulaVersion` on write (mod source
  `BepInExModsSource\EquipmentStatEditor\Core\RuneMemory.cs` / `EditorEngine.cs`), so it cannot create the
  version desync that the game's auto-delete path would act on.
- No mod source (ESE, Dimraeth ModPack, CheatMenu, or others) calls `DeletePlayer`, `DeleteAllPlayers`,
  `CopyToRecovery` or `QuarantineUnreadable` — grep across `modding\BepInExModsSource` and decompiled mod
  assemblies returns matches only inside the game's own decompile.

### Also ruled out

- **Steam Cloud:** every 9/26–27 sync only mirrors local state ("Need to forget/delete/upload…"). No
  "download/restore" of character files anywhere in the window; the 00:08:13 entry shows Cloud *propagating* the
  local deletion, not causing it (`cloud_log.txt:7308`).
- **Game update (build 25546572, 19:45:23):** it landed *after* the 15:05 batch, and chonks' deletion (00:01:53)
  carries the UI fingerprint as well. The version/cutoff auto-delete would also have swept all old characters at
  once at load and would not rewrite worlds (§4).
- **Crashes:** the two Dimraeth crashes (9/26 23:44, 9/27 10:10) are *after* both deletion episodes and match
  session ends, not deletions. The python.exe crashes are uv-managed CPython tooling in
  `C:\Users\game\AppData\Roaming\uv\python\…`, unrelated to the game.

---

## 7. Which log entries document the disappearances

**Direct deletion log lines do not exist anymore** — BepInEx `LogOutput.log` is overwritten per launch and Unity
keeps only two `Player.log` generations; the sessions that performed the deletions (D1, D2, E) are rotated out.
What survives and documents the events:

- **getrekt:** `Recovery\Characters\getrekt.jrf` (Created == Modified == 9/26 15:05:38) + `Worlds\getrekt's World.jrwf`
  rewritten at 15:05:38 (10294→10166 B). Steam Cloud aftermath:
  `cloud_log.txt:7196` `Need to forget file Mudtek/Dimraeth/Characters/getrekt.jrf` (9/26 23:45:02), repeated
  :7274 (23:59:44) and :7436 (9/27 00:08:14), with the scrubbed world re-uploaded at :7195/:7273/:7435.
- **Cumi / cumiterbang:** the *old* Cumi is `cumiterbang.jrf`, archived 9/26 15:05:41
  (`Recovery\Characters\cumiterbang.jrf`), Cloud aftermath `cloud_log.txt:7197/7275/7437`. The *current*
  `Characters\Cumi.jrf` is not lost at all — it was created 9/27 00:02:29, 36 s after chonks was deleted, and is
  being played right now. The ancient `Worlds/Cumi's World.jrwf` and `Worlds/cumiterbang's World.jrwf` are in
  Cloud's backlog already at 9/26 14:16:28 (`cloud_log.txt:5782–5783`) — pre-existing, unrelated losses.
- **chonks:** the only deletion with an explicit Cloud log entry: `cloud_log.txt:7308`
  (`"…chonks.jrf was removed while the game was running. Deleting from cloud and remote machines"`, 9/27 00:08:13)
  and :7444 (`Need to delete file … chonks.jrf`), plus `Recovery\Characters\chonks.jrf` 00:01:53 and the
  −128 B world rewrite at 00:01:54.
- **Morgen, RINDAMAN, Bonkraeth, DOOMraeth:** `Recovery\Characters\*.jrf` (deletion seconds 15:05:33/36/31/43) +
  same-second world scrubs + Cloud forget lines :7194/:7199/:7201/:7192.

---

## 8. Complete error / exception inventory (2026-09-26 → 2026-09-27)

### `Player-prev.log` (session A: 9/27 11:31–~12:20, character Cumi)
| Line | Content |
|---|---|
| 72 | `[SkillTree] Dropped free start node 'Minotaur_Start1' from Cumi's save - it belongs to another race/class tree and was never paid for.` (LogError; stack 75–78) |
| 82 | `[SkillTree] Dropped free start node 'Elf_Start1' from Cumi's save …` (LogError; stack 85–88) |
| 115 | `[LoadManager] Loading cancelled: Lost connection` (LogError; stack 118–120) |

### `Player.log` (session C: 9/27 14:10→, live — line numbers as of 14:50)
| Line | Content |
|---|---|
| 65, 73, 80, 179, 191, 201, 254, 264, 286 | `Failed to create agent because it is not close enough to the NavMesh` (warning) |
| 88–92 | `NullReferenceException: Object reference not set to an instance of an object.` at `NPCQuest.UpdateIcon` ← `NPCQuest+<ClientTagCheckRoutine>d__29.MoveNext` ← `NPCQuest.OnNetworkSpawn` |

### `BepInEx\LogOutput.log` (session C, live — file is still growing)
| Line | Content |
|---|---|
| 41 | `[Warning:Il2CppInterop] Class::Init signatures have been exhausted, using a substitute!` |
| 57 | `[Warning:Equipment Stat Editor] [RuneMemory] interop field offsets differ from cpp2il layout (Set=10 SlotType=14 Rarity=18 Stars=1C UUID=20 Level=60 Primary=70); using verified cpp2il layout instead.` |
| 69 … (66×) | `[Error:Equipment Stat Editor] [RuneMemory] pre-sign failed: Object reference not set to an instance of an object.` |
| 73 … (23×) | `[Warning:Equipment Stat Editor] [EquipmentStatEditor] UI edit to slot 0 was rejected by the game - rolling back.` |
| 128–132 | `[SkillPointMod] L2…L6: granted +2 (skill points …)` (info) |

### Windows Event Log (Application), 9/26–9/27
| Time (local) | Source / ID | Content |
|---|---|---|
| 9/26 23:44:46 | .NET Runtime 1023 | Dimraeth.exe terminated — internal CoreCLR error, exit code c0000005 |
| 9/26 23:44:49 | Application Error 1000 | coreclr.dll 0xc0000005 (fault +0x1d1fdd); process start 19:45:24 |
| 9/26 23:44:58 | WER 1001 | APPCRASH bucket for the above |
| 9/27 10:10:10 | .NET Runtime 1026 | unhandled exception c0000005 at 0x…7DC717E5 |
| 9/27 10:10:13 | Application Error 1000 | GameAssembly.dll 0xc0000005 (fault +0x24417e5) |
| 9/27 10:10:21 | WER 1001 | APPCRASH bucket |
| 9/27 10:10:24 | Application Error 1000 | GameAssembly.dll 0xc000041d (same offset, second fault) |
| 9/27 10:10:30 | WER 1001 | APPCRASH bucket |
| 9/26 00:43:33, 00:43:47, 00:56:17, 02:23:17, 02:29:58, 02:54:40, 11:22:54, 14:36:25, 15:03:29; 9/27 12:21:44 | Application Error 1000 | `python.exe` (uv CPython 3.12 tooling) 0xc0000005 in ucrtbase.dll — unrelated background tooling |

### Historical (captured before rotation — files since overwritten)
- Session B (9/27 08:32–10:10, Yoink, crashed): ~1450× `[FMOD] Event not found` `EventNotFoundException` spam
  (then Player.log lines 323–1774) and one LogError at line 1862 whose message was lost before capture.
- `BepInEx\ErrorLog.log` is 0 bytes (never used). `BepInEx\CheatMenu.log` ends 2026-09-24 20:26 (manual skill-point
  grants via CheatMenu through 9/24, incl. an XP overflow `Available XP: -1.273.868.404` on 9/22) — outside the
  incident window.

---

## 9. Recovery instructions (all seven are recoverable)

`Recovery\Characters\` holds **complete copies** of all seven deleted saves (byte sizes are in the normal range,
4390–11558 B). To restore, **with the game closed**:

1. Copy the desired file(s) from
   `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Recovery\Characters\`
   into `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Characters\`
   (e.g. `getrekt.jrf`, `Morgen.jrf`, `cumiterbang.jrf`, …).
2. Optionally restore the matching world's pre-scrub player record: each affected world's `.bak1`/`.bak2` in
   `Worlds\` still contains the deleted player's ~128-byte record (e.g. `Bonkraeth's World.jrwf.bak1` @ 15:05:21,
   `getrekt's World.jrwf.bak2` @ 10:47:55). Restoring the world backup alongside the character gives the exact
   pre-deletion state; otherwise the game may re-create the player record on next join (unverified).
3. Do it soon: `SaveSystem.CleanOldRecoveryFiles` (SaveSystem.txt:12462, `PruneDirectory` :15788,
   `DateTime.AddDays` retention constant :15909) prunes the Recovery folder periodically. The archived files date
   from 9/26–9/27.
4. Note: Steam Cloud was told to "forget"/"delete" these characters (they are gone from the cloud). Restoring
   locally is fine; the restored files will be re-uploaded at the next sync.

---

## 10. Log gaps and limitations (explicit)

1. **The deletion sessions' logs are unrecoverable.** `BepInEx\LogOutput.log` is overwritten on every launch
   (only session C survives), and Unity keeps only `Player.log` + `Player-prev.log` (sessions C and A). Sessions
   D1 (9/26 14:16–~19:45), D2 (19:45–23:44) and E (23:59–00:08) — which contain the actual delete actions —
   have no surviving game logs. The deletions are therefore proven by filesystem fingerprint + decompiled code +
   Steam Cloud log, not by a direct "deleted" log line.
2. **No actor attribution.** Nothing in any surviving source identifies *who* pressed delete. The behavioral
   pattern (save → 10 s later delete; 2-second intervals across 6 characters; a new character created 2 minutes
   later; repeated once for chonks → Cumi) indicates deliberate use of the character-select delete button rather
   than an automated sweep, but this cannot be proven further from logs.
3. **JRSF save containers are encrypted**, so the version/created fields inside the archived `.jrf` files cannot
   be read; the version-mismatch auto-delete hypothesis is excluded on the fingerprint (world rewrites) alone.
4. **Config history is not recoverable.** `com.custom.dimraethmodpack.cfg` has no backup; the alleged
   `SkillPointMultiplier` 5→1 change cannot be verified or timed. Current value: 2 (file written 9/27 14:18:06).
5. **Session A's 12:19 save stamps** (Cumi's saves that morning) were captured during the investigation but the
   rolling `.bak` files and ESE dump have since been overwritten by session C's saves; those specific timestamps
   rest on the earlier snapshot.
6. The game is **currently running** (session C, character Cumi) — all live files continue to change; quoted
   line numbers for `Player.log` / `LogOutput.log` are valid as of ~14:50 on 9/27.

---

*Report generated from read-only inspection on 2026-09-27. Primary evidence: NTFS timestamps of
`Recovery\Characters\` and `Worlds\*.jrwf`, `cloud_log.txt` lines 5775–7447 and 7750–7830, Windows Event Log
(Application), `Player-prev.log`, `BepInEx\LogOutput.log`, and the game's decompiled IL
(`SaveSystem.txt`, `CharacterSelection.txt`).*
