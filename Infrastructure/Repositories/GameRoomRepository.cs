using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;
using CodenameApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class GameRoomRepository : IGameRoomRepository
{
    private readonly AppDbContext _context;

    public GameRoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(GameRoom room)
    {
        await _context.GameRooms.AddAsync(room);
        await _context.SaveChangesAsync();
    }

    public async Task<GameRoom?> GetByCodeAsync(string code)
    {
        return await _context.GameRooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Code == code);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}