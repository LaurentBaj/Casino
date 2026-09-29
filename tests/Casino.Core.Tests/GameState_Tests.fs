module Casino.Core.Tests.GameState_Tests

open System
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


// Player Actions
open Casino.Core.DeckCreation
open Casino.Core.PlayerService
open Casino.Core.GameState
open Casino.Core.GameState.PlayerActions


let cards: Card list =
    [ { Rank = Number 2; Suit = Suit.Club }
      { Rank = Number 9; Suit = Suit.Diamond }
      { Rank = Jack; Suit = Suit.Heart } ]

let board: Board = { Slots = cards |> List.map (fun card -> toSlot card) }

let players: Map<Guid, Player> = initializePlayers 3

let state: GameState =
    { Deck = initializeDeck DeckStatus.Shuffled
      Players = players
      Board = board
      PlayerTurn = None
      Rounds = [ First; InBetween; InBetween; Final ]
      LastCaptured = None
      CurrentRound = First }


let ``Player Place Action`` () =

    let updateStateAfterDeal = dealCards state
    let randomPlayerId = players.Keys |> Seq.toArray |> Array.item 0

    let player = updateStateAfterDeal.Players[randomPlayerId]
    let playerAction = Place player.Hand.Head

    let stateAfterPlayerPlace = playerTurn updateStateAfterDeal playerAction

    match stateAfterPlayerPlace with
    | Valid state -> state.Board.Slots.Length |> should equal 1
    | Invalid msg -> failwith msg


let ``Player Collect Action`` () = 
    let updatedState = dealCards state
    let firstId: Guid = players.Keys |> Seq.toArray |> Array.item 0
    let player = updatedState.Players[firstId]

    let c1 = { Rank = Number 5; Suit = Heart }
    let c2 = { Rank = Number 6; Suit = Spade }
    let newSlot = { Cards = [ c1; c2 ]; AggregatePoints = 9 }
    let updatedState = { updatedState with Board = { Slots = [newSlot] @ updatedState.Board.Slots } }    

    let collectionCard = { Rank = Jack; Suit = Club }
    let updatedPlayer = { player with  Hand = [collectionCard] @ player.Hand }

    let slotsForCollection =
        state.Board.Slots
        |> List.filter (fun slot -> slot.AggregatePoints = rankValue collectionCard)
    
    let playerAction = Collect (updatedPlayer.Hand.Head, slotsForCollection)
    let updatedState = playerTurn updatedState playerAction

    // Verify it worked
    

