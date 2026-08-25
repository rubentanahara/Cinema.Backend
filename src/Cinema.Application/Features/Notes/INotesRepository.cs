using Cinema.Domain.Models.Notes;

namespace Cinema.Application.Features.Notes;

public interface INotesRepository
{
    Task<Note> AddAsync(string title, string content, CancellationToken cancellationToken);

    Task<Note?> GetAsync(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken);

    Task<Note?> UpdateAsync(long id, string title, string content, CancellationToken cancellationToken);

    Task<Note?> DeleteAsync(long id, CancellationToken cancellationToken);
}