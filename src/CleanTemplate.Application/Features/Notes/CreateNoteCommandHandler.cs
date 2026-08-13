using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Application.Features.Notes;

public sealed class CreateNoteCommandHandler(INotesRepository notesRepository) : IRequestHandler<CreateNoteCommand, Note>
{
    public Task<Note> Handle(CreateNoteCommand request, CancellationToken cancellationToken) =>
        notesRepository.AddAsync(request.Title, request.Content, cancellationToken);
}