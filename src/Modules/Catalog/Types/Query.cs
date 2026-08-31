namespace Cinema.Catalog.Types;

[QueryType]
public static partial class CatalogQueries
{
    public static CatalogStatus GetCatalogStatus() => new("catalog", DateTimeOffset.UtcNow);
}
