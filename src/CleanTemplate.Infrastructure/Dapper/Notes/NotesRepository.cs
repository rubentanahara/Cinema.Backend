using CleanTemplate.Application.Features.Notes;
using CleanTemplate.Domain.Models.Notes;
using CleanTemplate.Infrastructure.Common.Persistence;

using Dapper;

namespace CleanTemplate.Infrastructure.Dapper.Notes;

public class NotesRepository(IDbConnectionFactory connectionFactory) : INotesRepository
{
    public Task<Note> AddAsync(string title, string content, CancellationToken cancellationToken) =>
        connectionFactory.GetConnection().QuerySingleAsync<Note>(
            new CommandDefinition(
                "INSERT INTO Notes (Title, Content) VALUES (@Title, @Content) RETURNING Id, Title, Content",
                new { Title = title, Content = content },
                cancellationToken: cancellationToken));

    public Task<Note?> GetAsync(long id, CancellationToken cancellationToken) =>
        connectionFactory.GetConnection().QuerySingleOrDefaultAsync<Note>(
            new CommandDefinition(
                "SELECT Id, Title, Content FROM Notes WHERE Id = @Id",
                new { Id = id },
                cancellationToken: cancellationToken));

    public async Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken)
    {
        var notes = await connectionFactory.GetConnection().QueryAsync<Note>(
            new CommandDefinition(
                "SELECT Id, Title, Content FROM Notes ORDER BY Id",
                cancellationToken: cancellationToken));

        return notes.ToList();
    }

    public Task<Note?> UpdateAsync(Note note, CancellationToken cancellationToken) =>
        connectionFactory.GetConnection().QuerySingleOrDefaultAsync<Note>(
            new CommandDefinition(
                "UPDATE Notes SET Title = @Title, Content = @Content WHERE Id = @Id RETURNING Id, Title, Content",
                note,
                cancellationToken: cancellationToken));

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var deletedRows = await connectionFactory.GetConnection().ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM Notes WHERE Id = @Id",
                new { Id = id },
                cancellationToken: cancellationToken));

        return deletedRows > 0;
    }
}