using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;
using CleanTemplate.Domain.Models.Notes.Errors;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

public sealed class UpdateNoteCommandHandler(INotesRepository notesRepository, IPublisher publisher)
    : IRequestHandler<UpdateNoteCommand, ErrorOr<Note>>
{
    public async Task<ErrorOr<Note>> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return NoteErrors.EmptyTitle;
        }

        var note = await notesRepository.UpdateAsync(request.Id, request.Title, request.Content, cancellationToken);

        if (note is null)
        {
            return NoteErrors.NotFound;
        }

        await publisher.Publish(note, cancellationToken);

        return note;
    }
}