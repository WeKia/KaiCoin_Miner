namespace KaiCoinMiner.App.Domain

type ManualReward =
    { CoinsAwarded: decimal
      CashAwarded: decimal
      Message: string }

module Mining =
    let private clampMin minimum value =
        if value < minimum then minimum else value

    let private addMinedCoins amount (state: GameState) =
        let mined = clampMin 0m amount

        { state with
            Economy =
                { state.Economy with
                    Coins = state.Economy.Coins + mined
                    LifetimeMinedCoins = state.Economy.LifetimeMinedCoins + mined } }

    let private manualRewardForDifficulty difficulty : ManualReward =
        let safeDifficulty = max 1 difficulty
        let coins = decimal safeDifficulty
        let cash = if safeDifficulty % 5 = 0 then 1m else 0m

        let message =
            if cash > 0m then
                $"Correct! +{coins} KaiCoin, +{cash} Cash bonus."
            else
                $"Correct! +{coins} KaiCoin."

        { CoinsAwarded = coins
          CashAwarded = cash
          Message = message }

    let private challengeFromProblem feedback rewardMessage (problem: ArithmeticProblem) : ChallengeState =
        { Prompt = problem.Prompt
          ExpectedAnswer = Some problem.Answer
          Difficulty = problem.Difficulty
          LastWasCorrect = feedback |> Option.map (fun item -> item = ChallengeFeedback.O)
          LastFeedback = feedback
          LastRewardMessage = rewardMessage }

    let private nextNonce baseNonce offset =
        baseNonce + offset

    let ensureChallenge (nonce: int) (state: GameState) =
        match state.Challenge.ExpectedAnswer with
        | Some _ -> state
        | None ->
            let difficulty = Progression.manualDifficulty state
            let next = Challenge.generate difficulty (nextNonce nonce 13)
            { state with Challenge = challengeFromProblem None None next }

    let submitAnswer (answer: int) (nonce: int) (state: GameState) =
        let isCorrect =
            match state.Challenge.ExpectedAnswer with
            | Some expected -> expected = answer
            | None -> false

        let rewardedState, feedback, rewardMessage =
            if isCorrect then
                let reward = manualRewardForDifficulty state.Challenge.Difficulty

                let nextState =
                    addMinedCoins reward.CoinsAwarded state
                    |> fun current ->
                        { current with
                            Economy = { current.Economy with Cash = current.Economy.Cash + reward.CashAwarded } }

                nextState, Some ChallengeFeedback.O, Some reward.Message
            else
                state, Some ChallengeFeedback.X, Some "Wrong. No penalty."

        let nextDifficulty = Progression.manualDifficulty rewardedState
        let nextProblem = Challenge.generate nextDifficulty (nextNonce nonce 31)

        { rewardedState with
            Challenge = challengeFromProblem feedback rewardMessage nextProblem }

    let autoMinerCoinsPerSecond (state: GameState) =
        let baseRate =
            state.AutoMiners
            |> Map.toSeq
            |> Seq.sumBy (fun (_, miner) -> decimal miner.Owned * miner.OutputPerSecond)

        baseRate * Progression.autoMinerEfficiencyMultiplier state

    let applyAutoMining deltaSeconds (state: GameState) =
        let seconds = clampMin 0m deltaSeconds
        let mined = autoMinerCoinsPerSecond state * seconds
        addMinedCoins mined state
