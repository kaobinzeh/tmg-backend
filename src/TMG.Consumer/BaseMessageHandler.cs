using System.Diagnostics;
using TMG.Contracts.Common;
using TMG.Contracts.Commands;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer;

public abstract class BaseMessageHandler<TMessage>(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext) : IMessageHandler<TMessage>
{
    private static readonly ActivitySource ActivitySource = new(Observability.ActivitySourceName);

    protected ICustomTelemetryContext CustomTelemetryContext { get; } = customTelemetryContext;

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"{typeof(TMessage).Name}_process", ActivityKind.Consumer);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.MessageType, typeof(TMessage).Name);

        if (message is BaseEvent baseEvent)
        {
            if (baseEvent.ClientId == Guid.Empty)
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a valid client id.");
            }

            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            currentActorAccessor.Set(
                baseEvent.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                baseEvent.ClientId,
                messageContext.CorrelationId,
                baseEvent.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.PropertyNames.Common.MessageId, baseEvent.MessageId.ToString())
                .SetProperty("OccurredAt", baseEvent.OccuredAt.ToString("O"))
                .SetProperty(Observability.PropertyNames.Common.StakeholderId, baseEvent.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.ClientId, baseEvent.ClientId.ToString())
                .SetProperty(Observability.PropertyNames.Common.CorrelationId, messageContext.CorrelationId)
                .SetProperty(Observability.PropertyNames.Common.FlowId, baseEvent.FlowId ?? string.Empty);
        }
        else if (message is BaseCommand baseCommand)
        {
            if (baseCommand.ClientId == Guid.Empty)
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a valid client id.");
            }

            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            currentActorAccessor.Set(
                baseCommand.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                baseCommand.ClientId,
                messageContext.CorrelationId,
                baseCommand.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.PropertyNames.Common.MessageId, baseCommand.MessageId.ToString())
                .SetProperty("RequestedAt", baseCommand.RequestedAt.ToString("O"))
                .SetProperty(Observability.PropertyNames.Common.StakeholderId, baseCommand.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.ClientId, baseCommand.ClientId.ToString())
                .SetProperty(Observability.PropertyNames.Common.CorrelationId, messageContext.CorrelationId)
                .SetProperty(Observability.PropertyNames.Common.FlowId, baseCommand.FlowId ?? string.Empty);
        }
        else
        {
            throw new CannotProcessMessageNonTransientException(
                $"{typeof(TMessage).Name} must inherit from {nameof(BaseCommand)} or {nameof(BaseEvent)}.");
        }

        foreach (var telemetryParameter in GetTelemetryParameters(message))
        {
            CustomTelemetryContext.SetProperty(telemetryParameter.Key, telemetryParameter.Value);
        }

        await HandleAsyncInternal(message, cancellationToken);
    }

    protected virtual IEnumerable<(string Key, string Value)> GetTelemetryParameters(TMessage message) => [];

    protected abstract Task HandleAsyncInternal(TMessage message, CancellationToken cancellationToken);
}
