module KaiCoinMiner.App.Tests.GameplayLoopTests

open System
open Expecto
open KaiCoinMiner.App.Domain

let private seededState lifetimeMined reductionLevel monkeyOwned youthOwned gpuOwned =
    let upgrades =
        State.initial.Upgrades
        |> Map.add
            "ManualDifficultyReduction"
            { State.initial.Upgrades["ManualDifficultyReduction"] with
                Level = reductionLevel }

    let miners =
        State.initial.AutoMiners
        |> Map.add
            "Monkey"
            { State.initial.AutoMiners["Monkey"] with
                Owned = monkeyOwned }
        |> Map.add
            "RestingYouth"
            { State.initial.AutoMiners["RestingYouth"] with
                Owned = youthOwned }
        |> Map.add
            "Gpu"
            { State.initial.AutoMiners["Gpu"] with
                Owned = gpuOwned }

    { State.initial with
        Economy = { State.initial.Economy with LifetimeMinedCoins = lifetimeMined }
        Upgrades = upgrades
        AutoMiners = miners
        Challenge =
            { State.initial.Challenge with
                Prompt = "2+2"
                ExpectedAnswer = Some 4
                Difficulty = 1 } }

[<Tests>]
let gameplayLoopTests =
    testList "KAIM-003 gameplay loop" [
        testCase "wrong answer gives X feedback, no penalty, and rotates challenge immediately" <| fun _ ->
            let before =
                { seededState 0m 0 0 0 0 with
                    Economy = { State.initial.Economy with Coins = 10m; LifetimeMinedCoins = 10m } }

            let after, effects = Update.update (Msg.ChallengeSubmitted 999) before

            Expect.equal after.Challenge.LastFeedback (Some ChallengeFeedback.X) "wrong answer should produce X feedback"
            Expect.notEqual after.Challenge.Prompt before.Challenge.Prompt "challenge should rotate immediately"
            Expect.notEqual after.Challenge.ExpectedAnswer before.Challenge.ExpectedAnswer "expected answer should rotate immediately"
            Expect.equal after.Economy.Coins before.Economy.Coins "wrong answer should not reduce or increase coin balance"
            Expect.equal after.Economy.LifetimeMinedCoins before.Economy.LifetimeMinedCoins "wrong answer should not alter lifetime mined coins"
            Expect.isEmpty effects "challenge submission should be pure"

        testCase "correct answer gives O feedback and rewards manual mining" <| fun _ ->
            let before =
                { seededState 220m 0 0 0 0 with
                    Economy = { State.initial.Economy with Coins = 3m; LifetimeMinedCoins = 220m; Cash = 0m } }

            let after, _ = Update.update (Msg.ChallengeSubmitted 4) before

            Expect.equal after.Challenge.LastFeedback (Some ChallengeFeedback.O) "correct answer should produce O feedback"
            Expect.isGreaterThan after.Economy.Coins before.Economy.Coins "manual mining should increase spendable coins"
            Expect.isGreaterThan after.Economy.LifetimeMinedCoins before.Economy.LifetimeMinedCoins "manual mining should increase lifetime mined coins"

        testCase "difficulty increases every 100 lifetime mined coins and reduction upgrade softens it" <| fun _ ->
            let baseline = seededState 300m 0 0 0 0
            let softened = seededState 300m 2 0 0 0

            let baselineAfter, _ = Update.update (Msg.ChallengeSubmitted 4) baseline
            let softenedAfter, _ = Update.update (Msg.ChallengeSubmitted 4) softened

            Expect.equal baselineAfter.Challenge.Difficulty 4 "300 mined coins should produce base difficulty 4"
            Expect.equal softenedAfter.Challenge.Difficulty 2 "manual difficulty reduction level should soften generated challenge difficulty"

        testCase "auto-miner output matches Monkey 0.1/s, Resting Youth 1/s, GPU 10/s" <| fun _ ->
            let before = seededState 0m 0 1 1 1

            let firstTick, _ = Update.update (Msg.Tick (DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero))) before
            let secondTick, _ = Update.update (Msg.Tick (DateTimeOffset(2026, 5, 5, 0, 0, 10, TimeSpan.Zero))) firstTick

            Expect.equal secondTick.Economy.Coins 111m "10 seconds should produce 111 KaiCoin with one of each miner"
            Expect.equal secondTick.Economy.LifetimeMinedCoins 111m "auto-mined coins should count toward lifetime progression"

        testCase "challenge generator keeps arithmetic rapid-entry constraints" <| fun _ ->
            let generated =
                [ 0 .. 120 ]
                |> List.map (fun nonce -> Challenge.generate 3 nonce)

            for problem in generated do
                Expect.isLessThanOrEqual problem.Prompt.Length 12 "prompt should be short for rapid entry"
                match problem.Operator with
                | ArithmeticOperator.Subtract ->
                    Expect.isGreaterThanOrEqual problem.Left problem.Right "subtraction should be non-negative"
                    Expect.equal problem.Answer (problem.Left - problem.Right) "subtraction answer should match"
                | ArithmeticOperator.Divide ->
                    Expect.equal (problem.Left % problem.Right) 0 "division should avoid fractional answers"
                    Expect.equal problem.Answer (problem.Left / problem.Right) "division answer should be exact"
                | ArithmeticOperator.Add ->
                    Expect.equal problem.Answer (problem.Left + problem.Right) "addition answer should match"
                | ArithmeticOperator.Multiply ->
                    Expect.equal problem.Answer (problem.Left * problem.Right) "multiplication answer should match"
    ]
