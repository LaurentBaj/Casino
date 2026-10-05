module Casino.Core.PlayerService

open System
open Casino.Core.Models

let private createPlayer i =
    { Name = i.ToString()
      CapturedCards = []
      Hand = []
      Id = Guid.NewGuid()
      Sweeps = [] }

let private mapPlayer = fun table player -> Map.add player.Id player table

let internal initializePlayers count : Map<Guid, Player> =
    [ 1..count ] |> List.map createPlayer |> List.fold mapPlayer Map.empty

let internal updatedPlayerHand =
    fun player cards ->
        let playerHand = player.Hand
        let updatedHand = cards @ playerHand
        { player with Hand = updatedHand }

let internal handlePlayerSweep (playerId: Guid) (players: Map<Guid, Player>) : Map<Guid, Player> =
    players
    |> Map.map (fun id player ->
        if id <> playerId then
            { player with
                Sweeps =
                    match player.Sweeps with
                    | [] -> []
                    | _ :: tail -> tail }
        else
            player)
