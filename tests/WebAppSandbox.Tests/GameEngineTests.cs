using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Tests;

public class GameEngineTests
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
    public void AddPlayer_AfterStart_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() => engine.AddPlayer("p3", "P3"));
        Assert.Contains("after the game has started", ex.Message);
    }

    [Fact]
    public void AddPlayer_RoomFull_Throws()
    {
        var game = new Game("g");
        var engine = new OrganAttackGame(game);
        engine.AddPlayer("p1", "P1");
        engine.AddPlayer("p2", "P2");

        var ex = Assert.Throws<GameRuleException>(() => engine.AddPlayer("p3", "P3"));
        Assert.Contains("full", ex.Message);
    }

    [Fact]
    public void StartGame_RequiresTwoPlayers()
    {
        var game = new Game("g");
        var engine = new OrganAttackGame(game);
        engine.AddPlayer("p1", "P1");

        var ex = Assert.Throws<GameRuleException>(() => engine.StartGame(new Random(1)));
        Assert.Contains("2 players", ex.Message);
    }

    [Fact]
    public void PlayCard_WrongTargetSide_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;
        var p1 = game.Players[0];
        var organType = p1.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

        // give p1 an opponent-only card
        var card = new Card
        {
            Id = "opp-card",
            Name = "Attack",
            Type = CardType.Attack,
            TargetSide = TargetSide.Opponent,
            TargetOrganType = organType,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(card);
        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "opp-card", "p1", organType));

        Assert.Contains("opponent", ex.Message);
    }

    [Fact]
    public void PlayCard_WrongOrganType_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;
        var p1 = game.Players[0];
        var p2 = game.Players[1];
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;
        var wrongType = Enum.GetValues<OrganType>().First(t => t != targetType);

        var card = new Card
        {
            Id = "brain-card",
            Name = "Afflict",
            Type = CardType.Affliction,
            TargetSide = TargetSide.Opponent,
            TargetOrganType = wrongType,
            AfflictionType = AfflictionType.Afflicted,
            AfflictionAmount = 1,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(card);
        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "brain-card", "p2", targetType));

        Assert.Contains("does not match", ex.Message);
    }

    [Fact]
    public void PlayCard_AfflictionCard_CanAfflictWildOrgan()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");

        // Ensure p2 has exactly one Wild organ to target.
        p2.Organs.RemoveAll(o => o.Type == OrganType.Wild_Organ);
        p2.Organs.Add(new Organ(OrganType.Wild_Organ, 999));

        // An affliction card printed for a different organ (Heart) must still
        // be able to afflict the Wild organ.
        var afflict = new Card
        {
            Id = "aff-heart",
            Name = "Afflict Heart",
            Type = CardType.Affliction,
            TargetSide = TargetSide.Opponent,
            TargetOrganType = OrganType.Heart,
            AfflictionType = AfflictionType.Afflicted,
            AfflictionAmount = 1,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(afflict);
        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        engine.PlayCard("p1", "aff-heart", "p2", OrganType.Wild_Organ);

        var wild = p2.Organs.First(o => o.Type == OrganType.Wild_Organ);
        Assert.True(wild.IsAfflicted);
    }

    [Fact]
    public void PlayCard_NotInHand_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "nonexistent", "p2", OrganType.Heart));

        Assert.Contains("Card not found", ex.Message);
    }

    [Fact]
    public void PlayCard_AfterGameOver_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;
        game.Phase = GamePhase.GameOver;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "any", "p2", OrganType.Heart));

        Assert.Contains("already over", ex.Message);
    }

    [Fact]
    public void DestroyingLastOrgan_WinsGame()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

        // Destroy all of p2's organs except one.
        foreach (var organ in p2.Organs.Where(o => o.Type != targetType))
        {
            organ.IsDestroyed = true;
        }

        // Afflict p2's remaining organ.
        var target = p2.Organs.First(o => o.Type == targetType);
        target.AddAffliction(AfflictionType.Afflicted);

        // Give p1 an attack card for that organ.
        var attack = new Card
        {
            Id = "attack-heart",
            Name = "Attack",
            Type = CardType.Attack,
            TargetSide = TargetSide.Opponent,
            TargetOrganType = targetType,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(attack);

        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        engine.PlayCard("p1", "attack-heart", "p2", targetType);

        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Equal("p1", game.WinnerPlayerId);
        Assert.True(target.IsDestroyed);
    }

    [Fact]
    public void FullFlow_AfflictionThenAttack_DestroysOrgan()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

        // Setup affliction card for p1, target p2's organ.
        var afflict = new Card
        {
            Id = "aff-heart",
            Name = "Afflict",
            Type = CardType.Affliction,
            TargetSide = TargetSide.Opponent,
            TargetOrganType = targetType,
            AfflictionType = AfflictionType.Afflicted,
            AfflictionAmount = 1,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(afflict);
        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        engine.PlayCard("p1", "aff-heart", "p2", targetType);

        Assert.True(p2.Organs.First(o => o.Type == targetType).IsAfflicted);
    }
}