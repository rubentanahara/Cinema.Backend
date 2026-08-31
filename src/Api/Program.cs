using Cinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddGraphQL()
    .AddCatalogTypes()
    .AddSeatingTypes()
    .AddPricingTypes()
    .AddOrderingTypes()
    .AddPaymentsTypes()
    .AddTicketingTypes()
    .AddLoyaltyTypes()
    .AddConcessionsTypes()
    .AddIdentityTypes()
    .AddNotificationsTypes();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGraphQL();

await app.RunWithGraphQLCommandsAsync(args);
