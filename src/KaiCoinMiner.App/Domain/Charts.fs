namespace KaiCoinMiner.App.Domain

type RenderPoint =
    { X: float
      Y: float
      Tick: int64
      Price: decimal }

type ChartContext = { ShowPlaceholder: bool; MinPrice: decimal option; MaxPrice: decimal option }

module Charts =
    let maxPoints = 256

    let private clamp count value =
        if value < 1 then 1 else min count value

    let appendPricePoint tick price (state: GameState) =
        let point =
            { Tick = tick
              Price = max 0.0001m price }

        { state with Chart = point :: state.Chart |> List.truncate maxPoints }

    let toRenderPoints width height requestedPoints (state: GameState) =
        let source = state.Chart |> List.rev
        let count = clamp source.Length requestedPoints
        let selected = source |> List.rev |> List.truncate count |> List.rev

        match selected with
        | [] -> []
        | [ single ] ->
            [ { X = 0.0
                Y = height / 2.0
                Tick = single.Tick
                Price = single.Price } ]
        | _ ->
            let minPrice = selected |> List.minBy (fun p -> p.Price) |> fun p -> p.Price
            let maxPrice = selected |> List.maxBy (fun p -> p.Price) |> fun p -> p.Price
            let priceRange = max 0.0001m (maxPrice - minPrice)
            let spacing = if selected.Length <= 1 then width else width / float (selected.Length - 1)

            selected
            |> List.mapi (fun index point ->
                let normalized = float ((point.Price - minPrice) / priceRange)

                { X = spacing * float index
                  Y = height - (normalized * height)
                  Tick = point.Tick
                  Price = point.Price })

    let describeContext requestedPoints (state: GameState) =
        let source = state.Chart |> List.rev
        let count = clamp source.Length requestedPoints
        let selected = source |> List.rev |> List.truncate count |> List.rev

        match selected with
        | _ :: _ :: _
        | [ _; _ ] ->
            let minPrice = selected |> List.minBy (fun p -> p.Price) |> fun p -> p.Price
            let maxPrice = selected |> List.maxBy (fun p -> p.Price) |> fun p -> p.Price

            { ShowPlaceholder = false
              MinPrice = Some minPrice
              MaxPrice = Some maxPrice }
        | _ ->
            { ShowPlaceholder = true
              MinPrice = None
              MaxPrice = None }
