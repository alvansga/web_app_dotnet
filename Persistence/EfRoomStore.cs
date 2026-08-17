using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WebAppSandbox.Persistence;

/// <summary>
/// Stores serialized game snapshots in SQLite. Uses a context factory so the
/// singleton RoomManager can safely create a fresh context per operation.
/// </summary>
public class EfRoomStore : IRoomStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public EfRoomStore(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public void Save(GameStateSnapshot snapshot)
    {
        using var db = _factory.CreateDbContext();

        var existing = db.Rooms.Find(snapshot.RoomId);
        if (existing is null)
        {
            db.Rooms.Add(new SavedRoom
            {
                RoomId = snapshot.RoomId,
                Json = JsonSerializer.Serialize(snapshot, JsonOptions),
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Json = JsonSerializer.Serialize(snapshot, JsonOptions);
            existing.UpdatedAt = DateTime.UtcNow;
        }

        db.SaveChanges();
    }

    public bool TryLoad(string roomId, out GameStateSnapshot? snapshot)
    {
        using var db = _factory.CreateDbContext();
        var saved = db.Rooms.Find(roomId);
        if (saved is null)
        {
            snapshot = null;
            return false;
        }

        snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(saved.Json, JsonOptions);
        return snapshot is not null;
    }

    public void Delete(string roomId)
    {
        using var db = _factory.CreateDbContext();
        var saved = db.Rooms.Find(roomId);
        if (saved is not null)
        {
            db.Rooms.Remove(saved);
            db.SaveChanges();
        }
    }

    public IReadOnlyList<string> GetRoomIds()
    {
        using var db = _factory.CreateDbContext();
        return db.Rooms.Select(r => r.RoomId).ToList();
    }
}