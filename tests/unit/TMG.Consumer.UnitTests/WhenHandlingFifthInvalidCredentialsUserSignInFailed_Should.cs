using TMG.Contracts.Commands.Notifications;
using TMG.Consumer.Authentication;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingFifthInvalidCredentialsUserSignInFailed_Should
{
    [Fact]
    public async Task SendAccountLockedNotification()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<UserSignInFailedHandler>>();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 7, 10, 0, 0, TimeSpan.Zero));
        var lockedUntilUtc = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByEmailAsync(email).Returns(user);
        identityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        identityService.IsLockedOutAsync(user).Returns(true);
        identityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, Guid.CreateVersion7(), email, clientId, countryId, Guid.CreateVersion7(), "manager", "Ada", "Lovelace", null, false));

        await new UserSignInFailedHandler(customTelemetryContext, currentActorAccessor, messageContext, identityService, stakeholderReadModelRepository, commandSender, unitOfWork, timeProvider, logger).HandleAsync(
            new UserSignInFailed(
                email,
                ipAddress,
                userAgent,
                UserSignInFailureReasons.InvalidCredentials)
            {
                StakeholderId = stakeholderId,
                ClientId = clientId
            },
            CancellationToken.None);

        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.ClientId == clientId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.AccountLocked &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["LockedUntilUtc"] == DateTimeFormatter.FormatHumanReadableUtc(lockedUntilUtc, timeProvider.GetUtcNow())),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}





