using CleanTemplate.Domain.Common;

namespace CleanTemplate.Application.Common.Messaging;

public interface IPublisher
{
    Task Publish(Entity entity, CancellationToken cancellationToken = default);
}