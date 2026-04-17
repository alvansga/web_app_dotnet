using Microsoft.EntityFrameworkCore;
using CodenameApp.Domain;

namespace CodenameApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {}

    public DbSet<Codename> Codenames { get; set; } = null!;
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<GameRoom> GameRooms { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
        modelBuilder.Entity<GameRoom>()
            .HasMany(r => r.Players)
            .WithOne(p => p.GameRoom)
            .HasForeignKey(p => p.GameRoomId);
    }
}