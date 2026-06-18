using System;
using System.Collections.Generic;
using System.Linq;

namespace GachaPlugin;

public static class Statistics
{
    public static double AverageSum(IReadOnlyList<PullRecord> history)
        => history.Count == 0 ? 0 : history.Average(r => r.Sum);

    public static (DateTime Date, int Pulls, int TotalSum)? LuckiestDay(IReadOnlyList<PullRecord> history)
    {
        if (history.Count == 0) return null;

        var best = history
            .GroupBy(r => r.GetTime().Date)
            .Select(g => (Date: g.Key, Pulls: g.Count(), TotalSum: g.Sum(x => x.Sum)))
            .OrderByDescending(g => g.TotalSum)
            .First();

        return best;
    }
}
