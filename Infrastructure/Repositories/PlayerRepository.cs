using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;
using CodenameApp.Infrastructure.Data;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _context;

    public PlayerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
        await _context.SaveChangesAsync();
    }

    public async Task<Player?> GetByIdAsync(Guid id)
    {
        return await _context.Players.FindAsync(id);
    }
}