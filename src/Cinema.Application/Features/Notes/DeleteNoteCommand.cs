using Cinema.Application.Common.Messaging;

using ErrorOr;

namespace Cinema.Application.Features.Notes;

public sealed record DeleteNoteCommand(long Id) : IRequest<ErrorOr<Deleted>>;