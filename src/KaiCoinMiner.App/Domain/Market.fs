namespace KaiCoinMiner.App.Domain

open Common

module Market =

    let private config () = GameConfig.ensureLoaded ()

    let private randomWalkPercent nonce =
        let step = positiveModulo (nonce * 37 + 11) 11
        decimal (step - 5) / 100m

    let private applyPriceMultiplier currentPrice multiplier =
        let minPrice = (config ()).Global.MinCoinPrice
        clampMin minPrice (currentPrice * multiplier)

    let private appendHeadline headline ticker =
        headline :: ticker |> List.truncate 8

    let private marketAnalysisLevel (state: GameState) =
        let cfg = config ()
        GameConfig.findUpgradeByEffectType "marketAnalysis" cfg
        |> Option.bind (fun (key, _) -> state.Upgrades |> Map.tryFind key)
        |> Option.map (fun upgrade -> max 0 upgrade.Level)
        |> Option.defaultValue 0

    let private marketVolatilityMultiplier level =
        let cfg = config ()
        let effectPerLevel =
            GameConfig.findUpgradeByEffectType "marketAnalysis" cfg
            |> Option.map (fun (_, c) -> c.EffectPerLevel)
            |> Option.defaultValue 0.08m
        let minDamp = cfg.Global.MarketAnalysisMinDamp
        let damped = 1m - (decimal level * effectPerLevel)
        damped |> clampMin minDamp |> clampMax 1m

    let private dampNewsImpact multiplier newsMultiplier =
        1m + ((newsMultiplier - 1m) * multiplier)

    let setCoinPrice price (state: GameState) =
        let minPrice = (config ()).Global.MinCoinPrice
        { state with Economy = { state.Economy with CoinPrice = clampMin minPrice price } }

    let step deltaSeconds nonce (state: GameState) =
        let seconds = clampMin 0m deltaSeconds
        let walk = randomWalkPercent nonce
        let volatilityMultiplier = state |> marketAnalysisLevel |> marketVolatilityMultiplier
        let adjustedWalk = walk * volatilityMultiplier
        let walkScale = if seconds <= 0m then 0m else min 2m seconds
        let walkedPrice = applyPriceMultiplier state.Economy.CoinPrice (1m + (adjustedWalk * walkScale))
        let remainingUntilNews = state.Market.SecondsUntilNextNews - seconds

        if remainingUntilNews <= 0m then
            let news = News.generate nonce
            let newsImpactMultiplier = dampNewsImpact volatilityMultiplier news.PriceImpactMultiplier
            let priceAfterNews = applyPriceMultiplier walkedPrice newsImpactMultiplier
            let nextPressure = clampInt -4 8 (state.Market.DifficultyPressure + news.DifficultyPressureDelta)

            { state with
                Economy = { state.Economy with CoinPrice = priceAfterNews }
                Market =
                    { state.Market with
                        ActiveNews = Some news.Headline
                        NewsTicker = appendHeadline news.Headline state.Market.NewsTicker
                        SecondsUntilNextNews = news.CadenceSeconds
                        DifficultyPressure = nextPressure
                        LastPriceDrift = adjustedWalk } }
        else
            { state with
                Economy = { state.Economy with CoinPrice = walkedPrice }
                Market =
                    { state.Market with
                        ActiveNews = None
                        SecondsUntilNextNews = remainingUntilNews
                        LastPriceDrift = adjustedWalk } }
