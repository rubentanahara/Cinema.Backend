using CleanTemplate.Application.Features.Notes;
using CleanTemplate.Application.Features.Test;
using CleanTemplate.Infrastructure.Common.Persistence;
using CleanTemplate.Infrastructure.Dapper.Notes;
using CleanTemplate.Infrastructure.Dapper.Test;

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
        DatabaseInitializer.Initialize(connection);

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connection));
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ITestItemsRepository, TestItemsRepository>();
        services.AddScoped<INotesRepository, NotesRepository>();
    }
}