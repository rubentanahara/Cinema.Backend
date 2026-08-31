namespace Cinema.Seating.Types;

[QueryType]
public static partial class SeatingQueries
{
    public static SeatingStatus GetSeatingStatus() => new("seating", DateTimeOffset.UtcNow);
}
