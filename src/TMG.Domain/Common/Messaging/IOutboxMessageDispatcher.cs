namespace TMG.Domain.Common.Messaging;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken);
}
