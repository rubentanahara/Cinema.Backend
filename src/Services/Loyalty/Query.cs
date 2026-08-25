namespace Cinema.Loyalty;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("loyalty", DateTimeOffset.UtcNow);
}
