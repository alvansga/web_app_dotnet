using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Tests;

public class SwapCardsTests
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
    public void SwapCards_OneCard_DiscardsAndDrawsReplacement()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players[0];
        var handBefore = p1.Hand.Count;
        var selected = p1.Hand[0];

        engine.SwapCards("p1", new[] { selected.Id });

        Assert.Equal(handBefore, p1.Hand.Count);
        Assert.DoesNotContain(p1.Hand, c => c.Id == selected.Id);
        Assert.Contains(engine.Game.Deck!.DiscardPile, c => c.Id == selected.Id);
    }

    [Fact]
    public void SwapCards_TwoCards_DiscardsAndDrawsReplacements()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players[0];
        var handBefore = p1.Hand.Count;
        var selected = new[] { p1.Hand[0], p1.Hand[1] };

        engine.SwapCards("p1", selected.Select(c => c.Id).ToArray());

        Assert.Equal(handBefore, p1.Hand.Count);
        foreach (var card in selected)
        {
            Assert.DoesNotContain(p1.Hand, c => c.Id == card.Id);
            Assert.Contains(engine.Game.Deck!.DiscardPile, c => c.Id == card.Id);
        }
    }

    [Fact]
    public void SwapCards_ZeroCards_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p1", Array.Empty<string>()));

        Assert.Contains("at least 1", ex.Message);
    }

    [Fact]
    public void SwapCards_ThreeCards_Throws()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players[0];
        var ids = p1.Hand.Take(3).Select(c => c.Id).ToArray();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p1", ids));

        Assert.Contains("at most", ex.Message);
    }

    [Fact]
    public void SwapCards_DuplicateId_Throws()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players[0];
        var id = p1.Hand[0].Id;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p1", new[] { id, id }));

        Assert.Contains("same card twice", ex.Message);
    }

    [Fact]
    public void SwapCards_UnknownId_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p1", new[] { "does-not-exist" }));

        Assert.Contains("Card not found", ex.Message);
    }

    [Fact]
    public void SwapCards_OutOfTurn_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p2", new[] { "any" }));

        Assert.Contains("not your turn", ex.Message);
    }

    [Fact]
    public void SwapCards_ConsumesAction()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players[0];

        engine.SwapCards("p1", new[] { p1.Hand[0].Id });

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p1", new[] { p1.Hand[0].Id }));

        Assert.Contains("No more actions", ex.Message);
    }

    [Fact]
    public void SwapCards_BlocksNormalDraw()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players[0];

        engine.SwapCards("p1", new[] { p1.Hand[0].Id });

        var ex = Assert.Throws<GameRuleException>(() => engine.DrawCard("p1"));

        Assert.Contains("Already drawn", ex.Message);
    }
}