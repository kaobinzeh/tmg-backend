using TMG.Application.Authentication.Stakeholders;
using TMG.Application.Authentication.Constants;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;

namespace TMG.Application.Authentication.Features.GoogleSignIn;

public sealed class GoogleSignInHandler(
    IAuthenticationIdentityService identityService,
    IGoogleIdentityTokenService googleIdentityTokenService,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoogleSignInResult> HandleAsync(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignInStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var googleIdentity = await googleIdentityTokenService.ValidateAsync(request.IdToken, cancellationToken);
        if (googleIdentity is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidGoogleToken);
            return new GoogleSignInResult(GoogleSignInStatus.InvalidGoogleToken, null);
        }

        var user = await identityService.FindByLoginAsync(ExternalLoginProviders.Google, googleIdentity.Subject);
        if (user is null)
        {
            await PublishFailedAsync(
                stakeholderId: null,
                emailAddress: googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.UserNotFound,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.AccountNotRegistered, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.LockedOut,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? googleIdentity.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.EmailNotVerified,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new GoogleSignInResult(GoogleSignInStatus.EmailNotVerified, null);
        }

        var currentStakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        var accessToken = accessTokenService.Generate(
            user,
            currentStakeholder.Id,
            currentStakeholder.StakeholderType?.Key ?? string.Empty);
        var refreshToken = await refreshTokenService.IssueAsync(user, cancellationToken);

        await PublishSuccessfulAsync(
            stakeholderId: currentStakeholder.Id,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            request.ActorContext,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GoogleSignInResult(GoogleSignInStatus.Success, new AuthenticationTokens(accessToken, refreshToken));
    }

    private async Task PublishSuccessfulAsync(
        Guid stakeholderId,
        string ipAddress,
        string userAgent,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInSuccessful(ipAddress, userAgent)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignInCompleted,
            ObservabilityEventProperties.Create(actorContext, stakeholderId));
    }

    private async Task PublishFailedAsync(
        Guid? stakeholderId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        string failureReason,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInFailed(emailAddress, ipAddress, userAgent, failureReason)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        if (stakeholderId.HasValue)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
        }

        customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, failureReason);
    }
}
