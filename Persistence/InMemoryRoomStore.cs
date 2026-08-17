using System.Collections.Concurrent;

namespace WebAppSandbox.Persistence;

/// <summary>
/// Non-persistent store used by unit tests and the default RoomManager
/// constructor, keeping behavior identical to the previous in-memory-only flow.
/// </summary>
public class InMemoryRoomStore : IRoomStore
{
    private readonly ConcurrentDictionary<string, GameStateSnapshot> _snapshots = new();

    public void Save(GameStateSnapshot snapshot)
    {
        _snapshots[snapshot.RoomId] = snapshot;
    }

    public bool TryLoad(string roomId, out GameStateSnapshot? snapshot)
    {
        return _snapshots.TryGetValue(roomId, out snapshot);
    }

    public void Delete(string roomId)
    {
        _snapshots.TryRemove(roomId, out _);
    }

    public IReadOnlyList<string> GetRoomIds()
    {
        return _snapshots.Keys.ToList();
    }
}