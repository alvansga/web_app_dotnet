using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Tests;

public class CardResolverTests
{
    private static Card MakeCard(CardType type, OrganType? targetOrgan = null, TargetSide side = TargetSide.None, int afflictionAmount = 0)
    {
        return new Card
        {
            Id = $"test-{Guid.NewGuid()}",
            Name = type.ToString(),
            Type = type,
            TargetSide = side,
            TargetOrganType = targetOrgan,
            AfflictionType = type == CardType.Affliction ? AfflictionType.Afflicted : AfflictionType.None,
            AfflictionAmount = afflictionAmount,
            Description = "test"
        };
    }

    [Fact]
    public void Affliction_MatchingOrgan_Succeeds()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        var card = MakeCard(CardType.Affliction, OrganType.Heart);

        resolver.Resolve(card, organ);

        Assert.True(organ.IsAfflicted);
    }

    [Fact]
    public void Affliction_StandardOrgan_DiesAtTwoAfflictions()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        var card = MakeCard(CardType.Affliction, OrganType.Heart);

        resolver.Resolve(card, organ);
        Assert.True(organ.IsAfflicted);
        Assert.False(organ.IsDestroyed);

        resolver.Resolve(card, organ);

        Assert.Equal(2, organ.Afflictions.Count);
        Assert.True(organ.IsDestroyed);
    }

    [Fact]
    public void Necrosis_AppliesTwoAfflictions_DestroysStandardOrgan()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        var card = MakeCard(CardType.Attack, null, TargetSide.Opponent, afflictionAmount: 2);

        resolver.Resolve(card, organ);

        Assert.Equal(2, organ.Afflictions.Count);
        Assert.True(organ.IsDestroyed);
    }

    [Fact]
    public void Necrosis_OnShieldedOrgan_ConsumesShield()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0) { IsShielded = true };
        var card = MakeCard(CardType.Attack, null, TargetSide.Opponent, afflictionAmount: 2);

        resolver.Resolve(card, organ);

        Assert.False(organ.IsShielded);
        Assert.False(organ.IsAfflicted);
        Assert.True(resolver.ShieldConsumed);
    }

    [Fact]
    public void WildOrgan_DiesAtFourAfflictions()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Wild_Organ, 0);
        var card = MakeCard(CardType.Affliction, OrganType.Wild_Organ);

        for (int i = 0; i < 3; i++)
        {
            resolver.Resolve(card, organ);
        }

        Assert.False(organ.IsDestroyed);
        Assert.Equal(3, organ.Afflictions.Count);

        resolver.Resolve(card, organ);

        Assert.Equal(4, organ.Afflictions.Count);
        Assert.True(organ.IsDestroyed);
    }

    [Fact]
    public void StandardAttack_OnWildOrgan_Throws()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Wild_Organ, 0);
        organ.AddAffliction(AfflictionType.Afflicted);
        var card = MakeCard(CardType.Attack, OrganType.Wild_Organ);

        var ex = Assert.Throws<GameRuleException>(() => resolver.Resolve(card, organ));
        Assert.Contains("Wild organ", ex.Message);
    }

    [Fact]
    public void Attack_OnAfflictedOrgan_DestroysOrgan()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        organ.AddAffliction(AfflictionType.Afflicted);
        var card = MakeCard(CardType.Attack, OrganType.Heart);

        resolver.Resolve(card, organ);

        Assert.True(organ.IsDestroyed);
    }

    [Fact]
    public void Attack_OnUnafflictedOrgan_Throws()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        var card = MakeCard(CardType.Attack, OrganType.Heart);

        var ex = Assert.Throws<GameRuleException>(() => resolver.Resolve(card, organ));
        Assert.Contains("afflicted", ex.Message);
    }

    [Fact]
    public void Attack_OnShieldedOrgan_ConsumesShield()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        organ.AddAffliction(AfflictionType.Afflicted);
        organ.IsShielded = true;
        var card = MakeCard(CardType.Attack, OrganType.Heart);

        resolver.Resolve(card, organ);

        Assert.False(organ.IsDestroyed);
        Assert.False(organ.IsShielded);
        Assert.True(resolver.ShieldConsumed);
    }

    [Fact]
    public void Affliction_OnShieldedOrgan_ConsumesShield_NoAffliction()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        organ.IsShielded = true;
        var card = MakeCard(CardType.Affliction, OrganType.Heart);

        resolver.Resolve(card, organ);

        Assert.False(organ.IsShielded);
        Assert.False(organ.IsAfflicted);
        Assert.True(resolver.ShieldConsumed);
    }

    [Fact]
    public void Treatment_RemovesAffliction()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        organ.AddAffliction(AfflictionType.Afflicted);
        var card = MakeCard(CardType.Treatment);

        resolver.Resolve(card, organ);

        Assert.False(organ.IsAfflicted);
    }

    [Fact]
    public void Treatment_OnHealthyOrgan_Throws()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        var card = MakeCard(CardType.Treatment);

        var ex = Assert.Throws<GameRuleException>(() => resolver.Resolve(card, organ));
        Assert.Contains("not afflicted", ex.Message);
    }

    [Fact]
    public void Defense_SetsShield()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        var card = MakeCard(CardType.Defense);

        resolver.Resolve(card, organ);

        Assert.True(organ.IsShielded);
    }

    [Fact]
    public void Defense_AlreadyShielded_Throws()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0);
        organ.IsShielded = true;
        var card = MakeCard(CardType.Defense);

        var ex = Assert.Throws<GameRuleException>(() => resolver.Resolve(card, organ));
        Assert.Contains("already shielded", ex.Message);
    }

    [Fact]
    public void Resolve_OnDestroyedOrgan_Throws()
    {
        var resolver = new CardResolver();
        var organ = new Organ(OrganType.Heart, 0) { IsDestroyed = true };
        var card = MakeCard(CardType.Affliction, OrganType.Heart);

        var ex = Assert.Throws<GameRuleException>(() => resolver.Resolve(card, organ));
        Assert.Contains("destroyed", ex.Message);
    }
}