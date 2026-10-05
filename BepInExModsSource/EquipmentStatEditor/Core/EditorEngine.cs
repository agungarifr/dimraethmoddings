using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Unity.Collections;
using Unity.Netcode;

namespace EquipmentStatEditor.Core
{
    /// <summary>
    /// Orchestrates the dump -> edit -> apply -> native save workflow.
    ///
    /// Lifecycle:
    ///   * Poll: once the local player's rune lists are stable, run a cycle.
    ///   * Cycle: parse the config file, apply entries the user edited (detected via
    ///     the Hash= key), leave everything else untouched. A fresh dump is NOT
    ///     written yet - the edits are not on disk until the game itself saves.
    ///   * OnNativeSaveCompleted: the game just serialized our edited runes -> rewrite
    ///     the dump file so its Hash keys mark the edits as consumed. If the file was
    ///     edited mid-session (disk text differs from what we last read/wrote) the
    ///     rewrite is skipped so the user's pending edits survive to the next launch.
    /// </summary>
    public static class EditorEngine
    {
        private static Action<string> _logInfo = _ => { };
        private static Action<string> _logWarn = _ => { };
        private static Action<string> _logError = _ => { };

        private static string _charName;
        private static string _filePath;
        private static string _lastFileDigest = "";
        private static string _processedKey;
        private static Player _player;

        public static bool AutoApplyOnLoad = true;
        public static bool RewriteDumpAfterSave = true;
        public static bool SaveAfterApply = false;
        public static bool IncludeInventory = true;

        public static void BindLogger(Action<string> info, Action<string> warn, Action<string> error)
        {
            _logInfo = info ?? _logInfo;
            _logWarn = warn ?? _logWarn;
            _logError = error ?? _logError;
            RuneMemory.BindLogger(info, warn, error);
        }

        // ------------------------------------------------------------------
        // Player lookup / readiness
        // ------------------------------------------------------------------

        public static Player GetLocalPlayer()
        {
            try
            {
                if (DataStorage.Singleton != null && DataStorage.Singleton.Player != null)
                    return DataStorage.Singleton.Player;
            }
            catch { }

            try
            {
                var players = UnityEngine.Object.FindObjectsOfType<Player>();
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p != null && p.IsOwner) return p;
                    }
                    if (players.Length > 0) return players[0];
                }
            }
            catch { }

            return null;
        }

        private static bool IsPlayerReady(Player player)
        {
            try
            {
                if (player == null) return false;
                if (player.EquipmentWorn == null && player.RuneInventory == null) return false;
                return true;
            }
            catch { return false; }
        }

        private static string SnapshotSignature(Player player)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(GetCharacterName(player)).Append('|');
                if (player.EquipmentWorn != null)
                {
                    for (int i = 0; i < player.EquipmentWorn.Count; i++)
                    {
                        IntPtr d = RuneMemory.DataPtr(player.EquipmentWorn[i]);
                        if (d != IntPtr.Zero && !RuneMemory.IsEmptyRune(d))
                            sb.Append(RuneMemory.ReadUuid(d)).Append(',');
                    }
                }
                sb.Append('|');
                if (IncludeInventory && player.RuneInventory != null)
                {
                    for (int i = 0; i < player.RuneInventory.Count; i++)
                    {
                        IntPtr d = RuneMemory.DataPtr(player.RuneInventory[i]);
                        if (d != IntPtr.Zero && !RuneMemory.IsEmptyRune(d))
                            sb.Append(RuneMemory.ReadUuid(d)).Append(',');
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        public static string GetCharacterName(Player player)
        {
            try
            {
                string name = player?.Name != null ? player.Name.Value.ToString() : null;
                if (!string.IsNullOrWhiteSpace(name)) return name.Trim();
            }
            catch { }
            return "Unknown";
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        // ------------------------------------------------------------------
        // Poll + cycle
        // ------------------------------------------------------------------

        private static string _lastSignature = "";
        private static int _stableCount;

        /// <summary>Called about once per second from the controller.</summary>
        public static void Poll()
        {
            var player = GetLocalPlayer();
            if (!IsPlayerReady(player))
            {
                _stableCount = 0;
                _lastSignature = "";
                return;
            }

            string sig = SnapshotSignature(player);
            if (sig != _lastSignature)
            {
                _lastSignature = sig;
                _stableCount = 0;
                return;
            }

            // Wait for two identical snapshots so we never read a half-loaded character.
            if (++_stableCount < 2) return;
            _stableCount = 0;

            RunCycle(player, manual: false);
        }

        /// <summary>
        /// Parse the config file and apply the entries the user edited.
        /// Returns the number of runes modified.
        /// </summary>
        public static int RunCycle(Player player, bool manual)
        {
            try
            {
                if (player == null || !IsPlayerReady(player))
                {
                    _logWarn("[EquipmentStatEditor] no ready player - nothing to do.");
                    return 0;
                }

                _player = player;
                RuneMemory.EnsureOffsets();

                string charName = GetCharacterName(player);
                string file = Path.Combine(Paths.ConfigPath, $"EquipmentStatEditor.{SanitizeFileName(charName)}.cfg");
                string digest = File.Exists(file) ? Digest(File.ReadAllText(file)) : "";

                string cycleKey = charName + "|" + digest;
                if (!manual && cycleKey == _processedKey)
                    return 0; // file unchanged since the last cycle - nothing new to apply

                _processedKey = cycleKey;
                _charName = charName;
                _filePath = file;
                _lastFileDigest = digest;

                if (!File.Exists(file))
                {
                    _logInfo($"[EquipmentStatEditor] no config for '{charName}' yet - writing fresh dump: {file}");
                    WriteDump(player, charName, file);
                    return 0;
                }

                var entries = RuneConfigIO.Parse(file);
                var byUuid = new Dictionary<string, RuneEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in entries)
                {
                    if (e.Uuid.Length == 0) continue;
                    if (!byUuid.ContainsKey(e.Uuid)) byUuid[e.Uuid] = e;
                }

                int applied = ApplyEditedEntries(player, byUuid);
                _logInfo($"[EquipmentStatEditor] cycle done for '{charName}': {applied} rune(s) modified " +
                         $"({entries.Count} entries in file).");

                if (applied > 0 && SaveAfterApply)
                    SaveNow(player);

                return applied;
            }
            catch (Exception ex)
            {
                _logError($"[EquipmentStatEditor] cycle failed: {ex}");
                return 0;
            }
        }

        private static int ApplyEditedEntries(Player player, Dictionary<string, RuneEntry> byUuid)
        {
            int applied = 0;
            applied += ScanCollection(player, player.EquipmentWorn, typeof(NetworkList<Rune>),
                player.EquipmentWorn?.Count ?? 0, byUuid, "EquipmentWorn", equipped: true);
            if (IncludeInventory)
                applied += ScanCollection(player, player.RuneInventory, typeof(Il2CppSystem.Collections.Generic.List<Rune>),
                    player.RuneInventory?.Count ?? 0, byUuid, "RuneInventory", equipped: false);

            if (applied > 0)
            {
                RuneMemory.Rebaseline(player);
                RuneMemory.RecalcBonuses(player);
            }
            return applied;
        }

        private static int ScanCollection(Player player, Il2CppObjectBase collection, Type collectionType,
            int count, Dictionary<string, RuneEntry> byUuid, string listName, bool equipped)
        {
            if (collection == null || count <= 0) return 0;

            int applied = 0;
            for (int i = 0; i < count; i++)
            {
                Rune rune;
                try
                {
                    rune = equipped ? player.EquipmentWorn[i] : player.RuneInventory[i];
                }
                catch { continue; }

                IntPtr data = RuneMemory.DataPtr(rune);
                if (data == IntPtr.Zero || RuneMemory.IsEmptyRune(data)) continue;

                string uuid = RuneMemory.ReadUuid(data);
                if (!byUuid.TryGetValue(uuid, out var entry)) continue;

                entry.Matched = true;
                if (entry.Status.StartsWith("REJECTED")) continue;
                if (!entry.IsUserEdited) continue;

                // Fields that only make sense relative to the live rune.
                if (ValidateLiveEdit(entry, data, uuid)) continue;

                try
                {
                    RuneMemory.ApplyEntry(data, entry);
                    // [2026-09-28 10:40] OBSOLETE - PreSign called the game's private
                    // ComputeRuneHMAC directly, which ALWAYS threw NullReferenceException here
                    // ("[RuneMemory] pre-sign failed: Object reference not set to an instance of
                    // an object."). It never populated the store, so it was dead weight; the
                    // post-write Rebaseline (below/after) re-signs via the game's own
                    // RefreshAllRuneSignatures, which is the proven path used by the standalone
                    // EquippedStatModifier. Removed to stop the recurring error log.
                    // RuneMemory.PreSign(player, data);
                    RuneMemory.SetElementRaw(collection, collectionType, i, data);
                    entry.Applied = true;
                    applied++;
                    _logInfo($"[EquipmentStatEditor] applied '{uuid}' ({listName}[{i}]) - {Describe(entry)}");
                }
                catch (Exception ex)
                {
                    entry.Status = $"REJECTED: apply failed - {ex.Message}";
                    _logError($"[EquipmentStatEditor] apply failed for '{uuid}': {ex.Message}");
                }
            }
            return applied;
        }

        /// <summary>Live-only validation. Returns true when the entry must be rejected.</summary>
        private static bool ValidateLiveEdit(RuneEntry entry, IntPtr data, string uuid)
        {
            var snap = RuneMemory.Read(data);

            /* [2026-09-26 02:46] Obsolete: demanded exactly one value per secondary. The game
               layout is StatValues = secondaries + 1 (slot 0 = PrimaryStat magnitude), so this
               rejected legal game-layout edits (see the RuneConfigIO note).
            if (entry.HasStats == false && entry.HasValues && entry.StatValues.Count != snap.Stats.Length)
            {
                entry.Status = $"REJECTED: {entry.StatValues.Count} StatValues but rune has {snap.Stats.Length} secondaries";
                _logWarn($"[EquipmentStatEditor] '{uuid}': {entry.Status}");
                return true;
            }
            */
            // [2026-09-26 02:46] Fixed: accept the game layout (n + 1 values) and the
            // shorthand (n values, expanded at apply time in RuneMemory.ApplyEntry).
            if (entry.HasStats == false && entry.HasValues &&
                entry.StatValues.Count != snap.Values.Length && entry.StatValues.Count != snap.Stats.Length)
            {
                entry.Status = $"REJECTED: {entry.StatValues.Count} StatValues but rune has {snap.Stats.Length} secondaries (expected {snap.Values.Length} or {snap.Stats.Length})";
                _logWarn($"[EquipmentStatEditor] '{uuid}': {entry.Status}");
                return true;
            }
            if (entry.HasStats == false && entry.HasUpgrades && entry.StatUpgrades.Count != snap.Stats.Length)
            {
                entry.Status = $"REJECTED: {entry.StatUpgrades.Count} StatUpgrades but rune has {snap.Stats.Length} secondaries";
                _logWarn($"[EquipmentStatEditor] '{uuid}': {entry.Status}");
                return true;
            }
            return false;
        }

        private static string Describe(RuneEntry e)
        {
            var parts = new List<string>();
            if (e.Level.HasValue) parts.Add($"Level={e.Level}");
            if (e.PrimaryStat.HasValue) parts.Add($"Primary={e.PrimaryStat}");
            if (e.HasStats) parts.Add($"Secondaries=[{string.Join(",", e.SecondaryStats)}]");
            if (e.HasValues) parts.Add($"Values=[{string.Join(",", e.StatValues)}]");
            return string.Join(" ", parts);
        }

        /// <summary>
        /// [2026-09-26 01:59] New: apply one in-game UI edit (ported rune editor panel,
        /// EquipmentStatEditor/UI/RuneEditorUI.cs) to an equipped slot.
        /// Uses the exact proven write pipeline of ScanCollection (ApplyEntry -> PreSign ->
        /// SetElementRaw) followed by a full re-baseline + bonus recalc so the game's rune
        /// integrity check accepts the change. Kept separate from the config cycle so UI
        /// edits never depend on the config file (which remains the backup workflow).
        /// [2026-09-26 02:46] Hardened with error prevention & handler: input validation,
        /// pre-edit snapshot, immediate readback rollback, and post-apply verification -
        /// the game can strip an item a moment AFTER the write (user report 2026-09-26:
        /// "when I add stat number on attack speed around 20, the item suddenly gone"), and
        /// a bad edit must never destroy the item.
        /// </summary>
        public static bool ApplyUiEdit(Player player, int slotIndex, RuneEntry entry, out string error)
        {
            error = "";
            try
            {
                if (player == null || player.EquipmentWorn == null)
                {
                    error = "Player or EquipmentWorn is not available.";
                    return false;
                }
                if (slotIndex < 0 || slotIndex >= player.EquipmentWorn.Count)
                {
                    error = $"Slot index {slotIndex} out of bounds.";
                    return false;
                }

                // [2026-09-26 02:46] Added: central input validation before touching memory.
                if (!ValidateUiEntry(entry, out error)) return false;

                Rune rune = player.EquipmentWorn[slotIndex];
                IntPtr data = RuneMemory.DataPtr(rune);
                if (data == IntPtr.Zero || RuneMemory.IsEmptyRune(data))
                {
                    error = "No item equipped in that slot.";
                    return false;
                }
                // [2026-09-26 02:46] Added: pre-edit snapshot (rollback) + identity for the
                // post-apply verifier.
                string uuid = RuneMemory.ReadUuid(data);
                byte[] snapshot = RuneMemory.SnapshotRaw(data);

                RuneMemory.EnsureOffsets();
                RuneMemory.ApplyEntry(data, entry);
                // [2026-09-28 10:40] OBSOLETE - see ScanCollection: PreSign always threw
                // (ComputeRuneHMAC NRE) and never signed anything. Rebaseline below re-signs
                // through the game's RefreshAllRuneSignatures.
                // RuneMemory.PreSign(player, data);
                RuneMemory.SetElementRaw(player.EquipmentWorn, typeof(NetworkList<Rune>), slotIndex, data);
                RuneMemory.Rebaseline(player);
                RuneMemory.RecalcBonuses(player);

                // [2026-09-26 02:46] Added: immediate readback - if the game rejected the
                // write on the spot (item gone / replaced), roll back at once.
                Rune check = player.EquipmentWorn[slotIndex];
                IntPtr checkData = RuneMemory.DataPtr(check);
                bool intact = checkData != IntPtr.Zero && !RuneMemory.IsEmptyRune(checkData) &&
                              string.Equals(RuneMemory.ReadUuid(checkData), uuid, StringComparison.OrdinalIgnoreCase);
                if (!intact)
                {
                    _logWarn($"[EquipmentStatEditor] UI edit to slot {slotIndex} was rejected by the game - rolling back.");
                    RuneMemory.RestoreRaw(player, player.EquipmentWorn, typeof(NetworkList<Rune>), slotIndex, snapshot);
                    RuneMemory.Rebaseline(player);
                    RuneMemory.RecalcBonuses(player);
                    error = "The game rejected this edit - the item was rolled back to its previous state.";
                    return false;
                }

                // [2026-09-26 02:46] Added: keep watching the slot - the integrity coroutine
                // can strip the item seconds later (see VerifyPending).
                _pendingVerifies.Add(new PendingVerify { Slot = slotIndex, Uuid = uuid, Snapshot = snapshot });

                // Keep the post-save dump rewrite working even if no poll cycle ran yet.
                _player = player;
                if (_filePath == null)
                {
                    _charName = GetCharacterName(player);
                    _filePath = Path.Combine(Paths.ConfigPath, $"EquipmentStatEditor.{SanitizeFileName(_charName)}.cfg");
                    _lastFileDigest = File.Exists(_filePath) ? Digest(File.ReadAllText(_filePath)) : "";
                }

                _logInfo($"[EquipmentStatEditor] UI applied edit to slot {slotIndex} - {Describe(entry)}");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _logError($"[EquipmentStatEditor] UI apply failed: {ex}");
                return false;
            }
        }

        // ------------------------------------------------------------------
        // [2026-09-26 02:46] Added: apply-error prevention & handler
        // ------------------------------------------------------------------

        private class PendingVerify
        {
            public int Slot;
            public string Uuid;
            public byte[] Snapshot;
            public int ChecksLeft = 10; // ~10 s of poll ticks before we trust the item survived
        }

        private static readonly List<PendingVerify> _pendingVerifies = new();

        /// <summary>Notice for the UI status bar (set when the handler rolls an edit back).</summary>
        public static string UiNotice;

        /// <summary>
        /// Central validation for UI edits. Mirrors the config parser rules: at most
        /// RuneMemory.MaxSecondaryStats secondaries, no duplicates / primary-equal stats,
        /// finite non-negative values, and the StatValues count convention (one value per
        /// secondary, or the full n + 1 game layout - the PrimaryStat value slot is preserved
        /// automatically by RuneMemory.ApplyEntry).
        /// </summary>
        private static bool ValidateUiEntry(RuneEntry entry, out string error)
        {
            error = "";
            if (entry == null)
            {
                error = "No edit data.";
                return false;
            }

            if (entry.SecondaryStats != null)
            {
                if (entry.SecondaryStats.Count > RuneMemory.MaxSecondaryStats)
                {
                    error = $"Too many secondary stats ({entry.SecondaryStats.Count}) - max {RuneMemory.MaxSecondaryStats} " +
                            "(one value slot is reserved for the PrimaryStat).";
                    return false;
                }
                var seen = new HashSet<Runes.Stat>();
                foreach (var s in entry.SecondaryStats)
                {
                    if (entry.PrimaryStat.HasValue && s == entry.PrimaryStat.Value)
                    {
                        error = $"Secondary '{s}' equals the PrimaryStat.";
                        return false;
                    }
                    if (!seen.Add(s))
                    {
                        error = $"Duplicate secondary '{s}' not allowed.";
                        return false;
                    }
                }
            }

            if (entry.StatValues != null)
            {
                foreach (var v in entry.StatValues)
                {
                    if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f || v > 9999f)
                    {
                        error = $"Stat value {v} is invalid (must be a finite number in 0 - 9999).";
                        return false;
                    }
                }
                int secN = entry.SecondaryStats?.Count ?? -1;
                if (secN >= 0 && entry.StatValues.Count != secN && entry.StatValues.Count != secN + 1)
                {
                    error = $"{entry.StatValues.Count} values for {secN} secondary stats (expected {secN + 1} = one per secondary + PrimaryStat value).";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called every poll tick from the controller (regardless of AutoApplyOnLoad). Re-checks
        /// every recently edited slot: when the game strips the item after the write (integrity
        /// coroutine / plausibility checks), the pre-edit snapshot is written back so a bad edit
        /// can never destroy the item. If the slot simply holds a different item now (player
        /// swapped gear) the check is dropped without touching anything.
        /// </summary>
        public static void VerifyPending()
        {
            try
            {
                if (_pendingVerifies.Count == 0) return;
                var player = GetLocalPlayer();
                if (player == null || player.EquipmentWorn == null) return;

                for (int i = _pendingVerifies.Count - 1; i >= 0; i--)
                {
                    var pv = _pendingVerifies[i];
                    if (pv.Slot < 0 || pv.Slot >= player.EquipmentWorn.Count)
                    {
                        _pendingVerifies.RemoveAt(i);
                        continue;
                    }

                    Rune rune = player.EquipmentWorn[pv.Slot];
                    IntPtr data = RuneMemory.DataPtr(rune);
                    string uuid = data != IntPtr.Zero && !RuneMemory.IsEmptyRune(data)
                        ? RuneMemory.ReadUuid(data)
                        : "";

                    if (uuid.Length == 0)
                    {
                        _logWarn($"[EquipmentStatEditor] slot {pv.Slot}: item '{pv.Uuid}' vanished after the edit - rolling back.");
                        RuneMemory.RestoreRaw(player, player.EquipmentWorn, typeof(NetworkList<Rune>), pv.Slot, pv.Snapshot);
                        RuneMemory.Rebaseline(player);
                        RuneMemory.RecalcBonuses(player);
                        UiNotice = "<color=#FF8844>Game rejected the edit - item restored to its pre-edit state.</color>";
                        _pendingVerifies.RemoveAt(i);
                        continue;
                    }

                    if (!uuid.Equals(pv.Uuid, StringComparison.OrdinalIgnoreCase))
                    {
                        // Slot holds a different item now (player swapped gear) - not our concern.
                        _pendingVerifies.RemoveAt(i);
                        continue;
                    }

                    if (--pv.ChecksLeft <= 0)
                        _pendingVerifies.RemoveAt(i); // survived every check - trust the edit
                }
            }
            catch (Exception ex) { _logError($"[EquipmentStatEditor] pending-verify failed: {ex}"); }
        }

        // ------------------------------------------------------------------
        // Dump / rewrite
        // ------------------------------------------------------------------

        public static void SaveNow(Player player)
        {
            try
            {
                player.SaveGame();
                _logInfo("[EquipmentStatEditor] native save triggered (Player.SaveGame).");
            }
            catch (Exception ex) { _logError($"[EquipmentStatEditor] SaveGame failed: {ex.Message}"); }
        }

        /// <summary>
        /// [2026-09-27 15:15] Added: write the rune dump to the character's config file on demand.
        /// Called by the panel's "Dump Runes to File" button. Replaces the old unconditional
        /// rewrite-on-every-native-save (RewriteDumpAfterSave now defaults to false), so the
        /// config is only written when the user asks for it. Returns the file path on success.
        /// </summary>
        public static bool DumpToFile(out string path)
        {
            path = null;
            try
            {
                Player player = _player ?? GetLocalPlayer();
                if (player == null || !IsPlayerReady(player))
                {
                    _logWarn("[EquipmentStatEditor] dump skipped: no ready player.");
                    return false;
                }

                _player = player;
                string charName = _charName ?? GetCharacterName(player);
                string file = Path.Combine(Paths.ConfigPath, $"EquipmentStatEditor.{SanitizeFileName(charName)}.cfg");

                WriteDump(player, charName, file);

                _charName = charName;
                _filePath = file;
                _lastFileDigest = Digest(File.ReadAllText(file));
                path = file;
                _logInfo($"[EquipmentStatEditor] on-demand dump written: {file}");
                return true;
            }
            catch (Exception ex)
            {
                _logError($"[EquipmentStatEditor] on-demand dump failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Harmony postfix target: a real character save just happened.</summary>
        public static void OnNativeSaveCompleted()
        {
            try
            {
                if (!RewriteDumpAfterSave) return;
                if (_player == null || _filePath == null) return;

                // If the user edited the file mid-session, leave it alone: their edits
                // are applied on the next cycle / next launch instead of being eaten.
                if (File.Exists(_filePath) && Digest(File.ReadAllText(_filePath)) != _lastFileDigest)
                {
                    _logInfo("[EquipmentStatEditor] config changed on disk since last read - leaving it untouched.");
                    return;
                }

                WriteDump(_player, _charName, _filePath);
            }
            catch (Exception ex) { _logError($"[EquipmentStatEditor] dump rewrite failed: {ex.Message}"); }
        }

        private static void WriteDump(Player player, string charName, string file)
        {
            RuneMemory.EnsureOffsets();

            var current = new List<RuneEntry>();
            var carryOver = new List<RuneEntry>();

            // Preserve entries from the old file that never matched a rune this
            // session (other character, item not obtained yet, or rejected input).
            var previous = RuneConfigIO.Parse(file);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CollectList(player, player.EquipmentWorn, player.EquipmentWorn?.Count ?? 0, true, current, seen);
            if (IncludeInventory)
                CollectList(player, player.RuneInventory, player.RuneInventory?.Count ?? 0, false, current, seen);

            // [2026-09-26 02:46] Fixed: 'previous' comes from a FRESH parse, so e.Matched was
            // always false and every old entry was carried over on every dump - the file grew
            // without bound (1914 entries for 58 runes, the same section duplicated dozens of
            // times). Entries already collected from live memory and duplicate UUIDs are now
            // skipped, which also collapses the accidental growth on the next write.
            var carrySeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in previous)
            {
                if (e.Matched && !e.Status.StartsWith("REJECTED")) continue;
                if (e.Uuid.Length > 0)
                {
                    if (seen.Contains(e.Uuid)) continue;
                    if (!carrySeen.Add(e.Uuid)) continue;
                }
                if (e.Status.Length == 0)
                    e.Status = e.Matched ? "REJECTED: not applied" : "NOT FOUND on this character";
                carryOver.Add(e);
            }

            // [2026-09-26 23:55] Added: Write now returns false when the dump is unchanged, so an
            // identical poll/save no longer rewrites the file or spams the log.
            bool changed = RuneConfigIO.Write(file, charName, current, carryOver, includeHeader: true);
            _lastFileDigest = File.Exists(file) ? Digest(File.ReadAllText(file)) : "";
            if (changed)
                _logInfo($"[EquipmentStatEditor] dump written: {file} ({current.Count} runes).");
        }

        private static void CollectList(Player player, Il2CppObjectBase collection, int count,
            bool equipped, List<RuneEntry> into, HashSet<string> seen)
        {
            if (collection == null || count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                Rune rune;
                try
                {
                    rune = equipped ? player.EquipmentWorn[i] : player.RuneInventory[i];
                }
                catch { continue; }

                IntPtr data = RuneMemory.DataPtr(rune);
                if (data == IntPtr.Zero) continue;

                var snap = RuneMemory.Read(data);
                if (snap.Empty || snap.Uuid.Length == 0) continue;
                if (!seen.Add(snap.Uuid)) continue;

                var e = new RuneEntry
                {
                    Uuid = snap.Uuid,
                    Set = (Runes.RuneSet)snap.Set,
                    SlotType = (Runes.SlotType)snap.SlotType,
                    Rarity = (Runes.Rarity)snap.Rarity,
                    Stars = (Runes.Stars)snap.Stars,
                    Level = snap.Level,
                    PrimaryStat = (Runes.Stat)snap.PrimaryStat,
                    SecondaryStats = snap.Stats.Select(s => (Runes.Stat)s).ToList(),
                    StatValues = snap.Values.ToList(),
                    StatUpgrades = snap.Upgrades.ToList(),
                };
                e.StoredHash = e.ComputeHash();
                e.Status = equipped
                    ? $"INFO: equipped slot {i} ({(Runes.Slot)i})"
                    : $"INFO: rune inventory slot {i}";
                into.Add(e);
            }
        }

        private static string Digest(string text)
        {
            unchecked
            {
                uint h = 2166136261;
                for (int i = 0; i < text.Length; i++)
                {
                    h ^= text[i];
                    h *= 16777619;
                }
                return h.ToString("X8");
            }
        }
    }

    // ---------------------------------------------------------------------
    // Save hooks: "the game just serialized our character" signals.
    // ---------------------------------------------------------------------

    public static class SaveHooks
    {
        public static void Install(Harmony harmony, Action<string> logInfo, Action<string> logWarn)
        {
            string[] names = { "SavePlayerLocally", "SavePlayerData", "SaveCharacterImmediately" };
            var methods = AccessTools.GetDeclaredMethods(typeof(SaveSystem));
            int patched = 0;

            foreach (string name in names)
            {
                foreach (var m in methods)
                {
                    if (m.Name != name) continue;
                    try
                    {
                        harmony.Patch(m, postfix: new HarmonyMethod(AccessTools.Method(typeof(SaveHooks), nameof(OnSaved))));
                        patched++;
                    }
                    catch (Exception ex) { logWarn($"[EquipmentStatEditor] could not patch SaveSystem.{name}: {ex.Message}"); }
                }
            }

            logInfo($"[EquipmentStatEditor] save hooks installed: {patched} method(s).");
            if (patched == 0)
                logWarn("[EquipmentStatEditor] no save hooks installed - the dump file will not auto-refresh after saves.");
        }

        public static void OnSaved()
        {
            EditorEngine.OnNativeSaveCompleted();
        }
    }
}
