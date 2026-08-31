using Cinema.Seating.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Seating;

public static class SeatingModule
{
    public const string Schema = "seating";

    public static IHostApplicationBuilder AddSeating(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<SeatingDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<SeatingDbContext>(Schema);

        return builder;
    }
}
