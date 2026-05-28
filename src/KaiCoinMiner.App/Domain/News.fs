namespace KaiCoinMiner.App.Domain

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open Common

type NewsEvent =
    { Headline: string
      PriceImpactMultiplier: decimal
      DifficultyPressureDelta: int
      CadenceSeconds: decimal }

type NewsEventRaw =
    { Headline: string
      PriceImpactMultiplier: decimal
      DifficultyPressureDelta: int }

type NewsConfigRaw =
    { Events: NewsEventRaw list }

module News =
    let cadenceSeconds nonce =
        decimal (30 + positiveModulo nonce 31)

    let mutable private cachedCatalog: NewsEvent list option = None

    let private loadCatalog () =
        match cachedCatalog with
        | Some catalog -> catalog
        | None ->
            let path = Path.Combine(AppContext.BaseDirectory, "Assets", "config", "news.json")
            let catalog =
                try
                    if File.Exists(path) then
                        let json = File.ReadAllText(path)
                        let raw = JsonSerializer.Deserialize<NewsConfigRaw>(json, JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase))
                        raw.Events
                        |> List.map (fun e ->
                            { Headline = e.Headline
                              PriceImpactMultiplier = e.PriceImpactMultiplier
                              DifficultyPressureDelta = e.DifficultyPressureDelta
                              CadenceSeconds = 0m })
                    else
                        []
                with
                | _ ->
                    [ { Headline = "Whales buy the dip"; PriceImpactMultiplier = 1.08m; DifficultyPressureDelta = 0; CadenceSeconds = 0m }
                      { Headline = "Power grid strain hits mining farms"; PriceImpactMultiplier = 0.96m; DifficultyPressureDelta = 2; CadenceSeconds = 0m }
                      { Headline = "Campus hackathon boosts adoption"; PriceImpactMultiplier = 1.05m; DifficultyPressureDelta = 1; CadenceSeconds = 0m }
                      { Headline = "Regulatory rumor cools traders"; PriceImpactMultiplier = 0.93m; DifficultyPressureDelta = 3; CadenceSeconds = 0m }
                      { Headline = "ASIC shipment delays continue"; PriceImpactMultiplier = 0.98m; DifficultyPressureDelta = 2; CadenceSeconds = 0m }
                      { Headline = "Open-source miner patch improves yields"; PriceImpactMultiplier = 1.02m; DifficultyPressureDelta = -1; CadenceSeconds = 0m }
                      { Headline = "Speculators rotate into KaiCoin"; PriceImpactMultiplier = 1.07m; DifficultyPressureDelta = 1; CadenceSeconds = 0m }
                      { Headline = "Network congestion frustrates traders"; PriceImpactMultiplier = 0.95m; DifficultyPressureDelta = 1; CadenceSeconds = 0m } ]
            cachedCatalog <- Some catalog
            catalog

    let generate nonce =
        let catalog = loadCatalog ()
        if List.isEmpty catalog then
            { Headline = "Market quiet"; PriceImpactMultiplier = 1.0m; DifficultyPressureDelta = 0; CadenceSeconds = cadenceSeconds (nonce + 19) }
        else
            let index = positiveModulo nonce catalog.Length
            let selected = catalog[index]
            { selected with
                CadenceSeconds = cadenceSeconds (nonce + 19) }
