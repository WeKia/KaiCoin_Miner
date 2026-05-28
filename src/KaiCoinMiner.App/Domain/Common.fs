namespace KaiCoinMiner.App.Domain

module Common =
    let clampMin minimum value =
        if value < minimum then minimum else value

    let clampMax maximum value =
        if value > maximum then maximum else value

    let clampInt rangeMin rangeMax value =
        value |> max rangeMin |> min rangeMax

    let positiveModulo value modulus =
        ((value % modulus) + modulus) % modulus
