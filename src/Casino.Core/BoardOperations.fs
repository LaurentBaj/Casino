module Casino.Core.BoardOperations

open Casino.Core.Models
open Casino.Core.PlayerService
open System

/// <summary> Helper function for player 'Place' action</summary>
let internal addCardToBoard board card =
    { board with
        Cards = card :: board.Cards }

let private rankValue =
    fun card ->
        match card.Rank with
        | Number n -> n
        | Ace -> 14
        | King -> 13
        | Queen -> 12
        | Jack -> 11

/// <summary> Helper function for when a player collects cards from board</summary>
let internal collectFromBord
    (state: GameState)
    (playerId: Guid)
    (playerCard: Card)
    (cardsForCollection: Card list)
    : PlayerActionResult =

    let playerCardValue = rankValue playerCard
    let collectionSum = cardsForCollection |> List.sumBy rankValue

    if collectionSum > 0 && collectionSum = playerCardValue then

        let updatedBoardCards = state.Board.Cards |> List.except cardsForCollection

        let updatedBoard =
            { state.Board with
                Cards = updatedBoardCards }

        let player = state.Players.[playerId]
        let updatedHand = player.Hand |> List.filter (fun c -> c <> playerCard)
        let updatedCaptured = player.CapturedCards @ playerCard :: cardsForCollection

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
        Invalid "Insufficient (rank) points for collection"

// Merging goal: 7
// Cards on table mighht be: 2 - 5 - 7 - 9
// Available pairs: '2- 5'  and  '7'
let public mergeCards (state: GameState) (playerId: Guid) (cardsForMerging: Card list) : PlayerActionResult =

    let mergeSum = List.sumBy (fun card -> rankValue card) cardsForMerging


    Valid state
