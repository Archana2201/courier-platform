using Courier.Tracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Courier.Tracking.Data;

public class TrackingDbContext : DbContext
{
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options) : base(options) { }

    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackingEvent>(e =>
        {
            e.ToTable("tracking_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TrackingNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Location).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasIndex(x => x.TrackingNumber);
            e.HasIndex(x => x.SourceEventId).IsUnique();
        });
    }
}
