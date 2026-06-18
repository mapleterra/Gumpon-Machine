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
    public const int RollsNeeded = 3;
    public const int RollMax = 20;

    public GachaState State { get; private set; } = GachaState.Idle;
    public List<int> Rolls { get; private set; } = new();
    public GachaPrize? CurrentPrize { get; private set; }
    public int RollsRemaining => RollsNeeded - Rolls.Count;
    public int Sum => Rolls.Count > 0 ? Rolls[0] + (Rolls.Count > 1 ? Rolls[1] : 0) + (Rolls.Count > 2 ? Rolls[2] : 0) : 0;

    private static readonly List<GachaPrize> PrizeTable = new()
    {
        new GachaPrize
        {
            Tier = PrizeTier.Jackpot,
            Name = "JACKPOT! Legendary Gachapon Capsule",
            Description = "The rarest of all prizes. You are incredibly lucky!",
            Emoji = "★",
            Color = 0xFF00FFFF
        },
        new GachaPrize
        {
            Tier = PrizeTier.Legendary,
            Name = "Legendary Gachapon Capsule",
            Description = "An extraordinary pull! Something legendary awaits inside.",
            Emoji = "◈",
            Color = 0xFF00A5FF
        },
        new GachaPrize
        {
            Tier = PrizeTier.Epic,
            Name = "Epic Gachapon Capsule",
            Description = "A powerful pull! A rare and coveted prize.",
            Emoji = "◆",
            Color = 0xFFB000FF
        },
        new GachaPrize
        {
            Tier = PrizeTier.Rare,
            Name = "Rare Gachapon Capsule",
            Description = "Not bad! Something uncommon awaits.",
            Emoji = "●",
            Color = 0xFFFF7F00
        },
        new GachaPrize
        {
            Tier = PrizeTier.Uncommon,
            Name = "Uncommon Gachapon Capsule",
            Description = "A decent pull. Better luck next time!",
            Emoji = "◉",
            Color = 0xFF00FF00
        },
        new GachaPrize
        {
            Tier = PrizeTier.Common,
            Name = "Common Gachapon Capsule",
            Description = "The most common of prizes. Keep trying!",
            Emoji = "○",
            Color = 0xFFAAAAAA
        },
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
            CurrentPrize = DeterminePrize(Sum);
            State = GachaState.Complete;
        }

        return true;
    }

    private static GachaPrize DeterminePrize(int sum)
    {
        return sum switch
        {
            60 => PrizeTable[0],
            >= 51 => PrizeTable[1],
            >= 36 => PrizeTable[2],
            >= 21 => PrizeTable[3],
            >= 11 => PrizeTable[4],
            _ => PrizeTable[5]
        };
    }

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
