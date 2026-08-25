namespace Cinema.Payments;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("payments", DateTimeOffset.UtcNow);
}
