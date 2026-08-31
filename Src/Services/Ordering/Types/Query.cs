namespace Cinema.Ordering.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static OrderingStatus GetOrderingStatus() => new("ordering", DateTimeOffset.UtcNow);
}
