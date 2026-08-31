using Cinema.Payments.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Payments;

public static class PaymentsModule
{
    public const string Schema = "payments";

    public static IHostApplicationBuilder AddPayments(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<PaymentsDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<PaymentsDbContext>(Schema);

        return builder;
    }
}
