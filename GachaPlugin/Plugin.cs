using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using GachaPlugin.Windows;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GachaPlugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/gachapon";

    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;

    public Configuration Config { get; init; }

    private readonly GachaGame game;
    private readonly PartyTracker partyTracker;
    private readonly WindowSystem windowSystem = new("GachaPlugin");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;

    // Matches both "Random! Name rolls a 15 (out of 20)." and "Random! You roll a 15 (out of 20)."
    private static readonly Regex DiceRollRegex = new(
        @"Random! (?<name>.+?) rolls? (?:an? )?(?<roll>\d+) \(out of (?<max>\d+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;

        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.EnsureDefaults();

        game = new GachaGame(Config);
        partyTracker = new PartyTracker();

        configWindow = new ConfigWindow(game, Config);
        mainWindow = new MainWindow(game, Config, partyTracker, () => configWindow.IsOpen = true);

        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Gachapon Machine. Use \"/gachapon config\" for settings. Roll /random three times to play!"
        });

        ChatGui.ChatMessage += OnChatMessage;
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
    }

    public void Dispose()
    {
        ChatGui.ChatMessage -= OnChatMessage;
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;

        CommandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        configWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var a = args.Trim().ToLowerInvariant();
        if (a is "config" or "settings" or "cfg")
            configWindow.IsOpen = true;
        else
            mainWindow.IsOpen = true;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        var type = message.LogKind;
        if (type != XivChatType.SystemMessage && (int)type != 2122)
            return;

        var text = message.Message.TextValue;
        var match = DiceRollRegex.Match(text);
        if (!match.Success)
            return;

        if (!int.TryParse(match.Groups["max"].Value, out var max) || max != game.RollMax)
            return;

        if (!int.TryParse(match.Groups["roll"].Value, out var roll))
            return;

        var rollerName = match.Groups["name"].Value.Trim();
        var localName = PlayerState.IsLoaded ? PlayerState.CharacterName : null;

        bool isLocal = rollerName.Equals("You", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(localName) && rollerName.Equals(localName, StringComparison.OrdinalIgnoreCase));

        Log.Debug($"[GachaPlugin] Roll detected: name='{rollerName}' roll={roll}/{max} local={isLocal}");

        // Party tracking: record everyone (including the local player) when enabled.
        if (Config.PartyMode)
        {
            var displayName = isLocal
                ? (string.IsNullOrEmpty(localName) ? "You" : localName)
                : rollerName;

            var completed = partyTracker.RegisterRoll(displayName, roll, game.RollsNeeded);
            if (completed != null)
                FinalizePartyResult(displayName, completed);
        }

        // The animated machine only advances on the local player's rolls.
        if (isLocal && game.State == GachaState.WaitingForRolls)
        {
            if (!game.RegisterRoll(roll))
                return;

            if (game.State == GachaState.Complete && game.CurrentPrize != null)
            {
                FinalizeLocalGame();
            }
            else if (Config.AnnouncePrizeInChat)
            {
                int rollNumber = game.RollsNeeded - game.RollsRemaining;
                ChatGui.Print(new XivChatEntry
                {
                    Type = XivChatType.Echo,
                    Message = $"[Gachapon] Roll {rollNumber}/{game.RollsNeeded} recorded: {roll}. Running total: {game.Sum}. {game.RollsRemaining} roll(s) remaining — use /random {game.RollMax}!"
                });
            }
        }
    }

    private void FinalizeLocalGame()
    {
        var prize = game.CurrentPrize!;
        var localName = PlayerState.IsLoaded ? PlayerState.CharacterName : "You";

        Config.TotalGachaPulls++;
        Config.RecordTier(prize.Tier);
        if (prize.Tier == PrizeTier.Jackpot) Config.JackpotsHit = Config.JackpotCount;
        if (game.Sum > Config.BestSum) Config.BestSum = game.Sum;

        if ((int)prize.Tier >= Config.WinStreakTier)
        {
            Config.CurrentWinStreak++;
            if (Config.CurrentWinStreak > Config.BestWinStreak)
                Config.BestWinStreak = Config.CurrentWinStreak;
        }
        else
        {
            Config.CurrentWinStreak = 0;
        }

        int bet = 0, payout = 0;
        if (Config.EnableGilTracking)
        {
            bet = Config.DefaultBet;
            payout = Config.GetPrize(prize.Tier).Payout;
            Config.TotalGilBet += bet;
            Config.TotalGilWon += payout;
        }

        Config.History.Add(new PullRecord
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Rolls = new List<int>(game.Rolls),
            Sum = game.Sum,
            Tier = prize.Tier,
            PrizeName = prize.Name,
            Bet = bet,
            Payout = payout,
            Player = localName ?? "You"
        });
        TrimHistory();
        Config.Save();

        PlayPrizeSound(prize.Tier);

        if (Config.AnnouncePrizeInChat)
        {
            ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = $"[Gachapon] All {game.RollsNeeded} rolls complete! Sum: {game.Sum}/{game.MaxSum} — You got: {prize.Name}! Check the Gachapon window for details."
            });
        }

        mainWindow.IsOpen = true;
    }

    private void FinalizePartyResult(string player, List<int> rolls)
    {
        int sum = 0;
        foreach (var r in rolls) sum += r;

        var prize = game.GetPrizeForSum(sum);

        partyTracker.Results.Insert(0, new PartyResult
        {
            Player = player,
            Rolls = new List<int>(rolls),
            Sum = sum,
            Tier = prize.Tier,
            PrizeName = prize.Name,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        if (partyTracker.Results.Count > 50)
            partyTracker.Results.RemoveRange(50, partyTracker.Results.Count - 50);

        if (Config.AnnouncePrizeInChat)
        {
            ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Message = $"[Gachapon] {player} rolled {sum} → {GachaGame.GetTierDisplayName(prize.Tier)}: {prize.Name}!"
            });
        }
    }

    private void TrimHistory()
    {
        int cap = Config.MaxHistoryEntries > 0 ? Config.MaxHistoryEntries : 500;
        if (Config.History.Count > cap)
            Config.History.RemoveRange(0, Config.History.Count - cap);
    }

    private void PlayPrizeSound(PrizeTier tier)
    {
        if (!Config.EnableSound) return;
        int id = tier == PrizeTier.Jackpot ? Config.JackpotSoundId : Config.CompleteSoundId;
        PlaySoundEffect(id);
    }

    public static void PlaySoundEffect(int id)
    {
        if (id < 1 || id > 16) return;
        try
        {
            UIGlobals.PlayChatSoundEffect((uint)id);
        }
        catch (Exception ex)
        {
            Log?.Warning($"[GachaPlugin] Failed to play sound effect {id}: {ex.Message}");
        }
    }

    private void DrawUI() => windowSystem.Draw();
    private void OpenMainUi() => mainWindow.IsOpen = true;
    private void OpenConfigUi() => configWindow.IsOpen = true;
}
