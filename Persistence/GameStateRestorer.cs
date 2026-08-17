using WebAppSandbox.GameEngine.Cards;
using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;
using WebAppSandbox.Hubs;

namespace WebAppSandbox.Persistence;

/// <summary>
/// Rebuilds a live <see cref="GameRoom"/> from a persisted snapshot.
/// Card instances are reconstructed from <see cref="CardLibrary"/> and matched
/// by their stable ids, preserving deck pile order.
/// </summary>
public static class GameStateRestorer
{
    public static GameRoom Restore(GameStateSnapshot snapshot)
    {
        var phase = ParseEnum<GamePhase>(snapshot.Phase);

        var game = new Game(snapshot.RoomId)
        {
            Phase = phase,
            WinnerPlayerId = snapshot.WinnerPlayerId,
        };

        var engine = new OrganAttackGame(game);

        // Players and organs. Add directly to the game because a game that has
        // already started is not in WaitingForPlayer, and AddPlayer would throw.
        foreach (var playerSnapshot in snapshot.Players)
        {
            var player = new Player(playerSnapshot.Id, playerSnapshot.Name);
            foreach (var organSnapshot in playerSnapshot.Organs)
            {
                var organ = new Organ(ParseEnum<OrganType>(organSnapshot.Type), organSnapshot.Position)
                {
                    IsDestroyed = organSnapshot.IsDestroyed,
                    IsShielded = organSnapshot.IsShielded,
                };

                foreach (var affliction in organSnapshot.Afflictions)
                {
                    organ.AddAffliction(ParseEnum<AfflictionType>(affliction));
                }

                player.Organs.Add(organ);
            }

            game.Players.Add(player);
        }

        // Deck.
        var organTypes = snapshot.Deck.OrganTypes.Select(ParseEnum<OrganType>).ToList();
        game.DeckOrganTypes = organTypes;

        if (organTypes.Count > 0)
        {
            var cardPool = BuildCardPool(organTypes);

            var drawPile = snapshot.Deck.DrawPileCardIds
                .Select(id => TakeCard(cardPool, id))
                .Where(c => c is not null)
                .Cast<Card>()
                .ToList();
            var discardPile = snapshot.Deck.DiscardPileCardIds
                .Select(id => TakeCard(cardPool, id))
                .Where(c => c is not null)
                .Cast<Card>()
                .ToList();

            game.Deck = new Deck(new Random(), drawPile, discardPile);
        }

        // Hands (must be assigned after the deck exists so card references match).
        foreach (var playerSnapshot in snapshot.Players)
        {
            var player = game.Players.First(p => p.Id == playerSnapshot.Id);
            var cardPool = BuildCardPool(organTypes);
            foreach (var cardId in playerSnapshot.HandCardIds)
            {
                var card = TakeCard(cardPool, cardId);
                if (card is not null)
                {
                    player.Hand.Add(card);
                }
            }
        }

        // Turn.
        if (snapshot.Turn is not null)
        {
            game.Turn = new TurnState
            {
                CurrentPlayerId = snapshot.Turn.CurrentPlayerId,
                HasDrawnThisTurn = snapshot.Turn.HasDrawnThisTurn,
                ActionsUsed = snapshot.Turn.ActionsUsed,
                MaxActionsPerTurn = snapshot.Turn.MaxActionsPerTurn,
            };
        }

        // Pending attack.
        if (snapshot.PendingAttack is not null)
        {
            var pending = snapshot.PendingAttack;
            var caster = game.Players.First(p => p.Id == pending.CasterId);
            var targetOwner = game.Players.First(p => p.Id == pending.TargetOwnerId);
            var targetOrgan = targetOwner.Organs.First(o =>
                o.Type == ParseEnum<OrganType>(pending.TargetOrganType) &&
                o.Position == pending.TargetOrganPosition);
            var cardPool = BuildCardPool(organTypes);
            var card = TakeCard(cardPool, pending.CardId)
                ?? BuildFallbackCard(pending.CardId);

            game.PendingAttack = new PendingAttack
            {
                Id = pending.Id,
                Card = card,
                Caster = caster,
                TargetOwner = targetOwner,
                TargetOrgan = targetOrgan,
            };
        }

        return new GameRoom(snapshot.RoomId, engine);
    }

    private static Dictionary<string, Queue<Card>> BuildCardPool(IEnumerable<OrganType> organTypes)
    {
        var pool = new Dictionary<string, Queue<Card>>();
        foreach (var card in CardLibrary.BuildDeck(organTypes.ToList()))
        {
            if (!pool.TryGetValue(card.Id, out var queue))
            {
                queue = new Queue<Card>();
                pool[card.Id] = queue;
            }

            queue.Enqueue(card);
        }

        return pool;
    }

    private static Card? TakeCard(Dictionary<string, Queue<Card>> pool, string cardId)
    {
        if (!pool.TryGetValue(cardId, out var queue) || queue.Count == 0)
        {
            return null;
        }

        return queue.Dequeue();
    }

    private static Card BuildFallbackCard(string cardId)
    {
        return new Card
        {
            Id = cardId,
            Name = cardId,
            Type = CardType.Instant,
            SpecialCard = SpecialCardType.ImmunityBoost,
            Description = "Restored card",
        };
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, out var result)
            ? result
            : default;
    }
}