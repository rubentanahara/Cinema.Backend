namespace Cinema.Identity;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("identity", DateTimeOffset.UtcNow);
}
