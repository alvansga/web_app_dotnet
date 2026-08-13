namespace WebAppSandbox.GameEngine.Rules;

public class GameRuleException : Exception
{
    public GameRuleException(string message) : base(message)
    {
    }
}