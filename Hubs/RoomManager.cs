using System.Collections.Concurrent;
using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;

namespace WebAppSandbox.Hubs;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly ConcurrentDictionary<string, string> _connectionToRoom = new();

    public string CreateRoom(string playerId, string playerName)
    {
        var roomId = Guid.NewGuid().ToString("N")[..6];

        var game = new Game(roomId);
        var engine = new OrganAttackGame(game);
        engine.AddPlayer(playerId, playerName);

        var room = new GameRoom(roomId, engine);
        if (!_rooms.TryAdd(roomId, room))
        {
            throw new InvalidOperationException("Failed to create room.");
        }

        return roomId;
    }

    public GameRoom JoinRoom(string roomId, string playerId, string playerName)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            throw new GameRuleException("Room not found.");
        }

        room.Engine.AddPlayer(playerId, playerName);
        return room;
    }

    public List<GameRoom> GetOpenRooms()
    {
        return _rooms.Values
            .Where(r => r.Engine.Game.Phase == GamePhase.WaitingForPlayer &&
                        r.Engine.Game.Players.Count < GameRules.MaxPlayers)
            .ToList();
    }

    public GameRoom GetRoom(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            throw new GameRuleException("Room not found.");
        }

        return room;
    }

    public bool TryGetRoom(string roomId, out GameRoom? room)
    {
        return _rooms.TryGetValue(roomId, out room);
    }

    public void RegisterConnection(string connectionId, string roomId)
    {
        _connectionToRoom[connectionId] = roomId;
    }

    public string? GetRoomIdForConnection(string connectionId)
    {
        return _connectionToRoom.TryGetValue(connectionId, out var roomId)
            ? roomId
            : null;
    }

    public void RemoveConnection(string connectionId)
    {
        _connectionToRoom.TryRemove(connectionId, out _);
    }
}

public class GameRoom
{
    public string RoomId { get; }
    public OrganAttackGame Engine { get; }
    public Player? Winner => Engine.Game.WinnerPlayerId is null
        ? null
        : Engine.Game.Players.FirstOrDefault(p => p.Id == Engine.Game.WinnerPlayerId);

    public GameRoom(string roomId, OrganAttackGame engine)
    {
        RoomId = roomId;
        Engine = engine;
    }
}