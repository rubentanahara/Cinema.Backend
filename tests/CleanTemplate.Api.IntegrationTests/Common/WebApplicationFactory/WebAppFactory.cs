using CleanTemplate.Infrastructure.Common.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Api.IntegrationTests.Common.WebApplicationFactory;

public class WebAppFactory : WebApplicationFactory<Program>
{
    public void ResetDatabase()
    {
        var connection = Services.GetRequiredService<IDbConnectionFactory>().GetConnection();

        DatabaseInitializer.Initialize(connection);
    }
}