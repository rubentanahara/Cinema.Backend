using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes;
using Cinema.Domain.Models.Notes.Errors;

using ErrorOr;

namespace Cinema.Application.Features.Notes;

public sealed class CreateNoteCommandHandler(INotesRepository notesRepository, IPublisher publisher)
    : IRequestHandler<CreateNoteCommand, ErrorOr<Note>>
{
    public async Task<ErrorOr<Note>> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return NoteErrors.EmptyTitle;
        }

        var note = await notesRepository.AddAsync(request.Title, request.Content, cancellationToken);

        await publisher.Publish(note, cancellationToken);

        return note;
    }
}