namespace Cinema.Catalog.Types;

[QueryType]
public static partial class ServiceQueries
{
    public static CatalogStatus GetCatalogStatus() => new("catalog", DateTimeOffset.UtcNow);
}
