module Casino.Core.Models

open System

type Suit =
    | Diamond
    | Club
    | Heart
    | Spade

type Rank =
    | Number of int
    | Jack
    | Queen
    | King
    | Ace

type Card = { Suit: Suit; Rank: Rank }

type DeckStatus =
    | Shuffled
    | UnShuffled

type Player =
    { Id: Guid
      Name: string
      Hand: Card list
      CapturedCards: Card list
      Sweeps: Card list }

// Represents slot on the board. Comprised of one or many cards
type Slot =
    { Cards: Card list
      AggregatePoints: int }

type Board = { Slots: Slot list }

type Round =
    | First 
    | InBetween
    | Final 

type GameState =
    { Deck: Card list
      Players: Map<Guid, Player>
      Board: Board
      PlayerTurn: Guid option
      LastCaptured: Guid option
      Rounds: Round list
      CurrentRound: Round }

type PlayerAction =
    | Place of playerId: Guid * playerCard: Card
    | Merge of playerId: Guid * playerCard: Card * mergeCards: Slot list list // Perhaps I could use grouping
    | Collect of playerId: Guid * playerCard: Card * collectionCards: Slot list

type PlayerActionResult =
    | Valid of GameState
    | Invalid of string
