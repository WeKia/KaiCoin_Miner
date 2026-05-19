namespace KaiCoinMiner.App.UI

open Avalonia.Input

type ActiveField =
    | Monitor
    | Exchange

type KeyboardIntent =
    | Append of char
    | Backspace
    | Submit
    | Ignore

type RoutedInput =
    { MonitorText: string
      ExchangeText: string
      SubmitTarget: ActiveField option }

module Interactions =
    let classifyKey (key: Key) =
        match key with
        | Key.D0
        | Key.NumPad0 -> KeyboardIntent.Append '0'
        | Key.D1
        | Key.NumPad1 -> KeyboardIntent.Append '1'
        | Key.D2
        | Key.NumPad2 -> KeyboardIntent.Append '2'
        | Key.D3
        | Key.NumPad3 -> KeyboardIntent.Append '3'
        | Key.D4
        | Key.NumPad4 -> KeyboardIntent.Append '4'
        | Key.D5
        | Key.NumPad5 -> KeyboardIntent.Append '5'
        | Key.D6
        | Key.NumPad6 -> KeyboardIntent.Append '6'
        | Key.D7
        | Key.NumPad7 -> KeyboardIntent.Append '7'
        | Key.D8
        | Key.NumPad8 -> KeyboardIntent.Append '8'
        | Key.D9
        | Key.NumPad9 -> KeyboardIntent.Append '9'
        | Key.Decimal
        | Key.OemPeriod -> KeyboardIntent.Append '.'
        | Key.Back -> KeyboardIntent.Backspace
        | Key.Enter
        | Key.Return -> KeyboardIntent.Submit
        | _ -> KeyboardIntent.Ignore

    let activeField isExchangeOpen =
        if isExchangeOpen then ActiveField.Exchange else ActiveField.Monitor

    let private trimLast (value: string) =
        if System.String.IsNullOrEmpty(value) then ""
        else value.Substring(0, value.Length - 1)

    let private appendExchangeChar (exchangeText: string) (c: char) =
        if c = '.' && exchangeText.Contains('.') then exchangeText
        else exchangeText + string c

    let routeIntent field intent monitorText exchangeText =
        match field, intent with
        | _, KeyboardIntent.Ignore ->
            { MonitorText = monitorText
              ExchangeText = exchangeText
              SubmitTarget = None }
          | ActiveField.Monitor, KeyboardIntent.Append '.' ->
            { MonitorText = monitorText
              ExchangeText = exchangeText
              SubmitTarget = None }
          | ActiveField.Monitor, KeyboardIntent.Append c ->
              { MonitorText = monitorText + string c
                ExchangeText = exchangeText
                SubmitTarget = None }
          | ActiveField.Exchange, KeyboardIntent.Append c ->
            { MonitorText = monitorText
              ExchangeText = appendExchangeChar exchangeText c
              SubmitTarget = None }
        | ActiveField.Monitor, KeyboardIntent.Backspace ->
            { MonitorText = trimLast monitorText
              ExchangeText = exchangeText
              SubmitTarget = None }
        | ActiveField.Exchange, KeyboardIntent.Backspace ->
            { MonitorText = monitorText
              ExchangeText = trimLast exchangeText
              SubmitTarget = None }
        | ActiveField.Monitor, KeyboardIntent.Submit ->
            { MonitorText = monitorText
              ExchangeText = exchangeText
              SubmitTarget = Some ActiveField.Monitor }
        | ActiveField.Exchange, KeyboardIntent.Submit ->
            { MonitorText = monitorText
              ExchangeText = exchangeText
              SubmitTarget = Some ActiveField.Exchange }
