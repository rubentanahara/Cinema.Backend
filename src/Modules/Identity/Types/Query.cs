namespace Cinema.Identity.Types;

[QueryType]
public static partial class IdentityQueries
{
    public static IdentityStatus GetIdentityStatus() => new("identity", DateTimeOffset.UtcNow);
}
