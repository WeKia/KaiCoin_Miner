namespace KaiCoinMiner.App.Domain

open System

type InputTarget =
    | MonitorAnswer
    | ExchangeSellQuantity

type AutoMinerKind =
    | Monkey
    | RestingYouth
    | Gpu

type UpgradeKind =
    | ManualDifficultyReduction
    | AutoMinerEfficiency
    | MarketAnalysis

type ChallengeFeedback =
  | O
  | X

type WinState =
    | NotWon
    | Launching
    | Won

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
    { Kind: AutoMinerKind
      Owned: int
      OutputPerSecond: decimal
      NextCostCoins: decimal }

type UpgradeState =
    { Kind: UpgradeKind
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
      AutoMiners: Map<AutoMinerKind, AutoMinerState>
      Upgrades: Map<UpgradeKind, UpgradeState>
      Ui: UiState
      Chart: ChartPoint list
      Timer: TimerState
      Market: MarketState
      WinState: WinState }

module State =
    let private autoMiners =
        [ (Monkey, { Kind = Monkey; Owned = 0; OutputPerSecond = 0.1m; NextCostCoins = 10m })
          (RestingYouth, { Kind = RestingYouth; Owned = 0; OutputPerSecond = 1m; NextCostCoins = 100m })
          (Gpu, { Kind = Gpu; Owned = 0; OutputPerSecond = 10m; NextCostCoins = 1000m }) ]
        |> Map.ofList

    let private upgrades =
        [ (ManualDifficultyReduction, { Kind = ManualDifficultyReduction; Level = 0; NextCostCash = 100m })
          (AutoMinerEfficiency, { Kind = AutoMinerEfficiency; Level = 0; NextCostCash = 250m })
          (MarketAnalysis, { Kind = MarketAnalysis; Level = 0; NextCostCash = 400m }) ]
        |> Map.ofList

    let initial =
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
