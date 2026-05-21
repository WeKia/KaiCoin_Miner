namespace KaiCoinMiner.App.Domain

open System

type FinalShopItem =
    { Name: string
      CostCash: decimal
      ClearTrigger: WinState
      Description: string }

module Shop =
    let spaceshipToMars : FinalShopItem =
        { Name = "Spaceship to Mars"
          CostCash = 250000m
          ClearTrigger = WinState.Launching
          Description = "One-way ticket from idle grind to interplanetary clear." }

    let private roundCurrency (value: decimal) =
        Decimal.Round(value, 2, MidpointRounding.AwayFromZero)

    let private autoMinerGrowthRates =
        Map.ofList [
            AutoMinerKind.Monkey, 1.15m
            AutoMinerKind.RestingYouth, 1.18m
            AutoMinerKind.Gpu, 1.22m
        ]

    let private upgradeGrowthRates =
        Map.ofList [
            UpgradeKind.ManualDifficultyReduction, 1.45m
            UpgradeKind.AutoMinerEfficiency, 1.55m
            UpgradeKind.MarketAnalysis, 1.6m
        ]

    let private autoMinerGrowth kind =
        Map.tryFind kind autoMinerGrowthRates |> Option.defaultValue 1.2m

    let private upgradeGrowth kind =
        Map.tryFind kind upgradeGrowthRates |> Option.defaultValue 1.5m

    let autoMinerNextCost kind (state: GameState) =
        state.AutoMiners
        |> Map.tryFind kind
        |> Option.map (fun miner -> roundCurrency miner.NextCostCoins)

    let canAffordAutoMiner kind (state: GameState) =
        autoMinerNextCost kind state
        |> Option.exists (fun cost -> state.Economy.Coins >= cost)

    let buyAutoMiner kind (state: GameState) =
        match Map.tryFind kind state.AutoMiners with
        | None -> state
        | Some miner ->
            let cost = roundCurrency miner.NextCostCoins

            if state.Economy.Coins < cost then
                state
            else
                let nextCost = roundCurrency (cost * autoMinerGrowth kind)
                let updatedMiner =
                    { miner with
                        Owned = miner.Owned + 1
                        NextCostCoins = nextCost }

                { state with
                    Economy = { state.Economy with Coins = state.Economy.Coins - cost }
                    AutoMiners = state.AutoMiners |> Map.add kind updatedMiner }

    let upgradeNextCost kind (state: GameState) =
        state.Upgrades
        |> Map.tryFind kind
        |> Option.map (fun upgrade -> roundCurrency upgrade.NextCostCash)

    let canAffordUpgrade kind (state: GameState) =
        upgradeNextCost kind state
        |> Option.exists (fun cost -> state.Economy.Cash >= cost)

    let buyUpgrade kind (state: GameState) =
        match Map.tryFind kind state.Upgrades with
        | None -> state
        | Some upgrade ->
            let cost = roundCurrency upgrade.NextCostCash

            if state.Economy.Cash < cost then
                state
            else
                let nextCost = roundCurrency (cost * upgradeGrowth kind)
                let updatedUpgrade =
                    { upgrade with
                        Level = upgrade.Level + 1
                        NextCostCash = nextCost }

                { state with
                    Economy = { state.Economy with Cash = state.Economy.Cash - cost }
                    Upgrades = state.Upgrades |> Map.add kind updatedUpgrade }

    let canBuySpaceshipToMars (state: GameState) =
        state.WinState = WinState.NotWon
        && state.Economy.Cash >= spaceshipToMars.CostCash

    let buySpaceshipToMars (state: GameState) =
        if canBuySpaceshipToMars state then
            { state with
                Economy = { state.Economy with Cash = state.Economy.Cash - spaceshipToMars.CostCash }
                WinState = spaceshipToMars.ClearTrigger },
            true
        else
            state, false

    let sellCoins quantity (state: GameState) =
        let safeQuantity = max 0m quantity
        let sold = min safeQuantity state.Economy.Coins
        let proceeds = sold * state.Economy.CoinPrice

        { state with
            Economy =
                { state.Economy with
                    Coins = state.Economy.Coins - sold
                    Cash = state.Economy.Cash + proceeds } }
