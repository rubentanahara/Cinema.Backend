using Cinema.Concessions.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Concessions;

public static class ConcessionsModule
{
    public const string Schema = "concessions";

    public static IHostApplicationBuilder AddConcessions(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<ConcessionsDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<ConcessionsDbContext>(Schema);

        return builder;
    }
}
