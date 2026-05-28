namespace KaiCoinMiner.App.Domain

type InputTarget =
    | MonitorAnswer
    | ExchangeSellQuantity

type ChallengeFeedback =
    | O
    | X

type WinState =
    | NotWon
    | Launching
    | Won
