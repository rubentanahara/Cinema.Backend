using Cinema.Loyalty.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Loyalty;

public static class LoyaltyModule
{
    public const string Schema = "loyalty";

    public static IHostApplicationBuilder AddLoyalty(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<LoyaltyDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<LoyaltyDbContext>(Schema);

        return builder;
    }
}
