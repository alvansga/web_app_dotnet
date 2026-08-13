using WebAppSandbox.GameEngine.Cards;
using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.Tests;

public class DeckTests
{
    [Fact]
    public void Build_Produces_ExpectedCardCount()
    {
        var deck = new Deck(new Random(1));
        deck.Build(CardLibrary.BuildDeck());

        // 5 organs * 2 affliction + 5 organs * 2 attack + 4 treatment + 4 defense = 28
        Assert.Equal(28, deck.DrawPile.Count);
    }

    [Fact]
    public void Shuffle_DoesNotChangeCardCountOrComposition()
    {
        var expected = CardLibrary.BuildDeck();
        var deck = new Deck(new Random(1));
        deck.Build(CardLibrary.BuildDeck());
        deck.Shuffle();

        Assert.Equal(expected.Count, deck.DrawPile.Count);

        var actualIds = deck.DrawPile.Select(c => c.Id).OrderBy(x => x).ToArray();
        var expectedIds = expected.Select(c => c.Id).OrderBy(x => x).ToArray();
        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void Draw_MovesCardFromDeckToHand()
    {
        var deck = new Deck(new Random(1));
        deck.Build(CardLibrary.BuildDeck());
        var countBefore = deck.DrawPile.Count;

        var card = deck.Draw();

        Assert.NotNull(card);
        Assert.Equal(countBefore - 1, deck.DrawPile.Count);
    }

    [Fact]
    public void Draw_WhenDrawPileEmpty_ReshufflesFromDiscard()
    {
        var deck = new Deck(new Random(1));
        deck.Build(CardLibrary.BuildDeck());

        // draw all cards into discard (simulate)
        int total = deck.DrawPile.Count;
        var drawn = new List<Card>();
        for (int i = 0; i < total; i++)
        {
            var c = deck.Draw();
            Assert.NotNull(c);
            deck.Discard(c!);
        }

        Assert.Empty(deck.DrawPile);
        Assert.Equal(total, deck.DiscardPile.Count);

        var reshuffled = deck.Draw();

        Assert.NotNull(reshuffled);
        Assert.Equal(total - 1, deck.DrawPile.Count);
    }

    [Fact]
    public void Draw_WhenDeckAndDiscardEmpty_ReturnsNull()
    {
        var deck = new Deck(new Random(1));
        deck.Build(new List<Card>());

        var card = deck.Draw();

        Assert.Null(card);
    }
}