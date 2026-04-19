using CodenameApp.Domain;

public interface IPlayerRepository
{
    Task AddAsync(Player player);
    Task<Player?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
}