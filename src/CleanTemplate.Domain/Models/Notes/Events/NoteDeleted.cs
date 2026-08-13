using CleanTemplate.Domain.Common;

namespace CleanTemplate.Domain.Models.Notes.Events;

public sealed record NoteDeleted(long Id) : IDomainEvent;