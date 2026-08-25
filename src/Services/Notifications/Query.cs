namespace Cinema.Notifications;

[QueryType]
public static partial class ServiceQueries
{
    public static ServiceStatus GetServiceStatus() => new("notifications", DateTimeOffset.UtcNow);
}
