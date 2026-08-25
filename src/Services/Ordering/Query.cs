namespace Cinema.Ordering;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("ordering", DateTimeOffset.UtcNow);
}
