using ErrorOr;

namespace Cinema.Domain.Models.Notes.Errors;

public static class NoteErrors
{
    public static Error NotFound => Error.NotFound("Note.NotFound", "Note not found.");

    public static Error EmptyTitle => Error.Validation("Note.EmptyTitle", "Title is required.");
}