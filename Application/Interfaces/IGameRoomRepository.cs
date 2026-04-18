using CodenameApp.Domain;

public interface IGameRoomRepository
{
    Task AddAsync(GameRoom room);
    Task<GameRoom?> GetByCodeAsync(string code);
    Task<List<GameRoom>> GetAllAsync();
    Task SaveChangesAsync();
}