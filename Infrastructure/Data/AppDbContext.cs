using Microsoft.EntityFrameworkCore;
using CodenameApp.Domain;

namespace CodenameApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {}

    public DbSet<Codename> Codenames { get; set; }
    public DbSet<Player> Players { get; set; }
}