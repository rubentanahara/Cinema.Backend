using Cinema.Application.Features.Test;
using Cinema.Infrastructure.Common.Persistence;

using Dapper;

namespace Cinema.Infrastructure.Dapper.Test;

public class TestItemsRepository(IDbConnectionFactory connectionFactory) : ITestItemsRepository
{
    public Task<string> GetFirstMessageAsync(CancellationToken cancellationToken)
    {
        var connection = connectionFactory.GetConnection();

        return connection.QuerySingleAsync<string>(
            new CommandDefinition("SELECT Message FROM TestItems LIMIT 1", cancellationToken: cancellationToken));
    }
}