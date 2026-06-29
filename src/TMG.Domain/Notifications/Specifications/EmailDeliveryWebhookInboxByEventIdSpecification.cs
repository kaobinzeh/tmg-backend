using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class EmailDeliveryWebhookInboxByEventIdSpecification : Specification<EmailDeliveryWebhookInbox>
{
    public EmailDeliveryWebhookInboxByEventIdSpecification(Guid providerId, string webhookEventId)
    {
        Where(inbox => inbox.ProviderId == providerId && inbox.WebhookEventId == webhookEventId);
    }
}
