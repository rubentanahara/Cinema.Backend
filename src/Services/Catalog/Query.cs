namespace Cinema.Catalog;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("catalog", DateTimeOffset.UtcNow);
}
