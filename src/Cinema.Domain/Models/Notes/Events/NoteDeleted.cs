using Cinema.Domain.Common;

namespace Cinema.Domain.Models.Notes.Events;

public sealed record NoteDeleted(long Id) : IDomainEvent;