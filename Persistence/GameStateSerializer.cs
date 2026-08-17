using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.Hubs;

namespace WebAppSandbox.Persistence;

/// <summary>
/// Converts an in-memory game room into a persistable snapshot.
/// </summary>
public static class GameStateSerializer
{
    public static GameStateSnapshot ToSnapshot(GameRoom room)
    {
        var game = room.Engine.Game;

        var snapshot = new GameStateSnapshot
        {
            RoomId = room.RoomId,
            Phase = game.Phase.ToString(),
            WinnerPlayerId = game.WinnerPlayerId,
            Players = game.Players.Select(ToPlayerSnapshot).ToList(),
            Turn = game.Turn is null
                ? null
                : new TurnSnapshot
                {
                    CurrentPlayerId = game.Turn.CurrentPlayerId,
                    HasDrawnThisTurn = game.Turn.HasDrawnThisTurn,
                    ActionsUsed = game.Turn.ActionsUsed,
                    MaxActionsPerTurn = game.Turn.MaxActionsPerTurn,
                },
            Deck = new DeckSnapshot
            {
                OrganTypes = game.DeckOrganTypes.Select(t => t.ToString()).ToList(),
                DrawPileCardIds = game.Deck?.DrawPile.Select(c => c.Id).ToList() ?? new(),
                DiscardPileCardIds = game.Deck?.DiscardPile.Select(c => c.Id).ToList() ?? new(),
            },
            PendingAttack = game.PendingAttack is null
                ? null
                : new PendingAttackSnapshot
                {
                    Id = game.PendingAttack.Id,
                    CardId = game.PendingAttack.Card.Id,
                    CasterId = game.PendingAttack.Caster.Id,
                    TargetOwnerId = game.PendingAttack.TargetOwner.Id,
                    TargetOrganType = game.PendingAttack.TargetOrgan.Type.ToString(),
                    TargetOrganPosition = game.PendingAttack.TargetOrgan.Position,
                },
        };

        return snapshot;
    }

    private static PlayerSnapshot ToPlayerSnapshot(Player player)
    {
        return new PlayerSnapshot
        {
            Id = player.Id,
            Name = player.Name,
            Organs = player.Organs.Select(o => new OrganSnapshot
            {
                Type = o.Type.ToString(),
                Position = o.Position,
                IsDestroyed = o.IsDestroyed,
                IsShielded = o.IsShielded,
                Afflictions = o.Afflictions.Select(a => a.ToString()).ToList(),
            }).ToList(),
            HandCardIds = player.Hand.Select(c => c.Id).ToList(),
        };
    }
}