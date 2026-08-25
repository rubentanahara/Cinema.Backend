using Carter;

using Cinema.Application.Common.Messaging;
using Cinema.Application.Features.Test;

namespace Cinema.Api.Features.Test;

public class TestModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/test", async (ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(new TestQuery(), cancellationToken));
    }
}