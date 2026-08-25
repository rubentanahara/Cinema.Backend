namespace Cinema.Pricing;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("pricing", DateTimeOffset.UtcNow);
}
