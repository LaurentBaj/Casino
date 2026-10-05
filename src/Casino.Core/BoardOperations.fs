module Casino.Core.BoardOperations

open Casino.Core.DeckOperations
open Casino.Core.Models
open Casino.Core.PlayerService
open System

/// <summary> Helper function for player 'Place' action</summary>
//  <remark> There are no possible invalid states that I can see here</remark>
let internal addCardToBoard state playerId playerCard : PlayerActionResult =
    let player = state.Players[playerId]
    let playerHasCard = player.Hand |> List.exists (fun card -> card = playerCard)
    
    if not playerHasCard then
        Invalid "Player does not possess card for placement"
    else
        let placementCardRank: int = rankValue playerCard

        let newSlot: Slot =
            { Cards = [ playerCard ]
              AggregatePoints = placementCardRank }

        let updatedBoard =
            { state.Board with
                Slots = [ newSlot ] @ state.Board.Slots }

        let updatedPlayer =
            { player with
                Hand = player.Hand |> List.except [ playerCard ] }

        let updatedPlayers = Map.add playerId updatedPlayer state.Players
        
        Valid { state with
                    Board = updatedBoard
                    Players = updatedPlayers }

/// <summary>
///  Helper function for when a player collects cards from board
/// </summary>
/// <remark>
/// Cards for collection should be chosen by player and not automatically be calculated
/// </remark>
let internal collectFromBord
    (state: GameState)
    (playerId: Guid)
    (playerCard: Card)
    (cardsForCollection: Slot list)
    : PlayerActionResult =

    let playerCardValue = rankValue playerCard

    let collectionSum = List.sumBy _.AggregatePoints cardsForCollection

    if collectionSum > 0 && collectionSum = playerCardValue then

        let updatedSlots =
            state.Board.Slots
            |> List.filter (fun slot -> not (cardsForCollection |> List.contains slot))

        let updatedBoard =
            { state.Board with
                Slots = updatedSlots }

        let player = state.Players[playerId]
        let updatedHand = player.Hand |> List.filter (fun c -> c <> playerCard)

        let cardsCollected =
            cardsForCollection
            |> List.fold (fun cardList currentSlot -> currentSlot.Cards @ cardList) []

        let isSweep: bool = updatedBoard.Slots.Length = 0

        let otherPlayersHaveSweep =
            state.Players
            |> Map.exists (fun id player -> id <> playerId && not player.Sweeps.IsEmpty)

        let playersAfterSweepPenalty =
            if isSweep && otherPlayersHaveSweep then
                handlePlayerSweep player.Id state.Players
            else
                state.Players

        let updatedCaptured =
            match isSweep with
            | true -> player.CapturedCards @ cardsCollected
            | _ -> player.CapturedCards @ playerCard :: cardsCollected

        let updateSweepCards =
            match isSweep with
            | true when not otherPlayersHaveSweep -> player.Sweeps @ [ playerCard ]
            | _ -> player.Sweeps

        let updatedPlayer =
            { player with
                Hand = updatedHand
                CapturedCards = updatedCaptured
                Sweeps = updateSweepCards }

        let updatedPlayers = playersAfterSweepPenalty |> Map.add playerId updatedPlayer

        let updatedState =
            { state with
                Board = updatedBoard
                Players = updatedPlayers }

        Valid updatedState
    else
        Invalid $"Insufficient (rank) points for collection. Target: {collectionSum} - Player card: {playerCard.Rank}"


// Merging goal: 7
// Cards on table mighht be: 2 - 5 - 7 - 9
// Available pairs: '2- 5'  and  '7'
let public mergeCards
    (state: GameState)
    (playerId: Guid)
    (cardsForMerging: Slot list list)
    (playerCard: Card)
    : PlayerActionResult =

    let verifySlotGrouping =
        fun (slotList: Slot list) ->
            slotList |> List.forall (fun slot -> slot.Cards.Length = 1)
            && slotList |> List.sumBy (fun slot -> slot.AggregatePoints) = rankValue playerCard

    let isValidCollection = List.forall verifySlotGrouping cardsForMerging

    let player = state.Players[playerId]

    let isValidPlayerAction =
        player.Hand
        |> List.exists (fun card -> card <> playerCard && rankValue playerCard = rankValue card)

    if not isValidCollection then
        Invalid "Invalid slot grouping or cards for merging"
    elif not isValidPlayerAction then
        Invalid "Player does not hold card required for collecting merge collection later"
    else
        let updatedBoard =
            { state.Board with
                Slots = state.Board.Slots |> List.except (cardsForMerging |> List.concat) }

        let updatedPlayer =
            { player with
                Hand = player.Hand |> List.except [ playerCard ] }

        let updatedPlayers = Map.add player.Id updatedPlayer state.Players

        Valid
            { state with
                Board = updatedBoard
                Players = updatedPlayers }
