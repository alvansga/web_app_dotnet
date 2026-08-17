namespace WebAppSandbox.Persistence;

/// <summary>
/// Persistence boundary for saved game rooms. Implementations are expected to
/// be stateless/thread-safe and create their own database context per call.
/// </summary>
public interface IRoomStore
{
    void Save(GameStateSnapshot snapshot);
    bool TryLoad(string roomId, out GameStateSnapshot? snapshot);
    void Delete(string roomId);
    IReadOnlyList<string> GetRoomIds();
}