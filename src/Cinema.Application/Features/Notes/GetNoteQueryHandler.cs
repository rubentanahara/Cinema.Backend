using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes;
using Cinema.Domain.Models.Notes.Errors;

using ErrorOr;

namespace Cinema.Application.Features.Notes;

public sealed class GetNoteQueryHandler(INotesRepository notesRepository) : IRequestHandler<GetNoteQuery, ErrorOr<Note>>
{
    public async Task<ErrorOr<Note>> Handle(GetNoteQuery request, CancellationToken cancellationToken)
    {
        var note = await notesRepository.GetAsync(request.Id, cancellationToken);

        return note is null ? NoteErrors.NotFound : note;
    }
}