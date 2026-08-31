using Cinema.ServiceDefaults;
using Cinema.Ticketing.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Ticketing;

public static class TicketingModule
{
    public const string Schema = "ticketing";

    public static IHostApplicationBuilder AddTicketing(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<TicketingDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<TicketingDbContext>(Schema);

        return builder;
    }
}
