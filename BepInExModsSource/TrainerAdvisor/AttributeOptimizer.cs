using System;
using System.Collections.Generic;

namespace DimraethTrainerAdvisor
{
    public enum DamageFocus
    {
        Physical,
        Magic,
        Hybrid
    }

    // Snapshot of the local player state that the recommendation is computed from.
    // All values are plain managed copies; nothing here writes back to the game.
    public sealed class AdvisorState
    {
        public int Race;              // Race enum value (Human=1, Elf=2, Minotaur=3)
        public int Class;             // Class enum value (Magician=1, Brawler=4, Shadow=5)
        public int Level;             // current player level
        public int AccumulatedXp;     // XP bar progress toward the next level
        public int AllTimeXp;         // lifetime XP spent (drives level, used for the cap budget)
        public int[] Attributes;      // current attributes, order [Mem,Cha,Adv,Phy,Int,Agi,Str,Ene]

        public AdvisorState Clone()
        {
            return new AdvisorState
            {
                Race = Race,
                Class = Class,
                Level = Level,
                AccumulatedXp = AccumulatedXp,
                AllTimeXp = AllTimeXp,
                Attributes = (int[])Attributes.Clone()
            };
        }
    }

    // One recommended final build: the points to add from the current state, the
    // resulting spread, the projected combat power, and the simulated XP/level outcome.
    public sealed class BuildRecommendation
    {
        public DamageFocus Focus;
        public int[] AddedPoints;      // additional points per attribute (8 entries)
        public int[] FinalAttributes;  // resulting attribute values (8 entries)
        public double CombatPower;     // CP for the focus damage type
        public double SecondaryPower;  // CP for the other damage type at this spread
        public double SpentXp;         // simulated XP spent to reach the level cap
        public int EndLevel;           // simulated level after spending
        public int PointsBought;       // total additional points purchased
    }

    // Port of the verified calculator engine from docs/Dimraeth_Attribute_Calculator.html,
    // generalised so the optimisation can start from the player's *current* attributes,
    // level and XP bar rather than a fresh level-1 character. The exact optimum is found
    // with a branch-and-bound search seeded by a greedy lower bound; on the reference
    // archetypes this reproduces every value in docs/Dimraeth_Optimal_Attribute_Builds.md.
    public static class AttributeOptimizer
    {
        public const int AttrCap = 99;
        public const int LevelCap = 25;

        // Highest number of additional points worth searching. The level-25 budget only
        // ever affords ~44-49 points, so 56 is a safe, tight ceiling.
        private const int MaxAdditionalPoints = 56;

        private static readonly string[] AttrNames =
            { "Mem", "Cha", "Adv", "Phy", "Int", "Agi", "Str", "Ene" };

        // Race/class attribute bonuses and per-point base costs, order [Mem,Cha,Adv,Phy,Int,Agi,Str,Ene].
        // Reverse-engineered from the serialized RaceDefinition / ClassDefinition assets; these are the
        // same tables the web calculator is verified against. Only the three playable races and three
        // playable classes exist, so an unsupported combo yields no recommendation.
        private static readonly Dictionary<int, int[]> RaceBonus = new Dictionary<int, int[]>
        {
            { 1, new[] { 3, 3, 3, 3, 3, 3, 3, 3 } }, // Human
            { 2, new[] { 3, 3, 3, 1, 3, 5, 3, 3 } }, // Elf
            { 3, new[] { 2, 2, 3, 4, 1, 3, 5, 4 } }, // Minotaur
        };

        private static readonly Dictionary<int, int[]> RaceCost = new Dictionary<int, int[]>
        {
            { 1, new[] { 200, 200, 200, 200, 200, 200, 200, 200 } }, // Human
            { 2, new[] { 200, 240, 200, 280, 160, 120, 240, 160 } }, // Elf
            { 3, new[] { 240, 240, 200, 160, 200, 280, 120, 160 } }, // Minotaur
        };

        private static readonly Dictionary<int, int[]> ClassBonus = new Dictionary<int, int[]>
        {
            { 1, new[] { 4, 4, 3, 2, 5, 2, 1, 3 } }, // Magician
            { 4, new[] { 2, 2, 3, 4, 1, 3, 5, 4 } }, // Brawler
            { 5, new[] { 3, 3, 3, 1, 3, 5, 3, 3 } }, // Shadow
        };

        private static readonly Dictionary<int, int[]> ClassCost = new Dictionary<int, int[]>
        {
            { 1, new[] { 160, 160, 200, 240, 120, 240, 280, 200 } }, // Magician
            { 4, new[] { 240, 240, 200, 160, 280, 200, 120, 160 } }, // Brawler
            { 5, new[] { 160, 240, 200, 280, 200, 120, 240, 160 } }, // Shadow
        };

        // Total lifetime XP required to reach the level cap from level 1.
        public static readonly int XpToLevel25 = TotalXpToLevel(LevelCap);

        private static readonly Dictionary<DamageFocus, int[]> Candidates = new Dictionary<DamageFocus, int[]>
        {
            { DamageFocus.Physical, new[] { 6, 5, 2 } },        // Str, Agi, Adv  (coefficients 1.0, 0.5, 0.5)
            { DamageFocus.Magic,    new[] { 4, 1, 2 } },        // Int, Cha, Adv
            { DamageFocus.Hybrid,   new[] { 2, 6, 4, 1, 5 } },  // Adv, Str, Int, Cha, Agi (1.0 first, then 0.5)
        };

        public static string AttributeName(int index) => AttrNames[index];

        public static int XpRequiredForLevel(int level) =>
            RoundHalfEven(100.0 * Math.Pow(level, 1.5) + 150.0);

        private static int TotalXpToLevel(int cap)
        {
            int sum = 0;
            for (int level = 1; level < cap; level++) sum += XpRequiredForLevel(level);
            return sum;
        }

        public static double PhysicalCombatPower(int[] attributes) =>
            attributes[6] + 0.5 * (attributes[5] + attributes[2]);

        public static double MagicCombatPower(int[] attributes) =>
            attributes[4] + 0.5 * (attributes[1] + attributes[2]);

        // XP still required to reach the level cap from the player's current level and XP bar.
        // Derived from level/progress rather than lifetime XP, so it stays correct after a respec.
        public static int RemainingXpToCap(AdvisorState state)
        {
            if (state.Level >= LevelCap) return 0;
            int sum = -state.AccumulatedXp;
            for (int level = Math.Max(1, state.Level); level < LevelCap; level++)
                sum += XpRequiredForLevel(level);
            return Math.Max(0, sum);
        }

        // Lifetime XP implied by the player's current level and XP bar. Spending XP on an
        // attribute always advances both AllTimeXP and the level progress, so these should
        // match; a mismatch means the save was edited or otherwise inconsistent.
        public static int ExpectedLifetimeXp(AdvisorState state)
        {
            int sum = state.AccumulatedXp;
            for (int level = 1; level < state.Level && level < LevelCap; level++)
                sum += XpRequiredForLevel(level);
            return sum;
        }

        public static bool SupportsBuild(int race, int cls) =>
            RaceBonus.ContainsKey(race) && ClassBonus.ContainsKey(cls);

        public static BuildRecommendation Recommend(AdvisorState state, DamageFocus focus)
        {
            if (state == null || state.Attributes == null || state.Attributes.Length != 8)
                return null;
            if (!SupportsBuild(state.Race, state.Class))
                return null;

            int[] baseStart = StartingAttributes(state.Race, state.Class);
            int[] current = state.Attributes;
            int level = Math.Max(1, state.Level);
            int accum = Math.Max(0, state.AccumulatedXp);

            return Optimize(state.Race, state.Class, baseStart, current, level, accum, focus);
        }

        private static int[] StartingAttributes(int race, int cls)
        {
            int[] rb = RaceBonus[race];
            int[] cb = ClassBonus[cls];
            int[] start = new int[8];
            for (int i = 0; i < 8; i++) start[i] = rb[i] + cb[i];
            return start;
        }

        private static int BaseCost(int race, int cls, int attribute)
        {
            int rv = RaceCost[race][attribute] > 0 ? RaceCost[race][attribute] : 200;
            int cv = ClassCost[cls][attribute] > 0 ? ClassCost[cls][attribute] : 200;
            return (int)Math.Round(0.5 * (rv + cv));
        }

        // Per-attribute cost table: table[i][p] is the XP cost of the p-th point above the
        // attribute's starting value, *excluding* the +100*level term (added during the
        // simulation, since the level rises as points are bought).
        private static double[][] BuildCostTables(int race, int cls)
        {
            var tables = new double[8][];
            for (int i = 0; i < 8; i++)
            {
                double baseCost = BaseCost(race, cls, i);
                tables[i] = new double[AttrCap + 2];
                for (int p = 1; p <= AttrCap + 1; p++)
                    tables[i][p] = baseCost * Math.Pow(1.0 + (p - 1) / 3.0, 1.1);
            }
            return tables;
        }

        private static double[] Coefficients(DamageFocus focus)
        {
            var c = new double[8];
            switch (focus)
            {
                case DamageFocus.Physical:
                    c[6] = 1.0; c[5] = 0.5; c[2] = 0.5;
                    break;
                case DamageFocus.Magic:
                    c[4] = 1.0; c[1] = 0.5; c[2] = 0.5;
                    break;
                default: // Hybrid — maximise physical + magic together.
                    c[2] = 1.0; c[6] = 1.0; c[4] = 1.0; c[1] = 0.5; c[5] = 0.5;
                    break;
            }
            return c;
        }

        private sealed class Context
        {
            public double[][] Tables;
            public double[] Requirements; // Requirements[L] = XP needed to go from L to L+1
            public int[] BaseStart;
            public double StartProgress;
            public int StartLevel;
            public int LevelCap;
        }

        private sealed class Simulation
        {
            public int[] Bought;
            public double Spent;
            public double Progress;
            public int Level;
            public List<int> Order;
        }

        private static BuildRecommendation Optimize(
            int race, int cls, int[] baseStart, int[] current, int startLevel, int accum, DamageFocus focus)
        {
            var coeff = Coefficients(focus);
            int[] cand = Candidates[focus];
            int n = cand.Length;

            var ctx = new Context
            {
                Tables = BuildCostTables(race, cls),
                Requirements = BuildRequirements(),
                BaseStart = baseStart,
                StartProgress = accum,
                StartLevel = startLevel,
                LevelCap = LevelCap,
            };

            int[] initBought = new int[8];
            for (int i = 0; i < 8; i++)
                initBought[i] = Math.Max(0, current[i] - baseStart[i]);

            double startValue = 0;
            for (int i = 0; i < 8; i++) startValue += coeff[i] * current[i];

            // suffixMax[k] = best coefficient still assignable from candidate depth k onward.
            var suffixMax = new double[n + 1];
            for (int k = n - 1; k >= 0; k--)
                suffixMax[k] = Math.Max(suffixMax[k + 1], coeff[cand[k]]);

            var counts = new int[8];
            var lim = new int[8];
            var ptr = new int[8];
            var taken = new int[8];

            double EvalValue()
            {
                int level = ctx.StartLevel;
                double progress = ctx.StartProgress;
                for (int i = 0; i < 8; i++)
                    lim[i] = Math.Min(initBought[i] + counts[i], AttrCap - ctx.BaseStart[i]);
                for (int i = 0; i < 8; i++) { ptr[i] = initBought[i]; taken[i] = 0; }

                while (true)
                {
                    int best = -1;
                    double bestCost = double.PositiveInfinity;
                    for (int i = 0; i < 8; i++)
                    {
                        if (ptr[i] < lim[i])
                        {
                            double cost = ctx.Tables[i][ptr[i] + 1];
                            if (cost < bestCost) { bestCost = cost; best = i; }
                        }
                    }
                    if (best < 0 || level >= ctx.LevelCap) break;

                    double actual = bestCost + 100.0 * level;
                    progress += actual;
                    taken[best]++;
                    ptr[best]++;
                    while (level < ctx.LevelCap && progress >= ctx.Requirements[level])
                    {
                        progress -= ctx.Requirements[level];
                        level++;
                    }
                }

                double value = startValue;
                for (int i = 0; i < 8; i++) value += coeff[i] * taken[i];
                return value;
            }

            // Greedy lower bound: repeatedly buy the candidate with the best value/cost ratio.
            double GreedyValue()
            {
                var bought = (int[])initBought.Clone();
                int level = ctx.StartLevel;
                double progress = ctx.StartProgress;
                double value = startValue;
                for (int guard = 0; guard < 400 && level < ctx.LevelCap; guard++)
                {
                    int best = -1;
                    double bestRatio = 0, bestActual = 0;
                    foreach (int i in cand)
                    {
                        if (coeff[i] <= 0) continue;
                        double next = ctx.Tables[i][bought[i] + 1];
                        if (next <= 0) continue;
                        double actual = next + 100.0 * level;
                        double ratio = coeff[i] / actual;
                        if (ratio > bestRatio) { bestRatio = ratio; best = i; bestActual = actual; }
                    }
                    if (best < 0) break;
                    progress += bestActual;
                    bought[best]++;
                    value += coeff[best];
                    while (level < ctx.LevelCap && progress >= ctx.Requirements[level])
                    {
                        progress -= ctx.Requirements[level];
                        level++;
                    }
                }
                return value;
            }

            double bestValue = GreedyValue();
            int[] bestRequested = null;

            void Search(int k, int remaining, double partialValue)
            {
                if (startValue + partialValue + remaining * suffixMax[k] < bestValue - 1e-9) return;
                if (k == n)
                {
                    double cp = EvalValue();
                    if (cp > bestValue + 1e-9 || (bestRequested == null && cp >= bestValue - 1e-9))
                    {
                        if (cp > bestValue + 1e-9) bestValue = cp;
                        bestRequested = (int[])counts.Clone();
                    }
                    return;
                }
                int i = cand[k];
                double w = coeff[i];
                for (int c = 0; c <= remaining; c++)
                {
                    counts[i] = c;
                    Search(k + 1, remaining - c, partialValue + w * c);
                }
                counts[i] = 0;
            }

            Search(0, MaxAdditionalPoints, 0);
            if (bestRequested == null) bestRequested = (int[])initBought.Clone();

            Simulation sim = Simulate(ctx, bestRequested, cand, initBought);

            var added = new int[8];
            var final = new int[8];
            int total = 0;
            for (int i = 0; i < 8; i++)
            {
                added[i] = Math.Max(0, sim.Bought[i] - initBought[i]);
                final[i] = baseStart[i] + sim.Bought[i];
                total += added[i];
            }

            return new BuildRecommendation
            {
                Focus = focus,
                AddedPoints = added,
                FinalAttributes = final,
                CombatPower = ValueFor(final, coeff),
                SecondaryPower = focus == DamageFocus.Physical ? MagicCombatPower(final)
                    : focus == DamageFocus.Magic ? PhysicalCombatPower(final)
                    : PhysicalCombatPower(final) + MagicCombatPower(final),
                SpentXp = sim.Spent,
                EndLevel = sim.Level,
                PointsBought = total,
            };
        }

        private static Simulation Simulate(Context ctx, int[] requested, int[] cand, int[] initBought)
        {
            var lim = new int[8];
            var ptr = new int[8];
            foreach (int i in cand)
                lim[i] = Math.Min(initBought[i] + requested[i], AttrCap - ctx.BaseStart[i]);

            var sim = new Simulation { Bought = new int[8], Order = new List<int>() };
            for (int i = 0; i < 8; i++) ptr[i] = initBought[i];

            int level = ctx.StartLevel;
            double progress = ctx.StartProgress;

            while (true)
            {
                int best = -1;
                double bestCost = double.PositiveInfinity;
                for (int i = 0; i < 8; i++)
                {
                    if (ptr[i] < lim[i])
                    {
                        double cost = ctx.Tables[i][ptr[i] + 1];
                        if (cost < bestCost) { bestCost = cost; best = i; }
                    }
                }
                if (best < 0 || level >= ctx.LevelCap) break;

                double actual = bestCost + 100.0 * level;
                progress += actual;
                sim.Spent += actual;
                sim.Bought[best]++;
                ptr[best]++;
                sim.Order.Add(best);
                while (level < ctx.LevelCap && progress >= ctx.Requirements[level])
                {
                    progress -= ctx.Requirements[level];
                    level++;
                }
            }

            sim.Level = level;
            sim.Progress = progress;
            return sim;
        }

        private static double[] BuildRequirements()
        {
            var req = new double[LevelCap + 2];
            for (int level = 1; level <= LevelCap; level++) req[level] = XpRequiredForLevel(level);
            return req;
        }

        private static double ValueFor(int[] attributes, double[] coeff)
        {
            double value = 0;
            for (int i = 0; i < 8; i++) value += coeff[i] * attributes[i];
            return value;
        }

        private static int RoundHalfEven(double value)
        {
            double floor = Math.Floor(value);
            double frac = value - floor;
            if (frac > 0.5) return (int)floor + 1;
            if (frac < 0.5) return (int)floor;
            return ((long)floor % 2 == 0) ? (int)floor : (int)floor + 1;
        }
    }
}
