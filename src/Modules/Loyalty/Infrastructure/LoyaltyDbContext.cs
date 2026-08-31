using Microsoft.EntityFrameworkCore;

namespace Cinema.Loyalty.Infrastructure;

public sealed class LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(LoyaltyModule.Schema);
}
