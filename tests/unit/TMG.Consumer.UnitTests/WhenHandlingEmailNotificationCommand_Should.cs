using TMG.Consumer.Notifications;
using TMG.Domain.Common.Auditing;
using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingEmailNotificationCommand_Should
{
    [Fact]
    public async Task SendThroughEmailNotificationService()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var emailNotificationService = Substitute.For<IEmailNotificationService>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        currentActorAccessor.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        currentActorAccessor.FlowId.Returns(Guid.CreateVersion7().ToString("N"));
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                ConsumerTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1",
                    ["UserAgent"] = "Test Agent"
                }));
        emailNotificationService.SendAsync(command, Arg.Any<CancellationToken>())
            .Returns(new EmailNotificationSendResult("mailtrap", "mailtrap-message-id"));

        await new SendNotificationHandler(customTelemetryContext, currentActorAccessor, messageContext, emailNotificationService)
            .HandleAsync(command, CancellationToken.None);

        await emailNotificationService.Received(1).SendAsync(command, Arg.Any<CancellationToken>());
        customTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Notifications.EmailSent,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.MessageId] == command.MessageId.ToString() &&
                properties[Observability.PropertyNames.Notifications.ProviderKey] == "mailtrap" &&
                properties[Observability.PropertyNames.Notifications.ProviderMessageId] == "mailtrap-message-id" &&
                properties[Observability.PropertyNames.Notifications.NotificationType] == NotificationType.SignInSuccessful.ToString()));
    }
}

