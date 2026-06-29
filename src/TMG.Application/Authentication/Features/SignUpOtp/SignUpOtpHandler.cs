using TMG.Application.Authentication.Stakeholders;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;

namespace TMG.Application.Authentication.Features.SignUpOtp;

public sealed class SignUpOtpHandler(
    IAuthenticationIdentityService identityService,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignUpOtpResult> HandleAsync(SignUpOtpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var user = await identityService.FindByEmailAsync(request.Email);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        if (user.EmailConfirmed)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.AlreadyConfirmed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.AlreadyConfirmed));
            return new SignUpOtpResult(SignUpOtpStatus.AlreadyVerified);
        }

        if (!await identityService.VerifySignUpOtpAsync(user, request.Otp))
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        var now = timeProvider.GetUtcNow();
        user.MarkEmailVerified();
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var updateResult = await identityService.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("Failed to update the user after OTP verification.");
        }

        await eventPublisher.PublishAsync(new UserEmailConfirmed
        {
            StakeholderId = stakeholder.Id,
            FlowId = request.ActorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new SignUpOtpResult(SignUpOtpStatus.Success);
    }
}
