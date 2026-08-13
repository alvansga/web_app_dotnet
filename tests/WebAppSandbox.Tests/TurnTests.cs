using WebAppSandbox.GameEngine.Cards;
using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Tests;

public class TurnTests
{
    private static OrganAttackGame CreateStartedGame()
    {
        var game = new Game("test-game");
        var engine = new OrganAttackGame(game);
        engine.AddPlayer("p1", "Player 1");
        engine.AddPlayer("p2", "Player 2");
        engine.StartGame(new Random(1));
        return engine;
    }

    [Fact]
    public void Game_Starts_With_Player1()
    {
        var engine = CreateStartedGame();

        Assert.Equal(GamePhase.Playing, engine.Game.Phase);
        Assert.Equal("p1", engine.Game.Turn!.CurrentPlayerId);
    }

    [Fact]
    public void StartTurn_DrawsCardAutomatically()
    {
        var engine = CreateStartedGame();

        // Starting hand 5 + 1 drawn at turn start = 6
        var p1 = engine.Game.Players[0];
        Assert.Equal(GameRules.StartingHandSize + 1, p1.Hand.Count);
    }

    [Fact]
    public void EndTurn_MovesToNextPlayer()
    {
        var engine = CreateStartedGame();

        engine.EndTurn("p1");

        Assert.Equal("p2", engine.Game.Turn!.CurrentPlayerId);
    }

    [Fact]
    public void EndTurn_WrapsFromPlayer2ToPlayer1()
    {
        var engine = CreateStartedGame();

        engine.EndTurn("p1");
        engine.EndTurn("p2");

        Assert.Equal("p1", engine.Game.Turn!.CurrentPlayerId);
    }

    [Fact]
    public void PlayCard_OutOfTurn_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p2", "any", "p1", OrganType.Heart));

        Assert.Contains("not your turn", ex.Message);
    }

    [Fact]
    public void RecordAction_ExceedingMaxActions_Throws()
    {
        var engine = CreateStartedGame();
        var tm = new TurnManager(engine.Game);

        tm.RecordAction("p1"); // uses 1 action (max = 1)

        var ex = Assert.Throws<GameRuleException>(() => tm.RecordAction("p1"));
        Assert.Contains("No more actions", ex.Message);
    }
}