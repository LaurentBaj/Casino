open System
open System.IO
open System.Net.WebSockets
open System.Text
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Giraffe
open Casino.Core
open Casino.Core.Models
open Casino.UI

let processWebSocket (webSocket: WebSocket) (ctx: HttpContext) =
    task {
        let playerId = GameManager.handleJoin webSocket

        let buffer = Array.zeroCreate 4096
        let! result = webSocket.ReceiveAsync(ArraySegment<byte>(buffer), CancellationToken.None)

        let mutable res = result
        while not res.CloseStatus.HasValue do
            let msgStr = Encoding.UTF8.GetString(buffer, 0, res.Count)
            printfn "Received from client %A: %s" playerId msgStr
            let! nextResult = webSocket.ReceiveAsync(ArraySegment<byte>(buffer), CancellationToken.None)
            res <- nextResult

        GameManager.removePlayer playerId
        do! webSocket.CloseAsync(res.CloseStatus.Value, res.CloseStatusDescription, CancellationToken.None)
    }

let webSocketHandler : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if ctx.WebSockets.IsWebSocketRequest then
                let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                do! processWebSocket webSocket ctx
                return Some ctx
            else
                ctx.Response.StatusCode <- 400
                return Some ctx
        }

let webApp =
    choose [
        route "/ws" >=> webSocketHandler
    ]

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    builder.Services.AddGiraffe() |> ignore

    let app = builder.Build()

    // Serve static files from Blazor's output directory
    app.UseStaticFiles() |> ignore
    app.UseBlazorFrameworkFiles() |> ignore

    app.UseRouting() |> ignore

    app.UseWebSockets() |> ignore
    app.UseGiraffe webApp

    // Crucial for SPA routing (like /game) to fall back to Blazor's index.html
    app.UseEndpoints(fun endpoints ->
        endpoints.MapFallbackToFile("index.html") |> ignore
    ) |> ignore

    app.Run()
    0
