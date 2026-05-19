namespace KaiCoinMiner.App.Domain

open System

module Progression =
    let private clampMin minimum value =
        if value < minimum then minimum else value

    let private clampIntMin minimum value =
        if value < minimum then minimum else value

    let private clampInt rangeMin rangeMax value =
        value |> max rangeMin |> min rangeMax

    let milestoneSize = 100m

    let difficultyFromLifetimeMined (lifetimeMinedCoins: decimal) =
        let safeLifetime = clampMin 0m lifetimeMinedCoins
        let tier = int (Decimal.Truncate(safeLifetime / milestoneSize))
        1 + tier

    let manualDifficultyReductionLevel (state: GameState) =
        state.Upgrades
        |> Map.tryFind UpgradeKind.ManualDifficultyReduction
        |> Option.map (fun upgrade -> clampIntMin 0 upgrade.Level)
        |> Option.defaultValue 0

    let manualDifficulty (state: GameState) =
        let baseDifficulty = difficultyFromLifetimeMined state.Economy.LifetimeMinedCoins
        let reduction = manualDifficultyReductionLevel state
        let pressure = clampInt -4 8 state.Market.DifficultyPressure
        clampIntMin 1 (baseDifficulty - reduction + pressure)

    let autoMinerEfficiencyMultiplier (state: GameState) =
        let level =
            state.Upgrades
            |> Map.tryFind UpgradeKind.AutoMinerEfficiency
            |> Option.map (fun upgrade -> clampIntMin 0 upgrade.Level)
            |> Option.defaultValue 0

        1m + (decimal level * 0.25m)
