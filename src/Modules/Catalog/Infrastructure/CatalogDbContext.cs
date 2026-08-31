using Cinema.Catalog.Domain;

using Microsoft.EntityFrameworkCore;

namespace Cinema.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(CatalogModule.Schema);

        modelBuilder.Entity<Movie>(movie =>
        {
            movie.HasKey(m => m.Id);
            movie.Property(m => m.Title).HasMaxLength(200).IsRequired();
            movie.HasIndex(m => m.Title);
        });
    }
}
