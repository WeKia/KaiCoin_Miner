namespace KaiCoinMiner.App.Domain

open System

type EconomyState =
    { Coins: decimal
      LifetimeMinedCoins: decimal
      Cash: decimal
      CoinPrice: decimal }

type ChallengeState =
    { Prompt: string
      ExpectedAnswer: int option
      Difficulty: int
      LastWasCorrect: bool option
      LastFeedback: ChallengeFeedback option
      LastRewardMessage: string option }

type AutoMinerState =
    { Key: string
      Owned: int
      OutputPerSecond: decimal
      NextCostCoins: decimal }

type UpgradeState =
    { Key: string
      Level: int
      NextCostCash: decimal }

type UiState =
    { IsExchangeModalOpen: bool
      ActiveInput: InputTarget
      PendingSellQuantity: decimal }

type ChartPoint =
    { Tick: int64
      Price: decimal }

type TimerState =
    { TickCount: int64
      LastTickAt: DateTimeOffset option }

type MarketState =
    { NewsTicker: string list;
      ActiveNews: string option;
      SecondsUntilNextNews: decimal;
      DifficultyPressure: int;
      LastPriceDrift: decimal }

type GameState =
    { IsInitialized: bool
      Economy: EconomyState
      Challenge: ChallengeState
      AutoMiners: Map<string, AutoMinerState>
      Upgrades: Map<string, UpgradeState>
      Ui: UiState
      Chart: ChartPoint list
      Timer: TimerState
      Market: MarketState
      WinState: WinState }

module State =
    let initial =
        let config = GameConfig.ensureLoaded ()

        let autoMiners =
            config.AutoMiners
            |> Map.map (fun key cfg ->
                { Key = key; Owned = 0; OutputPerSecond = cfg.OutputPerSecond; NextCostCoins = cfg.InitialCost })

        let upgrades =
            config.Upgrades
            |> Map.map (fun key cfg ->
                { Key = key; Level = 0; NextCostCash = cfg.InitialCost })

        { IsInitialized = true
          Economy = { Coins = 0m; LifetimeMinedCoins = 0m; Cash = 0m; CoinPrice = 1m }
          Challenge =
            { Prompt = ""
              ExpectedAnswer = None
              Difficulty = 1
              LastWasCorrect = None
              LastFeedback = None
              LastRewardMessage = None }
          AutoMiners = autoMiners
          Upgrades = upgrades
          Ui = { IsExchangeModalOpen = false; ActiveInput = MonitorAnswer; PendingSellQuantity = 0m }
          Chart = []
          Timer = { TickCount = 0L; LastTickAt = None }
          Market =
            { NewsTicker = []
              ActiveNews = None
              SecondsUntilNextNews = 45m
              DifficultyPressure = 0
              LastPriceDrift = 0m }
          WinState = NotWon }
