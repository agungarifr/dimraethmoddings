# Dimraeth — Chest Item-Loss Reinvestigation (2026-10-01)

**Symptom (user):** after a chest "deposit all", items are visible in the chest in-session,
but vanish after closing and restarting the game.

**Scope:** read-only save/log analysis + mod source. No game or save file was modified.

## TL;DR

The captured world save **retains** every deposited item, including items in slots past the
vanilla authored size. The load path also preserves them. So for the save we inspected, the
loss is **not** a chest `StorageData` truncation. One real invariant violation introduced by the
mod was found and fixed (the parallel `Runes` list was left short of `StorageSize`).

Remaining plausible cause of the *reported* loss: the **pet courier's carried payload in
transit**, which lives in the pet inventory (not the chest record) and is not obviously
persisted, and/or a chest-UI slot-count display artifact. Chase that with logging (below).

## Evidence

### 1. The save contains the items

Decoded `C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Worlds\ars's World.jrwf` (quit save,
11:34:49) via `modding\SaveProbe`. Chest index 17 (`631d574100d9496e9fe6f9`):

| field | value |
|---|---|
| `StorageSize` | 216 |
| `Entries.Count` | 216 |
| non-empty entries | 152 (slots 0–151) |

Items persist even in slots ≥ 18, i.e. well past the authored `DEFAULT_CHEST_SLOTS = 18`.
The same is true for chest 30 (items in slots 18–28).

### 2. Load does not drop them

- `SceneHandler+<SpawnStoragesCoroutine>d__163.MoveNext` first loop matches each saved
  `StorageData` to a scene spawn via `SceneHandler.FindMatchingStorageInScene`, which
  compares **name only** (`FixedString32Bytes.op_Equality` on `+0x10`). An expanded chest
  still matches its spawn.
- `SceneHandler.UpdateStorageData(storage, store)` copies only `ChestItems` (`+0x80`) and
  `RespawnTimer` (`+0x88`), and only when `StartingItems > 0 && RespawnSeconds > 0`. It never
  touches `Entries`.
- `Storage.Start()` reassigns the saved record by name; `Storage.SetStorageData()` calls
  `SyncWithStorageData()`, which copies `StorageData.Entries → Storage.Inventory` for
  `i < min(Entries.Count, Inventory.Count)`.
- `Storage.DepositEntryIntoRecord(data, entry, slot, limit)` writes `Entries` (`+0x60`) bounded
  by `StorageSize` (`+0x78`); deposits are applied to `this.StorageData`, the same object found
  at load. The player "deposit all" path (`StorageView.DepositAllIntoContainer →
  Storage.AddEntryToStorageServerRpc`) calls it with `this.StorageData`, then `SyncWithStorageData`.
- No storage/pet exceptions in `Player.log`.

### 3. Real anomaly found and fixed: `Runes` not expanded

Our capacity helpers grew `Entries` and `StorageSize` but **not** the parallel `Runes` list:

| chest | size | entries | runes |
|---|---|---|---|
| vanilla 64-slot (idx 1,12,13,32) | 64 | 64 | 64 |
| expanded (idx 15) | 108 | 108 | **18** |
| expanded (idx 16,17,30) | 216 | 216 | **18** |

Vanilla keeps `Runes.Count == Entries.Count == StorageSize` (`StorageData` ctor initialises all
three to `storageSize`; `Storage.SyncWithStorageData` syncs `StorageData.Runes → Storage.Runes`
bounded by both counts). A short `Runes` violates that invariant and can surface on save/load.

**Fix:** `ChestAndInventoryModule.EnsureStorageDataCapacity` / `EnsureStorageCapacity` now pad
`StorageData.Runes` with `Rune.Empty` in lockstep with `Entries`/`StorageSize`. (`PlayersOpened`
is intentionally left alone — vanilla keeps it empty even on 64-slot chests.)

## Recommended next diagnostic (to confirm/refute the remaining hypothesis)

Add temporary logging around the pet courier deposit and load:

1. In `Postfix_TryResolveDeliverable`, log `record`/`live` identity, `record.StorageName`,
   `record.StorageSize`, `record.Entries.Count`, and the same for `live.StorageData` (are they
   the same object?).
2. In `Postfix_BuildFor` (pet chest list) and `Prefix_OpenChest`, log `GetChestTargetSlotCount`
   and the resulting `Entries.Count` / `Inventory.Count`.
3. Reproduce: pet-deposit → confirm items in the target chest record → quit → reload → re-dump
   the chest record with `SaveProbe`. If the record still has them but the chest UI does not,
   it is a presentation/sync issue, not persistence.

## Implementation status (2026-10-01 14:30)

The recommended logging is now implemented (non-destructively) inside the modpack's new
`Modules\Pets\PetCargoModule.cs`, config section **`[Pets.PetCargo]`**, toggle `LogStorageRecords`
(default true). It patches, alongside `ChestAndInventoryModule` (which was left untouched):

- `StorageDirectory.TryResolveDeliverable` (postfix) — logs `record`/`live.StorageData`
  describe-strings, `sameObject=ReferenceEquals(record, live.StorageData)`, and `slotLimit`.
- `StorageDirectory.BuildFor` (postfix) — logs each row's `used/total` plus the live record.
- `Storage.OpenChest` (prefix) — logs `StorageData` + live `Inventory.Count`/`Runes.Count`.
- `PetManager.DepositCourierEntryServerRpc` / `DepositCourierRuneServerRpc` (prefix) — logs the
  carried entry with a WARNING when vanilla would silently drop it.
- The module UI also has a **"Dump current pet cargo (log)"** button (slots 150-161).

Repro still as documented: pet-deposit → confirm items in target chest record → quit → reload →
re-dump with `SaveProbe`. If the record still holds them but the chest UI does not, it is a
presentation/sync issue, not persistence.

## Artifacts

- Decoded saves: `modding\SaveProbe\out\ars's World.json`, `ars.json`
- Helpers: `out\inspect_parallel.py`, `out\scan_truncation.py`, `out\inspect_runes.py`
- Module: `modding\BepInExModsSource\DimraethModPack\Modules\Loot\ChestAndInventoryModule.cs`
