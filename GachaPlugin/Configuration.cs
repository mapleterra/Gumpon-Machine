using Dalamud.Configuration;
using System;

namespace GachaPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool ShowRollHistory { get; set; } = true;
    public bool AnnouncePrizeInChat { get; set; } = false;
    public int TotalGachaPulls { get; set; } = 0;
    public int JackpotsHit { get; set; } = 0;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
