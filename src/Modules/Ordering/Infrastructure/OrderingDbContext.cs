using Microsoft.EntityFrameworkCore;

namespace Cinema.Ordering.Infrastructure;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(OrderingModule.Schema);
}
