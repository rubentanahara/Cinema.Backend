using Cinema.Catalog;
using Cinema.Catalog.Infrastructure;
using Cinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddCatalog();

builder.AddGraphQL()
    .RegisterDbContextFactory<CatalogDbContext>()
    .AddFiltering()
    .AddSorting()
    .AddCatalogTypes();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);

public partial class Program
{
    protected Program()
    {
    }
}
