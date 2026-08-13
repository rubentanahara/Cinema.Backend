using System.Data;

namespace CleanTemplate.Infrastructure.Common.Persistence;

public class DbConnectionFactory(IDbConnection connection) : IDbConnectionFactory
{
    public IDbConnection GetConnection() => connection;
}