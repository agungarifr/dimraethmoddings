using System;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;

namespace EquipmentStatEditor.Core
{
    /// <summary>
    /// [2026-09-27 11:28] Added: one-shot runtime dump of the game's per-stat roll / ceiling table.
    ///
    /// Why runtime: the IL dump (<c>modding/cpp2il_isil_out</c>) predates the current
    /// GameAssembly.dll, so its constant addresses are stale and the roll values cannot be
    /// read statically. The live <see cref="RuneManager"/> exposes everything we need:
    ///   * <c>BaselineValues</c> / <c>ScalingValues</c> (per-stat constants)
    ///   * <c>CalculateStatBaseValue(stat, stars)</c> = baseline * (1 + scaling * stars)
    ///   * <c>CalculateFinalStatValue(base, level)</c> = base * (1 + StarGrowth*LevelScaling*(level-1))
    ///
    /// The game's own acceptance test is <c>GearLegality.TryStatValueCeiling</c> (internal, so it
    /// is not exposed to mods as a type). The ceiling columns are read live from it via
    /// <see cref="StatCapCatalog.TryGetCap"/>. For reference its formula was:
    ///   ceiling = 2 * CalculateFinalStatValue(base@Stars=2, level) + ObtainableStatValueTolerance
    /// with <c>level = 12</c> for a primary stat and <c>level = 5</c> for a secondary stat.
    /// Values above that ceiling get the item destroyed by <c>GearLegality.DestroyIfUnobtainable</c>.
    ///
    /// [2026-09-27 15:02] Changed: previously replicated the formula with a hard-coded
    /// ObtainableStatValueTolerance(2). A game update changed that constant, so the dump now uses
    /// the live value instead of guessing.
    ///
    /// The dump is written once per process, the first time the data is populated.
    /// </summary>
    public static class StatRollTableDump
    {
        // Game constants (GearLegality / GameConfig / Forge.MaxRuneLevel).
        private const int MaxRuneLevel = 12;          // Forge.MaxRuneLevel
        private const int SecondaryCeilingLevel = 5;  // TryStatValueCeiling: isPrimary == false
        // [2026-09-27 15:02] Obsolete: the ceiling columns no longer use this. The game's
        // tolerance is a build constant that changed in an update, so the dump now reads the
        // live ceiling via StatCapCatalog.TryGetCap. Kept (unused) for historical reference.
        // private const float Tolerance = 2f;        // GameConfig.ObtainableStatValueTolerance
        private const int MaxStars = 2;               // TryStatValueCeiling hardcodes Stars index 2

        private static bool _completed;

        public static string FilePath => Path.Combine(Paths.ConfigPath, "EquipmentStatEditor.StatRolls.csv");

        public static void TryDumpOnce(Action<string> log)
        {
            if (_completed) return;

            RuneManager rm;
            try { rm = RuneManager.Singleton; }
            catch { return; }
            if (rm == null) return;

            try
            {
                if (rm.BaselineValues == null || rm.ScalingValues == null) return;
                int rows = Dump(rm);
                _completed = true;
                log?.Invoke($"[StatRollTable] wrote {rows} stat rows -> {FilePath}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[StatRollTable] dump failed: {ex}");
            }
        }

        private static int Dump(RuneManager rm)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# EquipmentStatEditor stat roll / ceiling table (runtime dump)");
            sb.Append("Scalars: ");
            sb.Append("StarGrowth=").Append(F(rm.StarGrowth));
            sb.Append(" LevelScaling=").Append(F(rm.LevelScaling));
            // [2026-09-27 15:02] Changed: was " Tolerance=" + F(Tolerance) (hard-coded 2). The
            // game's tolerance is a build constant that a game update changed, so the ceiling
            // columns are now read live via StatCapCatalog.TryGetCap; this line only records that.
            // /* obsolete: */ sb.AppendLine(" Tolerance=" + F(Tolerance));
            sb.AppendLine(" ToleranceSource=GearLegality.TryStatValueCeiling(live)");
            sb.AppendLine("Stat,Percent,Baseline,Scaling,Base_Stars2,Value_L1,Value_L5,Value_L12,Ceiling_Secondary,Ceiling_Primary");

            int n = 0;
            foreach (Runes.Stat stat in (Runes.Stat[])Enum.GetValues(typeof(Runes.Stat)))
            {
                if (stat == Runes.Stat.None) continue;
                if (!rm.BaselineValues.ContainsKey(stat) || !rm.ScalingValues.ContainsKey(stat)) continue;

                float baseline = rm.BaselineValues[stat];
                float scaling = rm.ScalingValues[stat];
                bool percent = IsPercent(rm, stat);

                float baseStars2 = rm.CalculateStatBaseValue(stat, (Runes.Stars)MaxStars);
                float v1 = rm.CalculateFinalStatValue(baseStars2, 1);
                float v5 = rm.CalculateFinalStatValue(baseStars2, SecondaryCeilingLevel);
                float v12 = rm.CalculateFinalStatValue(baseStars2, MaxRuneLevel);

                sb.Append(stat.ToString()).Append(',').Append(percent ? '1' : '0').Append(',');
                sb.Append(F(baseline)).Append(',').Append(F(scaling)).Append(',');
                sb.Append(F(baseStars2)).Append(',').Append(F(v1)).Append(',');
                sb.Append(F(v5)).Append(',').Append(F(v12)).Append(',');
                // [2026-09-27 15:02] Changed: was
                //   sb.Append(F(2f * v5 + Tolerance)).Append(',');
                //   sb.AppendLine(F(2f * v12 + Tolerance));
                // which replicated the pre-update formula. Now read the game's own ceiling so the
                // CSV matches what the panel enforces (TryGetCap returns false for uncapped stats).
                // /* obsolete:
                // sb.Append(F(2f * v5 + Tolerance)).Append(',');
                // sb.AppendLine(F(2f * v12 + Tolerance));
                // */
                // [2026-09-29 10:29] Note: the CSV records the VANILLA ceilings on purpose -
                // it is a reference table and must not change with Safe/Bypass mode. Only the
                // panel + Apply validation use the mode-aware TryGetEffectiveCap.
                // [2026-09-30 02:47] Updated wording: "Safe/Bypass mode" -> "Vanilla/Modded caps".
                sb.Append(StatCapCatalog.TryGetCap(stat, false, out float capSec) ? F(capSec) : "").Append(',');
                sb.AppendLine(StatCapCatalog.TryGetCap(stat, true, out float capPri) ? F(capPri) : "");
                n++;
            }

            File.WriteAllText(FilePath, sb.ToString());
            return n;
        }

        private static bool IsPercent(RuneManager rm, Runes.Stat stat)
        {
            try
            {
                var list = rm.PercentageAttributes;
                return list != null && list.Contains(stat);
            }
            catch { return false; }
        }

        private static string F(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
