namespace Cinema.Catalog.Domain;

public sealed class Movie
{
    public Guid Id { get; init; }

    public required string Title { get; set; }

    public required int RuntimeMinutes { get; set; }

    public required DateOnly ReleasedOn { get; set; }
}
