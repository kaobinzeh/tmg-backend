using TMG.Application.Authentication.Stakeholders;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;

namespace TMG.Application.Authentication.Features.SignIn;

public sealed class SignInHandler(
    IAuthenticationIdentityService identityService,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignInResult> HandleAsync(SignInCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignInStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var user = await identityService.FindByEmailAsync(request.Email);

        if (user is null)
        {
            await PublishFailedAsync(
                stakeholderId: null,
                emailAddress: request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.UserNotFound,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.LockedOut,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.EmailNotVerified,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.EmailNotVerified, null);
        }

        if (!await identityService.CheckPasswordAsync(user, request.Password))
        {
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.InvalidCredentials,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        var currentStakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        var accessToken = accessTokenService.Generate(
            user,
            currentStakeholder.Id,
            currentStakeholder.StakeholderType?.Key ?? string.Empty,
            currentStakeholder.ClientId);
        var refreshToken = await refreshTokenService.IssueAsync(user, cancellationToken);

        await PublishSuccessfulAsync(
            stakeholderId: currentStakeholder.Id,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            request.ActorContext,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SignInResult(SignInStatus.Success, new AuthenticationTokens(accessToken, refreshToken));
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
            Observability.EventNames.Authentication.PasswordSignInCompleted,
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
