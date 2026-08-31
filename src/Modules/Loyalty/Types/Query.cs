namespace Cinema.Loyalty.Types;

[QueryType]
public static partial class LoyaltyQueries
{
    public static LoyaltyStatus GetLoyaltyStatus() => new("loyalty", DateTimeOffset.UtcNow);
}
