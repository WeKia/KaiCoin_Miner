namespace KaiCoinMiner.App.Domain

open System
open Common

module Progression =

    let private config () = GameConfig.ensureLoaded ()

    let milestoneSize =
        (config ()).Global.MilestoneSize

    let difficultyFromLifetimeMined (lifetimeMinedCoins: decimal) =
        let safeLifetime = clampMin 0m lifetimeMinedCoins
        let tier = int (System.Decimal.Truncate(safeLifetime / milestoneSize))
        1 + tier

    let manualDifficultyReductionLevel (state: GameState) =
        let cfg = config ()
        let effectPerLevel =
            GameConfig.findUpgradeByEffectType "manualDifficultyReduction" cfg
            |> Option.map (fun (_, c) -> c.EffectPerLevel)
            |> Option.defaultValue 1m
        let upgradeKey =
            GameConfig.findUpgradeByEffectType "manualDifficultyReduction" cfg
            |> Option.map fst
        match upgradeKey with
        | Some key ->
            state.Upgrades
            |> Map.tryFind key
            |> Option.map (fun upgrade -> int (decimal (clampMin 0 upgrade.Level) * effectPerLevel))
            |> Option.defaultValue 0
        | None -> 0

    let manualDifficulty (state: GameState) =
        let baseDifficulty = difficultyFromLifetimeMined state.Economy.LifetimeMinedCoins
        let reduction = manualDifficultyReductionLevel state
        let pressure = clampInt -4 8 state.Market.DifficultyPressure
        clampMin 1 (baseDifficulty - reduction + pressure)

    let autoMinerEfficiencyMultiplier (state: GameState) =
        let cfg = config ()
        let effectPerLevel =
            GameConfig.findUpgradeByEffectType "autoMinerEfficiency" cfg
            |> Option.map (fun (_, c) -> c.EffectPerLevel)
            |> Option.defaultValue 0.25m
        let upgradeKey =
            GameConfig.findUpgradeByEffectType "autoMinerEfficiency" cfg
            |> Option.map fst
        let level =
            match upgradeKey with
            | Some key ->
                state.Upgrades
                |> Map.tryFind key
                |> Option.map (fun upgrade -> clampMin 0 upgrade.Level)
                |> Option.defaultValue 0
            | None -> 0

        1m + (decimal level * effectPerLevel)
