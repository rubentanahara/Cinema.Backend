using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Application.Features.Notes;

public sealed class ListNotesQueryHandler(INotesRepository notesRepository) : IRequestHandler<ListNotesQuery, IReadOnlyList<Note>>
{
    public Task<IReadOnlyList<Note>> Handle(ListNotesQuery request, CancellationToken cancellationToken) =>
        notesRepository.ListAsync(cancellationToken);
}