using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Services;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Authentication;

public sealed class UserSignInSuccessfulHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    ILoginActivityIpAddressResolver loginActivityIpAddressResolver,
    IRepository<LoginActivity> loginActivityRepository,
    IUnitOfWork unitOfWork,
    IUserAgentParserService userAgentParserService,
    TimeProvider timeProvider) : BaseMessageHandler<UserSignInSuccessful>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserSignInSuccessful message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserSignInSuccessful must contain a valid stakeholder actor id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserSignInSuccessful because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        if (user is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserSignInSuccessful because user '{stakeholder.AppUserId}' could not be found.");
        }

        var resetResult = await identityService.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to reset access-failed count for user {user.Id}.");
        }

        var userAgentInfo = userAgentParserService.Parse(message.UserAgent);
        var ipAddressResolution = await loginActivityIpAddressResolver.ResolveAsync(message.IpAddress, cancellationToken);

        var loginActivity = LoginActivity.CreateInitialLogin(
            stakeholder.StakeholderId,
            stakeholder.ClientId,
            ipAddressResolution.IpAddressId,
            ipAddressResolution.IpAddressLocationId,
            message.UserAgent,
            userAgentInfo.DeviceName,
            userAgentInfo.DevicePlatform,
            userAgentInfo.BrowserName,
            timeProvider.GetUtcNow());

        await loginActivityRepository.AddAsync(loginActivity, cancellationToken);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.ClientId,
                stakeholder.CountryId,
                NotificationType.SignInSuccessful,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["IpAddress"] = message.IpAddress,
                        ["UserAgent"] = message.UserAgent
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SignInPostProcessingCompleted,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInSuccessful message)
    {
        yield break;
    }
}
