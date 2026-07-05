using TMG.Consumer.Tenancies;
using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace TMG.Consumer.UnitTests.Tenancies;

public sealed class When_HandlingTenantInvited_WithPendingTenant_Should
{
    [Fact]
    public async Task GenerateInvitationTokenAndQueueInvitationEmail()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 30, 10, 0, 0, TimeSpan.Zero));

        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var invitation = new TwoFactorOtp("INVITETOKEN123", timeProvider.GetUtcNow().AddDays(7));

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, clientId, countryId, Guid.CreateVersion7(), "tenant", firstName, lastName, null, false));
        twoFactorOtpService.OtpExistsAsync(appUserId, OtpIntent.TenancyInvitation, Arg.Any<CancellationToken>()).Returns(false);
        twoFactorOtpService.GenerateOtpAsync(appUserId, OtpIntent.TenancyInvitation, Arg.Any<CancellationToken>(), Arg.Any<int>(), Arg.Any<bool>())
            .Returns(invitation);

        await new TenantInvitedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            twoFactorOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            timeProvider).HandleAsync(
            new TenantInvited
            {
                StakeholderId = stakeholderId,
                ClientId = clientId,
                UnitLabel = "Flat 10",
                PropertyName = "Lekki Court"
            },
            CancellationToken.None);

        await twoFactorOtpService.Received(1).GenerateOtpAsync(appUserId, OtpIntent.TenancyInvitation, Arg.Any<CancellationToken>(), Arg.Any<int>(), Arg.Any<bool>());
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command => HasExpectedInvitationCommand(command, clientId, countryId, stakeholderId, email, firstName)),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static bool HasExpectedInvitationCommand(
        SendNotificationCommand command,
        Guid clientId,
        Guid countryId,
        Guid stakeholderId,
        string email,
        string firstName)
    {
        if (command.NotificationContent is not EmailNotificationContent content)
        {
            return false;
        }

        return command.ClientId == clientId &&
            command.CountryId == countryId &&
            command.NotificationType == NotificationType.UnitAllocationInvitation &&
            command.NotificationMedium == NotificationMedium.Email &&
            command.StakeholderId == stakeholderId &&
            content.To == email &&
            content.Content["FirstName"] == firstName &&
            content.Content["PropertyName"] == "Lekki Court" &&
            content.Content["UnitLabel"] == "Flat 10" &&
            content.Content["InvitationToken"] == "INVITETOKEN123";
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
