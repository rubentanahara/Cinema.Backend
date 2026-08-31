using System.Net.Http.Json;

using Cinema.Catalog.Domain;
using Cinema.Catalog.Infrastructure;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

using Testcontainers.PostgreSql;

using Xunit;

namespace Cinema.Catalog.Tests;

public sealed class CatalogQueryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await this.postgres.StartAsync();

        this.factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:cinema", this.postgres.GetConnectionString()));

        var contextFactory = this.factory.Services.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        await dbContext.Database.MigrateAsync();

        dbContext.Movies.Add(new Movie
        {
            Title = "Dune",
            RuntimeMinutes = 155,
            ReleasedOn = new DateOnly(2021, 10, 22),
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (this.factory is not null)
        {
            await this.factory.DisposeAsync();
        }

        await this.postgres.DisposeAsync();
    }

    [Fact]
    public async Task MoviesQueryReadsThroughTheCatalogSchema()
    {
        using var client = this.factory!.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/graphql",
            new { query = "{ movies { title runtimeMinutes } }" });

        var body = await response.Content.ReadAsStringAsync();

        body.ShouldContain("Dune");
        body.ShouldNotContain("errors");
    }

    [Fact]
    public async Task MigrationsHistoryLivesInTheModuleSchema()
    {
        var contextFactory = this.factory!.Services.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var schemas = await dbContext.Database
            .SqlQuery<string>($@"select table_schema from information_schema.tables
                                 where table_name = '__EFMigrationsHistory'")
            .ToListAsync();

        schemas.ShouldBe(["catalog"]);
    }
}
