using CodenameApp.Domain;

public interface IPlayerRepository
{
    Task AddAsync(Player player);
    Task<Player?> GetByIdAsync(Guid id);
    /// <summary>Returns null if player not found OR token doesn't match — always use for auth.</summary>
    Task<Player?> GetByIdAndTokenAsync(Guid id, string token);
    Task SaveChangesAsync();
}