using Carter;

namespace Cinema.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddCarter();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();

        return services;
    }
}