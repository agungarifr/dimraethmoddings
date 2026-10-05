using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EquipmentStatEditor.Core
{
    /// <summary>One [Rune_xxx] section of the dump/config file.</summary>
    public class RuneEntry
    {
        public string Section = "";
        public string Uuid = "";

        // Only fields the user actually wrote are set - partial edits are supported.
        public Runes.RuneSet? Set;
        public Runes.SlotType? SlotType;
        public Runes.Rarity? Rarity;
        public Runes.Stars? Stars;
        public int? Level;
        public Runes.Stat? PrimaryStat;
        public List<Runes.Stat> SecondaryStats;
        public List<float> StatValues;
        public List<int> StatUpgrades;

        // Hash= key as stored in the file (detects user edits). Empty = force apply.
        public string StoredHash = "";

        // Verbatim section lines, kept for entries we cannot apply (errors / not found).
        public List<string> RawLines = new List<string>();
        public string Status = "";

        public bool Matched;   // a rune with this UUID exists in the current character
        public bool Applied;   // successfully written this cycle

        public bool HasStats => SecondaryStats != null;
        public bool HasValues => StatValues != null;
        public bool HasUpgrades => StatUpgrades != null;

        /// <summary>
        /// True when the file content differs from the hash we stored at dump time,
        /// i.e. the user edited this entry and wants it applied. A missing Hash line
        /// forces an apply (useful to re-apply unchanged values).
        /// </summary>
        public bool IsUserEdited =>
            string.IsNullOrEmpty(StoredHash) ||
            !string.Equals(StoredHash, ComputeHash(), StringComparison.OrdinalIgnoreCase);

        public string ComputeHash()
        {
            var sb = new StringBuilder();
            sb.Append("Set=").Append(Set?.ToString() ?? "").Append('|');
            sb.Append("SlotType=").Append(SlotType?.ToString() ?? "").Append('|');
            sb.Append("Rarity=").Append(Rarity?.ToString() ?? "").Append('|');
            sb.Append("Stars=").Append(Stars?.ToString() ?? "").Append('|');
            sb.Append("Level=").Append(Level?.ToString(CultureInfo.InvariantCulture) ?? "").Append('|');
            sb.Append("PrimaryStat=").Append(PrimaryStat?.ToString() ?? "").Append('|');
            sb.Append("SecondaryStats=").Append(JoinStats(SecondaryStats)).Append('|');
            sb.Append("StatValues=").Append(JoinFloats(StatValues)).Append('|');
            sb.Append("StatUpgrades=").Append(JoinInts(StatUpgrades));

            // FNV-1a 32-bit over the canonical field text.
            unchecked
            {
                uint h = 2166136261;
                string s = sb.ToString();
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= s[i];
                    h *= 16777619;
                }
                return h.ToString("X8", CultureInfo.InvariantCulture);
            }
        }

        private static string JoinStats(List<Runes.Stat> list) =>
            list == null ? "" : string.Join(",", list.ConvertAll(s => s.ToString()));

        private static string JoinFloats(List<float> list) =>
            list == null ? "" : string.Join(",", list.ConvertAll(f => f.ToString("R", CultureInfo.InvariantCulture)));

        private static string JoinInts(List<int> list) =>
            list == null ? "" : string.Join(",", list.ConvertAll(i => i.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>INI-style reader/writer for the equipment dump/config file.</summary>
    public static class RuneConfigIO
    {
        public const string HashKey = "Hash";

        // ---------------------------------------------------------------
        // Parsing
        // ---------------------------------------------------------------

        /// <summary>
        /// Parse the file into entries. Never throws on bad content: broken sections
        /// are kept verbatim in their RawLines with a Status reason so they survive
        /// the next dump rewrite for the user to fix.
        /// </summary>
        public static List<RuneEntry> Parse(string path)
        {
            var entries = new List<RuneEntry>();
            if (!File.Exists(path)) return entries;

            RuneEntry current = null;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    current = new RuneEntry
                    {
                        Section = line.Substring(1, line.Length - 2),
                        RawLines = new List<string> { raw }
                    };
                    current.Uuid = current.Section.StartsWith("Rune_", StringComparison.OrdinalIgnoreCase)
                        ? current.Section.Substring(5)
                        : current.Section;
                    entries.Add(current);
                    continue;
                }

                if (current == null) continue;
                current.RawLines.Add(raw);

                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();

                try
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "set": current.Set = ParseEnum<Runes.RuneSet>(val); break;
                        case "slottype": current.SlotType = ParseEnum<Runes.SlotType>(val); break;
                        case "rarity": current.Rarity = ParseEnum<Runes.Rarity>(val); break;
                        case "stars": current.Stars = ParseEnum<Runes.Stars>(val); break;
                        case "level":
                            current.Level = int.Parse(val, CultureInfo.InvariantCulture);
                            if (current.Level < 0) throw new FormatException("Level must be >= 0");
                            break;
                        case "primarystat": current.PrimaryStat = ParseEnum<Runes.Stat>(val); break;
                        case "secondarystats":
                            current.SecondaryStats = new List<Runes.Stat>();
                            foreach (string tok in SplitList(val))
                                current.SecondaryStats.Add(ParseEnum<Runes.Stat>(tok));
                            break;
                        case "statvalues":
                            current.StatValues = new List<float>();
                            foreach (string tok in SplitList(val))
                            {
                                if (!float.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
                                    float.IsNaN(f) || float.IsInfinity(f))
                                    throw new FormatException($"bad StatValues entry '{tok}'");
                                current.StatValues.Add(f);
                            }
                            break;
                        case "statupgrades":
                            current.StatUpgrades = new List<int>();
                            foreach (string tok in SplitList(val))
                                current.StatUpgrades.Add(int.Parse(tok, CultureInfo.InvariantCulture));
                            break;
                        case "hash":
                            current.StoredHash = val;
                            break;
                        // Equipped / Source / comment-echo keys are dump metadata only.
                    }
                }
                catch (Exception ex)
                {
                    current.Status = $"REJECTED: key '{key}' - {ex.Message}";
                }
            }

            foreach (var e in entries)
                ValidateEntry(e);

            return entries;
        }

        private static void ValidateEntry(RuneEntry e)
        {
            if (e.Status.Length > 0) return;

            if (string.IsNullOrWhiteSpace(e.Uuid))
            {
                e.Status = "REJECTED: no UUID in section name";
                return;
            }

            int statCount = e.SecondaryStats?.Count ?? -1;
            int valueCount = e.StatValues?.Count ?? -1;
            int upgCount = e.StatUpgrades?.Count ?? -1;

            // StatValues / StatUpgrades alone are only legal on top of existing
            // secondaries (checked at apply time against the actual rune).
            //
            // [2026-09-26 02:46] Fixed: the game's real layout is
            //   SecondaryStats = n, StatValues = n + 1, StatUpgrades = n
            // where StatValues[0] holds the PRIMARY stat's magnitude (verified on vanilla
            // dump entries, e.g. PrimaryStat=BurningDamage / SecondaryStats=Resistance /
            // StatValues=12,6). The old "valueCount must equal statCount" rule REJECTED every
            // vanilla entry (see the file's "; REJECTED: 1 SecondaryStats but 2 StatValues"
            // lines) and made the UI write values one slot off, so the game displayed 0 for
            // edited secondaries. Secondary cap is 6, not 7: StatValues has 7 slots and one
            // belongs to the primary - 7 secondaries need 8 slots and the overflow destroys
            // the item (user report 2026-09-26).
            if (statCount >= 0)
            {
                if (statCount > RuneMemory.MaxSecondaryStats)
                {
                    e.Status = $"REJECTED: {statCount} SecondaryStats, max {RuneMemory.MaxSecondaryStats} (StatValues slot 0 belongs to PrimaryStat)";
                    return;
                }
                if (valueCount < 0)
                {
                    e.Status = "REJECTED: SecondaryStats given without StatValues";
                    return;
                }
                /* [2026-09-26 02:46] Obsolete: equal-count rule - wrong for the game layout,
                   it rejected every vanilla entry (see note above). Accepting both n + 1
                   (game layout) and n (shorthand, expanded to n + 1 at apply time).
                if (valueCount != statCount)
                {
                    e.Status = $"REJECTED: {statCount} SecondaryStats but {valueCount} StatValues";
                    return;
                }
                */
                if (valueCount != statCount && valueCount != statCount + 1)
                {
                    e.Status = $"REJECTED: {statCount} SecondaryStats but {valueCount} StatValues (expected {statCount + 1} = one per secondary + PrimaryStat value, or {statCount})";
                    return;
                }
                if (upgCount >= 0 && upgCount != statCount)
                {
                    e.Status = $"REJECTED: {statCount} SecondaryStats but {upgCount} StatUpgrades";
                    return;
                }

                var seen = new HashSet<Runes.Stat>();
                foreach (var s in e.SecondaryStats)
                {
                    if (e.PrimaryStat.HasValue && s == e.PrimaryStat.Value)
                    {
                        e.Status = $"REJECTED: secondary '{s}' equals PrimaryStat";
                        return;
                    }
                    if (!seen.Add(s))
                    {
                        e.Status = $"REJECTED: duplicate secondary '{s}'";
                        return;
                    }
                }
            }
            /* [2026-09-26 02:46] Obsolete: equal-count rule for values-only edits - the game
               layout has one more value than upgrades (the PrimaryStat value), so this
               rejected legal edits too (see note above).
            else if (valueCount >= 0 && upgCount >= 0 && valueCount != upgCount)
            {
                e.Status = $"REJECTED: {valueCount} StatValues but {upgCount} StatUpgrades";
                return;
            }
            */
            else if (valueCount >= 0 && upgCount >= 0 &&
                     valueCount != upgCount && valueCount != upgCount + 1)
            {
                e.Status = $"REJECTED: {valueCount} StatValues but {upgCount} StatUpgrades (expected {upgCount + 1} = one per secondary + PrimaryStat value)";
                return;
            }

            if (e.Level.HasValue && e.Level.Value > 12)
                e.Status = ""; // allowed - other mods may raise MaxRuneLevel.
        }

        private static T ParseEnum<T>(string val) where T : struct
        {
            if (!Enum.TryParse<T>(val, true, out T result))
                throw new FormatException($"unknown {typeof(T).Name} '{val}'");
            return result;
        }

        private static IEnumerable<string> SplitList(string val)
        {
            foreach (string tok in val.Split(','))
            {
                string t = tok.Trim();
                if (t.Length > 0) yield return t;
            }
        }

        // ---------------------------------------------------------------
        // Writing
        // ---------------------------------------------------------------

        public static bool Write(string path, string characterName, List<RuneEntry> current,
            List<RuneEntry> carryOver, bool includeHeader)
        {
            var sb = new StringBuilder();
            if (includeHeader)
                AppendHeader(sb, characterName);

            foreach (var e in current)
            {
                sb.AppendLine();
                sb.AppendLine($"[Rune_{e.Uuid}]");
                if (e.Status.Length > 0 && e.Status.StartsWith("INFO"))
                    sb.AppendLine($"; {e.Status}");
                AppendField(sb, "Set", e.Set?.ToString());
                AppendField(sb, "SlotType", e.SlotType?.ToString());
                AppendField(sb, "Rarity", e.Rarity?.ToString());
                AppendField(sb, "Stars", e.Stars?.ToString());
                AppendField(sb, "Level", e.Level?.ToString(CultureInfo.InvariantCulture));
                AppendField(sb, "PrimaryStat", e.PrimaryStat?.ToString());
                AppendField(sb, "SecondaryStats", e.SecondaryStats == null ? null : JoinList(e.SecondaryStats));
                AppendField(sb, "StatValues", e.StatValues == null ? null : JoinFloatList(e.StatValues));
                AppendField(sb, "StatUpgrades", e.StatUpgrades == null ? null : JoinIntList(e.StatUpgrades));
                sb.AppendLine($"{HashKey}={e.ComputeHash()}");
            }

            foreach (var e in carryOver)
            {
                /* [2026-09-26 23:55] Fixed unbounded dump growth. The status/verbatim comments used
                   to be emitted BEFORE the [Rune_*] header, so on the next parse they landed in the
                   PREVIOUS section's RawLines and were re-emitted, accumulating on every rewrite
                   (EquipmentStatEditor.Yoink.cfg reached 524,637 lines / 16.9 MB, 2,099 "dump
                   written" lines). The header is now emitted first, our comments go after it, and
                   any generated status comments are stripped from the verbatim body so they cannot
                   accumulate. The old block is preserved below.
                sb.AppendLine();
                if (e.Status.Length > 0)
                    sb.AppendLine($"; {e.Status}");
                sb.AppendLine("; (kept verbatim - not found on this character or not applied)");
                foreach (string line in e.RawLines)
                    sb.AppendLine(line);
                */
                string header = null;
                var body = new List<string>();
                foreach (string line in e.RawLines)
                {
                    string t = line.Trim();
                    if (header == null && t.StartsWith("[") && t.EndsWith("]")) { header = line; continue; }
                    if (t.StartsWith("; (kept verbatim")) continue;
                    if (t.StartsWith("; NOT FOUND on this character")) continue;
                    if (t.StartsWith("; REJECTED:", StringComparison.Ordinal)) continue;
                    body.Add(line);
                }
                sb.AppendLine();
                if (header != null) sb.AppendLine(header);
                if (e.Status.Length > 0) sb.AppendLine($"; {e.Status}");
                foreach (string line in body)
                    sb.AppendLine(line);
            }

            string text = sb.ToString();

            // [2026-09-26 23:55] Added: skip the write when nothing changed. A poll/save that
            // produces an identical dump no longer rewrites the file (and no longer logs), which
            // was itself feeding the "log feed" and the disk churn.
            try
            {
                if (File.Exists(path) && File.ReadAllText(path) == text)
                    return false;
            }
            catch { }

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text, new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            return true;
        }

        private static void AppendField(StringBuilder sb, string key, string val)
        {
            if (val != null) sb.AppendLine($"{key}={val}");
        }

        private static string JoinList(List<Runes.Stat> list) =>
            string.Join(",", list.ConvertAll(s => s.ToString()));

        private static string JoinFloatList(List<float> list) =>
            string.Join(",", list.ConvertAll(f => f.ToString("R", CultureInfo.InvariantCulture)));

        private static string JoinIntList(List<int> list) =>
            string.Join(",", list.ConvertAll(i => i.ToString(CultureInfo.InvariantCulture)));

        private static void AppendHeader(StringBuilder sb, string characterName)
        {
            sb.AppendLine("; ============================================================");
            sb.AppendLine("; Dimraeth - Equipment Stat Editor - equipment dump / config");
            sb.AppendLine($"; Character: {characterName}    Dumped: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine(";");
            sb.AppendLine("; HOW TO USE");
            sb.AppendLine(";   1. Edit the values below, then save this file.");
            sb.AppendLine(";   2. Start (or reload) the game with the mod: edited entries are applied");
            sb.AppendLine(";      automatically. Only fields you change matter - leave the rest alone.");
            sb.AppendLine(";   3. Trigger a normal game save (main menu / quit). Done! The result");
            sb.AppendLine(";      lives in your character save and works with no mod installed.");
            sb.AppendLine(";");
            sb.AppendLine("; RULES");
            sb.AppendLine(";   - The Hash= key detects YOUR changes. Do not edit it.");
            sb.AppendLine(";     Delete a Hash line to force that entry to be applied again.");
            sb.AppendLine(";   - SecondaryStats / StatValues / StatUpgrades are comma-separated and");
            /* [2026-09-26 02:46] Obsolete: "must have matching counts" described the wrong
               layout - the game stores one extra StatValues entry for the PrimaryStat.
            sb.AppendLine(";     must have matching counts (StatUpgrades optional, defaults to 0).");
            */
            sb.AppendLine(";     game layout: StatValues has one entry MORE than SecondaryStats -");
            sb.AppendLine(";     StatValues[0] is the PrimaryStat magnitude, StatValues[i+1] belongs");
            sb.AppendLine(";     to SecondaryStats[i]. StatUpgrades has one entry per secondary.");
            sb.AppendLine(";     Max 6 secondaries (7 value slots, one reserved for the PrimaryStat).");
            sb.AppendLine(";   - Rune identity (UUID, section name) cannot be changed.");
            sb.AppendLine(";   - Entries that fail or do not match a rune are kept verbatim at the");
            sb.AppendLine(";     bottom of this file with a REJECTED/not-found note.");
            sb.AppendLine(";   - This file is rewritten after each game save to show current state.");
            sb.AppendLine(";");
            sb.AppendLine("; Valid stats (Runes.Stat):");
            WrapNames(sb, Enum.GetNames(typeof(Runes.Stat)));
            sb.AppendLine("; ============================================================");
        }

        private static void WrapNames(StringBuilder sb, string[] names)
        {
            var line = new StringBuilder(";   ");
            foreach (string n in names)
            {
                if (line.Length + n.Length + 2 > 110)
                {
                    sb.AppendLine(line.ToString());
                    line = new StringBuilder(";   ");
                }
                line.Append(n).Append(", ");
            }
            if (line.Length > 4) sb.AppendLine(line.ToString().TrimEnd(',', ' '));
        }
    }
}
