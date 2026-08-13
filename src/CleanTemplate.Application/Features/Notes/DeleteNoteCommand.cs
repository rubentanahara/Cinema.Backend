using CleanTemplate.Application.Common.Messaging;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

public sealed record DeleteNoteCommand(long Id) : IRequest<ErrorOr<Deleted>>;