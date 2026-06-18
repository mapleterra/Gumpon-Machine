using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace GachaPlugin;

[Serializable]
public class PrizeConfig
{
    public PrizeTier Tier { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public int Payout { get; set; }
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    // Display toggles
    public bool ShowRollHistory { get; set; } = true;
    public bool AnnouncePrizeInChat { get; set; } = true;

    // Rules
    public int RollsNeeded { get; set; } = 3;
    public int DiceSize { get; set; } = 20;

    // Tier thresholds (percent of the maximum possible sum). Jackpot is always the exact max.
    public int LegendaryThresholdPct { get; set; } = 85;
    public int EpicThresholdPct { get; set; } = 60;
    public int RareThresholdPct { get; set; } = 35;
    public int UncommonThresholdPct { get; set; } = 18;

    // Custom prizes
    public List<PrizeConfig> Prizes { get; set; } = new();

    // Sound (chat sound effects 1-16; 0 = off for completion sound)
    public bool EnableSound { get; set; } = true;
    public int JackpotSoundId { get; set; } = 16;
    public int CompleteSoundId { get; set; } = 0;

    // Gil / betting
    public bool EnableGilTracking { get; set; } = false;
    public int DefaultBet { get; set; } = 0;

    // Party mode
    public bool PartyMode { get; set; } = false;

    // A "win" (for streaks) is any pull at or above this tier.
    public int WinStreakTier { get; set; } = (int)PrizeTier.Rare;

    // History
    public int MaxHistoryEntries { get; set; } = 500;
    public List<PullRecord> History { get; set; } = new();

    // Lifetime statistics
    public int TotalGachaPulls { get; set; } = 0;
    public int JackpotsHit { get; set; } = 0;
    public int BestSum { get; set; } = 0;
    public int CommonCount { get; set; }
    public int UncommonCount { get; set; }
    public int RareCount { get; set; }
    public int EpicCount { get; set; }
    public int LegendaryCount { get; set; }
    public int JackpotCount { get; set; }
    public int CurrentWinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public long TotalGilBet { get; set; }
    public long TotalGilWon { get; set; }

    public void EnsureDefaults()
    {
        Prizes ??= new List<PrizeConfig>();
        if (Prizes.Count == 0)
        {
            Prizes = GachaGame.DefaultPrizes();
        }
        else
        {
            foreach (var def in GachaGame.DefaultPrizes())
                if (!Prizes.Exists(p => p.Tier == def.Tier))
                    Prizes.Add(def);
        }

        History ??= new List<PullRecord>();

        // Migrate the legacy jackpot counter into the canonical one.
        if (JackpotCount == 0 && JackpotsHit > 0) JackpotCount = JackpotsHit;

        if (RollsNeeded < 1) RollsNeeded = 3;
        if (DiceSize < 2) DiceSize = 20;
        if (MaxHistoryEntries < 10) MaxHistoryEntries = 500;
    }

    public PrizeConfig GetPrize(PrizeTier tier)
    {
        var p = Prizes.Find(x => x.Tier == tier);
        if (p != null) return p;

        var def = GachaGame.DefaultPrizes().Find(x => x.Tier == tier)!;
        Prizes.Add(def);
        return def;
    }

    public void RecordTier(PrizeTier tier)
    {
        switch (tier)
        {
            case PrizeTier.Common: CommonCount++; break;
            case PrizeTier.Uncommon: UncommonCount++; break;
            case PrizeTier.Rare: RareCount++; break;
            case PrizeTier.Epic: EpicCount++; break;
            case PrizeTier.Legendary: LegendaryCount++; break;
            case PrizeTier.Jackpot: JackpotCount++; break;
        }
    }

    public int GetTierCount(PrizeTier tier) => tier switch
    {
        PrizeTier.Common => CommonCount,
        PrizeTier.Uncommon => UncommonCount,
        PrizeTier.Rare => RareCount,
        PrizeTier.Epic => EpicCount,
        PrizeTier.Legendary => LegendaryCount,
        PrizeTier.Jackpot => JackpotCount,
        _ => 0
    };

    public void ResetStatistics()
    {
        TotalGachaPulls = 0;
        JackpotsHit = 0;
        BestSum = 0;
        CommonCount = UncommonCount = RareCount = EpicCount = LegendaryCount = JackpotCount = 0;
        CurrentWinStreak = 0;
        BestWinStreak = 0;
        TotalGilBet = 0;
        TotalGilWon = 0;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
