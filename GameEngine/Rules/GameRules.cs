namespace WebAppSandbox.GameEngine.Rules;

public static class GameRules
{
    /// <summary>
    /// GameEngine rules version. Bump when the game logic changes.
    /// </summary>
    public const string Version = "0.3.0";

    public const int OrganCount = 5;
    public const int MaxPlayers = 2;
    public const int StartingHandSize = 5;
    public const int MaxHandSize = 7;
    public const int MaxActionsPerTurn = 1;
    public const int MaxSwapCards = 2;
    public const bool DrawAtTurnStart = false;
}