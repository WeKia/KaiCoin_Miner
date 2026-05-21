namespace KaiCoinMiner.App.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.UI

type ShopView() as this =
    inherit UserControl()

    let monkeyButton = lazy (this.FindControl<Button>("ShopMonkeyButton"))
    let youthButton = lazy (this.FindControl<Button>("ShopYouthButton"))
    let gpuButton = lazy (this.FindControl<Button>("ShopGpuButton"))
    let manualButton = lazy (this.FindControl<Button>("ShopManualButton"))
    let efficiencyButton = lazy (this.FindControl<Button>("ShopEfficiencyButton"))
    let marketButton = lazy (this.FindControl<Button>("ShopMarketButton"))
    let spaceshipButton = lazy (this.FindControl<Button>("ShopSpaceshipButton"))
    let monkeyLabel = lazy (this.FindControl<TextBlock>("ShopMonkeyLabel"))
    let youthLabel = lazy (this.FindControl<TextBlock>("ShopYouthLabel"))
    let gpuLabel = lazy (this.FindControl<TextBlock>("ShopGpuLabel"))
    let monkeyCost = lazy (this.FindControl<TextBlock>("ShopMonkeyCost"))
    let youthCost = lazy (this.FindControl<TextBlock>("ShopYouthCost"))
    let gpuCost = lazy (this.FindControl<TextBlock>("ShopGpuCost"))
    let spaceshipLabel = lazy (this.FindControl<TextBlock>("ShopSpaceshipLabel"))

    let mutable shopRefs: ShopControlRefs option = None

    do
        AvaloniaXamlLoader.Load(this)
        shopRefs <- Some
            { MonkeyButton = monkeyButton.Value
              YouthButton = youthButton.Value
              GpuButton = gpuButton.Value
              ManualButton = manualButton.Value
              EfficiencyButton = efficiencyButton.Value
              MarketButton = marketButton.Value
              SpaceshipButton = spaceshipButton.Value
              MonkeyLabel = monkeyLabel.Value
              YouthLabel = youthLabel.Value
              GpuLabel = gpuLabel.Value
              MonkeyCost = monkeyCost.Value
              YouthCost = youthCost.Value
              GpuCost = gpuCost.Value
              SpaceshipLabel = spaceshipLabel.Value }

    member this.SetupHandlers(dispatch: Msg -> unit) =
        monkeyButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner AutoMinerKind.Monkey))
        youthButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner AutoMinerKind.RestingYouth))
        gpuButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyAutoMiner AutoMinerKind.Gpu))
        manualButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade UpgradeKind.ManualDifficultyReduction))
        efficiencyButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade UpgradeKind.AutoMinerEfficiency))
        marketButton.Value.Click.Add(fun _ -> dispatch (Msg.BuyUpgrade UpgradeKind.MarketAnalysis))
        spaceshipButton.Value.Click.Add(fun _ -> dispatch Msg.BuySpaceshipToMars)

    member this.Refresh(state: GameState) =
        match shopRefs with
        | Some refs -> Renderer.refreshShop refs state
        | None -> ()
