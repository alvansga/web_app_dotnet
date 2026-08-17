namespace WebAppSandbox.GameEngine.Models;

public class Game
{
    public string Id { get; init; }
    public GamePhase Phase { get; set; } = GamePhase.WaitingForPlayer;
    public List<Player> Players { get; } = new();
    public List<OrganType> DeckOrganTypes { get; set; } = new();
    public Deck? Deck { get; set; }
    public TurnState? Turn { get; set; }
    public string? WinnerPlayerId { get; set; }
    public PendingAttack? PendingAttack { get; set; }

    public Player? CurrentPlayer =>
        Turn is null ? null : Players.FirstOrDefault(p => p.Id == Turn.CurrentPlayerId);

    public Player? OpponentOf(string playerId) =>
        Players.FirstOrDefault(p => p.Id != playerId);

    public Game(string id)
    {
        Id = id;
    }
}