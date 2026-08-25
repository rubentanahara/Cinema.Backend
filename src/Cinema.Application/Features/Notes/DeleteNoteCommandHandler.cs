using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes.Errors;

using ErrorOr;

namespace Cinema.Application.Features.Notes;

public sealed class DeleteNoteCommandHandler(INotesRepository notesRepository, IPublisher publisher)
    : IRequestHandler<DeleteNoteCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await notesRepository.DeleteAsync(request.Id, cancellationToken);

        if (note is null)
        {
            return NoteErrors.NotFound;
        }

        await publisher.Publish(note, cancellationToken);

        return Result.Deleted;
    }
}