using TMG.Consumer.Authentication;
using TMG.Contracts.Commands.Authentication;
using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Formatting;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingResetPassword_Should
{
    [Fact]
    public async Task GenerateOtpAndSendNotification()
    {
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var email = ConsumerTestData.Email();
        var otp = new TwoFactorOtp(ConsumerTestData.Otp(), timeProvider.GetUtcNow().AddMinutes(2));

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        twoFactorOtpService.OtpExistsAsync(appUserId, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(false);
        twoFactorOtpService.GenerateOtpAsync(
                appUserId,
                OtpIntent.PasswordReset,
                Arg.Any<CancellationToken>(),
                6,
                false)
            .Returns(otp);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, clientId, countryId, Guid.CreateVersion7(), "manager", firstName, lastName, null, false));

        await new ResetPasswordHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            twoFactorOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            timeProvider).HandleAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholderId,
                ClientId = clientId
            },
            CancellationToken.None);

        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.ClientId == clientId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.ResetPasswordOtp &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["FirstName"] == firstName &&
                ((EmailNotificationContent)command.NotificationContent).Content["LastName"] == lastName &&
                ((EmailNotificationContent)command.NotificationContent).Content["OtpCode"] == otp.Code &&
                ((EmailNotificationContent)command.NotificationContent).Content["OtpExpiresAtUtc"] == DateTimeFormatter.FormatHumanReadableUtc(otp.ExpiresAtUtc, timeProvider.GetUtcNow())),
            Arg.Any<CancellationToken>());
        await twoFactorOtpService.Received(1).GenerateOtpAsync(
            appUserId,
            OtpIntent.PasswordReset,
            Arg.Any<CancellationToken>(),
            6,
            false);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

