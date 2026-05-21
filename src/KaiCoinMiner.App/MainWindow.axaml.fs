namespace KaiCoinMiner.App

open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Markup.Xaml
open Avalonia.Threading
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.Infrastructure
open KaiCoinMiner.App.UI
open System
open System.IO

type MainWindow () as this = 
    inherit Window ()

    let savePath = Save.getDefaultSavePath ()

    let timer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds(200.0))

    let mutable state = Mining.ensureChallenge 7 State.initial
    let mutable monitorText = ""
    let mutable exchangeText = ""
    let mutable tickerOffset = 0
    let mutable previousModalOpen = false
    let mutable cursorVisible = false

    let launchAnim = Animations.create ()

    let headerView = lazy (this.FindControl<Views.HeaderView>("HeaderView"))
    let challengeView = lazy (this.FindControl<Views.ChallengeView>("ChallengeView"))
    let shopView = lazy (this.FindControl<Views.ShopView>("ShopView"))
    let exchangeView = lazy (this.FindControl<Views.ExchangeView>("ExchangeView"))
    let launchView = lazy (this.FindControl<Views.LaunchView>("LaunchView"))
    let smartphoneButton = lazy (this.FindControl<Button>("SmartphoneButton"))

    let routeFocusToActiveInput () =
        if state.Ui.IsExchangeModalOpen then
            exchangeView.Value.SellQuantityBox.Focus() |> ignore

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
            | Update.Effect.LoadState ->
                dispatch (Msg.Loaded (Save.tryLoad savePath))

        this.RefreshUi()

    and submitMonitorAnswer () =
        let submitted, nextText = Input.submitMonitorAnswer monitorText dispatch
        monitorText <- nextText
        if not submitted then this.RefreshUi()

    and submitExchangeSell () =
        let submitted, nextText = Input.submitExchangeSell exchangeText exchangeView.Value.SellQuantityBox dispatch
        exchangeText <- nextText
        if not submitted then this.RefreshUi()

    and onWindowTick () =
        tickerOffset <- tickerOffset + 1
        if tickerOffset % 3 = 0 then
            cursorVisible <- not cursorVisible
        dispatch (Msg.Tick DateTimeOffset.UtcNow)
        Animations.applyStep launchAnim state.WinState dispatch

    and onGlobalKeyDown (args: KeyEventArgs) =
        let intent = Interactions.classifyKey args.Key

        if intent <> KeyboardIntent.Ignore then
            let activeField = Interactions.activeField state.Ui.IsExchangeModalOpen
            let routed = Interactions.routeIntent activeField intent monitorText exchangeText

            monitorText <- routed.MonitorText
            exchangeText <- routed.ExchangeText
            exchangeView.Value.SellQuantityBox.Text <- exchangeText

            match routed.SubmitTarget with
            | Some ActiveField.Monitor -> submitMonitorAnswer ()
            | Some ActiveField.Exchange -> submitExchangeSell ()
            | None -> this.RefreshUi()

            args.Handled <- true

    do
        this.InitializeComponent()

        launchView.Value.RocketVisual.RenderTransform <- launchAnim.Transform

        smartphoneButton.Value.Click.Add(fun _ ->
            dispatch (Msg.SetExchangeModalOpen true))

        exchangeView.Value.SetupHandlers(
            (fun () -> dispatch (Msg.SetExchangeModalOpen false)),
            (fun () -> submitExchangeSell ()))

        shopView.Value.SetupHandlers(dispatch)

        this.KeyDown.Add(onGlobalKeyDown)

        timer.Tick.Add(fun _ -> onWindowTick ())
        timer.Start()

        this.Opened.Add(fun _ ->
            this.RefreshUi()
            routeFocusToActiveInput ())

    member private this.RefreshUi() =
        headerView.Value.Refresh(state, tickerOffset)
        challengeView.Value.Refresh(state.Challenge.Prompt, monitorText, cursorVisible)
        shopView.Value.Refresh(state)
        exchangeView.Value.Refresh(state)
        launchView.Value.Refresh(state)

        if previousModalOpen <> state.Ui.IsExchangeModalOpen then
            previousModalOpen <- state.Ui.IsExchangeModalOpen
            routeFocusToActiveInput ()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
