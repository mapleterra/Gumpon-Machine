using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace GachaPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly GachaGame game;
    private readonly Configuration config;

    private float animationTimer = 0f;
    private const float AnimSpeed = 2.5f;

    public MainWindow(GachaGame game, Configuration config)
        : base("Gachapon Machine##GachaPlugin", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.game = game;
        this.config = config;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(340, 400),
            MaximumSize = new Vector2(500, 600)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        animationTimer += ImGui.GetIO().DeltaTime;

        DrawHeader();
        ImGui.Separator();

        switch (game.State)
        {
            case GachaState.Idle:
                DrawIdleState();
                break;
            case GachaState.WaitingForRolls:
                DrawWaitingState();
                break;
            case GachaState.Complete:
                DrawResultState();
                break;
        }

        ImGui.Separator();
        DrawFooter();
    }

    private void DrawHeader()
    {
        var windowWidth = ImGui.GetWindowWidth();

        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize("GACHAPON MACHINE").X) / 2f);
        ImGui.TextColored(new Vector4(1f, 0.84f, 0f, 1f), "GACHAPON MACHINE");

        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize("~ Roll Your Fate ~").X) / 2f);
        ImGui.TextDisabled("~ Roll Your Fate ~");
        ImGui.Spacing();
    }

    private void DrawIdleState()
    {
        var windowWidth = ImGui.GetWindowWidth();

        ImGui.Spacing();
        ImGui.Spacing();

        var machineText = "[ G A C H A P O N ]";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(machineText).X) / 2f);
        float pulse = (MathF.Sin(animationTimer * AnimSpeed) + 1f) / 2f;
        ImGui.TextColored(new Vector4(0.8f + pulse * 0.2f, 0.6f, 0.9f + pulse * 0.1f, 1f), machineText);

        ImGui.Spacing();
        ImGui.Spacing();

        var desc1 = "Insert a coin and try your luck!";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(desc1).X) / 2f);
        ImGui.TextWrapped(desc1);

        ImGui.Spacing();

        DrawPrizeGuide();

        ImGui.Spacing();
        ImGui.Spacing();

        var btnWidth = 200f;
        ImGui.SetCursorPosX((windowWidth - btnWidth) / 2f);
        if (ImGui.Button("Insert Coin & Start!", new Vector2(btnWidth, 35)))
        {
            game.StartGame();
        }

        ImGui.Spacing();
        var hint = "Use /random 20 three times to play!";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(hint).X) / 2f);
        ImGui.TextDisabled(hint);
    }

    private void DrawWaitingState()
    {
        var windowWidth = ImGui.GetWindowWidth();
        ImGui.Spacing();

        var statusText = $"Rolls remaining: {game.RollsRemaining} / {GachaGame.RollsNeeded}";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(statusText).X) / 2f);
        ImGui.TextColored(new Vector4(0.4f, 0.9f, 1f, 1f), statusText);

        ImGui.Spacing();
        ImGui.ProgressBar(
            (float)game.Rolls.Count / GachaGame.RollsNeeded,
            new Vector2(-1, 20),
            ""
        );
        ImGui.Spacing();

        if (game.Rolls.Count > 0)
        {
            ImGui.Separator();
            ImGui.Spacing();

            var rollsLabel = "Rolls so far:";
            ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(rollsLabel).X) / 2f);
            ImGui.TextDisabled(rollsLabel);
            ImGui.Spacing();

            for (int i = 0; i < game.Rolls.Count; i++)
            {
                var rollText = $"Roll {i + 1}:  {game.Rolls[i]:D2}  / {GachaGame.RollMax}";
                ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(rollText).X) / 2f);
                ImGui.TextColored(new Vector4(1f, 0.9f, 0.4f, 1f), rollText);
            }

            if (game.Rolls.Count > 1)
            {
                ImGui.Spacing();
                var sumText = $"Current sum: {game.Sum}";
                ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(sumText).X) / 2f);
                ImGui.Text(sumText);
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var instructText = "Type /random 20 in chat to roll!";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(instructText).X) / 2f);
        float pulse = (MathF.Sin(animationTimer * AnimSpeed * 1.5f) + 1f) / 2f;
        ImGui.TextColored(new Vector4(0.5f + pulse * 0.5f, 1f, 0.5f + pulse * 0.5f, 1f), instructText);

        ImGui.Spacing();
        var cancelWidth = 160f;
        ImGui.SetCursorPosX((windowWidth - cancelWidth) / 2f);
        if (ImGui.Button("Cancel Game", new Vector2(cancelWidth, 28)))
        {
            game.Reset();
        }
    }

    private void DrawResultState()
    {
        if (game.CurrentPrize == null) return;

        var windowWidth = ImGui.GetWindowWidth();
        var prize = game.CurrentPrize;

        ImGui.Spacing();

        float pulse = (MathF.Sin(animationTimer * AnimSpeed * 2f) + 1f) / 2f;
        var r = (prize.Color & 0xFF) / 255f;
        var g = ((prize.Color >> 8) & 0xFF) / 255f;
        var b = ((prize.Color >> 16) & 0xFF) / 255f;
        var prizeColor = new Vector4(r, g, b, 0.85f + pulse * 0.15f);

        var tierLabel = $"[ {GachaGame.GetTierDisplayName(prize.Tier).ToUpper()} ]";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(tierLabel).X) / 2f);
        ImGui.TextColored(prizeColor, tierLabel);

        ImGui.Spacing();

        var emoji = prize.Emoji;
        var emojiWidth = ImGui.CalcTextSize(emoji).X;
        ImGui.SetCursorPosX((windowWidth - emojiWidth) / 2f);
        ImGui.TextColored(prizeColor, emoji);

        ImGui.Spacing();

        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(prize.Name).X) / 2f - 5f);
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
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(rollsLabel).X) / 2f);
        ImGui.TextDisabled(rollsLabel);

        ImGui.Spacing();
        for (int i = 0; i < game.Rolls.Count; i++)
        {
            var rollText = $"Roll {i + 1}:  {game.Rolls[i]:D2}  / {GachaGame.RollMax}";
            ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(rollText).X) / 2f);
            ImGui.TextColored(new Vector4(1f, 0.9f, 0.4f, 1f), rollText);
        }

        ImGui.Spacing();
        var sumText = $"Total:  {game.Sum}  / {GachaGame.RollsNeeded * GachaGame.RollMax}";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(sumText).X) / 2f);
        ImGui.Text(sumText);

        ImGui.Spacing();
        ImGui.Spacing();

        var btnWidth = 200f;
        ImGui.SetCursorPosX((windowWidth - btnWidth) / 2f);
        if (ImGui.Button("Play Again!", new Vector2(btnWidth, 35)))
        {
            game.StartGame();
        }

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
        var windowWidth = ImGui.GetWindowWidth();
        var guideLabel = "Prize Guide (sum of 3 rolls):";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(guideLabel).X) / 2f);
        ImGui.TextDisabled(guideLabel);
        ImGui.Spacing();

        DrawPrizeRow("60",       "JACKPOT",    new Vector4(0f, 1f, 1f, 1f));
        DrawPrizeRow("51 - 59",  "Legendary",  new Vector4(1f, 0.65f, 0f, 1f));
        DrawPrizeRow("36 - 50",  "Epic",       new Vector4(0.69f, 0f, 1f, 1f));
        DrawPrizeRow("21 - 35",  "Rare",       new Vector4(0f, 0.5f, 1f, 1f));
        DrawPrizeRow("11 - 20",  "Uncommon",   new Vector4(0f, 0.8f, 0f, 1f));
        DrawPrizeRow(" 3 - 10",  "Common",     new Vector4(0.67f, 0.67f, 0.67f, 1f));
    }

    private static void DrawPrizeRow(string range, string label, Vector4 color)
    {
        var windowWidth = ImGui.GetWindowWidth();
        var text = $"{range,7}  =>  {label}";
        ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize(text).X) / 2f);
        ImGui.TextColored(color, text);
    }

    private void DrawFooter()
    {
        ImGui.Spacing();
        ImGui.TextDisabled($"Total Pulls: {config.TotalGachaPulls}   Jackpots: {config.JackpotsHit}");
    }
}
