using System;
using System.Collections.Generic;

namespace GachaPlugin;

public class PartyResult
{
    public string Player { get; set; } = string.Empty;
    public List<int> Rolls { get; set; } = new();
    public int Sum { get; set; }
    public PrizeTier Tier { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public long Timestamp { get; set; }

    public DateTimeOffset GetTime() => DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToLocalTime();
}

public class PartyTracker
{
    private readonly Dictionary<string, List<int>> active = new(StringComparer.OrdinalIgnoreCase);

    public List<PartyResult> Results { get; } = new();

    public IReadOnlyDictionary<string, List<int>> Active => active;

    public List<int>? RegisterRoll(string player, int roll, int rollsNeeded)
    {
        if (string.IsNullOrEmpty(player)) return null;

        if (!active.TryGetValue(player, out var list))
        {
            list = new List<int>();
            active[player] = list;
        }

        list.Add(roll);

        if (list.Count >= rollsNeeded)
        {
            active.Remove(player);
            return list;
        }

        return null;
    }

    public void ClearActive() => active.Clear();

    public void ClearAll()
    {
        active.Clear();
        Results.Clear();
    }
}
