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
    public DbSet<GameCard> GameCards { get; set; } = null!;
    public DbSet<Clue> Clues { get; set; } = null!;
    public DbSet<GameActionLog> GameActionLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
        modelBuilder.Entity<GameRoom>()
            .HasMany(r => r.Players)
            .WithOne(p => p.GameRoom)
            .HasForeignKey(p => p.GameRoomId);

        modelBuilder.Entity<GameRoom>()
            .HasMany(r => r.Cards)
            .WithOne(c => c.GameRoom)
            .HasForeignKey(c => c.GameRoomId);

        modelBuilder.Entity<GameRoom>()
            .HasMany(r => r.Clues)
            .WithOne(c => c.GameRoom)
            .HasForeignKey(c => c.GameRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameRoom>()
            .HasMany(r => r.ActionLogs)
            .WithOne(l => l.GameRoom)
            .HasForeignKey(l => l.GameRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameCard>()
            .Property(c => c.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Clue>()
            .Property(c => c.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<GameActionLog>()
            .Property(l => l.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<GameCard>()
            .Property(c => c.Role)
            .HasConversion<string>();

        modelBuilder.Entity<GameRoom>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<GameRoom>()
            .OwnsOne(r => r.State, state =>
            {
                state.Property(s => s.Phase).HasConversion<string>();
            });

        modelBuilder.Entity<Player>()
            .Property(p => p.GameRole)
            .HasConversion<string>();
    }
}