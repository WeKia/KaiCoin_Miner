module KaiCoinMiner.App.Tests.EconomySystemsTests

open Expecto
open KaiCoinMiner.App.Domain

[<Tests>]
let economySystemsTests =
    testList "KAIM-004 economy systems" [
        testCase "buy auto-miner updates owned, coin balance, and next cost" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy = { State.initial.Economy with Coins = 25m } }

            let purchased = Shop.buyAutoMiner "Monkey" seeded
            let monkey = purchased.AutoMiners["Monkey"]

            Expect.equal purchased.Economy.Coins 15m "purchase should deduct current cost"
            Expect.equal monkey.Owned 1 "owned quantity should increment"
            Expect.isGreaterThan monkey.NextCostCoins 10m "next cost should increase"

        testCase "buy upgrade updates level and next cash cost" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy = { State.initial.Economy with Cash = 300m } }

            let purchased = Shop.buyUpgrade "AutoMinerEfficiency" seeded
            let upgrade = purchased.Upgrades["AutoMinerEfficiency"]

            Expect.equal purchased.Economy.Cash 50m "upgrade purchase should deduct current cost"
            Expect.equal upgrade.Level 1 "upgrade level should increment"
            Expect.isGreaterThan upgrade.NextCostCash 250m "next upgrade cost should increase"

        testCase "market tick performs random-walk movement" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy = { State.initial.Economy with CoinPrice = 10m }
                    Market = { State.initial.Market with SecondsUntilNextNews = 45m } }

            let stepped = Market.step 1m 17 seeded

            Expect.notEqual stepped.Economy.CoinPrice seeded.Economy.CoinPrice "price should move via random walk"
            Expect.isNone stepped.Market.ActiveNews "news should not trigger before cadence elapses"

        testCase "news cadence remains in 30-60 second range" <| fun _ ->
            let cadences =
                [ 0 .. 120 ]
                |> List.map News.cadenceSeconds

            for cadence in cadences do
                Expect.isGreaterThanOrEqual cadence 30m "cadence lower bound should hold"
                Expect.isLessThanOrEqual cadence 60m "cadence upper bound should hold"

        testCase "news-driven market step can affect both price and difficulty pressure" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy = { State.initial.Economy with CoinPrice = 5m }
                    Market =
                        { State.initial.Market with
                            SecondsUntilNextNews = 0.1m
                            DifficultyPressure = 0 } }

            let stepped = Market.step 1m 3 seeded

            Expect.isSome stepped.Market.ActiveNews "a news item should be generated"
            Expect.notEqual stepped.Economy.CoinPrice seeded.Economy.CoinPrice "news should alter market price dynamics"
            Expect.notEqual stepped.Market.DifficultyPressure 0 "news should apply difficulty pressure effects"

        testCase "market analysis upgrade level has deterministic effect on market drift" <| fun _ ->
            let withMarketAnalysis =
                { State.initial with
                    Upgrades =
                        State.initial.Upgrades
                        |> Map.add
                            "MarketAnalysis"
                            { State.initial.Upgrades["MarketAnalysis"] with
                                Level = 5 }
                    Economy = { State.initial.Economy with CoinPrice = 10m }
                    Market = { State.initial.Market with SecondsUntilNextNews = 45m } }

            let withoutMarketAnalysis =
                { State.initial with
                    Economy = { State.initial.Economy with CoinPrice = 10m }
                    Market = { State.initial.Market with SecondsUntilNextNews = 45m } }

            let steppedWith = Market.step 1m 42 withMarketAnalysis
            let steppedWithout = Market.step 1m 42 withoutMarketAnalysis

            Expect.notEqual steppedWith.Economy.CoinPrice steppedWithout.Economy.CoinPrice "market analysis should influence stepped price"
            Expect.isLessThan (abs steppedWith.Market.LastPriceDrift) (abs steppedWithout.Market.LastPriceDrift) "market analysis should reduce drift magnitude"

        testCase "chart render points are derived from game state and fit drawing bounds" <| fun _ ->
            let withPoints =
                [ 0L .. 4L ]
                |> List.fold (fun state tick -> Charts.appendPricePoint tick (decimal (tick + 1L)) state) State.initial

            let points = Charts.toRenderPoints 200.0 100.0 16 withPoints

            Expect.equal points.Length 5 "all source points should be available for rendering"
            points |> List.iter (fun point ->
                Expect.isGreaterThanOrEqual point.X 0.0 "x should be non-negative"
                Expect.isLessThanOrEqual point.X 200.0 "x should fit the target width"
                Expect.isGreaterThanOrEqual point.Y 0.0 "y should be non-negative"
                Expect.isLessThanOrEqual point.Y 100.0 "y should fit the target height")

        testCase "chart context reports placeholder for fewer than two points" <| fun _ ->
            let seeded =
                State.initial
                |> Charts.appendPricePoint 1L 12.5m

            let context = Charts.describeContext 64 seeded

            Expect.isTrue context.ShowPlaceholder "single-point series should show placeholder"
            Expect.isNone context.MinPrice "single-point series should not show min label"
            Expect.isNone context.MaxPrice "single-point series should not show max label"

        testCase "chart context exposes min and max labels for renderable series" <| fun _ ->
            let seeded =
                State.initial
                |> Charts.appendPricePoint 1L 12.5m
                |> Charts.appendPricePoint 2L 10m
                |> Charts.appendPricePoint 3L 18.75m

            let context = Charts.describeContext 64 seeded

            Expect.isFalse context.ShowPlaceholder "multi-point series should render contextual labels"
            Expect.equal context.MinPrice (Some 10m) "min price should be available for label"
            Expect.equal context.MaxPrice (Some 18.75m) "max price should be available for label"

        testCase "spaceship to mars purchase triggers launch clear hook" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy = { State.initial.Economy with Cash = Shop.spaceshipCost () } }

            let launched, didLaunch = Shop.buySpaceshipToMars seeded

            Expect.isTrue didLaunch "launch purchase should return true trigger"
            Expect.equal launched.WinState Shop.spaceshipToMars.ClearTrigger "win state should move to launch trigger"
    ]
