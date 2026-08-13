using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;
using CleanTemplate.Domain.Models.Notes.Errors;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

public sealed class GetNoteQueryHandler(INotesRepository notesRepository) : IRequestHandler<GetNoteQuery, ErrorOr<Note>>
{
    public async Task<ErrorOr<Note>> Handle(GetNoteQuery request, CancellationToken cancellationToken)
    {
        var note = await notesRepository.GetAsync(request.Id, cancellationToken);

        return note is null ? NoteErrors.NotFound : note;
    }
}