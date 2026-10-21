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
        | Place(playerId, playerCard) -> addCardToBoard state playerId playerCard
        | Collect(playerId, playerCard, cardsForCollection) ->
            collectFromBord state playerId playerCard cardsForCollection
        | Merge(playerId, playerCard, cardsForMerging) -> mergeCards state playerId cardsForMerging playerCard



module SummarizeState =
    
    open System

    let internal createPlayerStats (player: Player) (playerCount: int) (isLastCapture: bool) : PlayerStats =
        let newStats: PlayerStats =
            player.CapturedCards
            |> List.fold
                (fun stat card ->
                    match card with
                    | { Rank = Ace } ->
                        { stat with
                            Aces = card :: stat.Aces
                            Points = stat.Points + 1 }
                    | { Rank = Number 10; Suit = Diamond } ->
                        { stat with
                            Specials = card :: stat.Specials
                            Points = stat.Points + 2 }
                    | { Rank = Number 2; Suit = Club } ->
                        { stat with
                            Specials = card :: stat.Specials
                            Points = stat.Points + 1
                            Clubs = card :: stat.Clubs }
                    | { Suit = Club } -> { stat with Clubs = card :: stat.Clubs }
                    | _ -> stat)
                { Points = 0
                  Specials = []
                  Clubs = []
                  Aces = []
                  Sweeps = []
                  Player = player
                  LastCapture = isLastCapture
                  MostCards = None}

        let newStats =
            match player.Sweeps with
            | n when n.Length > 0 ->
                { newStats with
                    Points = newStats.Points + n.Length }
            | _ -> newStats

        let hasMostCards = (52 / playerCount) < player.CapturedCards.Length

        let newStats =
            if hasMostCards then
                { newStats with
                    Points = newStats.Points + 1
                    MostCards = Some player.Id }
            else
                newStats

        let capturedClubs = List.filter (fun card -> card.Suit = Club) player.CapturedCards

        { newStats with
            Points =
                if capturedClubs.Length >= 7 then
                    newStats.Points + 1
                else
                    newStats.Points }

    let public summarizeGame (state: GameState) : GameStats =
        let updatedState =
            match state.LastCaptured with
            | None -> state
            | Some playerId ->
                let remainingCards: Card list = state.Board.Slots |> List.map _.Cards |> List.concat 
                let player: Player = state.Players[playerId]
                let updatedPlayer: Player = { player with CapturedCards = player.CapturedCards @ remainingCards }
                let updatedPlayers: Map<Guid, Player> = Map.add playerId updatedPlayer state.Players
                {
                    state with 
                        Players = updatedPlayers
                        GameState.Board.Slots = []
                }

        let playersCount = updatedState.Players.Count
        let playersList = Seq.toList updatedState.Players.Values

        let playerStats: PlayerStats list =
            List.map
                (fun player ->
                    let isLastCapture =
                        match updatedState.LastCaptured with
                        | Some id -> id = player.Id
                        | None -> false

                    createPlayerStats player playersCount isLastCapture)
                playersList

        { Winner = playerStats |> List.maxBy _.Points |> _.Player
          Stats = playerStats }
