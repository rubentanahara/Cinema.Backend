namespace Cinema.Pricing.Types;

[QueryType]
public static partial class PricingQueries
{
    public static PricingStatus GetPricingStatus() => new("pricing", DateTimeOffset.UtcNow);
}
