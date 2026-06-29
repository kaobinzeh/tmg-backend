using TMG.Contracts.Events;
using TMG.Contracts.Payments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;
using TMG.Domain.Notifications.Specifications;
using TMG.Domain.Providers.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Notifications;

public sealed class EmailDeliveryWebhookReceivedHandler(
    IReadRepository<Provider> providerRepository,
    IRepository<EmailDeliveryWebhookInbox> emailDeliveryWebhookInboxRepository,
    IRepository<EmailNotificationLog> emailNotificationLogRepository,
    ICurrentActor currentActor,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IMessageHandler<EmailDeliveryWebhookReceived>
{
    public async Task HandleAsync(EmailDeliveryWebhookReceived message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ProviderMessageId))
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid provider message id.");
        }

        if (message.ProviderId == Guid.Empty)
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid provider id.");
        }

        if (string.IsNullOrWhiteSpace(message.EventId))
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid event id.");
        }

        var inbox = await emailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
            new EmailDeliveryWebhookInboxByEventIdSpecification(message.ProviderId, message.EventId),
            cancellationToken);
        if (inbox is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no webhook inbox was found for event '{message.EventId}'.");
        }

        if (inbox.WebhookProcessingStatus == WebhookProcessingStatus.Processed)
        {
            return;
        }

        var emailNotificationLog = await emailNotificationLogRepository.FirstOrDefaultAsync(
            new EmailNotificationLogByProviderMessageIdSpecification(message.ProviderMessageId),
            cancellationToken);
        if (emailNotificationLog is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no notification log was found for provider message '{message.ProviderMessageId}'.");
        }

        var now = timeProvider.GetUtcNow();
        var wasAlreadyDelivered = emailNotificationLog.DeliveredAtUtc.HasValue;
        emailNotificationLog.MarkDelivered(inbox.OccurredAtUtc);
        emailNotificationLogRepository.Update(emailNotificationLog);
        inbox.MarkProcessed(KnownWebhookStatusChangeReasons.Notifications.NotificationLogDelivered, now);
        emailDeliveryWebhookInboxRepository.Update(inbox);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasAlreadyDelivered)
        {
            return;
        }

        var provider = await providerRepository.FirstOrDefaultAsync(
            new ProviderByIdSpecification(message.ProviderId),
            cancellationToken);
        if (provider is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no provider was found for id '{message.ProviderId}'.");
        }

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Notifications.EmailDelivered,
            ObservabilityEventProperties.Create(
                currentActor,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.MessageId] = emailNotificationLog.MessageId.ToString(),
                    [Observability.PropertyNames.Notifications.ProviderKey] = provider.ProviderKey,
                    [Observability.PropertyNames.Notifications.ProviderMessageId] = emailNotificationLog.ProviderMessageId ?? message.ProviderMessageId,
                    [Observability.PropertyNames.Notifications.NotificationType] = emailNotificationLog.NotificationType.ToString()
                }));
    }
}

