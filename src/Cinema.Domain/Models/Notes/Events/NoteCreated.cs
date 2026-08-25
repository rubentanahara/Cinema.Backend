using Cinema.Domain.Common;

namespace Cinema.Domain.Models.Notes.Events;

public sealed record NoteCreated(long Id, string Title) : IDomainEvent;