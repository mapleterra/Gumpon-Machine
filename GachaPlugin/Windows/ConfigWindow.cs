using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GachaPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly GachaGame game;
    private readonly Configuration config;

    public ConfigWindow(GachaGame game, Configuration config)
        : base("Gachapon Settings###GachaPluginConfig")
    {
        this.game = game;
        this.config = config;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 360),
            MaximumSize = new Vector2(700, 900)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.CollapsingHeader("Game Rules", ImGuiTreeNodeFlags.DefaultOpen)) DrawRules();
        if (ImGui.CollapsingHeader("Tier Thresholds")) DrawThresholds();
        if (ImGui.CollapsingHeader("Custom Prizes")) DrawPrizes();
        if (ImGui.CollapsingHeader("Sound")) DrawSound();
        if (ImGui.CollapsingHeader("Gil / Betting")) DrawGil();
        if (ImGui.CollapsingHeader("Party Mode")) DrawParty();
        if (ImGui.CollapsingHeader("Display & History")) DrawDisplay();
    }

    private void DrawRules()
    {
        int rolls = config.RollsNeeded;
        if (ImGui.InputInt("Rolls per play", ref rolls))
        {
            config.RollsNeeded = Math.Clamp(rolls, 1, 10);
            config.Save();
        }

        int dice = config.DiceSize;
        if (ImGui.InputInt("Dice size (the N in /random N)", ref dice))
        {
            config.DiceSize = Math.Clamp(dice, 2, 1000);
            config.Save();
        }

        ImGui.TextDisabled($"Maximum possible sum: {config.RollsNeeded * config.DiceSize}");
        ImGui.TextWrapped($"Players must type  /random {config.DiceSize}  exactly {config.RollsNeeded} time(s) to complete a pull.");
    }

    private void DrawThresholds()
    {
        int max = config.RollsNeeded * config.DiceSize;
        ImGui.TextWrapped("Thresholds are a percentage of the maximum sum. A Jackpot is always the exact maximum.");
        ImGui.Spacing();

        ThresholdSlider("Legendary (%)##leg", max, () => config.LegendaryThresholdPct, v => config.LegendaryThresholdPct = v);
        ThresholdSlider("Epic (%)##epic", max, () => config.EpicThresholdPct, v => config.EpicThresholdPct = v);
        ThresholdSlider("Rare (%)##rare", max, () => config.RareThresholdPct, v => config.RareThresholdPct = v);
        ThresholdSlider("Uncommon (%)##unc", max, () => config.UncommonThresholdPct, v => config.UncommonThresholdPct = v);

        ImGui.Spacing();
        ImGui.TextDisabled("Anything below the Uncommon threshold is Common.");
    }

    private void ThresholdSlider(string label, int max, Func<int> get, Action<int> set)
    {
        int v = get();
        if (ImGui.SliderInt(label, ref v, 1, 100))
        {
            set(Math.Clamp(v, 1, 100));
            config.Save();
        }
        int abs = (int)Math.Ceiling(get() / 100.0 * max);
        ImGui.Indent();
        ImGui.TextDisabled($"needs sum >= {abs}");
        ImGui.Unindent();
    }

    private void DrawPrizes()
    {
        if (ImGui.Button("Reset prizes to defaults"))
        {
            config.Prizes = GachaGame.DefaultPrizes();
            config.Save();
        }
        ImGui.Separator();

        foreach (var tier in new[] { PrizeTier.Jackpot, PrizeTier.Legendary, PrizeTier.Epic, PrizeTier.Rare, PrizeTier.Uncommon, PrizeTier.Common })
        {
            var pc = config.GetPrize(tier);
            ImGui.TextColored(UiColors.Tier(tier), GachaGame.GetTierDisplayName(tier));

            var name = pc.Name;
            if (ImGui.InputText($"Name##{tier}", ref name, 128)) { pc.Name = name; config.Save(); }

            var emoji = pc.Emoji;
            if (ImGui.InputText($"Symbol##{tier}", ref emoji, 16)) { pc.Emoji = emoji; config.Save(); }

            var desc = pc.Description;
            if (ImGui.InputTextMultiline($"Description##{tier}", ref desc, 512, new Vector2(-1, 50))) { pc.Description = desc; config.Save(); }

            if (config.EnableGilTracking)
            {
                int pay = pc.Payout;
                if (ImGui.InputInt($"Payout (gil)##{tier}", ref pay)) { pc.Payout = Math.Max(0, pay); config.Save(); }
            }

            ImGui.Separator();
        }
    }

    private void DrawSound()
    {
        bool en = config.EnableSound;
        if (ImGui.Checkbox("Enable sound effects", ref en)) { config.EnableSound = en; config.Save(); }

        int js = config.JackpotSoundId;
        if (ImGui.SliderInt("Jackpot sound (1-16)", ref js, 1, 16)) { config.JackpotSoundId = js; config.Save(); }
        ImGui.SameLine();
        if (ImGui.Button("Test##jp")) Plugin.PlaySoundEffect(config.JackpotSoundId);

        int cs = config.CompleteSoundId;
        if (ImGui.SliderInt("Completion sound (0 = off)", ref cs, 0, 16)) { config.CompleteSoundId = cs; config.Save(); }
        ImGui.SameLine();
        if (ImGui.Button("Test##cp")) Plugin.PlaySoundEffect(config.CompleteSoundId);

        ImGui.TextWrapped("The completion sound plays on every finished pull; the jackpot sound replaces it on a jackpot.");
    }

    private void DrawGil()
    {
        bool g = config.EnableGilTracking;
        if (ImGui.Checkbox("Enable gil / betting tracking", ref g)) { config.EnableGilTracking = g; config.Save(); }

        int bet = config.DefaultBet;
        if (ImGui.InputInt("Default bet per play (gil)", ref bet)) { config.DefaultBet = Math.Max(0, bet); config.Save(); }

        ImGui.TextWrapped("Set per-tier payouts in the Custom Prizes section. Totals appear in the Stats tab.");
    }

    private void DrawParty()
    {
        bool pm = config.PartyMode;
        if (ImGui.Checkbox("Enable party mode (track everyone's rolls)", ref pm)) { config.PartyMode = pm; config.Save(); }

        ImGui.TextWrapped("When on, the plugin records /random rolls from all nearby players and builds a leaderboard in the Party tab of the main window.");
    }

    private void DrawDisplay()
    {
        bool srh = config.ShowRollHistory;
        if (ImGui.Checkbox("Show rolls in the Play window", ref srh)) { config.ShowRollHistory = srh; config.Save(); }

        bool ann = config.AnnouncePrizeInChat;
        if (ImGui.Checkbox("Announce results in chat log (/echo)", ref ann)) { config.AnnouncePrizeInChat = ann; config.Save(); }

        int win = config.WinStreakTier;
        if (ImGui.SliderInt("Win-streak counts tiers at or above", ref win, 0, 5))
        {
            config.WinStreakTier = Math.Clamp(win, 0, 5);
            config.Save();
        }
        ImGui.Indent();
        ImGui.TextDisabled($"= {GachaGame.GetTierDisplayName((PrizeTier)config.WinStreakTier)} or better");
        ImGui.Unindent();

        int cap = config.MaxHistoryEntries;
        if (ImGui.InputInt("Max history entries", ref cap)) { config.MaxHistoryEntries = Math.Clamp(cap, 10, 5000); config.Save(); }
    }
}
