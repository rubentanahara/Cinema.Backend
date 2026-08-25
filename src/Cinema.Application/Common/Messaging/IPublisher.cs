using Cinema.Domain.Common;

namespace Cinema.Application.Common.Messaging;

public interface IPublisher
{
    Task Publish(Entity entity, CancellationToken cancellationToken = default);
}