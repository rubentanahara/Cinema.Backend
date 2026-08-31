using Microsoft.EntityFrameworkCore;

namespace Cinema.Seating.Infrastructure;

public sealed class SeatingDbContext(DbContextOptions<SeatingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(SeatingModule.Schema);
}
