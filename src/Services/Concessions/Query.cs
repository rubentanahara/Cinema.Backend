namespace Cinema.Concessions;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("concessions", DateTimeOffset.UtcNow);
}
