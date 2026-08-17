using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Tests;

public class TransplantTests
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

    private static Card MakeTransplantCard() => new()
    {
        Id = "transplant-test",
        Name = "Transplant",
        Type = CardType.Special,
        TargetSide = TargetSide.Opponent,
        SpecialCard = SpecialCardType.Transplant,
        Description = "Steal a healthy organ."
    };

    private static void GiveCard(OrganAttackGame engine, string playerId, Card card)
    {
        var player = engine.Game.Players.First(p => p.Id == playerId);
        player.Hand.Clear();
        player.Hand.Add(card);
        engine.Game.Turn!.CurrentPlayerId = playerId;
        engine.Game.Turn.ActionsUsed = 0;
    }

    [Fact]
    public void Transplant_StealsHealthyOrgan()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players.First(p => p.Id == "p1");
        var p2 = engine.Game.Players.First(p => p.Id == "p2");

        var target = p2.Organs.First(o => !o.IsDestroyed && !o.IsAfflicted && !o.IsShielded);
        var targetType = target.Type;
        var p2OrganCount = p2.Organs.Count;
        var p1OrganCount = p1.Organs.Count;

        GiveCard(engine, "p1", MakeTransplantCard());

        engine.PlayCard("p1", "transplant-test", "p2", targetType);

        Assert.Equal(p2OrganCount - 1, p2.Organs.Count);
        Assert.Equal(p1OrganCount + 1, p1.Organs.Count);
        Assert.DoesNotContain(p2.Organs, o => o.Type == targetType);
        Assert.Contains(p1.Organs, o => o.Type == targetType);
    }

    [Fact]
    public void Transplant_ConsumesTurnAction()
    {
        var engine = CreateStartedGame();
        var p2 = engine.Game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First().Type;

        GiveCard(engine, "p1", MakeTransplantCard());

        engine.PlayCard("p1", "transplant-test", "p2", targetType);

        Assert.Equal(1, engine.Game.Turn!.ActionsUsed);
    }

    [Fact]
    public void Transplant_OnDestroyedOrgan_Throws()
    {
        var engine = CreateStartedGame();
        var p2 = engine.Game.Players.First(p => p.Id == "p2");
        var target = p2.Organs.First();
        target.IsDestroyed = true;

        GiveCard(engine, "p1", MakeTransplantCard());

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "transplant-test", "p2", target.Type));

        Assert.Contains("destroyed", ex.Message);
    }

    [Fact]
    public void Transplant_OnAfflictedOrgan_Throws()
    {
        var engine = CreateStartedGame();
        var p2 = engine.Game.Players.First(p => p.Id == "p2");
        var target = p2.Organs.First();
        target.AddAffliction(AfflictionType.Afflicted);

        GiveCard(engine, "p1", MakeTransplantCard());

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "transplant-test", "p2", target.Type));

        Assert.Contains("afflicted", ex.Message);
    }

    [Fact]
    public void Transplant_OnShieldedOrgan_Throws()
    {
        var engine = CreateStartedGame();
        var p2 = engine.Game.Players.First(p => p.Id == "p2");
        var target = p2.Organs.First();
        target.IsShielded = true;

        GiveCard(engine, "p1", MakeTransplantCard());

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "transplant-test", "p2", target.Type));

        Assert.Contains("shielded", ex.Message);
    }

    [Fact]
    public void Transplant_OnSelfOrgan_Throws()
    {
        var engine = CreateStartedGame();
        var p1 = engine.Game.Players.First(p => p.Id == "p1");
        var targetType = p1.Organs.First().Type;

        GiveCard(engine, "p1", MakeTransplantCard());

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "transplant-test", "p1", targetType));

        Assert.Contains("opponent", ex.Message);
    }
}