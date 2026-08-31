namespace Cinema.Concessions.Types;

[QueryType]
public static partial class ConcessionsQueries
{
    public static ConcessionsStatus GetConcessionsStatus() => new("concessions", DateTimeOffset.UtcNow);
}
