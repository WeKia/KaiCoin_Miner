# KaiCoin Miner

- Stack: F# on .NET 10 with an Avalonia desktop UI.
- Platform scope: Windows and macOS desktop targets are supported.
- Build: `dotnet build KaiCoinMiner.sln`.
- Run: `dotnet run --project src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj`.
- Test: `dotnet run --project tests/KaiCoinMiner.App.Tests/KaiCoinMiner.App.Tests.fsproj -- --summary`.
- macOS publish (Apple Silicon): `dotnet publish src/KaiCoinMiner.App/KaiCoinMiner.App.fsproj -r osx-arm64 -c Release --self-contained false`.
- Test runner note: the test project uses Expecto directly, so `dotnet test` is not the primary execution path here.
- Save path helper: `src/KaiCoinMiner.App/Infrastructure/Save.fs` chooses a platform-aware base path:
  - Windows: `%LOCALAPPDATA%`
  - macOS: `~/Library/Application Support`
  - Other Unix-like: `$XDG_DATA_HOME` when set, otherwise LocalApplicationData fallback
