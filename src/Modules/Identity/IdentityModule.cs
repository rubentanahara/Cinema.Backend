using Cinema.Identity.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Identity;

public static class IdentityModule
{
    public const string Schema = "identity";

    public static IHostApplicationBuilder AddIdentity(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<IdentityDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<IdentityDbContext>(Schema);

        return builder;
    }
}
