namespace Cinema.Seating;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("seating", DateTimeOffset.UtcNow);
}
