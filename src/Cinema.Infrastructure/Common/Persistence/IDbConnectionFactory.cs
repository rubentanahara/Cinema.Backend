using System.Data;

namespace Cinema.Infrastructure.Common.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection GetConnection();
}