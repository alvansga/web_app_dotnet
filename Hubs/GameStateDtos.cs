using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Hubs;

public class CardDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string TargetSide { get; init; } = "";
    public string? TargetOrganType { get; init; }
    public string Description { get; init; } = "";

    public static CardDto From(Card card) => new()
    {
        Id = card.Id,
        Name = card.Name,
        Type = card.Type.ToString(),
        TargetSide = card.TargetSide.ToString(),
        TargetOrganType = card.TargetOrganType?.ToString(),
        Description = card.Description,
    };
}

public class OrganDto
{
    public string Type { get; init; } = "";
    public int Position { get; init; }
    public bool IsDestroyed { get; init; }
    public bool IsShielded { get; init; }
    public bool IsAfflicted { get; init; }
    public int AfflictionCount { get; init; }
    public int AfflictionsToDestroy { get; init; }

    public static OrganDto From(Organ organ) => new()
    {
        Type = organ.Type.ToString(),
        Position = organ.Position,
        IsDestroyed = organ.IsDestroyed,
        IsShielded = organ.IsShielded,
        IsAfflicted = organ.IsAfflicted,
        AfflictionCount = organ.Afflictions.Count,
        AfflictionsToDestroy = organ.AfflictionsToDestroy,
    };
}

public class PlayerDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsAlive { get; init; }
    public int HandCount { get; init; }
    public List<OrganDto> Organs { get; init; } = new();

    public static PlayerDto From(Player player) => new()
    {
        Id = player.Id,
        Name = player.Name,
        IsAlive = player.IsAlive,
        HandCount = player.Hand.Count,
        Organs = player.Organs.Select(OrganDto.From).ToList(),
    };
}

public class TurnDto
{
    public string CurrentPlayerId { get; init; } = "";
    public int ActionsUsed { get; init; }
    public int MaxActionsPerTurn { get; init; }
    public bool HasDrawnThisTurn { get; init; }
}

public class RoomDto
{
    public string RoomId { get; init; } = "";
    public List<string> PlayerNames { get; init; } = new();
    public bool CanJoin { get; init; }

    public static RoomDto From(GameRoom room) => new()
    {
        RoomId = room.RoomId,
        PlayerNames = room.Engine.Game.Players.Select(p => p.Name).ToList(),
        CanJoin = room.Engine.Game.Phase == GamePhase.WaitingForPlayer &&
                  room.Engine.Game.Players.Count < GameRules.MaxPlayers,
    };
}

public class GameStateDto
{
    public string RoomId { get; init; } = "";
    public string Phase { get; init; } = "";
    public string? WinnerPlayerId { get; init; }
    public TurnDto? Turn { get; init; }
    public List<PlayerDto> Players { get; init; } = new();
    public List<CardDto> MyHand { get; init; } = new();
    public int DeckCount { get; init; }
    public int DiscardCount { get; init; }
}