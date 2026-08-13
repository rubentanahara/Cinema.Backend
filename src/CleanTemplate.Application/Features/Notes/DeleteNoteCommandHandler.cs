using CleanTemplate.Application.Common.Messaging;

namespace CleanTemplate.Application.Features.Notes;

public sealed class DeleteNoteCommandHandler(INotesRepository notesRepository) : IRequestHandler<DeleteNoteCommand, bool>
{
    public Task<bool> Handle(DeleteNoteCommand request, CancellationToken cancellationToken) =>
        notesRepository.DeleteAsync(request.Id, cancellationToken);
}