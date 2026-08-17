using WebAppSandbox.GameEngine.Cards;
using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.GameEngine.Rules;

public class OrganAttackGame
{
    private readonly Game _game;
    private readonly TurnManager _turnManager;

    public Game Game => _game;

    public OrganAttackGame(Game game)
    {
        _game = game;
        _turnManager = new TurnManager(game);
    }

    public void AddPlayer(string id, string name)
    {
        if (_game.Phase != GamePhase.WaitingForPlayer)
        {
            throw new GameRuleException("Cannot add player after the game has started.");
        }

        if (_game.Players.Count >= GameRules.MaxPlayers)
        {
            throw new GameRuleException("Room is full.");
        }

        if (_game.Players.Any(p => p.Id == id))
        {
            throw new GameRuleException("Player is already in the game.");
        }

        _game.Players.Add(new Player(id, name));
    }

    public void StartGame(Random random)
    {
        if (_game.Players.Count != GameRules.MaxPlayers)
        {
            throw new GameRuleException("Exactly 2 players are required to start.");
        }

        var allOrganTypes = new List<OrganType>();

        foreach (var player in _game.Players)
        {
            var organTypes = PickRandomOrganTypes(random);
            foreach (var organ in organTypes)
            {
                player.Organs.Add(new Organ(organ, (int)organ));
            }
            allOrganTypes.AddRange(organTypes);
        }

        _game.Deck = new Deck(random);
        _game.Deck.Build(CardLibrary.BuildDeck(allOrganTypes));

        foreach (var player in _game.Players)
        {
            for (int i = 0; i < GameRules.StartingHandSize; i++)
            {
                var card = _game.Deck.Draw();
                if (card is not null)
                {
                    player.Hand.Add(card);
                }
            }
        }

        _game.Phase = GamePhase.Playing;
        _turnManager.StartTurn(_game.Players[0].Id);
    }

    public void PlayCard(
        string playerId,
        string cardId,
        string targetOwnerPlayerId,
        OrganType targetOrganType)
    {
        EnsurePlaying();
        _turnManager.EnsureCurrentPlayer(playerId);

        var player = GetPlayer(playerId);
        var card = player.Hand.FirstOrDefault(c => c.Id == cardId)
            ?? throw new GameRuleException("Card not found in hand.");

        var targetOwner = GetPlayer(targetOwnerPlayerId);
        var targetOrgan = targetOwner.Organs.FirstOrDefault(o => o.Type == targetOrganType)
            ?? throw new GameRuleException("Target organ not found.");

        ValidateTargetSide(card, playerId, targetOwnerPlayerId);
        ValidateOrganMatch(card, targetOrgan);

        if (card.Type == CardType.Special)
        {
            ResolveSpecial(card, player, targetOwner, targetOrgan);
        }
        else
        {
            var resolver = new CardResolver();
            resolver.Resolve(card, targetOrgan);
        }

        player.Hand.Remove(card);
        _game.Deck!.Discard(card);
        _turnManager.RecordAction(playerId);

        CheckWinCondition();
    }

    public void DrawCard(string playerId)
    {
        EnsurePlaying();
        _turnManager.EnsureCurrentPlayer(playerId);
        _turnManager.DrawForPlayer(playerId);
    }

    /// <summary>
    /// Discards up to 2 selected cards from the player's hand, then draws the
    /// same number of replacement cards from the deck. Consumes the turn's
    /// action and prevents a normal draw for the rest of the turn.
    /// </summary>
    public void SwapCards(string playerId, IReadOnlyCollection<string> cardIds)
    {
        EnsurePlaying();
        _turnManager.EnsureCurrentPlayer(playerId);

        if (_game.Turn is not null && _game.Turn.HasDrawnThisTurn)
        {
            throw new GameRuleException("Cannot swap after drawing this turn.");
        }

        if (cardIds is null || cardIds.Count == 0)
        {
            throw new GameRuleException("Select at least 1 card to swap.");
        }

        if (cardIds.Count > GameRules.MaxSwapCards)
        {
            throw new GameRuleException($"You can swap at most {GameRules.MaxSwapCards} cards.");
        }

        if (cardIds.Distinct().Count() != cardIds.Count())
        {
            throw new GameRuleException("Cannot select the same card twice.");
        }

        var player = GetPlayer(playerId);

        var cards = new List<Card>(cardIds.Count);
        foreach (var id in cardIds)
        {
            cards.Add(player.Hand.FirstOrDefault(c => c.Id == id)
                ?? throw new GameRuleException("Card not found in hand."));
        }

        // Discard first, then draw the same number of replacements.
        foreach (var card in cards)
        {
            player.Hand.Remove(card);
            _game.Deck!.Discard(card);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            var drawn = _game.Deck!.Draw();
            if (drawn is not null)
            {
                player.Hand.Add(drawn);
            }
        }

        _turnManager.RecordAction(playerId);

        // Swap blocks the normal draw for the rest of this turn.
        if (_game.Turn is not null)
        {
            _game.Turn.HasDrawnThisTurn = true;
        }
    }

    public void EndTurn(string playerId)
    {
        EnsurePlaying();
        _turnManager.EndTurn(playerId);
    }

    /// <summary>
    /// Picks a random set of organ types for the game. The Wild organ is
    /// always included; the rest are chosen randomly from the standard organs.
    /// MODIFIED: wild organ will be include randomly, not always. The number of organs is determined by GameRules.OrganCount.
    /// </summary>
    private static IReadOnlyList<OrganType> PickRandomOrganTypes(Random random)
    {
        var selected = Enum.GetValues<OrganType>()
            // .Where(t => t != OrganType.Wild_Organ)
            .OrderBy(_ => random.Next())
            .Take(GameRules.OrganCount)
            .ToList();

        // selected.Add(OrganType.Wild_Organ);
        return selected;
    }

    private void ValidateTargetSide(Card card, string playerId, string targetOwnerPlayerId)
    {
        switch (card.TargetSide)
        {
            case TargetSide.Opponent:
                if (targetOwnerPlayerId == playerId)
                {
                    throw new GameRuleException("Card must target an opponent's organ.");
                }
                break;
            case TargetSide.Self:
                if (targetOwnerPlayerId != playerId)
                {
                    throw new GameRuleException("Card must target your own organ.");
                }
                break;
            case TargetSide.None:
            default:
                throw new GameRuleException("Card has no valid target side.");
        }
    }

    private void ResolveSpecial(Card card, Player caster, Player targetOwner, Organ targetOrgan)
    {
        switch (card.SpecialCard)
        {
            case SpecialCardType.Transplant:
                ResolveTransplant(caster, targetOwner, targetOrgan);
                break;
            default:
                throw new GameRuleException(
                    $"Special card '{card.Name}' is not implemented.");
        }
    }

    private void ResolveTransplant(Player caster, Player targetOwner, Organ targetOrgan)
    {
        if (targetOrgan.IsDestroyed)
        {
            throw new GameRuleException("Cannot transplant a destroyed organ.");
        }

        if (targetOrgan.IsAfflicted)
        {
            throw new GameRuleException("Cannot transplant an afflicted organ.");
        }

        if (targetOrgan.IsShielded)
        {
            throw new GameRuleException("Cannot transplant a shielded organ.");
        }

        targetOwner.Organs.Remove(targetOrgan);

        var position = caster.Organs.Count == 0 ? 0 : caster.Organs.Max(o => o.Position) + 1;
        caster.Organs.Add(new Organ(targetOrgan.Type, position));
    }

    private void ValidateOrganMatch(Card card, Organ targetOrgan)
    {
        // The Wild organ can be afflicted by any affliction card,
        // regardless of the organ type printed on the card.
        if (targetOrgan.Type == OrganType.Wild_Organ && card.Type == CardType.Affliction)
        {
            return;
        }

        if (card.RequiresOrganMatch())
        {
            if (card.TargetOrganType.HasValue)
            {
                if (card.TargetOrganType != targetOrgan.Type)
                {
                    throw new GameRuleException("Card's organ type does not match the target.");
                }

                return;
            }

            if (!card.TargetOrganTypes.Contains(targetOrgan.Type))
            {
                throw new GameRuleException("Card's organ type does not match the target.");
            }
        }
    }

    private void CheckWinCondition()
    {
        var loser = _game.Players.FirstOrDefault(p => !p.IsAlive);
        if (loser is null)
        {
            return;
        }

        _game.Phase = GamePhase.GameOver;
        _game.WinnerPlayerId = _game.Players.First(p => p.Id != loser.Id).Id;
    }

    private void EnsurePlaying()
    {
        if (_game.Phase == GamePhase.WaitingForPlayer)
        {
            throw new GameRuleException("Game has not started.");
        }

        if (_game.Phase == GamePhase.GameOver)
        {
            throw new GameRuleException("Game is already over.");
        }
    }

    private Player GetPlayer(string playerId)
    {
        return _game.Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new GameRuleException("Player not found.");
    }
}