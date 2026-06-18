using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace GachaPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly GachaGame game;
    private readonly Configuration config;
    private readonly PartyTracker party;
    private readonly Action openConfig;

    private float animationTimer = 0f;
    private const float AnimSpeed = 2.5f;

    public MainWindow(GachaGame game, Configuration config, PartyTracker party, Action openConfig)
        : base("Gachapon Machine##GachaPlugin")
    {
        this.game = game;
        this.config = config;
        this.party = party;
        this.openConfig = openConfig;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 420),
            MaximumSize = new Vector2(640, 820)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        animationTimer += ImGui.GetIO().DeltaTime;

        DrawHeader();
        ImGui.Separator();

        if (ImGui.BeginTabBar("gachatabs"))
        {
            if (ImGui.BeginTabItem("Play")) { DrawPlayTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("History")) { DrawHistoryTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Stats")) { DrawStatsTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Party")) { DrawPartyTab(); ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }

        ImGui.Separator();
        DrawFooter();
    }

    private static void Center(string text)
    {
        var w = ImGui.GetWindowWidth();
        ImGui.SetCursorPosX((w - ImGui.CalcTextSize(text).X) / 2f);
    }

    private void DrawHeader()
    {
        Center("GACHAPON MACHINE");
        ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), "GACHAPON MACHINE");

        Center("~ Roll Your Fate ~");
        ImGui.TextDisabled("~ Roll Your Fate ~");
        ImGui.Spacing();
    }

    // ---------------- Play tab ----------------

    private void DrawPlayTab()
    {
        switch (game.State)
        {
            case GachaState.Idle: DrawIdleState(); break;
            case GachaState.WaitingForRolls: DrawWaitingState(); break;
            case GachaState.Complete: DrawResultState(); break;
        }
    }

    private void DrawIdleState()
    {
        var windowWidth = ImGui.GetWindowWidth();

        ImGui.Spacing();
        ImGui.Spacing();

        var machineText = "[ G A C H A P O N ]";
        Center(machineText);
        float pulse = (MathF.Sin(animationTimer * AnimSpeed) + 1f) / 2f;
        ImGui.TextColored(new Vector4(0.8f + pulse * 0.2f, 0.6f, 0.9f + pulse * 0.1f, 1f), machineText);

        ImGui.Spacing();
        ImGui.Spacing();

        var desc1 = "Insert a coin and try your luck!";
        Center(desc1);
        ImGui.TextWrapped(desc1);

        ImGui.Spacing();

        DrawPrizeGuide();

        ImGui.Spacing();
        ImGui.Spacing();

        var btnWidth = 200f;
        ImGui.SetCursorPosX((windowWidth - btnWidth) / 2f);
        if (ImGui.Button("Insert Coin & Start!", new Vector2(btnWidth, 35)))
            game.StartGame();

        ImGui.Spacing();
        var hint = $"Use /random {game.RollMax} {game.RollsNeeded} times to play!";
        Center(hint);
        ImGui.TextDisabled(hint);
    }

    private void DrawWaitingState()
    {
        var windowWidth = ImGui.GetWindowWidth();
        ImGui.Spacing();

        var statusText = $"Rolls remaining: {game.RollsRemaining} / {game.RollsNeeded}";
        Center(statusText);
        ImGui.TextColored(new Vector4(0.4f, 0.9f, 1f, 1f), statusText);

        ImGui.Spacing();
        ImGui.ProgressBar((float)game.Rolls.Count / game.RollsNeeded, new Vector2(-1, 20), "");
        ImGui.Spacing();

        if (config.ShowRollHistory && game.Rolls.Count > 0)
        {
            ImGui.Separator();
            ImGui.Spacing();

            var rollsLabel = "Rolls so far:";
            Center(rollsLabel);
            ImGui.TextDisabled(rollsLabel);
            ImGui.Spacing();

            for (int i = 0; i < game.Rolls.Count; i++)
            {
                var rollText = $"Roll {i + 1}:  {game.Rolls[i]:D2}  / {game.RollMax}";
                Center(rollText);
                ImGui.TextColored(new Vector4(1f, 0.9f, 0.4f, 1f), rollText);
            }

            if (game.Rolls.Count > 1)
            {
                ImGui.Spacing();
                var sumText = $"Current sum: {game.Sum}";
                Center(sumText);
                ImGui.Text(sumText);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var instructText = $"Type /random {game.RollMax} in chat to roll!";
        Center(instructText);
        float pulse = (MathF.Sin(animationTimer * AnimSpeed * 1.5f) + 1f) / 2f;
        ImGui.TextColored(new Vector4(0.5f + pulse * 0.5f, 1f, 0.5f + pulse * 0.5f, 1f), instructText);

        ImGui.Spacing();
        var cancelWidth = 160f;
        ImGui.SetCursorPosX((windowWidth - cancelWidth) / 2f);
        if (ImGui.Button("Cancel Game", new Vector2(cancelWidth, 28)))
            game.Reset();
    }

    private void DrawResultState()
    {
        if (game.CurrentPrize == null) return;

        var windowWidth = ImGui.GetWindowWidth();
        var prize = game.CurrentPrize;

        ImGui.Spacing();

        float pulse = (MathF.Sin(animationTimer * AnimSpeed * 2f) + 1f) / 2f;
        var baseColor = UiColors.Tier(prize.Tier);
        var prizeColor = new Vector4(baseColor.X, baseColor.Y, baseColor.Z, 0.85f + pulse * 0.15f);

        var tierLabel = $"[ {GachaGame.GetTierDisplayName(prize.Tier).ToUpper()} ]";
        Center(tierLabel);
        ImGui.TextColored(prizeColor, tierLabel);

        ImGui.Spacing();

        var emoji = prize.Emoji;
        if (!string.IsNullOrEmpty(emoji))
        {
            Center(emoji);
            ImGui.TextColored(prizeColor, emoji);
            ImGui.Spacing();
        }

        Center(prize.Name);
        ImGui.TextColored(new Vector4(1f, 1f, 1f, 1f), prize.Name);

        ImGui.Spacing();

        ImGui.PushTextWrapPos(windowWidth - 20f);
        ImGui.SetCursorPosX(10f);
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), prize.Description);
        ImGui.PopTextWrapPos();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var rollsLabel = "Your rolls:";
        Center(rollsLabel);
        ImGui.TextDisabled(rollsLabel);

        ImGui.Spacing();
        for (int i = 0; i < game.Rolls.Count; i++)
        {
            var rollText = $"Roll {i + 1}:  {game.Rolls[i]:D2}  / {game.RollMax}";
            Center(rollText);
            ImGui.TextColored(new Vector4(1f, 0.9f, 0.4f, 1f), rollText);
        }

        ImGui.Spacing();
        var sumText = $"Total:  {game.Sum}  / {game.MaxSum}";
        Center(sumText);
        ImGui.Text(sumText);

        ImGui.Spacing();
        ImGui.Spacing();

        var btnWidth = 200f;
        ImGui.SetCursorPosX((windowWidth - btnWidth) / 2f);
        if (ImGui.Button("Play Again!", new Vector2(btnWidth, 35)))
            game.StartGame();

        ImGui.Spacing();
        var resetWidth = 120f;
        ImGui.SetCursorPosX((windowWidth - resetWidth) / 2f);
        if (ImGui.Button("Close", new Vector2(resetWidth, 24)))
        {
            game.Reset();
            IsOpen = false;
        }
    }

    private void DrawPrizeGuide()
    {
        var guideLabel = $"Prize Guide (sum of {game.RollsNeeded} rolls, max {game.MaxSum}):";
        Center(guideLabel);
        ImGui.TextDisabled(guideLabel);
        ImGui.Spacing();

        int max = game.MaxSum;
        int leg = game.ThresholdSum(PrizeTier.Legendary);
        int epic = game.ThresholdSum(PrizeTier.Epic);
        int rare = game.ThresholdSum(PrizeTier.Rare);
        int unc = game.ThresholdSum(PrizeTier.Uncommon);
        int minSum = game.RollsNeeded;

        DrawPrizeRow($"{max}", GachaGame.GetTierDisplayName(PrizeTier.Jackpot), UiColors.Tier(PrizeTier.Jackpot));
        DrawPrizeRow(Range(leg, max - 1), GachaGame.GetTierDisplayName(PrizeTier.Legendary), UiColors.Tier(PrizeTier.Legendary));
        DrawPrizeRow(Range(epic, leg - 1), GachaGame.GetTierDisplayName(PrizeTier.Epic), UiColors.Tier(PrizeTier.Epic));
        DrawPrizeRow(Range(rare, epic - 1), GachaGame.GetTierDisplayName(PrizeTier.Rare), UiColors.Tier(PrizeTier.Rare));
        DrawPrizeRow(Range(unc, rare - 1), GachaGame.GetTierDisplayName(PrizeTier.Uncommon), UiColors.Tier(PrizeTier.Uncommon));
        DrawPrizeRow(Range(minSum, unc - 1), GachaGame.GetTierDisplayName(PrizeTier.Common), UiColors.Tier(PrizeTier.Common));
    }

    private static string Range(int a, int b) => a >= b ? $"{a}" : $"{a} - {b}";

    private static void DrawPrizeRow(string range, string label, Vector4 color)
    {
        var text = $"{range,7}  =>  {label}";
        Center(text);
        ImGui.TextColored(color, text);
    }

    // ---------------- History tab ----------------

    private void DrawHistoryTab()
    {
        var h = config.History;
        if (h.Count == 0)
        {
            ImGui.TextDisabled("No pulls recorded yet. Play to build history!");
            return;
        }

        if (ImGui.Button("Clear History"))
        {
            h.Clear();
            config.Save();
            return;
        }
        ImGui.SameLine();
        ImGui.TextDisabled($"{h.Count} recorded");
        ImGui.Separator();

        bool gil = config.EnableGilTracking;
        using var child = ImRaii.Child("histchild", new Vector2(0, 0), false);
        if (child.Success && ImGui.BeginTable("histtable", gil ? 5 : 4,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("When");
            ImGui.TableSetupColumn("Rolls");
            ImGui.TableSetupColumn("Sum");
            ImGui.TableSetupColumn("Prize");
            if (gil) ImGui.TableSetupColumn("Net");
            ImGui.TableHeadersRow();

            for (int i = h.Count - 1; i >= 0; i--)
            {
                var r = h[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.Text(r.GetTime().ToString("MM/dd HH:mm"));
                ImGui.TableNextColumn(); ImGui.Text(string.Join(" ", r.Rolls));
                ImGui.TableNextColumn(); ImGui.Text(r.Sum.ToString());
                ImGui.TableNextColumn(); ImGui.TextColored(UiColors.Tier(r.Tier), GachaGame.GetTierDisplayName(r.Tier));
                if (gil) { ImGui.TableNextColumn(); ImGui.Text((r.Payout - r.Bet).ToString("N0")); }
            }

            ImGui.EndTable();
        }
    }

    // ---------------- Stats tab ----------------

    private void DrawStatsTab()
    {
        ImGui.Text($"Total pulls: {config.TotalGachaPulls}");
        ImGui.Text($"Best sum: {config.BestSum} / {game.MaxSum}");
        ImGui.Text($"Jackpots hit: {config.JackpotCount}");
        ImGui.Text($"Current win streak: {config.CurrentWinStreak}");
        ImGui.Text($"Best win streak: {config.BestWinStreak}");

        var avg = Statistics.AverageSum(config.History);
        ImGui.Text($"Average sum (recorded): {avg:0.0}");

        var luck = Statistics.LuckiestDay(config.History);
        if (luck != null)
            ImGui.Text($"Luckiest day: {luck.Value.Date:MM/dd/yyyy} (total {luck.Value.TotalSum} over {luck.Value.Pulls} pulls)");

        ImGui.Separator();
        ImGui.Text("Prizes won by tier:");
        foreach (var t in new[] { PrizeTier.Jackpot, PrizeTier.Legendary, PrizeTier.Epic, PrizeTier.Rare, PrizeTier.Uncommon, PrizeTier.Common })
            ImGui.TextColored(UiColors.Tier(t), $"   {GachaGame.GetTierDisplayName(t)}: {config.GetTierCount(t)}");

        if (config.EnableGilTracking)
        {
            ImGui.Separator();
            ImGui.Text($"Total gil bet: {config.TotalGilBet:N0}");
            ImGui.Text($"Total gil won: {config.TotalGilWon:N0}");
            long net = config.TotalGilWon - config.TotalGilBet;
            ImGui.TextColored(net >= 0 ? new Vector4(0.4f, 1f, 0.4f, 1f) : new Vector4(1f, 0.4f, 0.4f, 1f), $"Net: {net:N0}");
        }

        ImGui.Separator();
        if (ImGui.Button("Reset Statistics"))
        {
            config.ResetStatistics();
            config.Save();
        }
    }

    // ---------------- Party tab ----------------

    private void DrawPartyTab()
    {
        if (!config.PartyMode)
        {
            ImGui.TextWrapped("Party mode is OFF. Turn it on in Settings to track every player's /random rolls and build a group leaderboard.");
            ImGui.Spacing();
            if (ImGui.Button("Open Settings"))
                openConfig();
            return;
        }

        ImGui.TextWrapped($"Listening for everyone's /random {game.RollMax} rolls. Each player needs {game.RollsNeeded} rolls to complete a pull.");
        ImGui.Separator();

        if (party.Active.Count > 0)
        {
            ImGui.Text("In progress:");
            foreach (var kv in party.Active)
                ImGui.TextDisabled($"   {kv.Key}: {kv.Value.Count}/{game.RollsNeeded}  ({string.Join(" ", kv.Value)})");
            ImGui.Separator();
        }

        if (ImGui.Button("Clear Party Results"))
        {
            party.ClearAll();
            return;
        }
        ImGui.SameLine();
        if (ImGui.Button("Sort by Score"))
            party.Results.Sort((a, b) => b.Sum.CompareTo(a.Sum));

        ImGui.Spacing();

        var res = party.Results;
        if (res.Count == 0)
        {
            ImGui.TextDisabled("No completed party rolls yet.");
            return;
        }

        using var child = ImRaii.Child("partychild", new Vector2(0, 0), false);
        if (child.Success && ImGui.BeginTable("partytable", 4,
                ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Player");
            ImGui.TableSetupColumn("Rolls");
            ImGui.TableSetupColumn("Sum");
            ImGui.TableSetupColumn("Prize");
            ImGui.TableHeadersRow();

            foreach (var r in res)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.Text(r.Player);
                ImGui.TableNextColumn(); ImGui.Text(string.Join(" ", r.Rolls));
                ImGui.TableNextColumn(); ImGui.Text(r.Sum.ToString());
                ImGui.TableNextColumn(); ImGui.TextColored(UiColors.Tier(r.Tier), GachaGame.GetTierDisplayName(r.Tier));
            }

            ImGui.EndTable();
        }
    }

    private void DrawFooter()
    {
        ImGui.TextDisabled($"Total Pulls: {config.TotalGachaPulls}   Jackpots: {config.JackpotCount}");
        ImGui.SameLine();

        float btnW = 90f;
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - btnW - 8f);
        if (ImGui.Button("Settings", new Vector2(btnW, 0)))
            openConfig();
    }
}
