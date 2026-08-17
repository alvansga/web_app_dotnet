using System.Collections.Concurrent;
using WebAppSandbox.GameEngine.Models;
using WebAppSandbox.GameEngine.Rules;
using WebAppSandbox.Persistence;

namespace WebAppSandbox.Hubs;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly ConcurrentDictionary<string, string> _connectionToRoom = new();
    private readonly IRoomStore _store;

    public RoomManager()
        : this(new InMemoryRoomStore())
    {
    }

    public RoomManager(IRoomStore store)
    {
        _store = store;
    }

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

        SaveRoom(room);
        return roomId;
    }

    public GameRoom JoinRoom(string roomId, string playerId, string playerName)
    {
        var room = GetOrLoadRoom(roomId)
            ?? throw new GameRuleException("Room not found.");

        room.Engine.AddPlayer(playerId, playerName);

        SaveRoom(room);
        return room;
    }

    public List<GameRoom> GetOpenRooms()
    {
        // Make persisted rooms visible in the lobby even after a server restart.
        foreach (var roomId in _store.GetRoomIds())
        {
            if (!_rooms.ContainsKey(roomId))
            {
                GetOrLoadRoom(roomId);
            }
        }

        return _rooms.Values
            .Where(r => r.Engine.Game.Phase == GamePhase.WaitingForPlayer &&
                        r.Engine.Game.Players.Count < GameRules.MaxPlayers)
            .ToList();
    }

    public GameRoom GetRoom(string roomId)
    {
        return GetOrLoadRoom(roomId)
            ?? throw new GameRuleException("Room not found.");
    }

    public bool TryGetRoom(string roomId, out GameRoom? room)
    {
        room = GetOrLoadRoom(roomId);
        return room is not null;
    }

    public void SaveRoom(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            SaveRoom(room);
        }
    }

    public void DeleteRoom(string roomId)
    {
        _rooms.TryRemove(roomId, out _);
        _store.Delete(roomId);
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

    private GameRoom? GetOrLoadRoom(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room;
        }

        if (_store.TryLoad(roomId, out var snapshot) && snapshot is not null)
        {
            var restored = GameStateRestorer.Restore(snapshot);
            _rooms.TryAdd(roomId, restored);
            return _rooms[roomId];
        }

        return null;
    }

    private void SaveRoom(GameRoom room)
    {
        _store.Save(GameStateSerializer.ToSnapshot(room));
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