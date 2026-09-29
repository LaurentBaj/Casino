module Casino.Core.Models

open System
open System.Collections.Generic

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

// Represents card stack bound by a single card or aggregate of multiple cards
type Slot =
    { mutable Cards: Card list
      mutable AggregatePoints: int }

type Board = { Slots: Slot list }

type Round =
    | First // Table draws four cards as well as players
    | Final //  Should notify
    | InBetween // draw only to players

// Modify GameState objects as the fields don't make sense
type GameState =
    { Deck: Card list
      Players: Map<Guid, Player>
      Board: Board
      PlayerTurn: Guid option
      LastCaptured: Guid option
      Rounds: Round list
      CurrentRound: Round }

type PlayerAction =
    | Place of Card
    | Merge of Card list
    | Collect of playerCard: Card * collectionCards: Slot list

type PlayerActionResult =
    | Valid of GameState
    | Invalid of string
