using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;
using CleanTemplate.Domain.Models.Notes.Errors;

using ErrorOr;

namespace CleanTemplate.Application.Features.Notes;

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