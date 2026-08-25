using System.Data;

namespace Cinema.Infrastructure.Common.Persistence;

public class DbConnectionFactory(IDbConnection connection) : IDbConnectionFactory
{
    public IDbConnection GetConnection() => connection;
}