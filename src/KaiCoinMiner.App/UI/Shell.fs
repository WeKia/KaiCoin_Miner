namespace KaiCoinMiner.App.UI

open KaiCoinMiner.App.Domain

module Shell =
    let title (state: GameState) =
        if state.IsInitialized then "KaiCoin Miner" else "Starting"
