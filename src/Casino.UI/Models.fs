namespace Casino.UI

open System

// Communication DTOs between Server and Client

type PlayerStateDto = {
    Id: Guid
    CardCount: int
    CapturedCount: int
    SweepsCount: int
}

type SlotDto = {
    Cards: string list
    AggregatePoints: int
}

type ClientGameStateDto = {
    CurrentRound: string
    BoardSlots: SlotDto list
    MyHand: string list
    MyCaptured: string list
    MySweeps: string list
    Opponents: PlayerStateDto list
    IsMyTurn: bool
}

type ClientMessage =
    | Join of string
    | PlayCard of string

type ServerMessage =
    | GameState of ClientGameStateDto
    | WaitingForPlayers of int
    | Error of string
