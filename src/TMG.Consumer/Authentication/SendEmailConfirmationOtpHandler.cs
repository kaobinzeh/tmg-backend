using TMG.Contracts.Commands.Authentication;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Authentication;

public sealed class SendEmailConfirmationOtpHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    EmailConfirmationOtpSender emailConfirmationOtpSender,
    ILogger<SendEmailConfirmationOtpHandler> logger) : BaseMessageHandler<SendEmailConfirmationOtpCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SendEmailConfirmationOtpCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("SendEmailConfirmationOtpCommand must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process SendEmailConfirmationOtpCommand because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());

        // An explicit resend always issues a fresh code; the previous one stops working.
        var outcome = await emailConfirmationOtpSender.SendAsync(stakeholder, replaceActiveOtp: true, cancellationToken);
        if (outcome is EmailConfirmationOtpSendOutcome.AlreadyConfirmed)
        {
            logger.LogWarning(
                "Skipping sign-up OTP resend for email {EmailAddress} because the email is already confirmed.",
                stakeholder.EmailAddress);

            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.AlreadyConfirmed);

            return;
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSent,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(SendEmailConfirmationOtpCommand message)
    {
        yield break;
    }
}
