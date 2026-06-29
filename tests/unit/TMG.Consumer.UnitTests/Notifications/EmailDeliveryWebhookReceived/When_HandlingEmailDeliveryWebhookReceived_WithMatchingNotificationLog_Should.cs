using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Observability;
using TMG.Domain.Notifications.Entities;
using TMG.Domain.Notifications.Specifications;
using TMG.Domain.Providers.Entities;
using NSubstitute;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = TMG.Contracts.Events.EmailDeliveryWebhookReceived;

namespace TMG.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithMatchingNotificationLog_Should
{
    [Fact]
    public async Task MarkNotificationLogDelivered()
    {
        var context = new NotificationsConsumerTestContext();
        var providerId = Guid.CreateVersion7();
        var log = EmailNotificationLog.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            context.Clock.GetUtcNow());
        var deliveredAtUtc = DateTimeOffset.Parse("2026-05-03T13:05:00+00:00");
        var provider = Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true);
        var inbox = EmailDeliveryWebhookInbox.Create(
            providerId,
            "evt_123",
            "mailtrap-message-id",
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            deliveredAtUtc,
            context.Clock.GetUtcNow());

        log.MarkSent("mailtrap-message-id", context.Clock.GetUtcNow());
        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<EmailDeliveryWebhookInboxByEventIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(inbox);
        context.EmailNotificationLogRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByProviderMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(log);
        context.ProviderRepository.FirstOrDefaultAsync(
                Arg.Any<ProviderByIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(provider);

        await context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = "mailtrap-message-id",
                EventId = "evt_123"
            },
            CancellationToken.None);

        log.DeliveredAtUtc.ShouldBe(deliveredAtUtc);
        inbox.WebhookProcessingStatus.ShouldBe(TMG.Contracts.Payments.WebhookProcessingStatus.Processed);
        context.EmailNotificationLogRepository.Received(1).Update(log);
        context.EmailDeliveryWebhookInboxRepository.Received(1).Update(inbox);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        context.CustomTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Notifications.EmailDelivered,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.MessageId] == log.MessageId.ToString() &&
                properties[Observability.PropertyNames.Notifications.ProviderKey] == "mailtrap" &&
                properties[Observability.PropertyNames.Notifications.ProviderMessageId] == "mailtrap-message-id" &&
                properties[Observability.PropertyNames.Notifications.NotificationType] == NotificationType.SignInSuccessful.ToString()));
    }
}


