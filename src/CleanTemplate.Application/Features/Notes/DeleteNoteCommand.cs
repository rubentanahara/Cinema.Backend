using CleanTemplate.Application.Common.Messaging;

namespace CleanTemplate.Application.Features.Notes;

public sealed record DeleteNoteCommand(long Id) : IRequest<bool>;