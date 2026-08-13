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

        foreach (var player in _game.Players)
        {
            var organTypes = Enum.GetValues<OrganType>();
            foreach (var organ in organTypes)
            {
                player.Organs.Add(new Organ(organ, (int)organ));
            }
        }

        _game.Deck = new Deck(random);
        _game.Deck.Build(CardLibrary.BuildDeck());

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

        var resolver = new CardResolver();
        resolver.Resolve(card, targetOrgan);

        player.Hand.Remove(card);
        _game.Deck!.Discard(card);
        _turnManager.RecordAction(playerId);

        CheckWinCondition();
    }

    public void EndTurn(string playerId)
    {
        EnsurePlaying();
        _turnManager.EndTurn(playerId);
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

    private void ValidateOrganMatch(Card card, Organ targetOrgan)
    {
        if (card.RequiresOrganMatch && card.TargetOrganType != targetOrgan.Type)
        {
            throw new GameRuleException("Card's organ type does not match the target.");
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