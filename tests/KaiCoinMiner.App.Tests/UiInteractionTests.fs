module KaiCoinMiner.App.Tests.UiInteractionTests

open Avalonia.Input
open Expecto
open KaiCoinMiner.App.UI

[<Tests>]
let uiInteractionTests =
    testList "KAIM-005 ui interactions" [
        testCase "digits are captured from both keyboard rows" <| fun _ ->
            Expect.equal (Interactions.classifyKey Key.D0) (KeyboardIntent.Append '0') "top-row 0 should append"
            Expect.equal (Interactions.classifyKey Key.D9) (KeyboardIntent.Append '9') "top-row 9 should append"
            Expect.equal (Interactions.classifyKey Key.NumPad4) (KeyboardIntent.Append '4') "numpad key should append"

        testCase "active field follows modal open state" <| fun _ ->
            Expect.equal (Interactions.activeField false) ActiveField.Monitor "closed modal should target monitor"
            Expect.equal (Interactions.activeField true) ActiveField.Exchange "open modal should target exchange"

        testCase "enter submits the active field only" <| fun _ ->
            let monitorResult =
                Interactions.routeIntent
                    ActiveField.Monitor
                    KeyboardIntent.Submit
                    "123"
                    "7"

            let exchangeResult =
                Interactions.routeIntent
                    ActiveField.Exchange
                    KeyboardIntent.Submit
                    "123"
                    "7"

            Expect.equal monitorResult.SubmitTarget (Some ActiveField.Monitor) "monitor should submit when active"
            Expect.equal exchangeResult.SubmitTarget (Some ActiveField.Exchange) "exchange should submit when active"
            Expect.equal monitorResult.MonitorText "123" "submit should not mutate monitor text directly"
            Expect.equal exchangeResult.ExchangeText "7" "submit should not mutate exchange text directly"

        testCase "numeric input routes to exchange only while modal is open" <| fun _ ->
            let monitorField =
                Interactions.routeIntent
                    (Interactions.activeField false)
                    (KeyboardIntent.Append '5')
                    "1"
                    "2"

            let exchangeField =
                Interactions.routeIntent
                    (Interactions.activeField true)
                    (KeyboardIntent.Append '5')
                    "1"
                    "2"

            Expect.equal monitorField.MonitorText "15" "monitor should receive digits when modal is closed"
            Expect.equal monitorField.ExchangeText "2" "exchange should stay unchanged when inactive"
            Expect.equal exchangeField.MonitorText "1" "monitor should stay unchanged while modal is open"
            Expect.equal exchangeField.ExchangeText "25" "exchange should receive digits when modal is open"

        testCase "decimal key is ignored for monitor and accepted for exchange" <| fun _ ->
            let monitorWithDecimal =
                Interactions.routeIntent
                    ActiveField.Monitor
                    (KeyboardIntent.Append '.')
                    "12"
                    "3"

            let exchangeWithDecimal =
                Interactions.routeIntent
                    ActiveField.Exchange
                    (KeyboardIntent.Append '.')
                    "12"
                    "3"

            Expect.equal monitorWithDecimal.MonitorText "12" "monitor answer should stay integer-only"
            Expect.equal monitorWithDecimal.ExchangeText "3" "exchange input should stay unchanged when monitor is active"
            Expect.equal exchangeWithDecimal.MonitorText "12" "monitor should stay unchanged when exchange is active"
            Expect.equal exchangeWithDecimal.ExchangeText "3." "exchange input should allow decimal quantities"
    ]
