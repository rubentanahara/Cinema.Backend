using CleanTemplate.Domain.Common;

namespace CleanTemplate.Domain.Models.Notes.Events;

public sealed record NoteCreated(long Id, string Title) : IDomainEvent;