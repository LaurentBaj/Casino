module Casino.Core.BoardOperations

open Casino.Core.DeckOperations
open Casino.Core.Models
open Casino.Core.PlayerService
open System


/// <summary> Helper function for player 'Place' action</summary>
let internal addCardToBoard board card : Board =
    let placementCardRank: int = rankValue card

    let newSlot: Slot =
        { Cards = [ card ]
          AggregatePoints = placementCardRank }

    { board with
        Slots = [ newSlot ] @ board.Slots }

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

    let collectionSum =
        cardsForCollection |> List.sumBy (fun slot -> slot.AggregatePoints)

    if collectionSum > 0 && collectionSum = playerCardValue then

        let updatedSlots =
            state.Board.Slots
            |> List.filter (fun slot -> slot.AggregatePoints <> collectionSum)

        let updatedBoard =
            { state.Board with
                Slots = updatedSlots }

        let player = state.Players.[playerId]
        let updatedHand = player.Hand |> List.filter (fun c -> c <> playerCard)

        // TODO: add helper for mapping slot <--> card
        let cardsCollected =
            cardsForCollection
            |> List.fold (fun cardList currentSlot -> currentSlot.Cards @ cardList) []

        let updatedCaptured = player.CapturedCards @ playerCard :: cardsCollected

        let updatedPlayer =
            { player with
                Hand = updatedHand
                CapturedCards = updatedCaptured }

        let updatedPlayers = state.Players |> Map.add playerId updatedPlayer

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
let public mergeCards (state: GameState) (playerId: Guid) (cardsForMerging: Card list) : PlayerActionResult =

    let mergeSum = List.sumBy (fun card -> rankValue card) cardsForMerging

    Valid state
