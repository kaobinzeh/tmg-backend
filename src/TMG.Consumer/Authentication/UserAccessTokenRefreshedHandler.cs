using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Services;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Authentication;

public sealed class UserAccessTokenRefreshedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ILoginActivityIpAddressResolver loginActivityIpAddressResolver,
    IRepository<LoginActivity> loginActivityRepository,
    IUnitOfWork unitOfWork,
    IUserAgentParserService userAgentParserService,
    TimeProvider timeProvider) : BaseMessageHandler<UserAccessTokenRefreshed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserAccessTokenRefreshed message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserAccessTokenRefreshed must contain a valid stakeholder actor id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserAccessTokenRefreshed because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var userAgentInfo = userAgentParserService.Parse(message.UserAgent);
        var ipAddressResolution = await loginActivityIpAddressResolver.ResolveAsync(message.IpAddress, cancellationToken);

        var loginActivity = LoginActivity.CreateTokenRefresh(
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SessionRefreshPostProcessingCompleted,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserAccessTokenRefreshed message)
    {
        yield break;
    }
}
