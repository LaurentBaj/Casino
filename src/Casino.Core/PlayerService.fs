module Casino.Core.PlayerService

open System
open Casino.Core.Models

let private createPlayer i =
    { Name = i.ToString()
      CapturedCards = []
      Hand = []
      Id = Guid.NewGuid() }

let private mapPlayer = fun acc player -> Map.add player.Id player acc

let internal initializePlayers count : Map<Guid, Player> =
    [ 1..count ] |> List.map createPlayer |> List.fold mapPlayer Map.empty

let internal updatedPlayerHand =
    fun player cards ->
        let playerHand = player.Hand
        let updatedHand = cards @ playerHand
        { player with Hand = updatedHand }
