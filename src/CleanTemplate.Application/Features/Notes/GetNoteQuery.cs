using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

public sealed record GetNoteQuery(long Id) : IRequest<ErrorOr<Note>>;