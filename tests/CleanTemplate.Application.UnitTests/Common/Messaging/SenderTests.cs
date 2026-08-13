using CleanTemplate.Application.Common.Messaging;
using CleanTemplate.Application.Features.Test;

using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Application.UnitTests.Common.Messaging;

public class SenderTests
{
    [Fact]
    public async Task Send_DispatchesToTheRegisteredHandler()
    {
        var handler = Substitute.For<IRequestHandler<TestQuery, string>>();
        handler.Handle(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>()).Returns("test");

        var services = new ServiceCollection();
        services.AddSingleton(handler);
        var sender = new Sender(services.BuildServiceProvider());

        var result = await sender.Send(new TestQuery());

        result.ShouldBe("test");
    }
}