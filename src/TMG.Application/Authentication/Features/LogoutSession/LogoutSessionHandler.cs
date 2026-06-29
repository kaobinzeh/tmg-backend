using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Observability;

namespace TMG.Application.Authentication.Features.LogoutSession;

public sealed class LogoutSessionHandler(
    IAccessTokenRevocationService accessTokenRevocationService,
    ICustomTelemetryContext customTelemetryContext)
{
    public async Task<LogoutSessionResult> HandleAsync(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenId))
        {
            return new LogoutSessionResult(LogoutSessionStatus.InvalidToken);
        }

        await accessTokenRevocationService.RevokeAsync(request.TokenId, request.ExpiresAtUtc, cancellationToken);

        if (request.StakeholderId.HasValue)
        {
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.SignOutCompleted,
                ObservabilityEventProperties.Create(request.ActorContext, request.StakeholderId.Value));
        }

        return new LogoutSessionResult(LogoutSessionStatus.Success);
    }
}
