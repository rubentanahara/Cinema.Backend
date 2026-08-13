using CleanTemplate.Application.Common.Messaging;

namespace CleanTemplate.Application.Test;

public sealed class TestQueryHandler(ITestItemsRepository testItemsRepository) : IRequestHandler<TestQuery, string>
{
    public Task<string> Handle(TestQuery request, CancellationToken cancellationToken) =>
        testItemsRepository.GetFirstMessageAsync(cancellationToken);
}