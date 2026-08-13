using Carter;

using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Application.Features.Test;

namespace CleanTemplate.Api.Features.Test;

public class TestModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/test", async (ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(new TestQuery(), cancellationToken));
    }
}