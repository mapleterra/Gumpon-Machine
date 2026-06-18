using System;
using System.Collections.Generic;

namespace GachaPlugin;

public enum GachaState
{
    Idle,
    WaitingForRolls,
    Complete
}

public enum PrizeTier
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Jackpot
}

public class GachaPrize
{
    public PrizeTier Tier { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Emoji { get; init; } = string.Empty;
    public uint Color { get; init; }
}

public class GachaGame
{
    private readonly Configuration config;

    public GachaGame(Configuration config)
    {
        this.config = config;
    }

    public int RollsNeeded => config.RollsNeeded;
    public int RollMax => config.DiceSize;
    public int MaxSum => RollsNeeded * RollMax;

    public GachaState State { get; private set; } = GachaState.Idle;
    public List<int> Rolls { get; private set; } = new();
    public GachaPrize? CurrentPrize { get; private set; }
    public int RollsRemaining => Math.Max(0, RollsNeeded - Rolls.Count);

    public int Sum
    {
        get
        {
            int s = 0;
            foreach (var r in Rolls) s += r;
            return s;
        }
    }

    public static List<PrizeConfig> DefaultPrizes() => new()
    {
        new PrizeConfig { Tier = PrizeTier.Jackpot, Name = "JACKPOT! Legendary Gachapon Capsule", Description = "The rarest of all prizes. You are incredibly lucky!", Emoji = "★" },
        new PrizeConfig { Tier = PrizeTier.Legendary, Name = "Legendary Gachapon Capsule", Description = "An extraordinary pull! Something legendary awaits inside.", Emoji = "◈" },
        new PrizeConfig { Tier = PrizeTier.Epic, Name = "Epic Gachapon Capsule", Description = "A powerful pull! A rare and coveted prize.", Emoji = "◆" },
        new PrizeConfig { Tier = PrizeTier.Rare, Name = "Rare Gachapon Capsule", Description = "Not bad! Something uncommon awaits.", Emoji = "●" },
        new PrizeConfig { Tier = PrizeTier.Uncommon, Name = "Uncommon Gachapon Capsule", Description = "A decent pull. Better luck next time!", Emoji = "◉" },
        new PrizeConfig { Tier = PrizeTier.Common, Name = "Common Gachapon Capsule", Description = "The most common of prizes. Keep trying!", Emoji = "○" },
    };

    public void StartGame()
    {
        Rolls.Clear();
        CurrentPrize = null;
        State = GachaState.WaitingForRolls;
    }

    public void Reset()
    {
        Rolls.Clear();
        CurrentPrize = null;
        State = GachaState.Idle;
    }

    public bool RegisterRoll(int value)
    {
        if (State != GachaState.WaitingForRolls) return false;
        if (value < 1 || value > RollMax) return false;

        Rolls.Add(value);

        if (Rolls.Count >= RollsNeeded)
        {
            CurrentPrize = GetPrizeForSum(Sum);
            State = GachaState.Complete;
        }

        return true;
    }

    private static int Cut(int pct, int max) => (int)Math.Ceiling(pct / 100.0 * max);

    public PrizeTier GetTierForSum(int sum)
    {
        int max = MaxSum;
        if (sum >= max) return PrizeTier.Jackpot;
        if (sum >= Cut(config.LegendaryThresholdPct, max)) return PrizeTier.Legendary;
        if (sum >= Cut(config.EpicThresholdPct, max)) return PrizeTier.Epic;
        if (sum >= Cut(config.RareThresholdPct, max)) return PrizeTier.Rare;
        if (sum >= Cut(config.UncommonThresholdPct, max)) return PrizeTier.Uncommon;
        return PrizeTier.Common;
    }

    public GachaPrize GetPrizeForSum(int sum)
    {
        var tier = GetTierForSum(sum);
        var pc = config.GetPrize(tier);
        return new GachaPrize
        {
            Tier = tier,
            Name = pc.Name,
            Description = pc.Description,
            Emoji = pc.Emoji,
            Color = 0xFFFFFFFF
        };
    }

    public int ThresholdSum(PrizeTier tier) => tier switch
    {
        PrizeTier.Jackpot => MaxSum,
        PrizeTier.Legendary => Cut(config.LegendaryThresholdPct, MaxSum),
        PrizeTier.Epic => Cut(config.EpicThresholdPct, MaxSum),
        PrizeTier.Rare => Cut(config.RareThresholdPct, MaxSum),
        PrizeTier.Uncommon => Cut(config.UncommonThresholdPct, MaxSum),
        PrizeTier.Common => RollsNeeded,
        _ => 0
    };

    public static string GetTierDisplayName(PrizeTier tier) => tier switch
    {
        PrizeTier.Jackpot => "JACKPOT",
        PrizeTier.Legendary => "Legendary",
        PrizeTier.Epic => "Epic",
        PrizeTier.Rare => "Rare",
        PrizeTier.Uncommon => "Uncommon",
        PrizeTier.Common => "Common",
        _ => "Unknown"
    };
}
