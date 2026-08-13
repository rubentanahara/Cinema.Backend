using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

public sealed record CreateNoteCommand(string Title, string Content) : IRequest<ErrorOr<Note>>;