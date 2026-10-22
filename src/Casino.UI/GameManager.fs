namespace Casino.UI

open System
open System.Collections.Concurrent
open System.Net.WebSockets
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading
open Casino.Core
open Casino.Core.Models

module GameManager =

    type ConnectedPlayer = {
        Id: Guid
        Socket: WebSocket
    }

    let mutable activeGame : GameState option = None
    let players = ConcurrentDictionary<Guid, ConnectedPlayer>()

    let mapCardToString (c: Card) =
        let suitStr =
            match c.Suit with
            | Spade -> "Spades"
            | Club -> "Clubs"
            | Heart -> "Hearts"
            | Diamond -> "Diamonds"

        let rankStr =
            match c.Rank with
            | Number n -> string n
            | Jack -> "Jack"
            | Queen -> "Queen"
            | King -> "King"
            | Ace -> "Ace"

        $"{rankStr} of {suitStr}"

    let getClientState (state: GameState) (playerId: Guid) : ClientGameStateDto =
        let myPlayer = state.Players[playerId]

        let opponents =
            state.Players
            |> Map.toList
            |> List.filter (fun (id, _) -> id <> playerId)
            |> List.map (fun (id, p) ->
                { Casino.UI.PlayerStateDto.Id = id
                  CardCount = p.Hand.Length
                  CapturedCount = p.CapturedCards.Length
                  SweepsCount = p.Sweeps.Length })

        let slots =
            state.Board.Slots
            |> List.map (fun s ->
                let dto: Casino.UI.SlotDto = {
                  Cards = s.Cards |> List.map mapCardToString
                  AggregatePoints = s.AggregatePoints
                }
                dto)

        { CurrentRound = (sprintf "%A" state.CurrentRound)
          BoardSlots = slots
          MyHand = myPlayer.Hand |> List.map mapCardToString
          MyCaptured = myPlayer.CapturedCards |> List.map mapCardToString
          MySweeps = myPlayer.Sweeps |> List.map mapCardToString
          Opponents = opponents
          IsMyTurn = (state.PlayerTurn = Some playerId) }

    // Helper to serialize discriminated unions cleanly for Blazor
    let serializeOptions = JsonSerializerOptions()
    serializeOptions.Converters.Add(JsonFSharpConverter())

    let broadcastState (state: GameState) =
        task {
            for kvp in players do
                let p = kvp.Value
                if p.Socket.State = WebSocketState.Open then
                    let clientState = getClientState state p.Id
                    let msg = ServerMessage.GameState clientState
                    let json = JsonSerializer.Serialize(msg, serializeOptions)
                    let bytes = System.Text.Encoding.UTF8.GetBytes(json)
                    do! p.Socket.SendAsync(ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None)
        }

    let initializeGameIfReady () =
        if players.Count = 2 then
            let playerIds = players.Keys |> Seq.toList
            let deck = DeckCreation.initializeDeck Shuffled

            let p1 : Player = { Id = playerIds.[0]; Name = "Player 1"; Hand = []; CapturedCards = []; Sweeps = [] }
            let p2 : Player = { Id = playerIds.[1]; Name = "Player 2"; Hand = []; CapturedCards = []; Sweeps = [] }

            let playerMap =
                Map.empty
                |> Map.add p1.Id p1
                |> Map.add p2.Id p2

            let initialState = {
                Board = { Slots = [] }
                Deck = deck
                Players = playerMap
                LastCaptured = None
                CurrentRound = First
                Rounds = []
                PlayerTurn = Some p1.Id
            }

            let stateAfterDeal = DeckOperations.dealCards initialState
            activeGame <- Some stateAfterDeal

            broadcastState stateAfterDeal |> ignore

    let handleJoin (socket: WebSocket) =
        let id = Guid.NewGuid()
        let player = { Id = id; Socket = socket }
        players.TryAdd(id, player) |> ignore

        task {
            if players.Count < 2 then
                let msg = ServerMessage.WaitingForPlayers (2 - players.Count)
                let json = JsonSerializer.Serialize(msg, serializeOptions)
                let bytes = System.Text.Encoding.UTF8.GetBytes(json)
                do! socket.SendAsync(ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None)
            else
                initializeGameIfReady ()
        } |> ignore

        id

    let removePlayer (id: Guid) =
        players.TryRemove(id) |> ignore
        if players.Count < 2 then
            activeGame <- None
