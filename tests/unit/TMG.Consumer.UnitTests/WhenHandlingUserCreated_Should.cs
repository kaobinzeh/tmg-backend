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
using TMG.Infrastructure.Authentication;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingUserCreated_Should
{
    [Fact]
    public async Task GenerateSignUpOtpAndQueueNotificationCommand()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var lockoutOptions = Options.Create(new AuthenticationLockoutOptions { Duration = TimeSpan.FromHours(12) });
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var otpCode = ConsumerTestData.Otp();
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, email, clientId, countryId, Guid.CreateVersion7(), "manager", firstName, lastName, null, false));
        identityService.FindByIdAsync(user.Id).Returns(user);
        identityService.GenerateSignUpOtpAsync(user).Returns(otpCode);

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            timeProvider,
            lockoutOptions,
            logger).HandleAsync(
            new UserCreated
            {
                StakeholderId = stakeholderId,
                ClientId = clientId
            },
            CancellationToken.None);

        await identityService.Received(1).GenerateSignUpOtpAsync(user);
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
                DateTimeFormatter.FormatHumanReadableUtc(timeProvider.GetUtcNow().Add(lockoutOptions.Value.Duration), timeProvider.GetUtcNow()))),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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





