module KaiCoinMiner.App.Tests.StateContractsTests

open System
open Expecto
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.Infrastructure
open KaiCoinMiner.App.Tests.UiInteractionTests

[<Tests>]
let updateTests =
    testList "KAIM-002 update contracts" [
        testCase "mining increments lifetime coins separately from spendable balance" <| fun _ ->
            let starting =
                { State.initial with
                    Economy =
                        { State.initial.Economy with
                            Coins = 10m
                            LifetimeMinedCoins = 90m } }

            let minedState, effects = Update.update (Msg.MineRequested 10m) starting

            Expect.equal minedState.Economy.Coins 20m "spendable balance should increase"
            Expect.equal minedState.Economy.LifetimeMinedCoins 100m "lifetime mined coins should track milestones"
            Expect.isEmpty effects "mining should be pure"

        testCase "save and load messages are isolated as effects" <| fun _ ->
            let stateAfterSave, saveEffects = Update.update Msg.SaveRequested State.initial
            let stateAfterLoad, loadEffects = Update.update Msg.LoadRequested State.initial

            Expect.equal stateAfterSave State.initial "save should not mutate state"
            Expect.equal stateAfterLoad State.initial "load should not mutate state"
            Expect.equal saveEffects [ Update.Effect.SaveState ] "save must be isolated as side effect"
            Expect.equal loadEffects [ Update.Effect.LoadState ] "load must be isolated as side effect"

        testCase "sell confirmation uses current coin price" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy =
                        { State.initial.Economy with
                            Coins = 40m
                            Cash = 5m
                            CoinPrice = 2.5m }
                    Ui = { State.initial.Ui with PendingSellQuantity = 8m } }

            let nextState, _ = Update.update Msg.SellCoinsConfirmed seeded

            Expect.equal nextState.Economy.Coins 32m "sold quantity should be deducted"
            Expect.equal nextState.Economy.Cash 25m "cash should increase by quantity * price"
            Expect.equal nextState.Ui.PendingSellQuantity 0m "pending quantity should be consumed"

        testCase "open exchange modal sets active input for modal" <| fun _ ->
            let nextState, _ = Update.update (Msg.SetExchangeModalOpen true) State.initial
            Expect.isTrue nextState.Ui.IsExchangeModalOpen "modal should open"
            Expect.equal nextState.Ui.ActiveInput InputTarget.ExchangeSellQuantity "active input should route to modal"
    ]

[<Tests>]
let saveTests =
    testList "KAIM-002 save contracts" [
        testCase "snapshot roundtrip preserves auto-miner and upgrade next costs" <| fun _ ->
            let seeded =
                { State.initial with
                    Economy = { State.initial.Economy with Coins = 1000m; Cash = 5000m } }

            let progressed =
                seeded
                |> Shop.buyAutoMiner AutoMinerKind.Monkey
                |> Shop.buyAutoMiner AutoMinerKind.Monkey
                |> Shop.buyUpgrade UpgradeKind.AutoMinerEfficiency

            let monkeyBefore = progressed.AutoMiners[AutoMinerKind.Monkey]
            let upgradeBefore = progressed.Upgrades[UpgradeKind.AutoMinerEfficiency]
            let loaded = progressed |> Save.toSnapshot |> Save.fromSnapshot
            let monkeyAfter = loaded.AutoMiners[AutoMinerKind.Monkey]
            let upgradeAfter = loaded.Upgrades[UpgradeKind.AutoMinerEfficiency]

            Expect.equal monkeyAfter.Owned monkeyBefore.Owned "owned quantity should persist"
            Expect.equal monkeyAfter.NextCostCoins monkeyBefore.NextCostCoins "auto-miner next cost should persist"
            Expect.equal upgradeAfter.Level upgradeBefore.Level "upgrade level should persist"
            Expect.equal upgradeAfter.NextCostCash upgradeBefore.NextCostCash "upgrade next cost should persist"

        testCase "legacy snapshot fallback keeps seeded next costs when saved values are invalid" <| fun _ ->
            let legacyLikeSnapshot : SaveSnapshot =
                { Coins = 100m
                  LifetimeMinedCoins = 100m
                  Cash = 100m
                  CoinPrice = 1m
                  ChallengeDifficulty = 1
                  AutoMiners =
                    [ { Kind = "Monkey"
                        Owned = 2
                        NextCostCoins = 0m } ]
                  Upgrades =
                    [ { Kind = "AutoMinerEfficiency"
                        Level = 1
                        NextCostCash = 0m } ]
                  Chart = []
                  WinState = "NotWon" }

            let loaded = Save.fromSnapshot legacyLikeSnapshot
            let monkey = loaded.AutoMiners[AutoMinerKind.Monkey]
            let autoMinerEfficiency = loaded.Upgrades[UpgradeKind.AutoMinerEfficiency]

            Expect.equal monkey.Owned 2 "owned count should still restore"
            Expect.equal monkey.NextCostCoins State.initial.AutoMiners[AutoMinerKind.Monkey].NextCostCoins "invalid legacy auto-miner next cost should fallback"
            Expect.equal autoMinerEfficiency.Level 1 "upgrade level should still restore"
            Expect.equal autoMinerEfficiency.NextCostCash State.initial.Upgrades[UpgradeKind.AutoMinerEfficiency].NextCostCash "invalid legacy upgrade next cost should fallback"

        testCase "snapshot roundtrip preserves core economy and progression fields" <| fun _ ->
            let original =
                { State.initial with
                    Economy =
                        { State.initial.Economy with
                            Coins = 123.45m
                            LifetimeMinedCoins = 500m
                            Cash = 999m
                            CoinPrice = 7.25m }
                    Challenge = { State.initial.Challenge with Difficulty = 4 }
                    WinState = WinState.Launching }

            let loaded = Save.fromSnapshot (Save.toSnapshot original)

            Expect.equal loaded.Economy.Coins original.Economy.Coins "coins should roundtrip"
            Expect.equal loaded.Economy.LifetimeMinedCoins original.Economy.LifetimeMinedCoins "lifetime mined coins should roundtrip"
            Expect.equal loaded.Economy.Cash original.Economy.Cash "cash should roundtrip"
            Expect.equal loaded.Economy.CoinPrice original.Economy.CoinPrice "price should roundtrip"
            Expect.equal loaded.Challenge.Difficulty original.Challenge.Difficulty "difficulty should roundtrip"
            Expect.equal loaded.WinState original.WinState "win state should roundtrip"
            Expect.isFalse loaded.Ui.IsExchangeModalOpen "ui-only state should not be loaded from persistence"

        testCase "save and load to local json file succeeds" <| fun _ ->
            let savePath =
                IO.Path.Combine(IO.Path.GetTempPath(), $"kaicoin-{Guid.NewGuid()}.json")

            let expected =
                { State.initial with
                    Economy =
                        { State.initial.Economy with
                            Coins = 77m
                            LifetimeMinedCoins = 155m
                            Cash = 33m
                            CoinPrice = 1.5m } }

            Save.save savePath expected
            let actual = Save.tryLoad savePath

            Expect.isTrue (actual.IsSome) "saved data should be loadable"

            let loaded = actual.Value
            Expect.equal loaded.Economy.Coins expected.Economy.Coins "coins should persist"
            Expect.equal loaded.Economy.LifetimeMinedCoins expected.Economy.LifetimeMinedCoins "lifetime mined coins should persist"

            if IO.File.Exists savePath then
                IO.File.Delete savePath

        testCase "default save base path uses LocalApplicationData on Windows" <| fun _ ->
            let localPath = IO.Path.Combine("C:\\Users", "Alice", "AppData", "Local")
            let actual = Save.getDefaultSaveBasePath true false localPath "/Users/alice" (Some "/tmp/xdg")

            Expect.equal actual localPath "Windows should use LocalApplicationData"

        testCase "default save base path uses Library Application Support on macOS" <| fun _ ->
            let homePath = "/Users/alice"
            let actual = Save.getDefaultSaveBasePath false true "/tmp/localapp" homePath None
            let expected = IO.Path.Combine(homePath, "Library", "Application Support")

            Expect.equal actual expected "macOS should use ~/Library/Application Support"

        testCase "default save base path uses XDG_DATA_HOME on Unix-like systems" <| fun _ ->
            let xdgPath = "/home/alice/.local/share"
            let actual = Save.getDefaultSaveBasePath false false "/tmp/localapp" "/home/alice" (Some xdgPath)

            Expect.equal actual xdgPath "Unix-like systems should prefer XDG_DATA_HOME"

        testCase "default save base path falls back when XDG_DATA_HOME is missing" <| fun _ ->
            let fallback = "/tmp/localapp"
            let actual = Save.getDefaultSaveBasePath false false fallback "/home/alice" None

            Expect.equal actual fallback "missing XDG_DATA_HOME should fallback to LocalApplicationData"
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs
        []
        argv
        (testList
            "all"
            [ updateTests
              saveTests
              uiInteractionTests
              GameplayLoopTests.gameplayLoopTests
              EconomySystemsTests.economySystemsTests ])
