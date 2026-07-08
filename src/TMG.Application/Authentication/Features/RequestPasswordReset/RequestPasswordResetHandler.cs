using TMG.Application.Authentication.Stakeholders;
using TMG.Contracts.Commands.Authentication;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;

namespace TMG.Application.Authentication.Features.RequestPasswordReset;

public sealed class RequestPasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ICommandSender commandSender,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<RequestPasswordResetResult> HandleAsync(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await identityService.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.UserNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordResetRequestFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.UserNotFound));
            return new RequestPasswordResetResult(RequestPasswordResetStatus.UserNotFound);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

        await commandSender.SendAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholder.Id,
                ClientId = stakeholder.ClientId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetRequested,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new RequestPasswordResetResult(RequestPasswordResetStatus.Success);
    }
}
