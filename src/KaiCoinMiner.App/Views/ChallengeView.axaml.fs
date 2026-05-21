namespace KaiCoinMiner.App.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open System

type ChallengeView() as this =
    inherit UserControl()

    let challengePromptText = lazy (this.FindControl<TextBlock>("ChallengePromptText"))
    let monitorInputText = lazy (this.FindControl<TextBlock>("MonitorInputText"))

    do AvaloniaXamlLoader.Load(this)

    member this.Refresh(prompt: string, monitorText: string, cursorVisible: bool) =
        challengePromptText.Value.Text <-
            if String.IsNullOrWhiteSpace(prompt) then "Preparing challenge..."
            else prompt

        let cursorStr = if cursorVisible then "▮" else "▯"
        monitorInputText.Value.Text <- monitorText + cursorStr
