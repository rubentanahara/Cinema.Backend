using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Application.Common.Messaging;

public class Sender(IServiceProvider serviceProvider) : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);

        return (Task<TResponse>)handlerType
            .GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!
            .Invoke(handler, [request, cancellationToken])!;
    }
}