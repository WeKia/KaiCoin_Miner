namespace KaiCoinMiner.App

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Markup.Xaml
open Avalonia.Media
open Avalonia.Threading
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.Infrastructure
open KaiCoinMiner.App.UI
open KaiCoinMiner.App.Views
open System
open System.Globalization
open System.IO

type MainWindow () as this = 
    inherit Window ()

    let savePath = Save.getDefaultSavePath ()

    let tickerSeparator = "  ||  "
    let greenBrush = SolidColorBrush(Color.Parse("#25C47A"))
    let redBrush = SolidColorBrush(Color.Parse("#D04C4C"))
    let warningBrush = SolidColorBrush(Color.Parse("#EAA43C"))
    let rocketTransform = TranslateTransform(0.0, 0.0)
    let timer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds(200.0))

    let mutable state = Mining.ensureChallenge 7 State.initial
    let mutable monitorText = ""
    let mutable exchangeText = ""
    let mutable tickerOffset = 0
    let mutable launchProgress = 0.0
    let mutable lastTransientAlert = ""
    let mutable previousModalOpen = false

    let coinsText = lazy (this.FindControl<TextBlock>("CoinsText"))
    let cashText = lazy (this.FindControl<TextBlock>("CashText"))
    let priceText = lazy (this.FindControl<TextBlock>("PriceText"))
    let newsTickerText = lazy (this.FindControl<TextBlock>("NewsTickerText"))
    let challengePromptText = lazy (this.FindControl<TextBlock>("ChallengePromptText"))
    let challengeDifficultyText = lazy (this.FindControl<TextBlock>("ChallengeDifficultyText"))
    let monitorAnswerBox = lazy (this.FindControl<TextBox>("MonitorAnswerBox"))
    let smartphoneButton = lazy (this.FindControl<Button>("SmartphoneButton"))
    let feedbackText = lazy (this.FindControl<TextBlock>("FeedbackText"))
    let rewardMessageText = lazy (this.FindControl<TextBlock>("RewardMessageText"))
    let alertText = lazy (this.FindControl<TextBlock>("AlertText"))
    let monkeyButton = lazy (this.FindControl<Button>("MonkeyButton"))
    let youthButton = lazy (this.FindControl<Button>("YouthButton"))
    let gpuButton = lazy (this.FindControl<Button>("GpuButton"))
    let manualUpgradeButton = lazy (this.FindControl<Button>("ManualUpgradeButton"))
    let efficiencyUpgradeButton = lazy (this.FindControl<Button>("EfficiencyUpgradeButton"))
    let marketUpgradeButton = lazy (this.FindControl<Button>("MarketUpgradeButton"))
    let spaceshipButton = lazy (this.FindControl<Button>("SpaceshipButton"))

    let exchangeOverlay = lazy (this.FindControl<Border>("ExchangeOverlay"))
    let closeExchangeButton = lazy (this.FindControl<Button>("CloseExchangeButton"))
    let exchangePricePolyline = lazy (this.FindControl<Polyline>("ExchangePricePolyline"))
    let exchangeGuideTop = lazy (this.FindControl<Border>("ExchangeGuideTop"))
    let exchangeGuideMid = lazy (this.FindControl<Border>("ExchangeGuideMid"))
    let exchangeGuideBottom = lazy (this.FindControl<Border>("ExchangeGuideBottom"))
    let exchangeChartPlaceholder = lazy (this.FindControl<TextBlock>("ExchangeChartPlaceholder"))
    let exchangeMinPriceText = lazy (this.FindControl<TextBlock>("ExchangeMinPriceText"))
    let exchangeMaxPriceText = lazy (this.FindControl<TextBlock>("ExchangeMaxPriceText"))
    let sellQuantityBox = lazy (this.FindControl<TextBox>("SellQuantityBox"))
    let sellButton = lazy (this.FindControl<Button>("SellButton"))
    let exchangeHintText = lazy (this.FindControl<TextBlock>("ExchangeHintText"))

    let launchOverlay = lazy (this.FindControl<Border>("LaunchOverlay"))
    let launchMessageText = lazy (this.FindControl<TextBlock>("LaunchMessageText"))
    let rocketVisual = lazy (this.FindControl<Border>("RocketVisual"))

    let tryParseDecimalInvariant (value: string) =
        let mutable parsed = 0m
        let ok = Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, &parsed)
        ok, parsed

    let routeFocusToActiveInput () =
        if state.Ui.IsExchangeModalOpen then
            sellQuantityBox.Value.Focus() |> ignore
        else
            monitorAnswerBox.Value.Focus() |> ignore

    let setButtonStyle (button: Button) isAffordable content tooltip =
        let status = if isAffordable then "READY" else "LOCKED"
        button.Content <- $"{content} | {status}"
        button.Foreground <- if isAffordable then greenBrush else redBrush
        ToolTip.SetTip(button, tooltip)

    let setListButtonStyle (button: Button) isAffordable label owned nextCost unitLabel tooltip =
        let content = Shell.formatListRowContent label owned nextCost unitLabel
        setButtonStyle button isAffordable content tooltip

    let setUpgradeTileButtonStyle (button: Button) isAffordable label level nextCostCash tooltip =
        let tile = Shell.describeUpgradeTile label level nextCostCash isAffordable
        let affordabilityBrush = if isAffordable then greenBrush else redBrush

        let headerText =
            TextBlock(
                Text = tile.Header,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap)

        let detailsText =
            TextBlock(
                Text = $"{tile.LevelText} | {tile.CostText}",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.9)

        let statusText =
            TextBlock(
                Text = tile.StatusText,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Right)

        let content = StackPanel(Spacing = 3.0)
        content.Children.Add(headerText) |> ignore
        content.Children.Add(detailsText) |> ignore
        content.Children.Add(statusText) |> ignore

        button.Content <- content
        button.Foreground <- affordabilityBrush
        ToolTip.SetTip(button, tooltip)

    let renderTicker (items: string list) =
        let baseTicker =
            if List.isEmpty items then
                "KaiCoin wire: waiting for the next market headline..."
            else
                items |> List.rev |> String.concat tickerSeparator

        if baseTicker.Length <= 90 then
            baseTicker
        else
            let looped = baseTicker + tickerSeparator + baseTicker
            let start = tickerOffset % baseTicker.Length
            looped.Substring(start, min 90 (looped.Length - start))

    let renderPricePolyline () =
        let renderPoints = Charts.toRenderPoints 580.0 150.0 64 state
        let chartContext = Charts.describeContext 64 state
        let points = Avalonia.Collections.AvaloniaList<Point>()

        let hasRenderableSeries = renderPoints.Length >= 2

        if hasRenderableSeries then
            renderPoints
            |> List.iter (fun p -> points.Add(Point(p.X, p.Y + 4.0)))

        exchangePricePolyline.Value.Points <- points
        exchangePricePolyline.Value.IsVisible <- hasRenderableSeries
        exchangeChartPlaceholder.Value.IsVisible <- chartContext.ShowPlaceholder
        exchangeGuideTop.Value.IsVisible <- not chartContext.ShowPlaceholder
        exchangeGuideMid.Value.IsVisible <- not chartContext.ShowPlaceholder
        exchangeGuideBottom.Value.IsVisible <- not chartContext.ShowPlaceholder
        exchangeMinPriceText.Value.Text <- chartContext.MinPrice |> Option.map (fun p -> $"Min ${p:F3}") |> Option.defaultValue ""
        exchangeMaxPriceText.Value.Text <- chartContext.MaxPrice |> Option.map (fun p -> $"Max ${p:F3}") |> Option.defaultValue ""

    let rec dispatch msg =
        let nextState, effects = Update.update msg state
        state <- nextState

        for effect in effects do
            match effect with
            | Update.Effect.SaveState ->
                let saveDir = Path.GetDirectoryName(savePath)
                if not (String.IsNullOrWhiteSpace(saveDir)) then
                    Directory.CreateDirectory(saveDir) |> ignore
                Save.save savePath state
                lastTransientAlert <- $"Saved progress to {savePath}."
            | Update.Effect.LoadState ->
                dispatch (Msg.Loaded (Save.tryLoad savePath))

        this.RefreshUi()

    and submitMonitorAnswer () =
        let mutable parsed = 0

        if Int32.TryParse(monitorText, &parsed) then
            dispatch (Msg.ChallengeSubmitted parsed)
            monitorText <- ""
            monitorAnswerBox.Value.Text <- ""
            lastTransientAlert <- ""
        else
            lastTransientAlert <- "Type a numeric answer before submitting."
            this.RefreshUi()

    and submitExchangeSell () =
        let normalized = exchangeText.Trim()

        let ok, parsed = tryParseDecimalInvariant normalized

        if ok && parsed > 0m then
            dispatch (Msg.SetPendingSellQuantity parsed)
            dispatch Msg.SellCoinsConfirmed
            exchangeText <- ""
            sellQuantityBox.Value.Text <- ""
            lastTransientAlert <- ""
        else
            lastTransientAlert <- "Enter a sell quantity greater than zero."
            this.RefreshUi()

    and applyLaunchAnimationStep () =
        if state.WinState = WinState.Launching then
            launchProgress <- min 1.0 (launchProgress + 0.04)
            rocketTransform.Y <- -420.0 * launchProgress

            if launchProgress >= 1.0 then
                dispatch (Msg.SetWon WinState.Won)
        elif state.WinState = WinState.NotWon then
            launchProgress <- 0.0
            rocketTransform.Y <- 0.0

    and onWindowTick () =
        tickerOffset <- tickerOffset + 1
        dispatch (Msg.Tick DateTimeOffset.UtcNow)
        applyLaunchAnimationStep ()

    and onGlobalKeyDown (args: KeyEventArgs) =
        let intent = Interactions.classifyKey args.Key

        if intent <> KeyboardIntent.Ignore then
            let activeField = Interactions.activeField state.Ui.IsExchangeModalOpen
            let routed = Interactions.routeIntent activeField intent monitorText exchangeText

            monitorText <- routed.MonitorText
            exchangeText <- routed.ExchangeText
            monitorAnswerBox.Value.Text <- monitorText
            sellQuantityBox.Value.Text <- exchangeText

            match routed.SubmitTarget with
            | Some ActiveField.Monitor -> submitMonitorAnswer ()
            | Some ActiveField.Exchange -> submitExchangeSell ()
            | None -> this.RefreshUi()

            args.Handled <- true

    do
        this.InitializeComponent()

        rocketVisual.Value.RenderTransform <- rocketTransform

        smartphoneButton.Value.Click.Add(fun _ ->
            dispatch (Msg.SetExchangeModalOpen true))

        closeExchangeButton.Value.Click.Add(fun _ ->
            dispatch (Msg.SetExchangeModalOpen false))

        sellButton.Value.Click.Add(fun _ -> submitExchangeSell ())

        monkeyButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner AutoMinerKind.Monkey))
        youthButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner AutoMinerKind.RestingYouth))
        gpuButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner AutoMinerKind.Gpu))
        manualUpgradeButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade UpgradeKind.ManualDifficultyReduction))
        efficiencyUpgradeButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade UpgradeKind.AutoMinerEfficiency))
        marketUpgradeButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade UpgradeKind.MarketAnalysis))
        spaceshipButton.Value.Click.Add(fun _ -> dispatch Msg.BuySpaceshipToMars)
        this.KeyDown.Add(onGlobalKeyDown)

        timer.Tick.Add(fun _ -> onWindowTick ())
        timer.Start()

        this.Opened.Add(fun _ ->
            this.RefreshUi()
            routeFocusToActiveInput ())

    member private this.RefreshUi() =
        let coinValueInCash = state.Economy.Coins * state.Economy.CoinPrice
        coinsText.Value.Text <- $"Coin: {state.Economy.Coins:F2} KC (${coinValueInCash:F2})"
        cashText.Value.Text <- $"Cash: ${state.Economy.Cash:F2}"
        priceText.Value.Text <- $"Price: ${state.Economy.CoinPrice:F3} / KC"
        newsTickerText.Value.Text <- renderTicker state.Market.NewsTicker

        challengePromptText.Value.Text <-
            if String.IsNullOrWhiteSpace(state.Challenge.Prompt) then "Preparing challenge..."
            else state.Challenge.Prompt

        challengeDifficultyText.Value.Text <- $"Manual Difficulty: {state.Challenge.Difficulty}"

        feedbackText.Value.Text <-
            match state.Challenge.LastFeedback with
            | Some ChallengeFeedback.O -> "O"
            | Some ChallengeFeedback.X -> "X"
            | None -> "-"

        feedbackText.Value.Foreground <-
            match state.Challenge.LastFeedback with
            | Some ChallengeFeedback.O -> greenBrush
            | Some ChallengeFeedback.X -> redBrush
            | None -> warningBrush

        rewardMessageText.Value.Text <- state.Challenge.LastRewardMessage |> Option.defaultValue ""

        let marketAlert =
            state.Market.ActiveNews
            |> Option.map (fun headline -> $"Market alert: {headline}")
            |> Option.defaultValue ""

        alertText.Value.Text <-
            if String.IsNullOrWhiteSpace(lastTransientAlert) then marketAlert
            else
                if String.IsNullOrWhiteSpace(marketAlert) then lastTransientAlert
                else lastTransientAlert + "  " + marketAlert

        let monkey = state.AutoMiners[AutoMinerKind.Monkey]
        let youth = state.AutoMiners[AutoMinerKind.RestingYouth]
        let gpu = state.AutoMiners[AutoMinerKind.Gpu]

        let monkeyDesc = ShopCatalog.autoMinerDescriptors[AutoMinerKind.Monkey]
        let youthDesc = ShopCatalog.autoMinerDescriptors[AutoMinerKind.RestingYouth]
        let gpuDesc = ShopCatalog.autoMinerDescriptors[AutoMinerKind.Gpu]

        setListButtonStyle
            monkeyButton.Value
            (Shop.canAffordAutoMiner AutoMinerKind.Monkey state)
            monkeyDesc.Label
            monkey.Owned
            monkey.NextCostCoins
            "KC"
            ($"{monkeyDesc.Specs}\n{monkeyDesc.WittyDescription}")

        setListButtonStyle
            youthButton.Value
            (Shop.canAffordAutoMiner AutoMinerKind.RestingYouth state)
            youthDesc.Label
            youth.Owned
            youth.NextCostCoins
            "KC"
            ($"{youthDesc.Specs}\n{youthDesc.WittyDescription}")

        setListButtonStyle
            gpuButton.Value
            (Shop.canAffordAutoMiner AutoMinerKind.Gpu state)
            gpuDesc.Label
            gpu.Owned
            gpu.NextCostCoins
            "KC"
            ($"{gpuDesc.Specs}\n{gpuDesc.WittyDescription}")

        let manualUpgrade = state.Upgrades[UpgradeKind.ManualDifficultyReduction]
        let efficiencyUpgrade = state.Upgrades[UpgradeKind.AutoMinerEfficiency]
        let marketUpgrade = state.Upgrades[UpgradeKind.MarketAnalysis]

        let manualDesc = ShopCatalog.upgradeDescriptors[UpgradeKind.ManualDifficultyReduction]
        let efficiencyDesc = ShopCatalog.upgradeDescriptors[UpgradeKind.AutoMinerEfficiency]
        let marketDesc = ShopCatalog.upgradeDescriptors[UpgradeKind.MarketAnalysis]

        setUpgradeTileButtonStyle
            manualUpgradeButton.Value
            (Shop.canAffordUpgrade UpgradeKind.ManualDifficultyReduction state)
            manualDesc.Label
            manualUpgrade.Level
            manualUpgrade.NextCostCash
            ($"{manualDesc.Specs}\n{manualDesc.WittyDescription}")

        setUpgradeTileButtonStyle
            efficiencyUpgradeButton.Value
            (Shop.canAffordUpgrade UpgradeKind.AutoMinerEfficiency state)
            efficiencyDesc.Label
            efficiencyUpgrade.Level
            efficiencyUpgrade.NextCostCash
            ($"{efficiencyDesc.Specs}\n{efficiencyDesc.WittyDescription}")

        setUpgradeTileButtonStyle
            marketUpgradeButton.Value
            (Shop.canAffordUpgrade UpgradeKind.MarketAnalysis state)
            marketDesc.Label
            marketUpgrade.Level
            marketUpgrade.NextCostCash
            ($"{marketDesc.Specs}\n{marketDesc.WittyDescription}")

        let canLaunch = Shop.canBuySpaceshipToMars state
        let finalDesc = ShopCatalog.spaceshipDescriptor

        let finalText =
            match state.WinState with
            | WinState.NotWon -> $"{finalDesc.Label} | Cost ${Shop.spaceshipToMars.CostCash:F0}"
            | WinState.Launching -> "Spaceship to Mars | Launching..."
            | WinState.Won -> "Spaceship to Mars | Mission complete"

        setButtonStyle
            spaceshipButton.Value
            canLaunch
            finalText
            ($"{finalDesc.Specs}\n{finalDesc.WittyDescription}")

        spaceshipButton.Value.IsEnabled <- state.WinState = WinState.NotWon

        exchangeOverlay.Value.IsVisible <- state.Ui.IsExchangeModalOpen
        exchangeHintText.Value.Text <- if state.Ui.IsExchangeModalOpen then "ACTIVE" else ""
        renderPricePolyline ()

        launchOverlay.Value.IsVisible <- state.WinState <> WinState.NotWon
        launchMessageText.Value.Text <-
            match state.WinState with
            | WinState.Launching -> "Liftoff sequence in progress"
            | WinState.Won -> "Mission Clear: KaiCoin reaches Mars"
            | WinState.NotWon -> ""

        if previousModalOpen <> state.Ui.IsExchangeModalOpen then
            previousModalOpen <- state.Ui.IsExchangeModalOpen
            routeFocusToActiveInput ()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
