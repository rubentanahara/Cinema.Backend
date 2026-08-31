namespace Cinema.Ordering.Types;

[QueryType]
public static partial class OrderingQueries
{
    public static OrderingStatus GetOrderingStatus() => new("ordering", DateTimeOffset.UtcNow);
}
