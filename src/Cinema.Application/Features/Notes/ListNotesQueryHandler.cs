using Cinema.Application.Common.Messaging;
using Cinema.Domain.Models.Notes;

namespace Cinema.Application.Features.Notes;

public sealed class ListNotesQueryHandler(INotesRepository notesRepository) : IRequestHandler<ListNotesQuery, IReadOnlyList<Note>>
{
    public Task<IReadOnlyList<Note>> Handle(ListNotesQuery request, CancellationToken cancellationToken) =>
        notesRepository.ListAsync(cancellationToken);
}