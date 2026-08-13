using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Application.Features.Notes;

public sealed class UpdateNoteCommandHandler(INotesRepository notesRepository) : IRequestHandler<UpdateNoteCommand, Note?>
{
    public Task<Note?> Handle(UpdateNoteCommand request, CancellationToken cancellationToken) =>
        notesRepository.UpdateAsync(new Note(request.Id, request.Title, request.Content), cancellationToken);
}