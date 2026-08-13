using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Application.Features.Notes;

public sealed record CreateNoteCommand(string Title, string Content) : IRequest<Note>;