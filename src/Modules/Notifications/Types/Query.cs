namespace Cinema.Notifications.Types;

[QueryType]
public static partial class NotificationsQueries
{
    public static NotificationsStatus GetNotificationsStatus() => new("notifications", DateTimeOffset.UtcNow);
}
