namespace WebAppSandbox.GameEngine.Models;

public class TurnState
{
    public required string CurrentPlayerId { get; set; }
    public bool HasDrawnThisTurn { get; set; }
    public int ActionsUsed { get; set; }
    public int MaxActionsPerTurn { get; set; } = 1;

    public bool CanAct => ActionsUsed < MaxActionsPerTurn;
}