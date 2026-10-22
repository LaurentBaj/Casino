open System
open System.IO
open System.Net.WebSockets
open System.Text
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

// ---------------------------------
// WebSocket Handler
// ---------------------------------

/// <summary>
/// A simple function that handles an active WebSocket connection.
/// In a real app, this would tie into a background Game Actor or state manager.
/// </summary>
let processWebSocket (webSocket: WebSocket) (ctx: HttpContext) =
    task {
        let buffer = Array.zeroCreate 4096

        // Example: Wait for the user to send a message
        let! result = webSocket.ReceiveAsync(ArraySegment<byte>(buffer), CancellationToken.None)

        let mutable res = result
        while not res.CloseStatus.HasValue do
            let msg = Encoding.UTF8.GetString(buffer, 0, res.Count)
            printfn "Received from client: %s" msg

            // Dummy logic: We received a message, let's simulate a response that gives them
            // a "safe" view of the game state (like we discussed).
            let responseMsg =
                $"Server received '{msg}'. Here is your updated game state (dummy state)."

            let responseBytes = Encoding.UTF8.GetBytes(responseMsg)

            // Send back to the client
            do! webSocket.SendAsync(
                ArraySegment<byte>(responseBytes, 0, responseBytes.Length),
                res.MessageType,
                res.EndOfMessage,
                CancellationToken.None)

            // Wait for the next message
            let! nextResult = webSocket.ReceiveAsync(ArraySegment<byte>(buffer), CancellationToken.None)
            res <- nextResult

        do! webSocket.CloseAsync(res.CloseStatus.Value, res.CloseStatusDescription, CancellationToken.None)
    }

/// <summary>
/// The Giraffe HttpHandler that intercepts requests to /ws and upgrades them.
/// </summary>
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

// ---------------------------------
// Web App Configuration
// ---------------------------------

let webApp =
    choose [
        route "/ws" >=> webSocketHandler
        route "/" >=> htmlFile "wwwroot/index.html"
    ]

let configureApp (app: IApplicationBuilder) =
    app.UseWebSockets() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services

    let app = builder.Build()
    configureApp app

    app.Run()
    0
