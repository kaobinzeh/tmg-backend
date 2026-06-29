using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Notifications;

public sealed class SendNotificationHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IEmailNotificationService emailNotificationService) : BaseMessageHandler<SendNotificationCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SendNotificationCommand message, CancellationToken cancellationToken)
    {
        try
        {
            switch (message.NotificationMedium)
            {
                case NotificationMedium.Email:
                    var sendResult = await emailNotificationService.SendAsync(message, cancellationToken);
                    if (sendResult is not null)
                    {
                        CustomTelemetryContext.AddCustomEvent(
                            Observability.EventNames.Notifications.EmailSent,
                            ObservabilityEventProperties.Create(
                                CurrentActorAccessor,
                                message.StakeholderId,
                                additionalProperties: new Dictionary<string, string>
                                {
                                    [Observability.PropertyNames.Common.MessageId] = message.MessageId.ToString(),
                                    [Observability.PropertyNames.Notifications.ProviderKey] = sendResult.ProviderKey,
                                    [Observability.PropertyNames.Notifications.ProviderMessageId] = sendResult.ProviderMessageId,
                                    [Observability.PropertyNames.Notifications.NotificationType] = message.NotificationType.ToString()
                                }));
                    }
                    break;

                default:
                    throw new FailedToProcessMessageException(
                        $"Notification medium '{message.NotificationMedium}' is not supported.");
            }
        }
        catch (NotificationConfigurationException exception)
        {
            throw new CannotProcessMessageNonTransientException(exception.Message);
        }
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(SendNotificationCommand message)
    {
        yield break;
    }
}
