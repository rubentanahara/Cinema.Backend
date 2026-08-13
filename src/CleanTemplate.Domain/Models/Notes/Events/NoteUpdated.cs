using CleanTemplate.Domain.Common;

namespace CleanTemplate.Domain.Models.Notes.Events;

public sealed record NoteUpdated(long Id, string Title) : IDomainEvent;