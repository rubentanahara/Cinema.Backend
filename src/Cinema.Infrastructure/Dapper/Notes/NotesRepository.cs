using Cinema.Application.Features.Notes;
using Cinema.Domain.Models.Notes;
using Cinema.Infrastructure.Common.Persistence;

using Dapper;

namespace Cinema.Infrastructure.Dapper.Notes;

public class NotesRepository(IDbConnectionFactory connectionFactory) : INotesRepository
{
    public async Task<Note> AddAsync(string title, string content, CancellationToken cancellationToken)
    {
        var row = await connectionFactory.GetConnection().QuerySingleAsync<NoteRow>(
            new CommandDefinition(
                "INSERT INTO Notes (Title, Content) VALUES (@Title, @Content) RETURNING Id, Title, Content",
                new { Title = title, Content = content },
                cancellationToken: cancellationToken));

        return Note.Created(row.Id, row.Title, row.Content);
    }

    public async Task<Note?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var row = await connectionFactory.GetConnection().QuerySingleOrDefaultAsync<NoteRow>(
            new CommandDefinition(
                "SELECT Id, Title, Content FROM Notes WHERE Id = @Id",
                new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Note.Rehydrate(row.Id, row.Title, row.Content);
    }

    public async Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await connectionFactory.GetConnection().QueryAsync<NoteRow>(
            new CommandDefinition(
                "SELECT Id, Title, Content FROM Notes ORDER BY Id",
                cancellationToken: cancellationToken));

        return rows.Select(row => Note.Rehydrate(row.Id, row.Title, row.Content)).ToList();
    }

    public async Task<Note?> UpdateAsync(long id, string title, string content, CancellationToken cancellationToken)
    {
        var row = await connectionFactory.GetConnection().QuerySingleOrDefaultAsync<NoteRow>(
            new CommandDefinition(
                "UPDATE Notes SET Title = @Title, Content = @Content WHERE Id = @Id RETURNING Id, Title, Content",
                new { Id = id, Title = title, Content = content },
                cancellationToken: cancellationToken));

        return row is null ? null : Note.Updated(row.Id, row.Title, row.Content);
    }

    public async Task<Note?> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var row = await connectionFactory.GetConnection().QuerySingleOrDefaultAsync<NoteRow>(
            new CommandDefinition(
                "DELETE FROM Notes WHERE Id = @Id RETURNING Id, Title, Content",
                new { Id = id },
                cancellationToken: cancellationToken));

        return row is null ? null : Note.Deleted(row.Id, row.Title, row.Content);
    }
}