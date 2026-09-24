using TMG.Consumer.Authentication;
using TMG.Contracts.Commands.Authentication;
using TMG.Contracts.Commands.Notifications;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingSendEmailConfirmationOtp_Should
{
    [Fact]
    public async Task ReplaceAnOtpThatIsStillActiveSoTheResendAlwaysDelivers()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<SendEmailConfirmationOtpHandler>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, email, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "manager", firstName, lastName, null, false));
        identityService.FindByIdAsync(user.Id).Returns(user);
        twoFactorOtpService.OtpExistsAsync(user.Id, OtpIntent.EmailConfirmation, Arg.Any<CancellationToken>()).Returns(true);
        twoFactorOtpService.GenerateOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>(),
                Arg.Any<int>(),
                Arg.Any<bool>())
            .Returns(new TwoFactorOtp(ConsumerTestData.Otp(), timeProvider.GetUtcNow().AddMinutes(3)));

        await new SendEmailConfirmationOtpHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            new EmailConfirmationOtpSender(identityService, twoFactorOtpService, commandSender, unitOfWork, timeProvider),
            logger).HandleAsync(
            new SendEmailConfirmationOtpCommand { StakeholderId = stakeholderId, ClientId = Guid.CreateVersion7() },
            CancellationToken.None);

        await twoFactorOtpService.Received(1).GenerateOtpAsync(
            user.Id,
            OtpIntent.EmailConfirmation,
            Arg.Any<CancellationToken>(),
            characterLength: 6,
            isAlphaNumeric: false);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command => command.NotificationType == NotificationType.EmailConfirmationOtp),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
