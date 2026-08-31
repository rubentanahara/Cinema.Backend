namespace Cinema.Loyalty.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static LoyaltyStatus GetLoyaltyStatus() => new("loyalty", DateTimeOffset.UtcNow);
}
