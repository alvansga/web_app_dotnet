using CodenameApp.Application.Interfaces;
using CodenameApp.Domain;
using CodenameApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CodenameApp.Infrastructure.Repositories;

public class CodenameRepository : ICodenameRepository
{
    private readonly AppDbContext _context;

    public CodenameRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Codename codename)
    {
        await _context.Codenames.AddAsync(codename);
        await _context.SaveChangesAsync();
    }

    public async Task<Codename?> GetByIdAsync(Guid id)
    {
        return await _context.Codenames.FirstOrDefaultAsync(x => x.Id == id);
    }
}