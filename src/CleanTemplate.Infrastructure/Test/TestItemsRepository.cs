using CleanTemplate.Application.Test;
using CleanTemplate.Infrastructure.Common.Persistence;

using Dapper;

namespace CleanTemplate.Infrastructure.Test;

public class TestItemsRepository(IDbConnectionFactory connectionFactory) : ITestItemsRepository
{
    public Task<string> GetFirstMessageAsync(CancellationToken cancellationToken)
    {
        var connection = connectionFactory.GetConnection();

        return connection.QuerySingleAsync<string>(
            new CommandDefinition("SELECT Message FROM TestItems LIMIT 1", cancellationToken: cancellationToken));
    }
}