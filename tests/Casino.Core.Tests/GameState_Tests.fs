module Casino.Core.Tests.GameState_Tests

open Casino.Core
open Casino.Core.Models
open Casino.Core.GameState
open Xunit
open FsUnit.Xunit
open DeckOperations
open InitGameState

[<Theory>]
[<InlineData(2, 6)>]
[<InlineData(3, 4)>]
[<InlineData(4, 3)>]
let ``Board initialization rounds test`` (playerCount, expectedRounds) =
    let newGame = initializeGame playerCount

    newGame.Rounds.Length |> should equal expectedRounds
    newGame.Rounds.Head |> should equal First
    newGame.Rounds[expectedRounds - 1] |> should equal Final

    for i in 1 .. expectedRounds - 2 do
        newGame.Rounds[i] |> should equal InBetween

let printPlayerHand (hand: Card list) =
    [ for card in hand do
          yield sprintf "%A" card ]


[<Theory>]
[<InlineData(2)>]
[<InlineData(3)>]
[<InlineData(4)>]
let ``Deal cards to players and board if necessary`` playerCount =
    let newGame = initializeGame playerCount
    newGame.Players.Count |> should equal playerCount

    newGame.Players
    |> Map.iter (fun _ player -> player.Hand.Length |> should equal 0)

    let updatedState = dealCards newGame

    updatedState.Players
    |> Map.iter (fun _ player ->
        player.Hand.Length |> should equal 4
        printfn $"Player id {player.Id}: {printPlayerHand player.Hand}")
