using TMG.Application.Authentication.Stakeholders;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;

namespace TMG.Application.Authentication.Features.CompletePasswordReset;

public sealed class CompletePasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ITwoFactorOtpService twoFactorOtpService,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<CompletePasswordResetResult> HandleAsync(
        CompletePasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await identityService.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.UserNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordResetCompletionFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.UserNotFound));
            return new CompletePasswordResetResult(CompletePasswordResetStatus.UserNotFound);
        }

        var otpIsValid = await twoFactorOtpService.ValidateOtpAsync(
            user.Id,
            request.Otp,
            OtpIntent.PasswordReset,
            cancellationToken);
        if (!otpIsValid)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordResetCompletionFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new CompletePasswordResetResult(CompletePasswordResetStatus.InvalidOtp);
        }

        var resetResult = await identityService.ResetPasswordAsync(user, request.Password);
        if (!resetResult.Succeeded)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordResetCompletionFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new CompletePasswordResetResult(
                CompletePasswordResetStatus.ValidationFailed,
                resetResult.ToValidationDictionary());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var stakeholderId = await stakeholderResolver.GetRequiredIdAsync(user.Id, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholderId));

        return new CompletePasswordResetResult(CompletePasswordResetStatus.Success);
    }
}
