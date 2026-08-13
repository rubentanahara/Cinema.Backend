using CleanTemplate.Application.Test;
using CleanTemplate.Infrastructure.Common.Persistence;
using CleanTemplate.Infrastructure.Test;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddPersistence();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    private static void AddPersistence(this IServiceCollection services)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        SeedTestItems(connection);

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connection));
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ITestItemsRepository, TestItemsRepository>();
    }

    private static void SeedTestItems(SqliteConnection connection)
    {
        connection.Execute("""
            CREATE TABLE TestItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Message TEXT NOT NULL
            );

            INSERT INTO TestItems (Message) VALUES ('test');
            """);
    }
}