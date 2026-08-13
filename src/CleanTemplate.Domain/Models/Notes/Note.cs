using CleanTemplate.Domain.Common;
using CleanTemplate.Domain.Models.Notes.Events;

namespace CleanTemplate.Domain.Models.Notes;

public sealed class Note : Entity
{
    private Note(long id, string title, string content)
    {
        Id = id;
        Title = title;
        Content = content;
    }

    public long Id { get; }

    public string Title { get; }

    public string Content { get; }

    public static Note Created(long id, string title, string content)
    {
        var note = new Note(id, title, content);
        note.Raise(new NoteCreated(id, title));

        return note;
    }

    public static Note Updated(long id, string title, string content)
    {
        var note = new Note(id, title, content);
        note.Raise(new NoteUpdated(id, title));

        return note;
    }

    public static Note Deleted(long id, string title, string content)
    {
        var note = new Note(id, title, content);
        note.Raise(new NoteDeleted(id));

        return note;
    }

    public static Note Rehydrate(long id, string title, string content) => new(id, title, content);
}