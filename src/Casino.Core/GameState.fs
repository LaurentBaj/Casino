namespace Casino.Core.GameState

open Casino.Core.Models
open Casino.Core.DeckCreation
open Casino.Core.PlayerService

module InitGameState =

    let private getRoundsBasedOnPlayerCount count : Round list =
        let drawAmount = 4
        let initialDrawCount = (drawAmount * count) + drawAmount
        let remainingCards = 52 - initialDrawCount

        // initial round + (what remains after first draw) / cards per player
        let totalRounds = 1 + (remainingCards / (count * drawAmount))

        let inBetweenRounds = [ 2 .. totalRounds - 1 ] |> List.map (fun _ -> InBetween)

        [ yield First; yield! inBetweenRounds; yield Final ]


    /// <summary>
    /// Creates a new game state based on player count
    /// </summary>
    /// <remark>
    /// Ideal when starting a new game
    /// </remark>
    let public initializeGame playerCount : GameState =

        let fullDeck = initializeDeck Shuffled
        let players = initializePlayers playerCount
        let rounds = getRoundsBasedOnPlayerCount playerCount

        { Deck = fullDeck
          Board = { Slots = [] }
          Players = players
          Rounds = rounds
          LastCaptured = None
          PlayerTurn = None
          CurrentRound = First }



module PlayerActions =

    open Casino.Core.BoardOperations

    /// <summary>
    /// Returns a response based on player action
    /// </summary>
    /// <param name="state">GameState</param>
    /// <param name="action">PlayerAction</param>
    /// <returns>For now it returns an updated Game state</returns>
    let public playerTurn state action =
        match action with
        | Place   (playerId, playerCard) -> addCardToBoard state playerId playerCard
        | Collect (playerId, playerCard, cardsForCollection) -> collectFromBord state playerId playerCard cardsForCollection
        | Merge   (playerId, playerCard, cardsForMerging) -> mergeCards state playerId cardsForMerging playerCard
