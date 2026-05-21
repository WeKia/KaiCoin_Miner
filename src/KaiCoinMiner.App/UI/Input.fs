namespace KaiCoinMiner.App.UI

open System
open System.Globalization
open KaiCoinMiner.App.Domain

module Input =
    let tryParseDecimalInvariant (value: string) =
        let mutable parsed = 0m
        let ok = Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, &parsed)
        ok, parsed

    let submitMonitorAnswer (monitorText: string) (dispatch: Msg -> unit) =
        let mutable parsed = 0
        if Int32.TryParse(monitorText, &parsed) then
            dispatch (Msg.ChallengeSubmitted parsed)
            true, ""
        else
            false, monitorText

    let submitExchangeSell (exchangeText: string) (sellQuantityBox: Avalonia.Controls.TextBox) (dispatch: Msg -> unit) =
        let normalized = exchangeText.Trim()
        let ok, parsed = tryParseDecimalInvariant normalized
        if ok && parsed > 0m then
            dispatch (Msg.SetPendingSellQuantity parsed)
            dispatch Msg.SellCoinsConfirmed
            sellQuantityBox.Text <- ""
            true, ""
        else
            false, exchangeText
