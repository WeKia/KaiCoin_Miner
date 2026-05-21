namespace KaiCoinMiner.App.UI

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.Views

type ShopControlRefs =
    { MonkeyButton: Button
      YouthButton: Button
      GpuButton: Button
      ManualButton: Button
      EfficiencyButton: Button
      MarketButton: Button
      SpaceshipButton: Button
      MonkeyLabel: TextBlock
      YouthLabel: TextBlock
      GpuLabel: TextBlock
      MonkeyCost: TextBlock
      YouthCost: TextBlock
      GpuCost: TextBlock
      SpaceshipLabel: TextBlock }

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

    let refreshShop (refs: ShopControlRefs) (state: GameState) =
        let monkey = state.AutoMiners[AutoMinerKind.Monkey]
        let youth = state.AutoMiners[AutoMinerKind.RestingYouth]
        let gpu = state.AutoMiners[AutoMinerKind.Gpu]

        let monkeyDesc = ShopCatalog.autoMinerDescriptors[AutoMinerKind.Monkey]
        let youthDesc = ShopCatalog.autoMinerDescriptors[AutoMinerKind.RestingYouth]
        let gpuDesc = ShopCatalog.autoMinerDescriptors[AutoMinerKind.Gpu]

        let manualUpgrade = state.Upgrades[UpgradeKind.ManualDifficultyReduction]
        let efficiencyUpgrade = state.Upgrades[UpgradeKind.AutoMinerEfficiency]
        let marketUpgrade = state.Upgrades[UpgradeKind.MarketAnalysis]

        let manualDesc = ShopCatalog.upgradeDescriptors[UpgradeKind.ManualDifficultyReduction]
        let efficiencyDesc = ShopCatalog.upgradeDescriptors[UpgradeKind.AutoMinerEfficiency]
        let marketDesc = ShopCatalog.upgradeDescriptors[UpgradeKind.MarketAnalysis]

        let finalDesc = ShopCatalog.spaceshipDescriptor

        refs.MonkeyLabel.Text <- $"{monkeyDesc.Label} ({monkey.Owned})"
        refs.MonkeyCost.Text <- $"{monkey.NextCostCoins} KC"
        ToolTip.SetTip(refs.MonkeyButton, $"{monkeyDesc.Specs}\n{monkeyDesc.WittyDescription}")

        refs.YouthLabel.Text <- $"{youthDesc.Label} ({youth.Owned})"
        refs.YouthCost.Text <- $"{youth.NextCostCoins} KC"
        ToolTip.SetTip(refs.YouthButton, $"{youthDesc.Specs}\n{youthDesc.WittyDescription}")

        refs.GpuLabel.Text <- $"{gpuDesc.Label} ({gpu.Owned})"
        refs.GpuCost.Text <- $"{gpu.NextCostCoins} KC"
        ToolTip.SetTip(refs.GpuButton, $"{gpuDesc.Specs}\n{gpuDesc.WittyDescription}")

        ToolTip.SetTip(refs.ManualButton, $"{manualDesc.Label} (Lv.{manualUpgrade.Level})\n{manualDesc.Specs}\n{manualDesc.WittyDescription}\nCost: ${manualUpgrade.NextCostCash}")
        ToolTip.SetTip(refs.EfficiencyButton, $"{efficiencyDesc.Label} (Lv.{efficiencyUpgrade.Level})\n{efficiencyDesc.Specs}\n{efficiencyDesc.WittyDescription}\nCost: ${efficiencyUpgrade.NextCostCash}")
        ToolTip.SetTip(refs.MarketButton, $"{marketDesc.Label} (Lv.{marketUpgrade.Level})\n{marketDesc.Specs}\n{marketDesc.WittyDescription}\nCost: ${marketUpgrade.NextCostCash}")

        let shopSpaceshipText =
            match state.WinState with
            | WinState.NotWon -> $"{finalDesc.Label}"
            | WinState.Launching -> "Launching..."
            | WinState.Won -> "Mission Complete"

        refs.SpaceshipLabel.Text <- shopSpaceshipText
        ToolTip.SetTip(refs.SpaceshipButton, $"{finalDesc.Specs}\n{finalDesc.WittyDescription}")
