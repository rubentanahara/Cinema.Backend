using Cinema.Domain.Common;

namespace Cinema.Domain.Models.Notes.Events;

public sealed record NoteUpdated(long Id, string Title) : IDomainEvent;