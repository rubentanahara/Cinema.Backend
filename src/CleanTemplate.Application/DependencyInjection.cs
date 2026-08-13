using System.Reflection;

using CleanTemplate.Application.Common.Messaging;

using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddScoped<IPublisher, Publisher>();
        services.AddHandlers();

        return services;
    }

    private static void AddHandlers(this IServiceCollection services)
    {
        Type[] handlerInterfaceTypes = [typeof(IRequestHandler<,>), typeof(IDomainEventHandler<>)];

        var handlerRegistrations = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && handlerInterfaceTypes.Contains(i.GetGenericTypeDefinition()))
                .Select(i => (Interface: i, Implementation: type)));

        foreach (var (@interface, implementation) in handlerRegistrations)
        {
            services.AddScoped(@interface, implementation);
        }
    }
}