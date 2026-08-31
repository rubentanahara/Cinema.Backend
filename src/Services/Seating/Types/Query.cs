namespace Cinema.Seating.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static SeatingStatus GetSeatingStatus() => new("seating", DateTimeOffset.UtcNow);
}
