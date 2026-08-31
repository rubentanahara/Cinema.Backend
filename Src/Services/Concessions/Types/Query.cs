namespace Cinema.Concessions.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static ConcessionsStatus GetConcessionsStatus() => new("concessions", DateTimeOffset.UtcNow);
}
