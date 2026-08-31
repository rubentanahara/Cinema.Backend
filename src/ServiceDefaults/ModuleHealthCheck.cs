using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cinema.ServiceDefaults;

public sealed class ModuleHealthCheck<TContext>(IDbContextFactory<TContext> contextFactory)
    : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingCount = pendingMigrations.Count();

        return pendingCount is 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"{pendingCount} pending migrations");
    }
}
