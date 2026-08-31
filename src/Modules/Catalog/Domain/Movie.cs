using Cinema.SharedKernel;

namespace Cinema.Catalog.Domain;

public sealed class Movie : Entity
{
    public Movie(Guid id, string title, int runtimeMinutes, DateOnly releasedOn)
    {
        Id = id;
        Title = title;
        RuntimeMinutes = runtimeMinutes;
        ReleasedOn = releasedOn;
    }

    public Guid Id { get; private init; }

    public string Title { get; private set; }

    public int RuntimeMinutes { get; private init; }

    public DateOnly ReleasedOn { get; private init; }

    public void Retitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (title == Title)
        {
            return;
        }

        Title = title;
        Raise(new MovieRetitled(Id, title));
    }
}
