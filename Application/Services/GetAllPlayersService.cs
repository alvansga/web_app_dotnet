using CodenameApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class GetAllPlayersService
{
    private readonly AppDbContext _context;

    public GetAllPlayersService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlayerResponse>> Execute()
    {
        return await _context.Players
            .Select(p => new PlayerResponse
            {
                Id = p.Id,
                Name = p.Name,
                GameRoomId = p.GameRoomId
            })
            .ToListAsync();
    }
}