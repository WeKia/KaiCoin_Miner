namespace KaiCoinMiner.App.Views

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Markup.Xaml
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.UI

type ExchangeView() as this =
    inherit UserControl()

    let closeButton = lazy (this.FindControl<Button>("CloseExchangeButton"))
    let pricePolyline = lazy (this.FindControl<Polyline>("ExchangePricePolyline"))
    let guideTop = lazy (this.FindControl<Border>("ExchangeGuideTop"))
    let guideMid = lazy (this.FindControl<Border>("ExchangeGuideMid"))
    let guideBottom = lazy (this.FindControl<Border>("ExchangeGuideBottom"))
    let chartPlaceholder = lazy (this.FindControl<TextBlock>("ExchangeChartPlaceholder"))
    let minPriceText = lazy (this.FindControl<TextBlock>("ExchangeMinPriceText"))
    let maxPriceText = lazy (this.FindControl<TextBlock>("ExchangeMaxPriceText"))
    let sellQuantityBox = lazy (this.FindControl<TextBox>("SellQuantityBox"))
    let sellButton = lazy (this.FindControl<Button>("SellButton"))
    let hintText = lazy (this.FindControl<TextBlock>("ExchangeHintText"))

    do AvaloniaXamlLoader.Load(this)

    member this.SellQuantityBox = sellQuantityBox.Value
    member this.SellButton = sellButton.Value
    member this.CloseButton = closeButton.Value

    member this.Refresh(state: GameState) =
        this.IsVisible <- state.Ui.IsExchangeModalOpen
        hintText.Value.Text <- if state.Ui.IsExchangeModalOpen then "ACTIVE" else ""

        Renderer.renderPricePolyline
            pricePolyline.Value
            chartPlaceholder.Value
            guideTop.Value
            guideMid.Value
            guideBottom.Value
            minPriceText.Value
            maxPriceText.Value
            state

    member this.SetupHandlers(onClose: unit -> unit, onSell: unit -> unit) =
        closeButton.Value.Click.Add(fun _ -> onClose())
        sellButton.Value.Click.Add(fun _ -> onSell())
