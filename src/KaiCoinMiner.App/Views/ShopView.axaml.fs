namespace KaiCoinMiner.App.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Markup.Xaml
open Avalonia.Media
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.UI

type ShopView() as this =
    inherit UserControl()

    let upgradesPanel = lazy (this.FindControl<StackPanel>("UpgradesPanel"))
    let autoMinersPanel = lazy (this.FindControl<StackPanel>("AutoMinersPanel"))
    let finalItemsPanel = lazy (this.FindControl<StackPanel>("FinalItemsPanel"))

    let mutable autoMinerControls: Map<string, Button * TextBlock * TextBlock> = Map.empty
    let mutable upgradeControls: Map<string, Button> = Map.empty
    let mutable finalItemControl: (Button * TextBlock) option = None

    let createAutoMinerControl (key: string) (desc: ShopDescriptor) =
        let emoji = TextBlock(Text = desc.Emoji, FontSize = 28.0, VerticalAlignment = VerticalAlignment.Center)
        let label = TextBlock(FontSize = 16.0, FontWeight = FontWeight.SemiBold, Foreground = Theme.labelForeground, VerticalAlignment = VerticalAlignment.Center)
        let cost = TextBlock(FontSize = 14.0, Foreground = Theme.costForeground, VerticalAlignment = VerticalAlignment.Center)
        let buyText = TextBlock(Text = "BUY", FontSize = 14.0, FontWeight = FontWeight.Bold, Foreground = Theme.greenBrush, VerticalAlignment = VerticalAlignment.Center)

        let innerGrid = Grid()
        innerGrid.ColumnDefinitions <- ColumnDefinitions("Auto,*,Auto,Auto")
        innerGrid.ColumnSpacing <- 12.0

        innerGrid.Children.Add(emoji) |> ignore
        Grid.SetColumn(emoji, 0)
        innerGrid.Children.Add(label) |> ignore
        Grid.SetColumn(label, 1)
        innerGrid.Children.Add(cost) |> ignore
        Grid.SetColumn(cost, 2)
        innerGrid.Children.Add(buyText) |> ignore
        Grid.SetColumn(buyText, 3)

        let border =
            Border(
                Background = Theme.panelBackground,
                BorderBrush = Theme.panelBorder,
                BorderThickness = Thickness(2.0),
                CornerRadius = CornerRadius(4.0),
                Padding = Thickness(12.0, 8.0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = innerGrid
            )

        let button =
            Button(
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 120.0,
                Content = border
            )

        button, label, cost

    let createUpgradeControl (key: string) (desc: ShopDescriptor) =
        let emoji = TextBlock(Text = desc.Emoji, FontSize = 36.0, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center)

        let innerBorder =
            Border(
                Background = Theme.panelBackground,
                BorderBrush = Theme.panelBorder,
                BorderThickness = Thickness(2.0),
                CornerRadius = CornerRadius(4.0),
                Width = 80.0,
                Height = 80.0,
                Padding = Thickness(4.0),
                Child = emoji
            )

        let button =
            Button(
                Width = 80.0,
                Height = 80.0,
                Content = innerBorder
            )

        button

    let createFinalItemControl (desc: ShopDescriptor) =
        let emoji = TextBlock(Text = desc.Emoji, FontSize = 64.0, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center)
        let label = TextBlock(FontSize = 14.0, FontWeight = FontWeight.SemiBold, Foreground = Theme.labelForeground, TextAlignment = TextAlignment.Center)

        let innerGrid = Grid()
        innerGrid.RowDefinitions <- RowDefinitions("*,Auto")

        innerGrid.Children.Add(emoji) |> ignore
        Grid.SetRow(emoji, 0)
        innerGrid.Children.Add(label) |> ignore
        Grid.SetRow(label, 1)

        let border =
            Border(
                Background = Theme.panelBackground,
                BorderBrush = Theme.panelBorder,
                BorderThickness = Thickness(2.0),
                CornerRadius = CornerRadius(4.0),
                Width = 260.0,
                Height = 200.0,
                Padding = Thickness(8.0),
                Child = innerGrid
            )

        let button =
            Button(
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = border
            )

        button, label

    do
        AvaloniaXamlLoader.Load(this)

        for key, desc in ShopCatalog.autoMinerList do
            let button, label, cost = createAutoMinerControl key desc
            autoMinersPanel.Value.Children.Add(button)
            autoMinerControls <- Map.add key (button, label, cost) autoMinerControls

        for key, desc in ShopCatalog.upgradeList do
            let button = createUpgradeControl key desc
            upgradesPanel.Value.Children.Add(button)
            upgradeControls <- Map.add key button upgradeControls

        let spaceshipButton, spaceshipLabel = createFinalItemControl ShopCatalog.spaceshipDescriptor
        finalItemsPanel.Value.Children.Add(spaceshipButton)
        finalItemControl <- Some (spaceshipButton, spaceshipLabel)

    member this.SetupHandlers(dispatch: Msg -> unit) =
        for key, (button, _, _) in Map.toSeq autoMinerControls do
            button.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner key))

        for key, button in Map.toSeq upgradeControls do
            button.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade key))

        match finalItemControl with
        | Some (button, _) -> button.Click.Add(fun _ -> dispatch Msg.BuySpaceshipToMars)
        | None -> ()

    member this.Refresh(state: GameState) =
        for key, (button, label, costText) in Map.toSeq autoMinerControls do
            match Map.tryFind key state.AutoMiners with
            | Some miner ->
                let desc = ShopCatalog.autoMinerDescriptors[key]
                let affordable = Shop.canAffordAutoMiner key state
                label.Text <- $"{desc.Label} ({miner.Owned})"
                costText.Text <- $"{miner.NextCostCoins} KC"
                ToolTip.SetTip(button, $"{desc.Specs}\n{desc.WittyDescription}")
                button.IsEnabled <- affordable
            | None -> ()

        for key, button in Map.toSeq upgradeControls do
            match Map.tryFind key state.Upgrades with
            | Some upgrade ->
                let desc = ShopCatalog.upgradeDescriptors[key]
                let affordable = Shop.canAffordUpgrade key state
                ToolTip.SetTip(
                    button,
                    $"{desc.Label} (Lv.{upgrade.Level})\n{desc.Specs}\n{desc.WittyDescription}\nCost: ${upgrade.NextCostCash}"
                )
                button.IsEnabled <- affordable
            | None -> ()

        match finalItemControl with
        | Some (button, label) ->
            let desc = ShopCatalog.spaceshipDescriptor
            let canLaunch = Shop.canBuySpaceshipToMars state
            let text =
                match state.WinState with
                | WinState.NotWon -> desc.Label
                | WinState.Launching -> "Launching..."
                | WinState.Won -> "Mission Complete"
            label.Text <- text
            ToolTip.SetTip(button, $"{desc.Specs}\n{desc.WittyDescription}")
            button.IsEnabled <- canLaunch
        | None -> ()
