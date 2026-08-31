using Cinema.Ordering.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Ordering;

public static class OrderingModule
{
    public const string Schema = "ordering";

    public static IHostApplicationBuilder AddOrdering(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<OrderingDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<OrderingDbContext>(Schema);

        return builder;
    }
}
