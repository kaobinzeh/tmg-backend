using TMG.Consumer.Notifications;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Notifications.Specifications;
using TMG.Domain.Notifications.Entities;
using TMG.Domain.Providers.Entities;
using Microsoft.Extensions.Logging;

namespace TMG.Consumer.UnitTests.Notifications;

internal sealed class NotificationsConsumerTestContext
{
    public IReadRepository<Provider> ProviderRepository { get; } = Substitute.For<IReadRepository<Provider>>();
    public IRepository<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxRepository { get; } = Substitute.For<IRepository<EmailDeliveryWebhookInbox>>();
    public IRepository<EmailNotificationLog> EmailNotificationLogRepository { get; } = Substitute.For<IRepository<EmailNotificationLog>>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 3, 13, 0, 0, TimeSpan.Zero));

    public EmailDeliveryWebhookReceivedHandler CreateEmailDeliveryWebhookReceivedHandler() =>
        new(ProviderRepository, EmailDeliveryWebhookInboxRepository, EmailNotificationLogRepository, CurrentActor, CustomTelemetryContext, UnitOfWork, Clock);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
