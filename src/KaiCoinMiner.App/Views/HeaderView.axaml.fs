namespace KaiCoinMiner.App.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open KaiCoinMiner.App.Domain
open KaiCoinMiner.App.UI

type HeaderView() as this =
    inherit UserControl()

    let coinsText = lazy (this.FindControl<TextBlock>("CoinsText"))
    let cashText = lazy (this.FindControl<TextBlock>("CashText"))
    let priceText = lazy (this.FindControl<TextBlock>("PriceText"))
    let newsTickerText = lazy (this.FindControl<TextBlock>("NewsTickerText"))

    do AvaloniaXamlLoader.Load(this)

    member this.Refresh(state: GameState, tickerOffset: int) =
        let coinValueInCash = state.Economy.Coins * state.Economy.CoinPrice
        coinsText.Value.Text <- $"Coin: {state.Economy.Coins:F2} KC (${coinValueInCash:F2})"
        cashText.Value.Text <- $"Cash: ${state.Economy.Cash:F2}"
        priceText.Value.Text <- $"Price: ${state.Economy.CoinPrice:F3} / KC"
        newsTickerText.Value.Text <- Renderer.renderTicker state.Market.NewsTicker tickerOffset
