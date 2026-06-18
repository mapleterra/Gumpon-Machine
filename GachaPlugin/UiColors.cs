using System.Numerics;

namespace GachaPlugin;

public static class UiColors
{
    public static Vector4 Tier(PrizeTier tier) => tier switch
    {
        PrizeTier.Jackpot => new Vector4(0f, 1f, 1f, 1f),
        PrizeTier.Legendary => new Vector4(1f, 0.65f, 0f, 1f),
        PrizeTier.Epic => new Vector4(0.69f, 0f, 1f, 1f),
        PrizeTier.Rare => new Vector4(0f, 0.5f, 1f, 1f),
        PrizeTier.Uncommon => new Vector4(0f, 0.8f, 0f, 1f),
        PrizeTier.Common => new Vector4(0.67f, 0.67f, 0.67f, 1f),
        _ => new Vector4(1f, 1f, 1f, 1f)
    };
}
