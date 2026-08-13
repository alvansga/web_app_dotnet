using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.GameEngine.Rules;

public class TurnManager
{
    private readonly Game _game;

    public TurnManager(Game game)
    {
        _game = game;
    }

    public void StartTurn(string playerId)
    {
        _game.Turn = new TurnState
        {
            CurrentPlayerId = playerId,
            HasDrawnThisTurn = false,
            ActionsUsed = 0,
            MaxActionsPerTurn = GameRules.MaxActionsPerTurn
        };
    }

    public void DrawForPlayer(string playerId)
    {
        var player = GetPlayer(playerId);

        if (_game.Turn is not null && _game.Turn.HasDrawnThisTurn)
        {
            throw new GameRuleException("Already drawn this turn.");
        }

        if (_game.Deck is null)
        {
            throw new GameRuleException("Deck is not initialized.");
        }

        if (player.Hand.Count >= GameRules.MaxHandSize)
        {
            return; // hand penuh, skip draw
        }

        var card = _game.Deck.Draw();
        if (card is not null)
        {
            player.Hand.Add(card);
            if (_game.Turn is not null)
            {
                _game.Turn.HasDrawnThisTurn = true;
            }
        }
    }

    public void EndTurn(string playerId)
    {
        EnsureCurrentPlayer(playerId);

        var nextIndex = (_game.Players.FindIndex(p => p.Id == playerId) + 1) % GameRules.MaxPlayers;
        var nextPlayer = _game.Players[nextIndex];
        StartTurn(nextPlayer.Id);
    }

    public void RecordAction(string playerId)
    {
        EnsureCurrentPlayer(playerId);

        if (!_game.Turn!.CanAct)
        {
            throw new GameRuleException("No more actions this turn.");
        }

        _game.Turn.ActionsUsed++;
    }

    public void EnsureCurrentPlayer(string playerId)
    {
        if (_game.Turn is null)
        {
            throw new GameRuleException("Turn has not started.");
        }

        if (_game.Turn.CurrentPlayerId != playerId)
        {
            throw new GameRuleException("It is not your turn.");
        }
    }

    private Player GetPlayer(string playerId)
    {
        var player = _game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player is null)
        {
            throw new GameRuleException("Player not found.");
        }

        return player;
    }
}