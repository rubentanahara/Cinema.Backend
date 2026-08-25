var builder = DistributedApplication.CreateBuilder(args);

builder.AddNitroComposition();

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

var catalog = builder.AddProject<Projects.Cinema_Catalog>("catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var seating = builder.AddProject<Projects.Cinema_Seating>("seating")
    .WithReference(seatingDb)
    .WaitFor(seatingDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var pricing = builder.AddProject<Projects.Cinema_Pricing>("pricing")
    .WithReference(pricingDb)
    .WaitFor(pricingDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var ordering = builder.AddProject<Projects.Cinema_Ordering>("ordering")
    .WithReference(orderingDb)
    .WaitFor(orderingDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var payments = builder.AddProject<Projects.Cinema_Payments>("payments")
    .WithReference(paymentsDb)
    .WaitFor(paymentsDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var ticketing = builder.AddProject<Projects.Cinema_Ticketing>("ticketing")
    .WithReference(ticketingDb)
    .WaitFor(ticketingDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var loyalty = builder.AddProject<Projects.Cinema_Loyalty>("loyalty")
    .WithReference(loyaltyDb)
    .WaitFor(loyaltyDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var concessions = builder.AddProject<Projects.Cinema_Concessions>("concessions")
    .WithReference(concessionsDb)
    .WaitFor(concessionsDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var identity = builder.AddProject<Projects.Cinema_Identity>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

var notifications = builder.AddProject<Projects.Cinema_Notifications>("notifications")
    .WithReference(notificationsDb)
    .WaitFor(notificationsDb)
    .WithHttpHealthCheck("/health")
    .WithGraphQLHttpEndpoint();

builder.AddProject<Projects.Cinema_Gateway>("gateway")
    .WithReference(catalog)
    .WaitFor(catalog)
    .WithReference(seating)
    .WaitFor(seating)
    .WithReference(pricing)
    .WaitFor(pricing)
    .WithReference(ordering)
    .WaitFor(ordering)
    .WithReference(payments)
    .WaitFor(payments)
    .WithReference(ticketing)
    .WaitFor(ticketing)
    .WithReference(loyalty)
    .WaitFor(loyalty)
    .WithReference(concessions)
    .WaitFor(concessions)
    .WithReference(identity)
    .WaitFor(identity)
    .WithReference(notifications)
    .WaitFor(notifications)
    .WithEndpoint("http", endpoint => endpoint.Port = 5100)
    .WithNitroComposition();

await builder.Build().RunAsync();
