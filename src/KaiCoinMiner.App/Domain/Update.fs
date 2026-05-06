namespace KaiCoinMiner.App.Domain

open System

module Update =
    type Effect =
        | SaveState
        | LoadState

    let private clampMin minimum value =
        if value < minimum then minimum else value

    let private addMinedCoins amount (state: GameState) =
        let mined = clampMin 0m amount

        { state with
            Economy =
                { state.Economy with
                    Coins = state.Economy.Coins + mined
                    LifetimeMinedCoins = state.Economy.LifetimeMinedCoins + mined } }

    let update (msg: Msg) (state: GameState) : GameState * Effect list =
        match msg with
        | MineRequested amount ->
            addMinedCoins amount state, []
        | AutoMinerTicked amount ->
            addMinedCoins amount state, []
        | Tick at ->
            let deltaSeconds =
                match state.Timer.LastTickAt with
                | Some previous when at > previous -> decimal (at.Subtract(previous).TotalSeconds)
                | _ -> 0m

            let minedState = Mining.applyAutoMining deltaSeconds state
            let marketState = Market.step deltaSeconds (int state.Timer.TickCount) minedState
            let nextTickCount = state.Timer.TickCount + 1L
            let chartState = Charts.appendPricePoint nextTickCount marketState.Economy.CoinPrice marketState

            let nextState =
                { chartState with
                    Timer =
                        { TickCount = nextTickCount
                          LastTickAt = Some at } }
                |> Mining.ensureChallenge (int state.Timer.TickCount)

            nextState, []
        | CoinPriceChanged price ->
            Market.setCoinPrice price state, []
        | NewsReceived headline ->
            let ticker =
                headline :: state.Market.NewsTicker
                |> List.truncate 8

            { state with
                Market =
                    { state.Market with
                        ActiveNews = Some headline
                        NewsTicker = ticker } },
            []
        | ChallengePresented (prompt, expectedAnswer, difficulty) ->
            { state with
                Challenge =
                    { Prompt = prompt
                      ExpectedAnswer = Some expectedAnswer
                      Difficulty = max 1 difficulty
                      LastWasCorrect = None
                      LastFeedback = None
                      LastRewardMessage = None } },
            []
        | ChallengeSubmitted answer ->
            Mining.submitAnswer answer (int state.Timer.TickCount) state, []
        | SetExchangeModalOpen isOpen ->
            { state with
                Ui =
                    { state.Ui with
                        IsExchangeModalOpen = isOpen
                        ActiveInput = if isOpen then ExchangeSellQuantity else MonitorAnswer } },
            []
        | SetActiveInput target ->
            { state with Ui = { state.Ui with ActiveInput = target } }, []
        | SetPendingSellQuantity quantity ->
            { state with Ui = { state.Ui with PendingSellQuantity = clampMin 0m quantity } }, []
        | SellCoinsConfirmed ->
            let soldState = Shop.sellCoins state.Ui.PendingSellQuantity state

            { soldState with Ui = { soldState.Ui with PendingSellQuantity = 0m } },
            []
        | BuyAutoMiner kind -> Shop.buyAutoMiner kind state, []
        | BuyUpgrade kind -> Shop.buyUpgrade kind state, []
        | BuySpaceshipToMars ->
            let nextState, _ = Shop.buySpaceshipToMars state
            nextState, []
        | AppendPricePoint point ->
            Charts.appendPricePoint point.Tick point.Price state, []
        | SetWon winState ->
            { state with WinState = winState }, []
        | SaveRequested ->
            state, [ SaveState ]
        | LoadRequested ->
            state, [ LoadState ]
        | Loaded (Some loadedState) ->
            { loadedState with IsInitialized = true }, []
        | Loaded None ->
            state, []
