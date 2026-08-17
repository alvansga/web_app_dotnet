namespace WebAppSandbox.Persistence;

/// <summary>
/// Plain DTO snapshot of a game room used for database persistence.
/// Enums are stored as strings for resilience to enum reordering.
/// </summary>
public class GameStateSnapshot
{
    public string RoomId { get; set; } = "";
    public string Phase { get; set; } = "";
    public string? WinnerPlayerId { get; set; }
    public TurnSnapshot? Turn { get; set; }
    public List<PlayerSnapshot> Players { get; set; } = new();
    public DeckSnapshot Deck { get; set; } = new();
    public PendingAttackSnapshot? PendingAttack { get; set; }
}

public class TurnSnapshot
{
    public string CurrentPlayerId { get; set; } = "";
    public bool HasDrawnThisTurn { get; set; }
    public int ActionsUsed { get; set; }
    public int MaxActionsPerTurn { get; set; } = 1;
}

public class PlayerSnapshot
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<OrganSnapshot> Organs { get; set; } = new();
    public List<string> HandCardIds { get; set; } = new();
}

public class OrganSnapshot
{
    public string Type { get; set; } = "";
    public int Position { get; set; }
    public bool IsDestroyed { get; set; }
    public bool IsShielded { get; set; }
    public List<string> Afflictions { get; set; } = new();
}

public class DeckSnapshot
{
    public List<string> OrganTypes { get; set; } = new();
    public List<string> DrawPileCardIds { get; set; } = new();
    public List<string> DiscardPileCardIds { get; set; } = new();
}

public class PendingAttackSnapshot
{
    public string Id { get; set; } = "";
    public string CardId { get; set; } = "";
    public string CasterId { get; set; } = "";
    public string TargetOwnerId { get; set; } = "";
    public string TargetOrganType { get; set; } = "";
    public int TargetOrganPosition { get; set; }
}
