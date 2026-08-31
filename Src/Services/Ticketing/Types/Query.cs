namespace Cinema.Ticketing.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static TicketingStatus GetTicketingStatus() => new("ticketing", DateTimeOffset.UtcNow);
}
