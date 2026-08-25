namespace Cinema.Ticketing;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("ticketing", DateTimeOffset.UtcNow);
}
