namespace Cinema.Notifications.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static NotificationsStatus GetNotificationsStatus() => new("notifications", DateTimeOffset.UtcNow);
}
