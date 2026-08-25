using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes;

using ErrorOr;

namespace Cinema.Application.Features.Notes;

public sealed record CreateNoteCommand(string Title, string Content) : IRequest<ErrorOr<Note>>;