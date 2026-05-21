namespace KaiCoinMiner.App.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open KaiCoinMiner.App.Domain

type LaunchView() as this =
    inherit UserControl()

    let messageText = lazy (this.FindControl<TextBlock>("LaunchMessageText"))
    let rocketVisual = lazy (this.FindControl<Border>("RocketVisual"))

    do AvaloniaXamlLoader.Load(this)

    member this.RocketVisual = rocketVisual.Value

    member this.Refresh(state: GameState) =
        this.IsVisible <- state.WinState <> WinState.NotWon
        messageText.Value.Text <-
            match state.WinState with
            | WinState.Launching -> "Liftoff sequence in progress"
            | WinState.Won -> "Mission Clear: KaiCoin reaches Mars"
            | WinState.NotWon -> ""
