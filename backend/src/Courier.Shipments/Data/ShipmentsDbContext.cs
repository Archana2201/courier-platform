using Courier.BuildingBlocks.Outbox;
using Courier.Shipments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Courier.Shipments.Data;

public class ShipmentsDbContext : DbContext, IOutboxContext
{
    public ShipmentsDbContext(DbContextOptions<ShipmentsDbContext> options) : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentStatusChange> StatusChanges => Set<ShipmentStatusChange>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(e =>
        {
            e.ToTable("shipments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TrackingNumber).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.TrackingNumber).IsUnique();
            e.Property(x => x.ServiceLevel).HasMaxLength(20).IsRequired();
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.OwnsOne(x => x.Sender, a => ConfigureAddress(a, "sender"));
            e.OwnsOne(x => x.Recipient, a => ConfigureAddress(a, "recipient"));
            e.HasMany(x => x.History).WithOne().HasForeignKey(h => h.ShipmentId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Sender).IsRequired();
            e.Navigation(x => x.Recipient).IsRequired();
        });

        modelBuilder.Entity<ShipmentStatusChange>(e =>
        {
            e.ToTable("shipment_status_changes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Location).HasMaxLength(100);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Type).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ProcessedAtUtc);
        });
    }

    private static void ConfigureAddress(Microsoft.EntityFrameworkCore.Metadata.Builders.OwnedNavigationBuilder<Shipment, Address> a, string prefix)
    {
        a.Property(x => x.Name).HasColumnName($"{prefix}_name").HasMaxLength(120);
        a.Property(x => x.Line1).HasColumnName($"{prefix}_line1").HasMaxLength(200);
        a.Property(x => x.City).HasColumnName($"{prefix}_city").HasMaxLength(100);
        a.Property(x => x.State).HasColumnName($"{prefix}_state").HasMaxLength(10);
        a.Property(x => x.Postcode).HasColumnName($"{prefix}_postcode").HasMaxLength(10);
    }
}
