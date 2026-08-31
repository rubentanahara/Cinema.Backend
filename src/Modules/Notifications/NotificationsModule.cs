using Cinema.Notifications.Infrastructure;
using Cinema.ServiceDefaults;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinema.Notifications;

public static class NotificationsModule
{
    public const string Schema = "notifications";

    public static IHostApplicationBuilder AddNotifications(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<NotificationsDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("cinema"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)));

        builder.Services.AddHealthChecks()
            .AddModuleCheck<NotificationsDbContext>(Schema);

        return builder;
    }
}
