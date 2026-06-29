using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class PaymentWebhookInboxByEventIdSpecification : Specification<PaymentWebhookInbox>
{
    public PaymentWebhookInboxByEventIdSpecification(Guid paymentProviderId, string webhookEventId)
    {
        Where(inbox =>
            inbox.PaymentProviderId == paymentProviderId &&
            inbox.WebhookEventId == webhookEventId);
    }
}
