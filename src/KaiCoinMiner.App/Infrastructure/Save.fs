namespace KaiCoinMiner.App.Infrastructure

open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open KaiCoinMiner.App.Domain

type SavedAutoMiner =
    { Kind: string
      Owned: int
      NextCostCoins: decimal }

type SavedUpgrade =
    { Kind: string
      Level: int
      NextCostCash: decimal }

type SavedChartPoint =
    { Tick: int64
      Price: decimal }

type SaveSnapshot =
    { Coins: decimal
      LifetimeMinedCoins: decimal
      Cash: decimal
      CoinPrice: decimal
      ChallengeDifficulty: int
      AutoMiners: SavedAutoMiner list
      Upgrades: SavedUpgrade list
      Chart: SavedChartPoint list
      WinState: string }

module Save =
    let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

    let private winStateToString (state: WinState) : string =
        match state with
        | NotWon -> "NotWon"
        | Launching -> "Launching"
        | Won -> "Won"

    let private winStateOfString (state: string) : WinState =
        match state with
        | "Launching" -> Launching
        | "Won" -> Won
        | _ -> NotWon

    let private toSavedAutoMiner ((key, miner): string * AutoMinerState) : SavedAutoMiner =
        { Kind = key
          Owned = miner.Owned
          NextCostCoins = miner.NextCostCoins }

    let private toSavedUpgrade ((key, upgrade): string * UpgradeState) : SavedUpgrade =
        { Kind = key
          Level = upgrade.Level
          NextCostCash = upgrade.NextCostCash }

    let private toSavedChartPoint (point: ChartPoint) : SavedChartPoint =
        { Tick = point.Tick
          Price = point.Price }

    let private toChartPoint (point: SavedChartPoint) : ChartPoint =
        { Tick = point.Tick
          Price = point.Price }

    let getDefaultSaveBasePath
        (isWindows: bool)
        (isMacOS: bool)
        (localApplicationDataPath: string)
        (homeDirectoryPath: string)
        (xdgDataHome: string option)
        =
        if isWindows then
            localApplicationDataPath
        elif isMacOS then
            if String.IsNullOrWhiteSpace(homeDirectoryPath) then
                localApplicationDataPath
            else
                Path.Combine(homeDirectoryPath, "Library", "Application Support")
        else
            match xdgDataHome with
            | Some path when not (String.IsNullOrWhiteSpace(path)) -> path
            | _ -> localApplicationDataPath

    let getDefaultSavePath () =
        let localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        let homeDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        let xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") |> Option.ofObj

        let basePath =
            getDefaultSaveBasePath
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                localApplicationDataPath
                homeDirectoryPath
                xdgDataHome

        Path.Combine(basePath, "KaiCoinMiner", "save.json")

    let toSnapshot (state: GameState) : SaveSnapshot =
        { Coins = state.Economy.Coins
          LifetimeMinedCoins = state.Economy.LifetimeMinedCoins
          Cash = state.Economy.Cash
          CoinPrice = state.Economy.CoinPrice
          ChallengeDifficulty = state.Challenge.Difficulty
          AutoMiners = state.AutoMiners |> Map.toList |> List.map toSavedAutoMiner
          Upgrades = state.Upgrades |> Map.toList |> List.map toSavedUpgrade
          Chart = state.Chart |> List.map toSavedChartPoint
          WinState = winStateToString state.WinState }

    let fromSnapshot (snapshot: SaveSnapshot) : GameState =
        let seeded = State.initial

        let autoMiners =
            snapshot.AutoMiners
            |> List.fold
                (fun (acc: Map<string, AutoMinerState>) (saved: SavedAutoMiner) ->
                    match Map.tryFind saved.Kind acc with
                    | Some (current: AutoMinerState) ->
                        let nextCost =
                            if saved.NextCostCoins > 0m then
                                saved.NextCostCoins
                            else
                                current.NextCostCoins

                        Map.add
                            saved.Kind
                            { current with
                                Owned = max 0 saved.Owned
                                NextCostCoins = nextCost }
                            acc
                    | None -> acc)
                seeded.AutoMiners

        let upgrades =
            snapshot.Upgrades
            |> List.fold
                (fun (acc: Map<string, UpgradeState>) (saved: SavedUpgrade) ->
                    match Map.tryFind saved.Kind acc with
                    | Some (current: UpgradeState) ->
                        let nextCost =
                            if saved.NextCostCash > 0m then
                                saved.NextCostCash
                            else
                                current.NextCostCash

                        Map.add
                            saved.Kind
                            { current with
                                Level = max 0 saved.Level
                                NextCostCash = nextCost }
                            acc
                    | None -> acc)
                seeded.Upgrades

        { seeded with
            Economy =
                { Coins = snapshot.Coins
                  LifetimeMinedCoins = snapshot.LifetimeMinedCoins
                  Cash = snapshot.Cash
                  CoinPrice = if snapshot.CoinPrice <= 0m then 0.0001m else snapshot.CoinPrice }
            Challenge = { seeded.Challenge with Difficulty = max 1 snapshot.ChallengeDifficulty }
            AutoMiners = autoMiners
            Upgrades = upgrades
            Chart = snapshot.Chart |> List.map toChartPoint
            WinState = winStateOfString snapshot.WinState }

    let save (path: string) (state: GameState) =
        let directory = Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        let json = JsonSerializer.Serialize(toSnapshot state, jsonOptions)
        File.WriteAllText(path, json)

    let tryLoad (path: string) : GameState option =
        try
            if not (File.Exists(path)) then
                None
            else
                let json = File.ReadAllText(path)
                let snapshot = JsonSerializer.Deserialize<SaveSnapshot>(json, jsonOptions)

                if isNull (box snapshot) then
                    None
                else
                    Some (fromSnapshot snapshot)
        with
        | :? IOException -> None
        | :? UnauthorizedAccessException -> None
        | :? JsonException -> None

    let saveDefault (state: GameState) =
        save (getDefaultSavePath ()) state

    let tryLoadDefault () =
        tryLoad (getDefaultSavePath ())
