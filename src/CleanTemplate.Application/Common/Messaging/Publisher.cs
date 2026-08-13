using CleanTemplate.Domain.Common;

using Microsoft.Extensions.DependencyInjection;

namespace CleanTemplate.Application.Common.Messaging;

public class Publisher(IServiceProvider serviceProvider) : IPublisher
{
    public async Task Publish(Entity entity, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in entity.DomainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle))!;

            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }

        entity.ClearDomainEvents();
    }
}