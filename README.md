# KaiCoin Miner

KaiCoin Miner is a cross-platform desktop incremental game built with Avalonia.

## Requirements

- .NET 10 SDK
- Windows or macOS desktop runtime support for `net10.0`
- Avalonia desktop dependencies are restored through NuGet

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

Publish for macOS (Apple Silicon):

```bash
dotnet publish src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj -r osx-arm64 -c Release --self-contained false
```

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

- Windows and macOS desktop
- Avalonia UI client, not a web or console app
- Main game project: `src/KaiCoinMiner.App`
- Tests: `tests/KaiCoinMiner.App.Tests`

## Project Notes

- I provided the details of my game concept and asked the LLM to draft a Proposal document based on that information. I also asked it to review the concept and suggest areas for improvement.

- The initial draft assumed a CLI-based game. Since the game I am designing requires a clickable GUI, I reprompted the LLM to target a GUI platform.

- The LLM organized ideas effectively, but the readability was sometimes inconsistent and it occasionally included unnecessary details. I manually refined and reformatted the text.
