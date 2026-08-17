namespace WebAppSandbox.GameEngine.Models;

public class Deck
{
    private readonly Random _random;
    private readonly List<Card> _drawPile = new();
    private readonly List<Card> _discardPile = new();

    public IReadOnlyList<Card> DrawPile => _drawPile;
    public IReadOnlyList<Card> DiscardPile => _discardPile;

    public Deck(Random random)
    {
        _random = random;
    }

    /// <summary>
    /// Restores a deck from previously saved piles (used by persistence).
    /// </summary>
    public Deck(Random random, IEnumerable<Card> drawPile, IEnumerable<Card> discardPile)
    {
        _random = random;
        _drawPile.AddRange(drawPile);
        _discardPile.AddRange(discardPile);
    }

    public void Build(IEnumerable<Card> cards)
    {
        _drawPile.Clear();
        _discardPile.Clear();
        _drawPile.AddRange(cards);
        Shuffle();
    }

    public void Shuffle()
    {
        // Fisher-Yates
        for (int i = _drawPile.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
        }
    }

    /// <summary>
    /// Returns a shuffled copy of the provided cards without mutating the deck,
    /// using the same seeded RNG. Used by effects that pool players' hands
    /// (e.g. Chart Mix-up).
    /// </summary>
    public IReadOnlyList<Card> Shuffled(IEnumerable<Card> cards)
    {
        var list = cards.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    public Card? Draw()
    {
        if (_drawPile.Count == 0)
        {
            ReshuffleFromDiscard();
        }

        if (_drawPile.Count == 0)
        {
            return null;
        }

        var card = _drawPile[^1];
        _drawPile.RemoveAt(_drawPile.Count - 1);
        return card;
    }

    public void Discard(Card card)
    {
        _discardPile.Add(card);
    }

    public void ReshuffleFromDiscard()
    {
        if (_discardPile.Count == 0)
        {
            return;
        }

        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle();
    }
}