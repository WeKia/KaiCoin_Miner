namespace KaiCoinMiner.App.Views

open KaiCoinMiner.App.Domain

type ShopDescriptor =
    { Label: string
      Emoji: string
      Specs: string
      WittyDescription: string }

module ShopCatalog =
    let private config () = GameConfig.ensureLoaded ()

    let autoMinerDescriptors =
        let cfg = config ()
        cfg.AutoMiners
        |> Map.map (fun key c ->
            { Label = c.Label
              Emoji = c.Emoji
              Specs = $"Output: {c.OutputPerSecond} KaiCoin/sec"
              WittyDescription = c.Description })

    let upgradeDescriptors =
        let cfg = config ()
        cfg.Upgrades
        |> Map.map (fun key c ->
            let specs =
                match c.EffectType with
                | "autoMinerEfficiency" -> $"+{c.EffectPerLevel * 100m}%% auto-miner output per level"
                | _ -> c.Description
            { Label = c.Label
              Emoji = c.Emoji
              Specs = specs
              WittyDescription = c.Description })

    let spaceshipDescriptor =
        { Label = Shop.spaceshipToMars.Name
          Emoji = "🚀"
          Specs = $"Cost: ${Shop.spaceshipCost ()}"
          WittyDescription = "From dorm room miner to Martian legend." }

    let autoMinerList =
        let cfg = config ()
        autoMinerDescriptors
        |> Map.toList
        |> List.sortBy (fun (key, _) ->
            GameConfig.tryGetAutoMiner key cfg
            |> Option.map (fun c -> c.InitialCost)
            |> Option.defaultValue 0m)

    let upgradeList =
        let cfg = config ()
        upgradeDescriptors
        |> Map.toList
        |> List.sortBy (fun (key, _) ->
            GameConfig.tryGetUpgrade key cfg
            |> Option.map (fun c -> c.InitialCost)
            |> Option.defaultValue 0m)
