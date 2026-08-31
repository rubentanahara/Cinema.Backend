using Cinema.Catalog;
using Cinema.Catalog.Infrastructure;
using Cinema.Concessions;
using Cinema.Identity;
using Cinema.Loyalty;
using Cinema.Notifications;
using Cinema.Ordering;
using Cinema.Payments;
using Cinema.Pricing;
using Cinema.Seating;
using Cinema.ServiceDefaults;
using Cinema.Ticketing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCatalog();
builder.AddConcessions();
builder.AddIdentity();
builder.AddLoyalty();
builder.AddNotifications();
builder.AddOrdering();
builder.AddPayments();
builder.AddPricing();
builder.AddSeating();
builder.AddTicketing();

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
