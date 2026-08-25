using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes;

namespace Cinema.Application.Features.Notes;

public sealed record ListNotesQuery : IRequest<IReadOnlyList<Note>>;