namespace KaiCoinMiner.App.UI

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.Views

module Renderer =
    let greenBrush = SolidColorBrush(Color.Parse("#25C47A"))
    let redBrush = SolidColorBrush(Color.Parse("#D04C4C"))
    let tickerSeparator = "  ||  "

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

    let renderTicker items tickerOffset =
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

    let renderPricePolyline (pricePolyline: Polyline) (chartPlaceholder: TextBlock) (guideTop: Border) (guideMid: Border) (guideBottom: Border) (minPriceText: TextBlock) (maxPriceText: TextBlock) state =
        let renderPoints = Charts.toRenderPoints 580.0 150.0 64 state
        let chartContext = Charts.describeContext 64 state
        let points = Avalonia.Collections.AvaloniaList<Point>()

        let hasRenderableSeries = renderPoints.Length >= 2

        if hasRenderableSeries then
            renderPoints
            |> List.iter (fun p -> points.Add(Point(p.X, p.Y + 4.0)))

        pricePolyline.Points <- points
        pricePolyline.IsVisible <- hasRenderableSeries
        chartPlaceholder.IsVisible <- chartContext.ShowPlaceholder
        guideTop.IsVisible <- not chartContext.ShowPlaceholder
        guideMid.IsVisible <- not chartContext.ShowPlaceholder
        guideBottom.IsVisible <- not chartContext.ShowPlaceholder
        minPriceText.Text <- chartContext.MinPrice |> Option.map (fun p -> $"Min ${p:F3}") |> Option.defaultValue ""
        maxPriceText.Text <- chartContext.MaxPrice |> Option.map (fun p -> $"Max ${p:F3}") |> Option.defaultValue ""


