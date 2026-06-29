using TMG.Contracts.Events;

namespace TMG.Domain.Common.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : BaseEvent;

    Task SchedulePublishAsync<TEvent>(TEvent message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken)
        where TEvent : BaseEvent;
}
