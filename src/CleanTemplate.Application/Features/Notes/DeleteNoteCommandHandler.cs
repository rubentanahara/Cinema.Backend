using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes.Errors;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

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