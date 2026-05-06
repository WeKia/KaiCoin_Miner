namespace KaiCoinMiner.App.Views

open KaiCoinMiner.App.Domain

type ShopDescriptor =
    { Label: string
      Specs: string
      WittyDescription: string }

module ShopCatalog =
    let autoMinerDescriptors =
        Map.ofList [
            AutoMinerKind.Monkey,
            { Label = "Monkey"
              Specs = "Output: 0.1 KaiCoin/sec"
              WittyDescription = "Keeps mining even while you stare at math." }
            AutoMinerKind.RestingYouth,
            { Label = "Resting Youth"
              Specs = "Output: 1 KaiCoin/sec"
              WittyDescription = "Power naps, passive gains, zero regrets." }
            AutoMinerKind.Gpu,
            { Label = "GPU"
              Specs = "Output: 10 KaiCoin/sec"
              WittyDescription = "Runs hot, prints coins, terrifies your electric bill." }
        ]

    let upgradeDescriptors =
        Map.ofList [
            UpgradeKind.ManualDifficultyReduction,
            { Label = "Calculator Patch"
              Specs = "Lowers manual challenge difficulty"
              WittyDescription = "Makes your brain look overclocked." }
            UpgradeKind.AutoMinerEfficiency,
            { Label = "Cooling Upgrade"
              Specs = "+25% auto-miner output per level"
              WittyDescription = "Because melted fans mine nothing." }
            UpgradeKind.MarketAnalysis,
            { Label = "Insider Spreadsheet"
              Specs = "Reduces random market volatility"
              WittyDescription = "A very legal amount of market foresight." }
        ]

    let spaceshipDescriptor =
        { Label = Shop.spaceshipToMars.Name
          Specs = $"Cost: ${Shop.spaceshipToMars.CostCash}"
          WittyDescription = "From dorm room miner to Martian legend." }
