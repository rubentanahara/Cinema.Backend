using Cinema.SharedKernel;

namespace Cinema.Catalog.Domain;

public sealed record MovieRetitled(Guid MovieId, string Title) : IDomainEvent;
