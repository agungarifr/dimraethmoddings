using System;
using System.Collections.Generic;

namespace UpgradeBonusStatIsNotRandom.Core
{
    public static class StatCatalog
    {
        public static readonly string[] StatNames;
        public static readonly Runes.Stat[] StatValues;
        private static readonly Dictionary<string, Runes.Stat> NameToStat;

        static StatCatalog()
        {
            var values = (Runes.Stat[])Enum.GetValues(typeof(Runes.Stat));
            var listNames = new List<string>(values.Length + 1);
            var listVals = new List<Runes.Stat>(values.Length + 1);
            NameToStat = new Dictionary<string, Runes.Stat>(values.Length + 1, StringComparer.OrdinalIgnoreCase);

            listNames.Add("None (Vanilla RNG)");
            listVals.Add((Runes.Stat)(-1));

            for (int i = 0; i < values.Length; i++)
            {
                var val = values[i];
                string name = val.ToString();
                listNames.Add(name);
                listVals.Add(val);
                NameToStat[name] = val;
            }

            StatNames = listNames.ToArray();
            StatValues = listVals.ToArray();
        }

        public static bool TryGetStat(string name, out Runes.Stat stat)
        {
            if (string.IsNullOrEmpty(name) || name.Equals("None", StringComparison.OrdinalIgnoreCase) || name.StartsWith("None", StringComparison.OrdinalIgnoreCase))
            {
                stat = (Runes.Stat)(-1);
                return false;
            }
            return NameToStat.TryGetValue(name, out stat);
        }

        public static bool IsRuneAlreadyHasStat(Rune rune, Runes.Stat target)
        {
            if (rune.PrimaryStat == target) return true;
            int count = rune.SecondaryStatsCount;
            for (int i = 0; i < count; i++)
            {
                if (rune.SecondaryStats[i] == target) return true;
            }
            return false;
        }
    }
}
