namespace KaiCoinMiner.App.Domain

type NewsEvent =
    { Headline: string
      PriceImpactMultiplier: decimal
      DifficultyPressureDelta: int
      CadenceSeconds: decimal }

module News =
    let private positiveModulo value modulus =
        ((value % modulus) + modulus) % modulus

    let cadenceSeconds nonce =
        decimal (30 + positiveModulo nonce 31)

    let private catalog =
        [ { Headline = "Whales buy the dip"; PriceImpactMultiplier = 1.08m; DifficultyPressureDelta = 0; CadenceSeconds = 0m }
          { Headline = "Power grid strain hits mining farms"; PriceImpactMultiplier = 0.96m; DifficultyPressureDelta = 2; CadenceSeconds = 0m }
          { Headline = "Campus hackathon boosts adoption"; PriceImpactMultiplier = 1.05m; DifficultyPressureDelta = 1; CadenceSeconds = 0m }
          { Headline = "Regulatory rumor cools traders"; PriceImpactMultiplier = 0.93m; DifficultyPressureDelta = 3; CadenceSeconds = 0m }
          { Headline = "ASIC shipment delays continue"; PriceImpactMultiplier = 0.98m; DifficultyPressureDelta = 2; CadenceSeconds = 0m }
          { Headline = "Open-source miner patch improves yields"; PriceImpactMultiplier = 1.02m; DifficultyPressureDelta = -1; CadenceSeconds = 0m }
          { Headline = "Speculators rotate into KaiCoin"; PriceImpactMultiplier = 1.07m; DifficultyPressureDelta = 1; CadenceSeconds = 0m }
          { Headline = "Network congestion frustrates traders"; PriceImpactMultiplier = 0.95m; DifficultyPressureDelta = 1; CadenceSeconds = 0m } ]

    let generate nonce =
        let index = positiveModulo nonce catalog.Length
        let selected = catalog[index]

        { selected with
            CadenceSeconds = cadenceSeconds (nonce + 19) }
