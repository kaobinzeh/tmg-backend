using TMG.Application.Authentication.Stakeholders;
using TMG.Contracts.Commands.Authentication;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;

namespace TMG.Application.Authentication.Features.ResendSignUpOtp;

public sealed class ResendSignUpOtpHandler(
    IAuthenticationIdentityService identityService,
    ICommandSender commandSender,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<ResendSignUpOtpResult> HandleAsync(ResendSignUpOtpCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await identityService.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.UserNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationOtpResendFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.UserNotFound));
            return new ResendSignUpOtpResult(ResendSignUpOtpStatus.UserNotFound);
        }

        if (user.EmailConfirmed)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.AlreadyConfirmed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationOtpResendFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.AlreadyConfirmed));
            return new ResendSignUpOtpResult(ResendSignUpOtpStatus.AlreadyVerified);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

        await commandSender.SendAsync(
            new SendEmailConfirmationOtpCommand
            {
                StakeholderId = stakeholder.Id,
                ClientId = stakeholder.ClientId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpResendRequested,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new ResendSignUpOtpResult(ResendSignUpOtpStatus.Accepted);
    }
}
