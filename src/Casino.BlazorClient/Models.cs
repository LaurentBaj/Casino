using System;
using System.Collections.Generic;

namespace Casino.BlazorClient.Models;

public class PlayerStateDto
{
    public Guid Id { get; set; }
    public int CardCount { get; set; }
    public int CapturedCount { get; set; }
    public int SweepsCount { get; set; }
}

public class SlotDto
{
    public List<string> Cards { get; set; } = new();
    public int AggregatePoints { get; set; }
}

public class ClientGameStateDto
{
    public string CurrentRound { get; set; } = string.Empty;
    public List<SlotDto> BoardSlots { get; set; } = new();
    public List<string> MyHand { get; set; } = new();
    public List<string> MyCaptured { get; set; } = new();
    public List<string> MySweeps { get; set; } = new();
    public List<PlayerStateDto> Opponents { get; set; } = new();
    public bool IsMyTurn { get; set; }
}

public class ServerMessage
{
    public ClientGameStateDto? GameState { get; set; }
    public int? WaitingForPlayers { get; set; }
    public string? Error { get; set; }
}
