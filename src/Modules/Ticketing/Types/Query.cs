namespace Cinema.Ticketing.Types;

[QueryType]
public static partial class TicketingQueries
{
    public static TicketingStatus GetTicketingStatus() => new("ticketing", DateTimeOffset.UtcNow);
}
