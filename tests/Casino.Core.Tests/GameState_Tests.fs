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
          yield $"%A{card}" ]


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
open Casino.Core.GameState.PlayerActions


let cards: Card list =
    [ { Rank = Number 2; Suit = Suit.Club }
      { Rank = Number 9; Suit = Suit.Diamond }
      { Rank = Jack; Suit = Suit.Heart } ]

let board: Board = { Slots = cards |> List.map toSlot }

let players: Map<Guid, Player> = initializePlayers 3

let state: GameState =
    { Deck = initializeDeck DeckStatus.Shuffled
      Players = players
      Board = board
      PlayerTurn = None
      Rounds = [ First; InBetween; InBetween; Final ]
      LastCaptured = None
      CurrentRound = First }

[<Fact>]
let ``Player Place Action`` () =
    let playerCard = { Rank = Number 7; Suit = Club }
    let remainingCard = { Rank = Number 3; Suit = Diamond }
    let playerId = Guid.NewGuid()

    let player =
        { Id = playerId
          Name = "Player"
          Hand = [ playerCard; remainingCard ]
          CapturedCards = []
          Sweeps = [] }

    let testState =
        { state with
            Board = { Slots = [] }
            Players = [ playerId, player ] |> Map.ofList
            PlayerTurn = Some playerId }

    let stateAfterPlayerPlace = playerTurn testState (Place(playerId, playerCard))

    match stateAfterPlayerPlace with
    | Valid state ->
        let updatedPlayer = state.Players[playerId]

        state.Board.Slots.Length |> should equal 1
        state.Board.Slots.Head.Cards |> should equal [ playerCard ]
        updatedPlayer.Hand |> should equal [ remainingCard ]
    | Invalid msg -> failwith msg

[<Fact>]
let ``Player Place Action fails when player does not have card`` () =
    let playerCard = { Rank = Number 7; Suit = Club }
    let cardNotInHand = { Rank = Number 3; Suit = Diamond }
    let playerId = Guid.NewGuid()

    let player =
        { Id = playerId
          Name = "Player"
          Hand = [ playerCard ]
          CapturedCards = []
          Sweeps = [] }

    let testState =
        { state with
            Board = { Slots = [] }
            Players = [ playerId, player ] |> Map.ofList
            PlayerTurn = Some playerId }

    let result = playerTurn testState (Place(playerId, cardNotInHand))

    match result with
    | Valid _ -> failwith "Expected Place action to be invalid"
    | Invalid msg -> msg |> should equal "Player does not possess card for placement"

[<Fact>]
let ``Player Collect Action`` () =
    let collectingPlayerId = Guid.NewGuid()
    let otherPlayerId = Guid.NewGuid()

    let collectionCard = { Rank = Jack; Suit = Club }
    let c1 = { Rank = Number 5; Suit = Heart }
    let c2 = { Rank = Number 6; Suit = Spade }
    let slotForCollection = { Cards = [ c1; c2 ]; AggregatePoints = 11 }
    let existingSweepCard = { Rank = Ace; Suit = Diamond }

    let collectingPlayer =
        { Id = collectingPlayerId
          Name = "Collector"
          Hand = [ collectionCard ]
          CapturedCards = []
          Sweeps = [] }

    let otherPlayer =
        { Id = otherPlayerId
          Name = "Other Player"
          Hand = []
          CapturedCards = []
          Sweeps = [ existingSweepCard ] }
        
    let testPlayers =
        Map [ collectingPlayerId, collectingPlayer
              otherPlayerId, otherPlayer ]

    let testState =
        { state with
            Board = { Slots = [ slotForCollection ] }
            PlayerTurn = Some collectingPlayerId
            Players = testPlayers }

    let playerAction = Collect(collectingPlayerId, collectionCard, [ slotForCollection ])
    let updatedState = playerTurn testState playerAction

    match updatedState with
    | Invalid msg -> failwith msg
    | Valid state ->
        let updatedCollectingPlayer = state.Players[collectingPlayerId]
        let updatedOtherPlayer = state.Players[otherPlayerId]

        state.Board.Slots |> List.isEmpty |> should equal true
        updatedCollectingPlayer.Hand |> List.isEmpty |> should equal true
        updatedCollectingPlayer.CapturedCards |> should equal [ c1; c2 ]
        updatedCollectingPlayer.Sweeps |> List.isEmpty |> should equal true
        updatedOtherPlayer.Sweeps |> List.isEmpty |> should equal true
