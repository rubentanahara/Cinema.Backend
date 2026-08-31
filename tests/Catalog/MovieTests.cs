using Cinema.Catalog.Domain;

using Shouldly;

using Xunit;

namespace Cinema.Catalog.Tests;

public sealed class MovieTests
{
    [Fact]
    public void RetitleChangesTheTitleAndAnnouncesIt()
    {
        var movie = new Movie(Guid.NewGuid(), "Dune", 155, new DateOnly(2021, 10, 22));

        movie.Retitle("Dune: Part One");

        movie.Title.ShouldBe("Dune: Part One");
        movie.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBe(new MovieRetitled(movie.Id, "Dune: Part One"));
    }

    [Fact]
    public void RetitleToTheSameTitleAnnouncesNothing()
    {
        var movie = new Movie(Guid.NewGuid(), "Dune", 155, new DateOnly(2021, 10, 22));

        movie.Retitle("Dune");

        movie.GetDomainEvents().ShouldBeEmpty();
    }
}
