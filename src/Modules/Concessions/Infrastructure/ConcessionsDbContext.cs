using Microsoft.EntityFrameworkCore;

namespace Cinema.Concessions.Infrastructure;

public sealed class ConcessionsDbContext(DbContextOptions<ConcessionsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.HasDefaultSchema(ConcessionsModule.Schema);
}
