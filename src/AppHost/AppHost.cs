var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var catalogDb = postgres.AddDatabase("catalogdb");
var seatingDb = postgres.AddDatabase("seatingdb");
var pricingDb = postgres.AddDatabase("pricingdb");
var orderingDb = postgres.AddDatabase("orderingdb");
var paymentsDb = postgres.AddDatabase("paymentsdb");
var ticketingDb = postgres.AddDatabase("ticketingdb");
var loyaltyDb = postgres.AddDatabase("loyaltydb");
var concessionsDb = postgres.AddDatabase("concessionsdb");
var identityDb = postgres.AddDatabase("identitydb");
var notificationsDb = postgres.AddDatabase("notificationsdb");

builder.AddProject<Projects.Cinema_Catalog>("catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5101)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Seating>("seating")
    .WithReference(seatingDb)
    .WaitFor(seatingDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5102)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Pricing>("pricing")
    .WithReference(pricingDb)
    .WaitFor(pricingDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5103)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Ordering>("ordering")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5104)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Payments>("payments")
    .WithReference(paymentsDb)
    .WaitFor(paymentsDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5105)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Ticketing>("ticketing")
    .WithReference(ticketingDb)
    .WaitFor(ticketingDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5106)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Loyalty>("loyalty")
    .WithReference(loyaltyDb)
    .WaitFor(loyaltyDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5107)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Concessions>("concessions")
    .WithReference(concessionsDb)
    .WaitFor(concessionsDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5108)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Identity>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5109)
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Notifications>("notifications")
    .WithReference(notificationsDb)
    .WaitFor(notificationsDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5110)
    .WithGraphQLHttpEndpoint();

await builder.Build().RunAsync();
