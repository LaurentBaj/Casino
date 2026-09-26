namespace Casino.Core.GameState

open System
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
          Board = { Cards = [] }
          Players = players
          Rounds = rounds
          LastCaptured = None
          PlayerTurn = None
          CurrentRound = First }



module PlayerActions =

    open Casino.Core.BoardOperations

    let private verifyId (id: Guid option) (players: Map<Guid, Player>): Result<Player, string> =
        match Map.tryFind id.Value players with
        | Some player -> Ok player
        | None -> Error "Player was not found"

    let private UpdateStateAfterAction (state: GameState) action =
        match action with
        | Place card ->
            let updatedBoard = addCardToBoard state.Board card
            { state with Board = updatedBoard }
        | Merge cards ->
            let updatedState = mergeCards state state.PlayerTurn.Value cards
            match updatedState with
            | Valid state -> state
            | Invalid msg -> failwith msg
        | Collect (playerCard, cardsForCollection) ->
            let updatedState = collectFromBord state state.PlayerTurn.Value playerCard cardsForCollection
            match updatedState with
            | Valid state -> state
            | Invalid msg -> failwith msg
                
    /// <summary>
    /// Returns a response based on player action
    /// </summary>
    /// <param name="state">GameState</param>
    /// <param name="action">PlayerAction</param>
    /// <returns>For now it returns an updated Game state</returns>
    let public playerTurn state action =

        let player =
            match verifyId state.PlayerTurn state.Players with
            | Ok player -> player
            | Error msg -> failwith msg

        match action with
        | Place card ->
            let updatedBoard = addCardToBoard state.Board card 
            let updatedState = { state with Board = updatedBoard }
            Valid updatedState
        | Merge _ -> Valid state
        | Collect _ -> Valid state
            
