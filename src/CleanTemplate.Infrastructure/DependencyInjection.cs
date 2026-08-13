using CleanTemplate.Application.Features.Notes;
using CleanTemplate.Application.Features.Test;
using CleanTemplate.Infrastructure.Common.Persistence;
using CleanTemplate.Infrastructure.Dapper.Notes;
using CleanTemplate.Infrastructure.Dapper.Test;

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
        CreateNotesTable(connection);

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connection));
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ITestItemsRepository, TestItemsRepository>();
        services.AddScoped<INotesRepository, NotesRepository>();
    }

    private static void CreateNotesTable(SqliteConnection connection)
    {
        connection.Execute("""
            CREATE TABLE Notes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL
            );
            """);
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