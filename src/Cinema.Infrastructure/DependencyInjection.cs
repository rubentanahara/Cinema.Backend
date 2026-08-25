using Cinema.Application.Features.Notes;
using Cinema.Application.Features.Test;
using Cinema.Infrastructure.Common.Persistence;
using Cinema.Infrastructure.Dapper.Notes;
using Cinema.Infrastructure.Dapper.Test;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema.Infrastructure;

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
        DatabaseInitializer.Initialize(connection);

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connection));
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ITestItemsRepository, TestItemsRepository>();
        services.AddScoped<INotesRepository, NotesRepository>();
    }
}