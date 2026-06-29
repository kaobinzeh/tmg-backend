using TMG.Contracts.Payments;
using TMG.Domain.Common.Exceptions;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests.Payments.PaymentWebhookInboxes;

public sealed class When_MarkingWebhookAsProcessed_WithIgnoredStatus_Should
{
    [Fact]
    public void ThrowAggregateStateException()
    {
        var webhook = PaymentWebhookInbox.Create(
            Guid.CreateVersion7(),
            "merchant-ref",
            "provider-ref",
            "transaction.successful",
            "event-id",
            "{}",
            SignatureValidationStatus.Valid,
            null,
            DateTimeOffset.UtcNow);

        webhook.MarkIgnored("ignored", DateTimeOffset.UtcNow);

        Should.Throw<AggregateStateException>(() => webhook.MarkProcessed("processed", DateTimeOffset.UtcNow));
    }
}
