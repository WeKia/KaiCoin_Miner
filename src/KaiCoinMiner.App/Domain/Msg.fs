namespace KaiCoinMiner.App.Domain

open System

type Msg =
    | MineRequested of decimal
    | AutoMinerTicked of decimal
    | Tick of DateTimeOffset
    | CoinPriceChanged of decimal
    | NewsReceived of string
    | ChallengePresented of prompt: string * expectedAnswer: int * difficulty: int
    | ChallengeSubmitted of int
    | SetExchangeModalOpen of bool
    | SetActiveInput of InputTarget
    | SetPendingSellQuantity of decimal
    | SellCoinsConfirmed
    | BuyAutoMiner of string
    | BuyUpgrade of string
    | BuySpaceshipToMars
    | AppendPricePoint of ChartPoint
    | SetWon of WinState
    | SaveRequested
    | LoadRequested
    | Loaded of GameState option
