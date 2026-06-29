using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class EmailDeliveryWebhookInboxesByEventIdsSpecification : Specification<EmailDeliveryWebhookInbox>
{
    public EmailDeliveryWebhookInboxesByEventIdsSpecification(Guid providerId, IReadOnlyCollection<string> webhookEventIds)
    {
        Where(inbox => inbox.ProviderId == providerId && webhookEventIds.Contains(inbox.WebhookEventId));
    }
}
