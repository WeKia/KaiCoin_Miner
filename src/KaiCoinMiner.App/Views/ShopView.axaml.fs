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

    let mutable autoMinerControls: Map<AutoMinerKind, Button * TextBlock * TextBlock> = Map.empty
    let mutable upgradeControls: Map<UpgradeKind, Button> = Map.empty
    let mutable finalItemControl: (Button * TextBlock) option = None

    let greenBrush = SolidColorBrush(Color.Parse("#25C47A"))
    let redBrush = SolidColorBrush(Color.Parse("#D04C4C"))

    let createAutoMinerControl (kind: AutoMinerKind) (desc: ShopDescriptor) =
        let emoji = TextBlock(Text = desc.Emoji, FontSize = 28.0, VerticalAlignment = VerticalAlignment.Center)
        let label = TextBlock(FontSize = 16.0, FontWeight = FontWeight.SemiBold, Foreground = SolidColorBrush(Color.Parse("#E8F2F6")), VerticalAlignment = VerticalAlignment.Center)
        let cost = TextBlock(FontSize = 14.0, Foreground = SolidColorBrush(Color.Parse("#AEE9FF")), VerticalAlignment = VerticalAlignment.Center)
        let buyText = TextBlock(Text = "BUY", FontSize = 14.0, FontWeight = FontWeight.Bold, Foreground = greenBrush, VerticalAlignment = VerticalAlignment.Center)

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
                Background = SolidColorBrush(Color.Parse("#14232F")),
                BorderBrush = SolidColorBrush(Color.Parse("#2D4C5D")),
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

    let createUpgradeControl (kind: UpgradeKind) (desc: ShopDescriptor) =
        let emoji = TextBlock(Text = desc.Emoji, FontSize = 36.0, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center)

        let innerBorder =
            Border(
                Background = SolidColorBrush(Color.Parse("#14232F")),
                BorderBrush = SolidColorBrush(Color.Parse("#2D4C5D")),
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
        let label = TextBlock(FontSize = 14.0, FontWeight = FontWeight.SemiBold, Foreground = SolidColorBrush(Color.Parse("#E8F2F6")), TextAlignment = TextAlignment.Center)

        let innerGrid = Grid()
        innerGrid.RowDefinitions <- RowDefinitions("*,Auto")

        innerGrid.Children.Add(emoji) |> ignore
        Grid.SetRow(emoji, 0)
        innerGrid.Children.Add(label) |> ignore
        Grid.SetRow(label, 1)

        let border =
            Border(
                Background = SolidColorBrush(Color.Parse("#14232F")),
                BorderBrush = SolidColorBrush(Color.Parse("#2D4C5D")),
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

        for kind, desc in ShopCatalog.autoMinerList do
            let button, label, cost = createAutoMinerControl kind desc
            autoMinersPanel.Value.Children.Add(button)
            autoMinerControls <- Map.add kind (button, label, cost) autoMinerControls

        for kind, desc in ShopCatalog.upgradeList do
            let button = createUpgradeControl kind desc
            upgradesPanel.Value.Children.Add(button)
            upgradeControls <- Map.add kind button upgradeControls

        let spaceshipButton, spaceshipLabel = createFinalItemControl ShopCatalog.spaceshipDescriptor
        finalItemsPanel.Value.Children.Add(spaceshipButton)
        finalItemControl <- Some (spaceshipButton, spaceshipLabel)

    member this.SetupHandlers(dispatch: Msg -> unit) =
        for kind, (button, _, _) in Map.toSeq autoMinerControls do
            button.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner kind))

        for kind, button in Map.toSeq upgradeControls do
            button.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade kind))

        match finalItemControl with
        | Some (button, _) -> button.Click.Add(fun _ -> dispatch Msg.BuySpaceshipToMars)
        | None -> ()

    member this.Refresh(state: GameState) =
        for kind, (button, label, costText) in Map.toSeq autoMinerControls do
            match Map.tryFind kind state.AutoMiners with
            | Some miner ->
                let desc = ShopCatalog.autoMinerDescriptors[kind]
                let affordable = Shop.canAffordAutoMiner kind state
                label.Text <- $"{desc.Label} ({miner.Owned})"
                costText.Text <- $"{miner.NextCostCoins} KC"
                ToolTip.SetTip(button, $"{desc.Specs}\n{desc.WittyDescription}")
                button.IsEnabled <- affordable
            | None -> ()

        for kind, button in Map.toSeq upgradeControls do
            match Map.tryFind kind state.Upgrades with
            | Some upgrade ->
                let desc = ShopCatalog.upgradeDescriptors[kind]
                let affordable = Shop.canAffordUpgrade kind state
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
