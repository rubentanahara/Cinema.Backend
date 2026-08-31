using Microsoft.EntityFrameworkCore;

namespace Cinema.Ticketing.Infrastructure;

public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(TicketingModule.Schema);
}
