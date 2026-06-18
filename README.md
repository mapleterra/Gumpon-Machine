# Gachapon Machine

A gachapon gambling minigame for **Final Fantasy XIV** built on the [Dalamud](https://github.com/goatcorp/Dalamud) plugin framework.

Roll `/random 20` three times and the machine awards you a prize based on the **sum** of your rolls. The higher the total, the rarer the capsule!

---

## How to Play

1. Type `/gachapon` in-game to open the Gachapon Machine window.
2. Click **"Insert Coin & Start!"** to begin a round.
3. Type `/random 20` in chat **three times**.
   - The plugin reads the die-roll chat messages and records each result automatically.
   - You'll get an Echo message confirming each roll.
4. After your 3rd roll, the window reveals your prize based on the total.

---

## Prize Tiers

| Sum (3–60) | Tier       |
|------------|------------|
| 60         | ★ JACKPOT  |
| 51–59      | Legendary  |
| 36–50      | Epic       |
| 21–35      | Rare       |
| 11–20      | Uncommon   |
| 3–10       | Common     |

---

## Installing in-game (Custom Plugin Repository)

This repo doubles as a **Dalamud custom plugin repository**, so anyone can install Gachapon Machine directly in-game:

1. In-game, type `/xlsettings` to open the Dalamud settings.
2. Go to the **Experimental** tab.
3. Under **Custom Plugin Repositories**, paste this URL and click **+**, then **Save**:
   ```
   https://raw.githubusercontent.com/mapleterra/Gumpon-Machine/main/repo.json
   ```
4. Open `/xlplugins`, search for **Gachapon Machine**, and click **Install**.

> The install link points at the latest GitHub Release. You must publish at least one release (see [Releasing](#releasing)) before the plugin can be downloaded this way.

---

## Building from Source

### Prerequisites

This plugin assumes the following prerequisites are met:

- XIVLauncher, FINAL FANTASY XIV, and Dalamud have all been installed and the game has been run with Dalamud at least once.
- XIVLauncher is installed to its default directories and configuration.
  - If a custom path is required for Dalamud's dev directory, set it with the `DALAMUD_HOME` environment variable.
- A [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) is installed and available. (In most cases your IDE will take care of this.)

### Building

1. Open `GachaPlugin.sln` in your C# editor of choice (likely [Visual Studio](https://visualstudio.microsoft.com) or [JetBrains Rider](https://www.jetbrains.com/rider/)).
2. Build the solution. By default this produces a `Debug` build; switch to `Release` in your IDE if desired.
3. The resulting plugin can be found at `GachaPlugin/bin/x64/Debug/GachaPlugin/GachaPlugin.dll` (or `Release` if appropriate).

> Building requires Windows because Dalamud targets `net10.0-windows`. The target framework is supplied automatically by `Dalamud.NET.Sdk`.

### Activating in-game

1. Launch the game and use `/xlsettings` in chat (or `xlsettings` in the Dalamud Console) to open the Dalamud settings.
   - Go to **Experimental** and add the full path to `GachaPlugin.dll` to the list of **Dev Plugin Locations**.
2. Use `/xlplugins` (chat) or `xlplugins` (console) to open the Plugin Installer.
   - Go to **Dev Tools → Installed Dev Plugins**, find **Gachapon Machine**, and enable it.
3. Type `/gachapon` to open the machine and play!

You only need to add the Dev Plugin Location once; it is preserved afterwards. You can enable, disable, or load the plugin on startup through the Plugin Installer.

---

## Continuous Integration

`.github/workflows/pr-build.yml` builds the plugin on every pull request against `main`/`master`
and uploads the packaged output as a build artifact, so you can confirm a PR compiles before merging.

---

## Releasing

`.github/workflows/release.yml` publishes the plugin so the custom repository link works:

1. Bump `<Version>` in `GachaPlugin/GachaPlugin.csproj` and `AssemblyVersion` in `repo.json` to match.
2. Commit, then create and push a version tag:
   ```bash
   git tag v1.0.0.0
   git push origin v1.0.0.0
   ```
3. The workflow builds in `Release`, packages the plugin, and attaches `latest.zip` to a GitHub
   Release for that tag. The URLs in `repo.json` point at `releases/latest/download/latest.zip`,
   so Dalamud always pulls the newest release.

---

## Project Structure

```
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
│   ├── GachaGame.cs                # Game logic — state machine + prize table
│   ├── Configuration.cs            # Persistent stats
│   └── Windows/MainWindow.cs       # ImGui UI
├── GachaPlugin.sln                 # Solution file
├── repo.json                       # Dalamud custom plugin repository manifest
├── .editorconfig
├── .gitignore
└── LICENSE.md
```

---

## How It Works

- `Plugin.cs` subscribes to `ChatGui.ChatMessage` and matches die-roll messages
  (`"Random! <Name> rolls <N> (out of 20)."`).
- Only the **local player's** rolls count (matched against `ClientState.LocalPlayer`).
- `GachaGame.cs` is a state machine: `Idle → WaitingForRolls → Complete`.
- After 3 rolls, the sum maps to a prize tier shown in the ImGui window.

To customise prizes, edit the `PrizeTable` and `DeterminePrize` logic in `GachaGame.cs`.

---

## License

Released under the GNU AGPL-3.0-or-later License, matching the Dalamud SamplePlugin template. See [LICENSE.md](LICENSE.md).
