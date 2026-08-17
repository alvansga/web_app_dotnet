using Microsoft.EntityFrameworkCore;

namespace WebAppSandbox.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<SavedRoom> Rooms => Set<SavedRoom>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedRoom>(entity =>
        {
            entity.HasKey(e => e.RoomId);
            entity.Property(e => e.RoomId)
                .ValueGeneratedNever();
            entity.Property(e => e.Json)
                .IsRequired();
        });
    }
}

public class SavedRoom
{
    public string RoomId { get; set; } = "";
    public string Json { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}