using Cinema.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddGraphQLServer()
    .AddNotificationsTypes();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGraphQL();

await app.RunAsync();
