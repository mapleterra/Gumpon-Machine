using System;
using System.Collections.Generic;

namespace GachaPlugin;

[Serializable]
public class PullRecord
{
    public long Timestamp { get; set; }
    public List<int> Rolls { get; set; } = new();
    public int Sum { get; set; }
    public PrizeTier Tier { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public int Bet { get; set; }
    public int Payout { get; set; }
    public string Player { get; set; } = string.Empty;

    public DateTimeOffset GetTime() => DateTimeOffset.FromUnixTimeSeconds(Timestamp).ToLocalTime();
}
