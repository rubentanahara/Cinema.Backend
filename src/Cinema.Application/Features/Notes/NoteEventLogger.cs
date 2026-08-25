using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes.Events;

using Microsoft.Extensions.Logging;

namespace Cinema.Application.Features.Notes;

public sealed class NoteEventLogger(ILogger<NoteEventLogger> logger) :
    IDomainEventHandler<NoteCreated>,
    IDomainEventHandler<NoteUpdated>,
    IDomainEventHandler<NoteDeleted>
{
    public Task Handle(NoteCreated domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Note {NoteId} created with title {Title}", domainEvent.Id, domainEvent.Title);

        return Task.CompletedTask;
    }

    public Task Handle(NoteUpdated domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Note {NoteId} updated to title {Title}", domainEvent.Id, domainEvent.Title);

        return Task.CompletedTask;
    }

    public Task Handle(NoteDeleted domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Note {NoteId} deleted", domainEvent.Id);

        return Task.CompletedTask;
    }
}