using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Application.Features.Notes;

public interface INotesRepository
{
    Task<Note> AddAsync(string title, string content, CancellationToken cancellationToken);

    Task<Note?> GetAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken);

    Task<Note?> UpdateAsync(Note note, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
}