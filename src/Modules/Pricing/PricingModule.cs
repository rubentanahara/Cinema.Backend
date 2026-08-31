using Cinema.Pricing.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Pricing;

public static class PricingModule
{
    public const string Schema = "pricing";

    public static IHostApplicationBuilder AddPricing(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<PricingDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<PricingDbContext>(Schema);

        return builder;
    }
}
