using Cinema.Catalog.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Catalog;

public static class CatalogModule
{
    public const string Schema = "catalog";

    public static IHostApplicationBuilder AddCatalog(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<CatalogDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<CatalogDbContext>(Schema);

        return builder;
    }
}
