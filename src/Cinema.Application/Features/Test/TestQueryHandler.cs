using Cinema.Application.Common.Messaging;

namespace Cinema.Application.Features.Test;

public sealed class TestQueryHandler(ITestItemsRepository testItemsRepository) : IRequestHandler<TestQuery, string>
{
    public Task<string> Handle(TestQuery request, CancellationToken cancellationToken) =>
        testItemsRepository.GetFirstMessageAsync(cancellationToken);
}