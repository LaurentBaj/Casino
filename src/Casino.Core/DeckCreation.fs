namespace Casino.Core

open System
open Casino.Core.Models

module DeckCreation =

    let private createUnShuffledDeck () : Card list =
        [ for suit in [ Spade; Club; Heart; Diamond ] do
              for value in 2..10 do
                  yield { Suit = suit; Rank = Number value }

              for rank in [ Jack; Queen; King; Ace ] do
                  yield { Suit = suit; Rank = rank } ]

    let private shuffleDeck (deck: Card list) : Card list =
        let arr = deck |> List.toArray
        let rand = Random()

        for i in 0 .. arr.Length - 1 do
            let j = rand.Next(i + 1)

            let temp = arr[i]
            arr[i] <- arr[j]
            arr[j] <- temp

        arr |> Array.toList

    /// <summary>
    /// Creates a standard 52-card deck.
    /// </summary>
    /// <param name="shuffled">Determines whether the returned deck is shuffled or unshuffled.</param>
    /// <returns>A list of cards representing the initialized deck.</returns>
    let public initializeDeck shuffled =
        let deck = createUnShuffledDeck ()

        match shuffled with
        | Shuffled -> deck |> shuffleDeck
        | UnShuffled -> deck

module DeckOperations =

    open System
    open Casino.Core.Models

    let internal rankValue =
        fun card ->
            match card.Rank with
            | Number n -> n
            | Ace -> 14
            | King -> 13
            | Queen -> 12
            | Jack -> 11

    let private dealCardsToPlayers (players: Map<Guid, Player>) (deltCards: Card list) =
        let updatedPlayers, _ =
            Map.fold
                (fun (accMap, remainingCards) id player ->
                    let newHand, remainingCardsFromDeck = List.splitAt 4 remainingCards

                    let updatedPlayer =
                        { player with
                            Hand = player.Hand @ newHand }

                    let newMap = Map.add id updatedPlayer accMap
                    newMap, remainingCardsFromDeck)
                (Map.empty, deltCards)
                players

        updatedPlayers

    /// <summary>
    /// Deal cards to all players (and table if first round)
    /// </summary>
    let public dealCards state : GameState =
        let players = state.Players
        let deck = state.Deck

        let withdrawalAmount = 4
        let withDrawalAmountPerPlayer = players.Count * withdrawalAmount

        let cardsForPlayers, cardsForBoard, updatedDeck =
            match state.CurrentRound with
            | First ->
                let playerDraw, remaining = deck |> List.splitAt withDrawalAmountPerPlayer
                let boardDraw, finalDeck = remaining |> List.splitAt withdrawalAmount
                playerDraw, boardDraw, finalDeck
            | _ ->
                let playerDraw, finalDeck = List.splitAt withDrawalAmountPerPlayer deck
                playerDraw, [], finalDeck

        let updatedPlayers = dealCardsToPlayers players cardsForPlayers

        let slotsForFirstRound (initialBoardCards: Card list) : Slot list =
            initialBoardCards
            |> List.map (fun card ->
                let points = rankValue card

                { Cards = [ card ]
                  AggregateRankPoints = points })

        let slots =
            match state.CurrentRound with
            | First -> slotsForFirstRound cardsForBoard
            | _ -> []

        { state with
            Deck = updatedDeck
            Board = { state.Board with Slots = slots }
            Players = updatedPlayers }
