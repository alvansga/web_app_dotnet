namespace WebAppSandbox.GameEngine.Rules;

public static class GameRules
{
    /// <summary>
    /// GameEngine rules version. Bump when the game logic changes.
    /// </summary>
    public const string Version = "0.8.0";

    public const int OrganCount = 5;
    public const int MaxPlayers = 2;
    public const int StartingHandSize = 5;
    public const int MaxHandSize = 5;
    public const int MaxActionsPerTurn = 1;
    public const int MaxSwapCards = 2;
    public const bool DrawAtTurnStart = false;

    /// <summary>
    /// Seconds the defending player has to respond with an Instant before a
    /// staged attack resolves.
    /// </summary>
    public const int ResponseTimeoutSeconds = 5;
}