namespace KaiCoinMiner.App.UI

open Avalonia.Media
open KaiCoinMiner.App.Domain

type LaunchAnimation =
    { mutable Progress: float
      Transform: TranslateTransform }

module Animations =
    let create () =
        { Progress = 0.0
          Transform = TranslateTransform(0.0, 0.0) }

    let applyStep (anim: LaunchAnimation) (winState: WinState) (dispatch: Msg -> unit) =
        if winState = WinState.Launching then
            anim.Progress <- min 1.0 (anim.Progress + 0.04)
            anim.Transform.Y <- -420.0 * anim.Progress

            if anim.Progress >= 1.0 then
                dispatch (Msg.SetWon WinState.Won)
        elif winState = WinState.NotWon then
            anim.Progress <- 0.0
            anim.Transform.Y <- 0.0
