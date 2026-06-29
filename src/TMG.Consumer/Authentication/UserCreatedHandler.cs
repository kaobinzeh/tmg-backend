using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Formatting;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Infrastructure.Authentication;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace TMG.Consumer.Authentication;

public sealed class UserCreatedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<AuthenticationLockoutOptions> lockoutOptions,
    ILogger<UserCreatedHandler> logger) : BaseMessageHandler<UserCreated>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserCreated must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserCreated because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        if (user is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserCreated because no user could be found for stakeholder '{message.StakeholderId}'.");
        }

        if (user.EmailConfirmed)
        {
            logger.LogWarning(
                "Skipping sign-up OTP delivery for email {EmailAddress} because the email is already confirmed.",
                stakeholder.EmailAddress);

            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.AlreadyConfirmed);

            return;
        }

        var otpCode = await identityService.GenerateSignUpOtpAsync(user);
        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = now.Add(lockoutOptions.Value.Duration);
        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.ClientId,
                stakeholder.CountryId,
                NotificationType.EmailConfirmationOtp,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["OtpCode"] = otpCode,
                        ["OtpExpiresAtUtc"] = DateTimeFormatter.FormatHumanReadableUtc(expiresAtUtc, now),
                        ["VerifyUrl"] = string.Empty,
                        ["Product"] = "TMG"
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSent,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserCreated message)
    {
        yield break;
    }

}
