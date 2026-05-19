# KaiCoin Miner

KaiCoin Miner is a cross-platform desktop incremental game built with Avalonia.

## Requirements

- .NET 10 SDK (for development only)
- Windows, macOS, or Linux (Ubuntu/Debian-based distributions) desktop
- Avalonia desktop dependencies are restored through NuGet

> **Note on Linux**: The app is published as a self-contained executable, so no .NET runtime installation is required on the target machine. Linux builds require the standard X11 client libraries (`libX11-6`, `libICE6`, `libSM6`), which are pre-installed on Ubuntu Desktop. For minimal/WSL environments, install these via `sudo apt install libice6 libsm6 libx11-6`.

## Build

```bash
dotnet build KaiCoinMiner.sln
```

## Run

```bash
dotnet run --project src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj
```

## Test

The test project uses Expecto directly, so run the test executable instead of `dotnet test`:

```bash
dotnet run --project tests/KaiCoinMiner.App.Tests/KaiCoinMiner.App.Tests.fsproj -- --summary
```

## Publish

All publish commands produce **self-contained, single-file executables**. No .NET runtime or additional software installation is required on the target machine.

### Windows (x64)

```bash
dotnet publish src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj -r win-x64 -c Release
```

### macOS (Apple Silicon)

```bash
dotnet publish src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj -r osx-arm64 -c Release
```

### Linux (x64)

```bash
dotnet publish src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj -r linux-x64 -c Release
```

> **Linux Note**: The executable includes the .NET runtime and Avalonia native libraries. Standard X11 desktop libraries must be present on the system (pre-installed on Ubuntu Desktop). For WSL or headless environments, install `libice6`, `libsm6`, and `libx11-6` first.

## Save File

The game saves to a platform-aware default location:

- Windows: `%LOCALAPPDATA%\KaiCoinMiner\save.json`
- macOS: `~/Library/Application Support/KaiCoinMiner/save.json`
- Other Unix-like systems: `$XDG_DATA_HOME/KaiCoinMiner/save.json` when `XDG_DATA_HOME` is set; otherwise falls back to the runtime LocalApplicationData path

Examples:

```text
%LOCALAPPDATA%\KaiCoinMiner\save.json
```

```text
~/Library/Application Support/KaiCoinMiner/save.json
```

On a typical Windows profile, `%LOCALAPPDATA%\KaiCoinMiner\save.json` resolves to something like:

```text
C:\Users\<you>\AppData\Local\KaiCoinMiner\save.json
```

## Scope

- Windows, macOS, and Linux (Ubuntu/Debian) desktop
- Avalonia UI client, not a web or console app
- Self-contained single-file executables for all platforms
- Main game project: `src/KaiCoinMiner.App`
- Tests: `tests/KaiCoinMiner.App.Tests`

## Project Notes

- I provided the details of my game concept and asked the LLM to draft a Proposal document based on that information. I also asked it to review the concept and suggest areas for improvement.

- The initial draft assumed a CLI-based game. Since the game I am designing requires a clickable GUI, I reprompted the LLM to target a GUI platform.

- The LLM organized ideas effectively, but the readability was sometimes inconsistent and it occasionally included unnecessary details. I manually refined and reformatted the text.
