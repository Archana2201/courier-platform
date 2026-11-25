using Courier.Pricing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Courier.Pricing.Data;

public class PricingDbContext : DbContext
{
    public PricingDbContext(DbContextOptions<PricingDbContext> options) : base(options) { }

    public DbSet<RateCard> RateCards => Set<RateCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RateCard>(e =>
        {
            e.ToTable("rate_cards");
            e.HasKey(x => x.Id);
            e.Property(x => x.ServiceLevel).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ServiceLevel).IsUnique();
            e.Property(x => x.BasePrice).HasPrecision(10, 2);
            e.Property(x => x.PerKgPrice).HasPrecision(10, 2);
            e.HasData(
                new RateCard { Id = 1, ServiceLevel = "Standard", BasePrice = 9.50m, PerKgPrice = 1.80m, EstimatedDays = 4 },
                new RateCard { Id = 2, ServiceLevel = "Express", BasePrice = 14.50m, PerKgPrice = 2.60m, EstimatedDays = 2 },
                new RateCard { Id = 3, ServiceLevel = "Overnight", BasePrice = 22.00m, PerKgPrice = 3.90m, EstimatedDays = 1 });
        });
    }
}
