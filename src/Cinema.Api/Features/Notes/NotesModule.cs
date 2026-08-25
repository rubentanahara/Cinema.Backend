using Carter;

using Cinema.Application.Common.Messaging;
using Cinema.Application.Features.Notes;
using Cinema.Contracts.Notes;
using Cinema.Domain.Models.Notes;

using ErrorOr;

namespace Cinema.Api.Features.Notes;

public class NotesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/notes");

        notes.MapPost("/", async (CreateNoteRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateNoteCommand(request.Title, request.Content), cancellationToken);

            return result.MatchFirst(
                note => Results.Created($"/notes/{note.Id}", ToResponse(note)),
                Problem);
        });

        notes.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var notes = await sender.Send(new ListNotesQuery(), cancellationToken);

            return notes.Select(ToResponse);
        });

        notes.MapGet("/{id:long}", async (long id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetNoteQuery(id), cancellationToken);

            return result.MatchFirst(note => Results.Ok(ToResponse(note)), Problem);
        });

        notes.MapPut("/{id:long}", async (long id, UpdateNoteRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new UpdateNoteCommand(id, request.Title, request.Content), cancellationToken);

            return result.MatchFirst(note => Results.Ok(ToResponse(note)), Problem);
        });

        notes.MapDelete("/{id:long}", async (long id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteNoteCommand(id), cancellationToken);

            return result.MatchFirst(_ => Results.NoContent(), Problem);
        });
    }

    private static NoteResponse ToResponse(Note note) => new(note.Id, note.Title, note.Content);

    private static IResult Problem(Error error) => Results.Problem(
        statusCode: error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        },
        detail: error.Description);
}