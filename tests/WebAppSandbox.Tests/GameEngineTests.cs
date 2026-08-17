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

        engine.ResolvePendingAttack(game.PendingAttack!.Id, blocked: false);

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

        engine.ResolvePendingAttack(game.PendingAttack!.Id, blocked: false);

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

        engine.ResolvePendingAttack(game.PendingAttack!.Id, blocked: false);

        Assert.True(p2.Organs.First(o => o.Type == targetType).IsAfflicted);
    }

    [Fact]
    public void PlayCard_WhenNoActionsLeft_ThrowsWithoutChangingState()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

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
        game.Turn.ActionsUsed = game.Turn.MaxActionsPerTurn; // no actions left

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "aff-heart", "p2", targetType));

        Assert.Contains("No more actions", ex.Message);
        Assert.False(p2.Organs.First(o => o.Type == targetType).IsAfflicted);
        Assert.Contains(afflict, p1.Hand);
    }

    [Fact]
    public void SwapCards_WhenNoActionsLeft_ThrowsWithoutChangingHand()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var cardIds = p1.Hand.Select(c => c.Id).ToList();
        var handBefore = p1.Hand.Count;
        var deckBefore = game.Deck!.DrawPile.Count;
        var discardBefore = game.Deck.DiscardPile.Count;

        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = game.Turn.MaxActionsPerTurn; // no actions left
        game.Turn.HasDrawnThisTurn = false;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.SwapCards("p1", cardIds.Take(1).ToList()));

        Assert.Contains("No more actions", ex.Message);
        Assert.Equal(handBefore, p1.Hand.Count);
        Assert.Equal(deckBefore, game.Deck!.DrawPile.Count);
        Assert.Equal(discardBefore, game.Deck.DiscardPile.Count);
    }

    [Fact]
    public void ItsAlive_RevivesOwnDestroyedOrgan()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");

        // Pick one of p1's organs to "destroy", keeping the rest alive so the game is not over.
        var target = p1.Organs.First(o => o.Type != OrganType.Wild_Organ);
        target.IsDestroyed = true;
        target.IsShielded = true;
        target.AddAffliction(AfflictionType.Afflicted);

        var itsAlive = new Card
        {
            Id = "its-alive",
            Name = "It's Alive",
            Type = CardType.Special,
            TargetSide = TargetSide.Self,
            SpecialCard = SpecialCardType.ItsAlive,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(itsAlive);

        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        engine.PlayCard("p1", "its-alive", "p1", target.Type);

        Assert.False(target.IsDestroyed);
        Assert.False(target.IsShielded);
        Assert.False(target.IsAfflicted);
    }

    [Fact]
    public void ItsAlive_OnLivingOrgan_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var target = p1.Organs.First(o => !o.IsDestroyed);

        var itsAlive = new Card
        {
            Id = "its-alive",
            Name = "It's Alive",
            Type = CardType.Special,
            TargetSide = TargetSide.Self,
            SpecialCard = SpecialCardType.ItsAlive,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(itsAlive);

        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "its-alive", "p1", target.Type));

        Assert.Contains("not destroyed", ex.Message);
    }

    [Fact]
    public void PlayCard_InstantCard_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

        var instant = new Card
        {
            Id = "immunity-boost-0",
            Name = "Immunity Boost",
            Type = CardType.Instant,
            SpecialCard = SpecialCardType.ImmunityBoost,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(instant);

        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayCard("p1", "immunity-boost-0", "p1", targetType));

        Assert.Contains("response", ex.Message);
        Assert.Contains(instant, p1.Hand);
    }

    [Fact]
    public void PlayInstant_WithNoPendingAttack_Throws()
    {
        var engine = CreateStartedGame();

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayInstant("p2", "immunity-boost-0"));

        Assert.Contains("no incoming attack", ex.Message);
    }

    [Fact]
    public void PlayInstant_BlocksIncomingAffliction()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;
        var targetOrgan = p2.Organs.First(o => o.Type == targetType);

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
        Assert.NotNull(game.PendingAttack);

        var boost = new Card
        {
            Id = "immunity-boost-0",
            Name = "Immunity Boost",
            Type = CardType.Instant,
            SpecialCard = SpecialCardType.ImmunityBoost,
            Description = "test"
        };
        p2.Hand.Clear();
        p2.Hand.Add(boost);

        engine.PlayInstant("p2", "immunity-boost-0");

        Assert.Null(game.PendingAttack);
        Assert.False(targetOrgan.IsAfflicted);
        Assert.DoesNotContain(boost, p2.Hand);

        // The defender draws a replacement so their hand stays full.
        Assert.Single(p2.Hand);
    }

    [Fact]
    public void PlayInstant_WrongTarget_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

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

        var ex = Assert.Throws<GameRuleException>(() =>
            engine.PlayInstant("p1", "immunity-boost-0"));

        Assert.Contains("not the target", ex.Message);
        Assert.NotNull(game.PendingAttack);
    }

    [Fact]
    public void PlayInstant_OutOfTurn_DoesNotConsumeAction()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

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

        // It is p1's turn; p2 responds out of turn.
        var actionsBefore = game.Turn.ActionsUsed;
        var boost = new Card
        {
            Id = "immunity-boost-0",
            Name = "Immunity Boost",
            Type = CardType.Instant,
            SpecialCard = SpecialCardType.ImmunityBoost,
            Description = "test"
        };
        p2.Hand.Clear();
        p2.Hand.Add(boost);

        engine.PlayInstant("p2", "immunity-boost-0");

        // p1's action usage and turn ownership are unchanged.
        Assert.Equal(actionsBefore, game.Turn.ActionsUsed);
        Assert.Equal("p1", game.Turn.CurrentPlayerId);
    }

    [Fact]
    public void EndTurn_DuringPendingAttack_Throws()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");
        var targetType = p2.Organs.First(o => o.Type != OrganType.Wild_Organ).Type;

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

        var ex = Assert.Throws<GameRuleException>(() => engine.EndTurn("p1"));

        Assert.Contains("respons", ex.Message);
    }

    [Fact]
    public void ChartMixUp_PoolsAndRedealsHands_ExcludingPlayedCard()
    {
        var engine = CreateStartedGame();
        var game = engine.Game;

        var p1 = game.Players.First(p => p.Id == "p1");
        var p2 = game.Players.First(p => p.Id == "p2");

        // Give p1 the Chart Mix-up card plus one marker card.
        var chartMixUp = new Card
        {
            Id = "chart-mix-up-0",
            Name = "Chart Mix-up",
            Type = CardType.Special,
            TargetSide = TargetSide.None,
            SpecialCard = SpecialCardType.ChartMixUp,
            Description = "test"
        };
        var extraA = new Card
        {
            Id = "extra-a",
            Name = "Extra A",
            Type = CardType.Defense,
            TargetSide = TargetSide.Self,
            Description = "test"
        };
        p1.Hand.Clear();
        p1.Hand.Add(chartMixUp);
        p1.Hand.Add(extraA);

        // Give p2 one marker card.
        var extraB = new Card
        {
            Id = "extra-b",
            Name = "Extra B",
            Type = CardType.Defense,
            TargetSide = TargetSide.Self,
            Description = "test"
        };
        p2.Hand.Clear();
        p2.Hand.Add(extraB);

        game.Turn!.CurrentPlayerId = "p1";
        game.Turn.ActionsUsed = 0;

        engine.PlayNoTargetCard("p1", "chart-mix-up-0");

        // Played card is consumed and discarded.
        Assert.DoesNotContain(chartMixUp, p1.Hand);
        Assert.Contains(chartMixUp, game.Deck!.DiscardPile);

        // Pooled hands after excluding the played card had 2 cards total;
        // round-robin from the caster means p1 and p2 each receive 1.
        Assert.Single(p1.Hand);
        Assert.Single(p2.Hand);

        // Union of remaining hands is exactly the two pooled cards.
        var union = p1.Hand.Concat(p2.Hand).Select(c => c.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "extra-a", "extra-b" }, union);
    }
}
