using Carter;

using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Application.Test;

namespace CleanTemplate.Api;

public class TestModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/test", async (ISender sender, CancellationToken cancellationToken) =>
            await sender.Send(new TestQuery(), cancellationToken));
    }
}