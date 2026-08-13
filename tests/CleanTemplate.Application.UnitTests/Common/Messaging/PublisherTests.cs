using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;
using CleanTemplate.Domain.Models.Notes.Events;

using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Application.UnitTests.Common.Messaging;

public class PublisherTests
{
    [Fact]
    public async Task Publish_DispatchesToTheRegisteredDomainEventHandler()
    {
        var handler = Substitute.For<IDomainEventHandler<NoteCreated>>();
        var publisher = BuildPublisher(handler);
        var note = Note.Created(1, "groceries", "milk");

        await publisher.Publish(note);

        await handler.Received(1).Handle(Arg.Is<NoteCreated>(e => e.Id == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_ClearsTheDomainEvents()
    {
        var publisher = BuildPublisher(Substitute.For<IDomainEventHandler<NoteCreated>>());
        var note = Note.Created(1, "groceries", "milk");

        await publisher.Publish(note);

        note.DomainEvents.ShouldBeEmpty();
    }

    private static Publisher BuildPublisher(IDomainEventHandler<NoteCreated> handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(handler);

        return new Publisher(services.BuildServiceProvider());
    }
}