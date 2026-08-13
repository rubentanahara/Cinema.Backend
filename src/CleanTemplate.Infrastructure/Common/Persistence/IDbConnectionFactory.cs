using System.Data;

namespace CleanTemplate.Infrastructure.Common.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection GetConnection();
}