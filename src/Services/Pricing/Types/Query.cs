namespace Cinema.Pricing.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static PricingStatus GetPricingStatus() => new("pricing", DateTimeOffset.UtcNow);
}
