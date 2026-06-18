using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GachaPlugin.Windows;
using System;
using System.Text.RegularExpressions;

namespace GachaPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/gachapon";

    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    public Configuration Config { get; init; }
    private readonly GachaGame game;
    private readonly WindowSystem windowSystem = new("GachaPlugin");
    private readonly MainWindow mainWindow;

    private static readonly Regex DiceRollRegex = new(
        @"Random! .+ rolls (\d+) \(out of (\d+)\)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;

        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        game = new GachaGame();
        mainWindow = new MainWindow(game, Config);

        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Gachapon Machine. Use /random 20 three times to play!"
        });

        ChatGui.ChatMessage += OnChatMessage;
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenMainUi;
    }

    public void Dispose()
    {
        ChatGui.ChatMessage -= OnChatMessage;
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenMainUi;

        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        mainWindow.IsOpen = true;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (game.State != GachaState.WaitingForRolls)
            return;

        if (type != XivChatType.SystemMessage && (int)type != 2122)
            return;

        var text = message.TextValue;
        var match = DiceRollRegex.Match(text);

        if (!match.Success)
            return;

        if (!int.TryParse(match.Groups[2].Value, out var max) || max != GachaGame.RollMax)
            return;

        if (!int.TryParse(match.Groups[1].Value, out var roll))
            return;

        var localPlayerName = ClientState.LocalPlayer?.Name?.TextValue;
        var senderName = sender.TextValue;

        if (!string.IsNullOrEmpty(localPlayerName) && !string.IsNullOrEmpty(senderName))
        {
            if (!senderName.Contains(localPlayerName, StringComparison.OrdinalIgnoreCase) &&
                !text.Contains(localPlayerName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        Log.Debug($"[GachaPlugin] Detected roll: {roll} / {max}");

        bool registered = game.RegisterRoll(roll);
        if (!registered) return;

        int rollNumber = GachaGame.RollsNeeded - game.RollsRemaining;

        if (game.State == GachaState.Complete && game.CurrentPrize != null)
        {
            Config.TotalGachaPulls++;
            if (game.CurrentPrize.Tier == PrizeTier.Jackpot)
                Config.JackpotsHit++;
            Config.Save();

            ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = $"[Gachapon] All 3 rolls complete! Sum: {game.Sum}/60 — You got: {game.CurrentPrize.Name}! Check the Gachapon window for details."
            });

            mainWindow.IsOpen = true;
        }
        else
        {
            ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = $"[Gachapon] Roll {rollNumber}/{GachaGame.RollsNeeded} recorded: {roll}. Running total: {game.Sum}. {game.RollsRemaining} roll(s) remaining — use /random 20!"
            });
        }
    }

    private void DrawUI() => windowSystem.Draw();
    private void OpenMainUi() => mainWindow.IsOpen = true;
}
