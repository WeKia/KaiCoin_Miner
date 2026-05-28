namespace KaiCoinMiner.App.Domain

open System
open System.IO
open System.Text.Json

type AutoMinerConfig =
    { OutputPerSecond: decimal
      InitialCost: decimal
      GrowthRate: decimal
      Label: string
      Emoji: string
      Description: string }

type UpgradeConfig =
    { InitialCost: decimal
      GrowthRate: decimal
      Label: string
      Emoji: string
      Description: string
      EffectType: string
      EffectPerLevel: decimal }

type GlobalConfig =
    { MilestoneSize: decimal
      MinCoinPrice: decimal
      MarketAnalysisMinDamp: decimal
      SpaceshipCost: decimal }

type GameConfig =
    { AutoMiners: Map<string, AutoMinerConfig>
      Upgrades: Map<string, UpgradeConfig>
      Global: GlobalConfig }

module GameConfig =
    let private resolvePath filename =
        Path.Combine(AppContext.BaseDirectory, "Assets", "config", filename)

    let private loadJsonDocument filename =
        let path = resolvePath filename
        try
            if File.Exists(path) then
                let json = File.ReadAllText(path)
                Some (JsonDocument.Parse(json))
            else
                None
        with
        | _ -> None

    let private readDecimal (el: JsonElement) (name: string) =
        el.GetProperty(name).GetDecimal()

    let private readString (el: JsonElement) (name: string) =
        el.GetProperty(name).GetString()

    let private readAutoMinerConfig (el: JsonElement) =
        { OutputPerSecond = readDecimal el "outputPerSecond"
          InitialCost = readDecimal el "initialCost"
          GrowthRate = readDecimal el "growthRate"
          Label = readString el "label"
          Emoji = readString el "emoji"
          Description = readString el "description" }

    let private readUpgradeConfig (el: JsonElement) =
        { InitialCost = readDecimal el "initialCost"
          GrowthRate = readDecimal el "growthRate"
          Label = readString el "label"
          Emoji = readString el "emoji"
          Description = readString el "description"
          EffectType = readString el "effectType"
          EffectPerLevel = readDecimal el "effectPerLevel" }

    let private readGlobalConfig (el: JsonElement) =
        { MilestoneSize = readDecimal el "milestoneSize"
          MinCoinPrice = readDecimal el "minCoinPrice"
          MarketAnalysisMinDamp = readDecimal el "marketAnalysisMinDamp"
          SpaceshipCost = readDecimal el "spaceshipCost" }

    let private loadGameConfig () =
        match loadJsonDocument "game-config.json" with
        | None -> None
        | Some doc ->
            let root = doc.RootElement
            let autoMiners =
                root.GetProperty("autoMiners").EnumerateObject()
                |> Seq.map (fun prop -> prop.Name, readAutoMinerConfig prop.Value)
                |> Map.ofSeq
            let upgrades =
                root.GetProperty("upgrades").EnumerateObject()
                |> Seq.map (fun prop -> prop.Name, readUpgradeConfig prop.Value)
                |> Map.ofSeq
            let globalCfg = readGlobalConfig (root.GetProperty("global"))
            Some { AutoMiners = autoMiners; Upgrades = upgrades; Global = globalCfg }

    let mutable private cachedConfig: GameConfig option = None

    let load () : GameConfig option =
        match cachedConfig with
        | Some config -> Some config
        | None ->
            let config = loadGameConfig ()
            cachedConfig <- config
            config

    let tryGetAutoMiner (key: string) (config: GameConfig) =
        Map.tryFind key config.AutoMiners

    let tryGetUpgrade (key: string) (config: GameConfig) =
        Map.tryFind key config.Upgrades

    let getAutoMinerOrDefault (key: string) (config: GameConfig) =
        match tryGetAutoMiner key config with
        | Some cfg -> cfg
        | None ->
            { OutputPerSecond = 0.1m
              InitialCost = 10m
              GrowthRate = 1.2m
              Label = key
              Emoji = "?"
              Description = "" }

    let getUpgradeOrDefault (key: string) (config: GameConfig) =
        match tryGetUpgrade key config with
        | Some cfg -> cfg
        | None ->
            { InitialCost = 100m
              GrowthRate = 1.5m
              Label = key
              Emoji = "?"
              Description = ""
              EffectType = "none"
              EffectPerLevel = 0m }

    let findUpgradeByEffectType effectType (config: GameConfig) =
        config.Upgrades
        |> Map.tryPick (fun key cfg ->
            if cfg.EffectType = effectType then Some (key, cfg) else None)

    let defaultConfig () =
        { AutoMiners =
            [ ("Monkey", { OutputPerSecond = 0.1m; InitialCost = 10m; GrowthRate = 1.15m; Label = "Monkey"; Emoji = "🐵"; Description = "Keeps mining even while you stare at math." })
              ("RestingYouth", { OutputPerSecond = 1m; InitialCost = 100m; GrowthRate = 1.18m; Label = "Resting Youth"; Emoji = "😴"; Description = "Power naps, passive gains, zero regrets." })
              ("Gpu", { OutputPerSecond = 10m; InitialCost = 1000m; GrowthRate = 1.22m; Label = "GPU"; Emoji = "🎮"; Description = "Runs hot, prints coins, terrifies your electric bill." }) ]
            |> Map.ofList
          Upgrades =
            [ ("ManualDifficultyReduction", { InitialCost = 100m; GrowthRate = 1.45m; Label = "Calculator Patch"; Emoji = "🧮"; Description = "Makes your brain look overclocked."; EffectType = "manualDifficultyReduction"; EffectPerLevel = 1m })
              ("AutoMinerEfficiency", { InitialCost = 250m; GrowthRate = 1.55m; Label = "Cooling Upgrade"; Emoji = "❄️"; Description = "Because melted fans mine nothing."; EffectType = "autoMinerEfficiency"; EffectPerLevel = 0.25m })
              ("MarketAnalysis", { InitialCost = 400m; GrowthRate = 1.6m; Label = "Insider Spreadsheet"; Emoji = "📊"; Description = "A very legal amount of market foresight."; EffectType = "marketAnalysis"; EffectPerLevel = 0.08m }) ]
            |> Map.ofList
          Global =
            { MilestoneSize = 100m
              MinCoinPrice = 0.0001m
              MarketAnalysisMinDamp = 0.35m
              SpaceshipCost = 250000m } }

    let ensureLoaded () =
        match load () with
        | Some config -> config
        | None -> defaultConfig ()
