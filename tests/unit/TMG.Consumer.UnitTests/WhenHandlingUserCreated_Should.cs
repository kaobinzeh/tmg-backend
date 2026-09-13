using TMG.Consumer.Authentication;
using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Formatting;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingUserCreated_Should
{
    [Fact]
    public async Task GenerateSignUpOtpAndQueueNotificationCommand()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var otpCode = ConsumerTestData.Otp();
        var expiresAtUtc = timeProvider.GetUtcNow().AddMinutes(3);
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, email, clientId, countryId, Guid.CreateVersion7(), "manager", firstName, lastName, null, false));
        identityService.FindByIdAsync(user.Id).Returns(user);
        twoFactorOtpService.OtpExistsAsync(user.Id, OtpIntent.EmailConfirmation, Arg.Any<CancellationToken>()).Returns(false);
        twoFactorOtpService.GenerateOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>(),
                Arg.Any<int>(),
                Arg.Any<bool>())
            .Returns(new TwoFactorOtp(otpCode, expiresAtUtc));

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            new EmailConfirmationOtpSender(identityService, twoFactorOtpService, commandSender, unitOfWork, timeProvider),
            logger).HandleAsync(
            new UserCreated
            {
                StakeholderId = stakeholderId,
                ClientId = clientId
            },
            CancellationToken.None);

        await twoFactorOtpService.Received(1).GenerateOtpAsync(
            user.Id,
            OtpIntent.EmailConfirmation,
            Arg.Any<CancellationToken>(),
            characterLength: 6,
            isAlphaNumeric: false);
        await identityService.Received(1).FindByIdAsync(user.Id);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command => HasExpectedNotificationCommand(
                command,
                clientId,
                countryId,
                stakeholderId,
                email,
                firstName,
                lastName,
                otpCode,
                DateTimeFormatter.FormatHumanReadableUtc(expiresAtUtc, timeProvider.GetUtcNow()))),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotReplaceAnOtpThatIsStillActiveWhenTheEventIsRedelivered()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
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

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            new EmailConfirmationOtpSender(identityService, twoFactorOtpService, commandSender, unitOfWork, timeProvider),
            logger).HandleAsync(
            new UserCreated { StakeholderId = stakeholderId, ClientId = Guid.CreateVersion7() },
            CancellationToken.None);

        await twoFactorOtpService.DidNotReceive().GenerateOtpAsync(
            Arg.Any<Guid>(),
            Arg.Any<OtpIntent>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<int>(),
            Arg.Any<bool>());
        await commandSender.DidNotReceive().SendAsync(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipDeliveryWhenTheEmailIsAlreadyConfirmed()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName);
        user.MarkEmailVerified();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, email, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "manager", firstName, lastName, null, true));
        identityService.FindByIdAsync(user.Id).Returns(user);

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            stakeholderReadModelRepository,
            new EmailConfirmationOtpSender(identityService, twoFactorOtpService, commandSender, unitOfWork, timeProvider),
            logger).HandleAsync(
            new UserCreated { StakeholderId = stakeholderId, ClientId = Guid.CreateVersion7() },
            CancellationToken.None);

        user.EmailConfirmed.ShouldBeTrue();
        await commandSender.DidNotReceive().SendAsync(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    private static bool HasExpectedNotificationCommand(
        SendNotificationCommand command,
        Guid clientId,
        Guid countryId,
        Guid stakeholderId,
        string email,
        string firstName,
        string lastName,
        string otpCode,
        string otpExpiresAtUtc)
    {
        if (command.NotificationContent is not EmailNotificationContent content)
        {
            return false;
        }

        return command.ClientId == clientId &&
            command.CountryId == countryId &&
            command.NotificationType == NotificationType.EmailConfirmationOtp &&
            command.NotificationMedium == NotificationMedium.Email &&
            command.StakeholderId == stakeholderId &&
            content.To == email &&
            content.Content["FirstName"] == firstName &&
            content.Content["LastName"] == lastName &&
            content.Content["OtpCode"] == otpCode &&
            content.Content["OtpExpiresAtUtc"] == otpExpiresAtUtc &&
            content.Content["Product"] == "TMG";
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
