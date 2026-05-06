namespace KaiCoinMiner.App.Domain

type ArithmeticOperator =
    | Add
    | Subtract
    | Multiply
    | Divide

type ArithmeticProblem =
    { Prompt: string
      Operator: ArithmeticOperator
      Left: int
      Right: int
      Answer: int
      Difficulty: int }

module Challenge =
    let private positiveModulo value modulus =
        ((value % modulus) + modulus) % modulus

    let private pickInRange minValue maxValue seed =
        minValue + positiveModulo seed (maxValue - minValue + 1)

    let generate (difficulty: int) (nonce: int) : ArithmeticProblem =
        let safeDifficulty = max 1 difficulty
        let opIndex = positiveModulo nonce 4

        match opIndex with
        | 0 ->
            let maxOperand = min 999 (6 + (safeDifficulty * 4))
            let left = pickInRange 1 maxOperand (nonce + 11)
            let right = pickInRange 1 maxOperand (nonce + 23)

            { Prompt = $"{left}+{right}"
              Operator = ArithmeticOperator.Add
              Left = left
              Right = right
              Answer = left + right
              Difficulty = safeDifficulty }
        | 1 ->
            let maxOperand = min 999 (8 + (safeDifficulty * 5))
            let rawLeft = pickInRange 1 maxOperand (nonce + 17)
            let rawRight = pickInRange 1 maxOperand (nonce + 29)
            let left = max rawLeft rawRight
            let right = min rawLeft rawRight

            { Prompt = $"{left}-{right}"
              Operator = ArithmeticOperator.Subtract
              Left = left
              Right = right
              Answer = left - right
              Difficulty = safeDifficulty }
        | 2 ->
            let maxFactor = min 12 (2 + (safeDifficulty * 2))
            let left = pickInRange 1 maxFactor (nonce + 31)
            let right = pickInRange 1 maxFactor (nonce + 43)

            { Prompt = $"{left}*{right}"
              Operator = ArithmeticOperator.Multiply
              Left = left
              Right = right
              Answer = left * right
              Difficulty = safeDifficulty }
        | _ ->
            let maxDivisor = min 12 (2 + (safeDifficulty * 2))
            let divisor = pickInRange 2 maxDivisor (nonce + 37)
            let quotient = pickInRange 1 12 (nonce + 59)
            let dividend = divisor * quotient

            { Prompt = $"{dividend}/{divisor}"
              Operator = ArithmeticOperator.Divide
              Left = dividend
              Right = divisor
              Answer = quotient
              Difficulty = safeDifficulty }
