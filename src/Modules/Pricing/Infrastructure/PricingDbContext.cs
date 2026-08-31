using Microsoft.EntityFrameworkCore;

namespace Cinema.Pricing.Infrastructure;

public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(PricingModule.Schema);
}
