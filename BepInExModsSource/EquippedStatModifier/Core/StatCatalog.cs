using System;
using System.Collections.Generic;

namespace EquippedStatModifier.Core
{
    public static class StatCatalog
    {
        public static readonly string[] SlotDisplayNames = new string[]
        {
            "Slot 0: Rune",
            "Slot 1: Undercoat",
            "Slot 2: Belt (Sash)",
            "Slot 3: Charm",
            "Slot 4: Ring 1",
            "Slot 5: Ring 2",
            "Slot 6: Amulet 1",
            "Slot 7: Amulet 2",
            "Slot 8: Badge 1",
            "Slot 9: Badge 2",
            "Slot 10: Bracer 1",
            "Slot 11: Bracer 2"
        };

        public static readonly List<Runes.Stat> AllStats = new();
        public static readonly List<string> AllStatNames = new();
        private static readonly Dictionary<string, Runes.Stat> NameToStat = new(StringComparer.OrdinalIgnoreCase);

        static StatCatalog()
        {
            var values = (Runes.Stat[])Enum.GetValues(typeof(Runes.Stat));
            var sortedNames = new List<string>();

            foreach (var val in values)
            {
                if (val == Runes.Stat.None) continue;
                string name = val.ToString();
                sortedNames.Add(name);
                NameToStat[name] = val;
            }

            sortedNames.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var name in sortedNames)
            {
                AllStatNames.Add(name);
                AllStats.Add(NameToStat[name]);
            }
        }

        public static bool TryGetStat(string name, out Runes.Stat stat)
        {
            if (string.IsNullOrEmpty(name))
            {
                stat = Runes.Stat.None;
                return false;
            }
            return NameToStat.TryGetValue(name, out stat);
        }

        public static List<Runes.Stat> FilterStats(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return AllStats;

            string query = filter.Trim();
            var matches = new List<Runes.Stat>();
            foreach (var stat in AllStats)
            {
                if (stat.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(stat);
                }
            }
            return matches;
        }
    }
}
