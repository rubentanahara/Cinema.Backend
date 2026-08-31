namespace Cinema.Identity.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static IdentityStatus GetIdentityStatus() => new("identity", DateTimeOffset.UtcNow);
}
