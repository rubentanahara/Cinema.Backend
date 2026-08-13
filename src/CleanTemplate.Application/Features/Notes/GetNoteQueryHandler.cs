using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Application.Features.Notes;

public sealed class GetNoteQueryHandler(INotesRepository notesRepository) : IRequestHandler<GetNoteQuery, Note?>
{
    public Task<Note?> Handle(GetNoteQuery request, CancellationToken cancellationToken) =>
        notesRepository.GetAsync(request.Id, cancellationToken);
}