using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;
using WebAppSandbox.Hubs;

namespace WebAppSandbox.Tests;

public class VerticalSliceTests
{
    [Fact]
    public void TwoPlayers_FullGame_EndsWithWinner()
    {
        var rooms = new RoomManager();

        // Client 1 creates room, client 2 joins.
        var roomId = rooms.CreateRoom("conn1", "Alice");
        rooms.RegisterConnection("conn1", roomId);

        var room = rooms.JoinRoom(roomId, "conn2", "Bob");
        rooms.RegisterConnection("conn2", roomId);

        Assert.Equal(2, room.Engine.Game.Players.Count);
        Assert.Equal(roomId, rooms.GetRoomIdForConnection("conn1"));
        Assert.Equal(roomId, rooms.GetRoomIdForConnection("conn2"));

        // Start game.
        room.Engine.StartGame(new Random(1));
        var game = room.Engine.Game;

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal("conn1", game.Turn!.CurrentPlayerId);
        Assert.Equal(5, game.Players[0].Hand.Count);
        Assert.Equal(5, game.Players[1].Hand.Count);

        // Turn flow: Alice draws, ends turn to Bob, Bob ends back to Alice.
        var alice = game.Players.First(p => p.Id == "conn1");
        var bob = game.Players.First(p => p.Id == "conn2");

        var beforeDraw = alice.Hand.Count;
        room.Engine.DrawCard("conn1");
        Assert.Equal(beforeDraw + 1, alice.Hand.Count);
        Assert.True(game.Turn.HasDrawnThisTurn);

        room.Engine.EndTurn("conn1");
        Assert.Equal("conn2", game.Turn.CurrentPlayerId);

        room.Engine.EndTurn("conn2");
        Assert.Equal("conn1", game.Turn.CurrentPlayerId);

        // Force a winning position: Bob has only an afflicted Heart left.
        foreach (var organ in bob.Organs.Where(o => o.Type != OrganType.Heart))
        {
            organ.IsDestroyed = true;
        }

        bob.Organs.First(o => o.Type == OrganType.Heart).AddAffliction(AfflictionType.Afflicted);

        // Give Alice a matching Attack card.
        alice.Hand.Clear();
        alice.Hand.Add(new Card
        {
            Id = "attack-heart",
            Name = "Attack Heart",
            Type = CardType.Attack,
            TargetSide = TargetSide.Opponent,
            TargetOrganType = OrganType.Heart,
            Description = "Destroy target's afflicted Heart."
        });

        // Play the winning card.
        room.Engine.PlayCard("conn1", "attack-heart", "conn2", OrganType.Heart);

        Assert.True(bob.Organs.First(o => o.Type == OrganType.Heart).IsDestroyed);
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Equal("conn1", game.WinnerPlayerId);
    }

    [Fact]
    public void CannotStart_BeforeTwoPlayersJoin()
    {
        var rooms = new RoomManager();
        var roomId = rooms.CreateRoom("conn1", "Alice");
        rooms.RegisterConnection("conn1", roomId);

        var ex = Assert.Throws<GameRuleException>(() =>
            rooms.GetRoom(roomId).Engine.StartGame(new Random(1)));

        Assert.Contains("2 players", ex.Message);
    }
}