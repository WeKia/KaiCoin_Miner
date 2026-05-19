namespace KaiCoinMiner.App.UI

open KaiCoinMiner.App.Domain

module Shell =
    type UpgradeTileContent =
        { Header: string
          LevelText: string
          CostText: string
          StatusText: string }

    let title (state: GameState) =
        if state.IsInitialized then "KaiCoin Miner" else "Starting"

    let describeUpgradeTile (label: string) (level: int) (nextCostCash: decimal) (isAffordable: bool) =
        { Header = label
          LevelText = $"Level {level}"
          CostText = $"Next ${nextCostCash:F0}"
          StatusText = if isAffordable then "READY" else "LOCKED" }

    let formatListRowContent (label: string) (owned: int) (nextCost: decimal) (unitLabel: string) =
        $"{label} | Owned {owned} | Next {nextCost:F2} {unitLabel}"
