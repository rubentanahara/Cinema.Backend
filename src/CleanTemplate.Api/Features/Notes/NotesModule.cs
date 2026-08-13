using Carter;

using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Application.Features.Notes;
using CleanTemplate.Contracts.Notes;
using CleanTemplate.Domain.Models.Notes;

namespace CleanTemplate.Api.Features.Notes;

public class NotesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/notes");

        notes.MapPost("/", async (CreateNoteRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest("Title is required.");
            }

            var note = await sender.Send(new CreateNoteCommand(request.Title, request.Content), cancellationToken);

            return Results.Created($"/notes/{note.Id}", ToResponse(note));
        });

        notes.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var notes = await sender.Send(new ListNotesQuery(), cancellationToken);

            return notes.Select(ToResponse);
        });

        notes.MapGet("/{id:long}", async (long id, ISender sender, CancellationToken cancellationToken) =>
        {
            var note = await sender.Send(new GetNoteQuery(id), cancellationToken);

            return note is null ? Results.NotFound() : Results.Ok(ToResponse(note));
        });

        notes.MapPut("/{id:long}", async (long id, UpdateNoteRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest("Title is required.");
            }

            var note = await sender.Send(new UpdateNoteCommand(id, request.Title, request.Content), cancellationToken);

            return note is null ? Results.NotFound() : Results.Ok(ToResponse(note));
        });

        notes.MapDelete("/{id:long}", async (long id, ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(new DeleteNoteCommand(id), cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());
    }

    private static NoteResponse ToResponse(Note note) => new(note.Id, note.Title, note.Content);
}