using TMG.Consumer.Authentication;
using TMG.Contracts.Commands.Authentication;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace TMG.Consumer.UnitTests;

public sealed class WhenHandlingResetPasswordWithActiveOtp_Should
{
    [Fact]
    public async Task NotSendNotification()
    {
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clientId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        twoFactorOtpService.OtpExistsAsync(appUserId, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(true);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, clientId, countryId, Guid.CreateVersion7(), "manager", ConsumerTestData.FirstName(), ConsumerTestData.LastName(), null, false));

        await new ResetPasswordHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            twoFactorOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            TimeProvider.System).HandleAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholderId,
                ClientId = clientId
            },
            CancellationToken.None);

        await twoFactorOtpService.DidNotReceive().GenerateOtpAsync(
            Arg.Any<Guid>(),
            Arg.Any<OtpIntent>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<int>(),
            Arg.Any<bool>());
        await commandSender.DidNotReceive().SendAsync(Arg.Any<Contracts.Commands.Notifications.SendNotificationCommand>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

