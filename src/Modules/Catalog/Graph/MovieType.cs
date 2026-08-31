using Cinema.Catalog.Domain;

namespace Cinema.Catalog.Graph;

[ObjectType<Movie>]
public static partial class MovieType
{
    static partial void Configure(IObjectTypeDescriptor<Movie> descriptor)
        => descriptor.Ignore(movie => movie.GetDomainEvents());
}
