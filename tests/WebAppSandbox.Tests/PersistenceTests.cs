using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.Hubs;
using WebAppSandbox.Persistence;

namespace WebAppSandbox.Tests;

public class PersistenceTests
{
    [Fact]
    public void RoomSurvivesManagerRestart_AndCanContinue()
    {
        var store = new InMemoryRoomStore();

        // First "server instance".
        var rooms = new RoomManager(store);
        var roomId = rooms.CreateRoom("p1", "Alice");
        rooms.RegisterConnection("c1", roomId);

        var room = rooms.JoinRoom(roomId, "p2", "Bob");
        rooms.RegisterConnection("c2", roomId);

        room.Engine.StartGame(new Random(1));
        rooms.SaveRoom(roomId);

        var game = room.Engine.Game;
        var drawPileCount = game.Deck!.DrawPile.Count;
        var discardPileCount = game.Deck.DiscardPile.Count;
        var currentPlayerId = game.Turn!.CurrentPlayerId;
        var handCounts = game.Players.Select(p => p.Hand.Count).ToArray();

        // Simulate a server restart by creating a new RoomManager over the same store.
        var restarted = new RoomManager(store);
        var restored = restarted.GetRoom(roomId);
        var restoredGame = restored.Engine.Game;

        Assert.Equal(GamePhase.Playing, restoredGame.Phase);
        Assert.Equal(2, restoredGame.Players.Count);
        Assert.Equal(drawPileCount, restoredGame.Deck!.DrawPile.Count);
        Assert.Equal(discardPileCount, restoredGame.Deck.DiscardPile.Count);
        Assert.Equal(currentPlayerId, restoredGame.Turn!.CurrentPlayerId);
        Assert.Equal(handCounts, restoredGame.Players.Select(p => p.Hand.Count).ToArray());

        // The restored game must remain playable: end the current turn.
        restored.Engine.EndTurn(currentPlayerId);
        Assert.NotEqual(currentPlayerId, restoredGame.Turn.CurrentPlayerId);
    }

    [Fact]
    public void WaitingRoomSurvivesRestart_AndAcceptsSecondPlayer()
    {
        var store = new InMemoryRoomStore();

        var rooms = new RoomManager(store);
        var roomId = rooms.CreateRoom("p1", "Alice");

        var restarted = new RoomManager(store);
        var restored = restarted.GetRoom(roomId);

        Assert.Equal(GamePhase.WaitingForPlayer, restored.Engine.Game.Phase);
        Assert.Single(restored.Engine.Game.Players);

        // The second player can still join the restored room.
        restarted.JoinRoom(roomId, "p2", "Bob");
        Assert.Equal(2, restored.Engine.Game.Players.Count);
    }
}