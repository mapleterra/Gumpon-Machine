# Gumpon Machine

A gachapon gambling minigame for Final Fantasy XIV built on the Dalamud plugin framework.

Roll /random and the machine awards you a prize based on the sum of your rolls. The higher the total, the rarer the capsule — all the way up to the JACKPOT!

New in v1.0.1.0: a full pull-history log, detailed statistics, fully customizable prizes and rules, a jackpot sound effect, gil cost/payout tracking, and a party mode that tracks everyone's rolls on a leaderboard. See What's New.

## How to Play
Type /gachapon in-game to open the Gachapon Machine window.
Click "Insert Coin & Start!" to begin a round.
Type /random in chat the required number of times (default 3 rolls of a 20-sided die).
The plugin reads the die-roll chat messages and records each result automatically.
You'll get an Echo message confirming each roll.
After your final roll, the window reveals your prize based on the total. A jackpot plays a celebratory sound!
Both the number of rolls and the size of the die are adjustable in Settings — see Adjustable rules.

## What's New in v1.0.1.0
📜 Pull history log — every play is recorded with its rolls, sum, tier, prize, and timestamp. Browse it in the History tab.
📊 Detailed statistics — total pulls, best sum ever, current and best win streaks, per-tier win counts, your luckiest day, and gil totals, all in the Stats tab.
🎁 Custom prizes — rename every prize and give it your own description and emoji in Settings.
⚙️ Adjustable rules — change how many rolls a round needs, the die size, and the tier thresholds.
🔊 Jackpot sound — pick a chat sound effect that plays when you hit the jackpot (or turn it off).
💰 Gil tracking — set a cost per pull and a payout per tier; the plugin tracks your lifetime spend, winnings, and net profit.
🎉 Party mode — when enabled, the Party tab tracks everyone's /random rolls and ranks them on a leaderboard.
## Prize Tiers
Tiers are decided by the sum of your rolls. Thresholds are stored as a percentage of the maximum possible sum, so they scale automatically when you change the rolls or die size. With the default 3 × d20 (max 60):

Default Sum	Tier	Default Threshold
60	★ JACKPOT	exactly the max
51–59	Legendary	≥ 85%
36–50	Epic	≥ 60%
21–35	Rare	≥ 35%
11–20	Uncommon	≥ 18%
3–10	Common	below Uncommon
All thresholds, prize names, descriptions, and emojis are editable in Settings.

Settings
Open settings from the cog icon in the plugin installer, or the Settings button in the main window. From there you can:

Rules — set the number of rolls per round, the die size, and the percentage threshold for each tier.
Prizes — edit the name, description, and emoji for every tier.
Gil — set the cost per pull and the payout awarded for each tier.
Sound — toggle the jackpot sound and choose which chat sound effect (1–16) plays.
Party mode — toggle leaderboard tracking of other players' rolls.
History — set how many past pulls to keep, and reset history or statistics.
Installing in-game (Custom Plugin Repository)
This repo doubles as a Dalamud custom plugin repository, so anyone can install Gachapon Machine directly in-game:

In-game, type /xlsettings to open the Dalamud settings.
Go to the Experimental tab.
Under Custom Plugin Repositories, paste this URL and click +, then Save:
https://raw.githubusercontent.com/mapleterra/Gumpon-Machine/main/repo.json

Open /xlplugins, search for Gachapon Machine, and click Install.
The install link points at the latest GitHub Release. You must publish at least one release (see Releasing) before the plugin can be downloaded this way. Updates are picked up automatically once a release with a higher AssemblyVersion is published.

## Building from Source
Prerequisites
This plugin assumes the following prerequisites are met:

XIVLauncher, FINAL FANTASY XIV, and Dalamud have all been installed and the game has been run with Dalamud at least once.
XIVLauncher is installed to its default directories and configuration.
If a custom path is required for Dalamud's dev directory, set it with the DALAMUD_HOME environment variable.
A .NET 10 SDK is installed and available. (In most cases your IDE will take care of this.)
Building
Open GachaPlugin.sln in your C# editor of choice (likely Visual Studio or JetBrains Rider).
Build the solution. By default this produces a Debug build; switch to Release in your IDE if desired.
The resulting plugin can be found at GachaPlugin/bin/x64/Debug/GachaPlugin/GachaPlugin.dll (or Release if appropriate).
Building requires Windows because Dalamud targets net10.0-windows. The target framework is supplied automatically by Dalamud.NET.Sdk.

## Activating in-game
Launch the game and use /xlsettings in chat (or xlsettings in the Dalamud Console) to open the Dalamud settings.
Go to Experimental and add the full path to GachaPlugin.dll to the list of Dev Plugin Locations.
Use /xlplugins (chat) or xlplugins (console) to open the Plugin Installer.
Go to Dev Tools → Installed Dev Plugins, find Gachapon Machine, and enable it.
Type /gachapon to open the machine and play!
You only need to add the Dev Plugin Location once; it is preserved afterwards. You can enable, disable, or load the plugin on startup through the Plugin Installer.

## Continuous Integration
.github/workflows/pr-build.yml builds the plugin on every pull request against main/master and uploads the packaged output as a build artifact, so you can confirm a PR compiles before merging.

## Releasing
.github/workflows/release.yml publishes the plugin so the custom repository link works:

Bump <Version> in GachaPlugin/GachaPlugin.csproj and AssemblyVersion in repo.json to match (and keep GachaPlugin/GachaPlugin.json in sync).
Commit, then create and push a version tag:
git tag v1.0.1.0
git push origin v1.0.1.0

The workflow builds in Release, packages the plugin, and attaches latest.zip to a GitHub Release for that tag. The URLs in repo.json point at releases/latest/download/latest.zip, so Dalamud always pulls the newest release.
Each tag triggers the release workflow exactly once, so always use a fresh, higher version number for every release.

## Project Structure
Gachapon-Machine/
├── .github/workflows/
│   ├── pr-build.yml                # CI — builds the plugin on pull requests
│   └── release.yml                 # CD — packages & publishes latest.zip on version tags
├── Data/
│   └── icon.png                    # Plugin icon, 512x512 (copied to the build output)
├── GachaPlugin/
│   ├── GachaPlugin.csproj          # .NET project (Dalamud.NET.Sdk 15 → net10.0-windows)
│   ├── GachaPlugin.json            # Dalamud plugin manifest
│   ├── Plugin.cs                   # Entry point — command + chat hook
│   ├── GachaGame.cs                # Game logic — state machine + prize resolution
│   ├── Configuration.cs            # Persistent settings, stats & history
│   ├── PullRecord.cs               # A single recorded pull (rolls, sum, tier, prize, time)
│   ├── Statistics.cs               # Derived stats (streaks, luckiest day, averages)
│   ├── PartyTracker.cs             # Party-mode roll tracking & leaderboard
│   ├── UiColors.cs                 # Shared tier colors for the UI
│   └── Windows/
│       ├── MainWindow.cs           # ImGui UI — Play / History / Stats / Party tabs
│       └── ConfigWindow.cs         # ImGui settings UI
├── GachaPlugin.sln                 # Solution file
├── repo.json                       # Dalamud custom plugin repository manifest
├── .editorconfig
├── .gitignore
└── LICENSE.md

## How It Works
Plugin.cs subscribes to ChatGui.ChatMessage and matches die-roll messages (e.g. "Random! <Name> rolls <N> (out of 20).").
Only the local player's rolls count toward your own game — matched exactly against IPlayerState.CharacterName (or the "You" self-roll line), so other players with similar names are never mistaken for you.
When party mode is on, PartyTracker.cs records every player's rolls for the leaderboard, while your animated game still only follows your own rolls.
GachaGame.cs is a state machine: Idle → WaitingForRolls → Complete. After the required number of rolls, the sum maps to a prize tier shown in the ImGui window.
Settings, statistics, and pull history persist via Dalamud's configuration system (Configuration.cs).
To customise prizes and rules without touching code, use the in-game Settings window.

## License
Released under the GNU AGPL-3.0-or-later License, matching the Dalamud SamplePlugin template. See LICENSE.md.


