using TMG.Consumer.Notifications;
using TMG.Domain.Common.Auditing;
using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingUnsupportedNotificationMedium_Should
{
    [Fact]
    public async Task ThrowCannotProcessMessageException()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var emailNotificationService = Substitute.For<IEmailNotificationService>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Sms,
            new SmsNotificationContent(
                "+234",
                ConsumerTestData.PhoneNumber(),
                new Dictionary<string, string>
                {
                    ["Message"] = "A sign-in to your account was successful."
                }));

        var exception = await Should.ThrowAsync<FailedToProcessMessageException>(() =>
            new SendNotificationHandler(customTelemetryContext, currentActorAccessor, messageContext, emailNotificationService)
                .HandleAsync(command, CancellationToken.None));

        exception.Message.ShouldBe("Notification medium 'Sms' is not supported.");
        await emailNotificationService.DidNotReceive().SendAsync(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>());
    }
}

