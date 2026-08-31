using Cinema.Catalog.Domain;
using Cinema.Catalog.Infrastructure;

using GreenDonut.Data;

using HotChocolate.Data;

using Microsoft.EntityFrameworkCore;

namespace Cinema.Catalog.Graph;

[QueryType]
public static partial class CatalogQueries
{
    [UseFiltering]
    [UseSorting]
    public static async Task<IReadOnlyList<Movie>> GetMoviesAsync(
        QueryContext<Movie> query,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
        => await dbContext.Movies
            .With(query)
            .ToListAsync(cancellationToken);
}
