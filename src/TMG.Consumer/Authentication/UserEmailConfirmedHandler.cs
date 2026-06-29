using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace TMG.Consumer.Authentication;

public sealed class UserEmailConfirmedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ILogger<UserEmailConfirmedHandler> logger) : BaseMessageHandler<UserEmailConfirmed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override Task HandleAsyncInternal(UserEmailConfirmed message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processed user email confirmed event for stakeholder {StakeholderId}.",
            message.StakeholderId);

        if (message.StakeholderId.HasValue)
        {
            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, message.StakeholderId.Value.ToString());
        }

        return Task.CompletedTask;
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserEmailConfirmed message)
    {
        yield break;
    }
}
