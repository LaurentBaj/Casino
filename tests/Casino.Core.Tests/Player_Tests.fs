module Casino.Core.Tests.PlayerTests

open System
open Xunit
open FsUnit.Xunit
open Casino.Core.PlayerService
open Casino.Core.Models

[<Theory>]
[<InlineData(2)>]
[<InlineData(3)>]
[<InlineData(4)>]
let ``Player initialization`` (count: int) : unit =
    let players: Map<Guid, Player> = initializePlayers count
    players.Count |> should equal count

    for player in players.Values do
        player.Id |> should not' (equal Guid.Empty)
        player.CapturedCards.Length |> should equal 0
        player.Hand.Length |> should equal 0
        player.Name |> should not' (equal "")
