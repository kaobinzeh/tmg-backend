using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Formatting;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Tenancies;

public sealed class TenantInvitedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ITwoFactorOtpService twoFactorOtpService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : BaseMessageHandler<TenantInvited>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(TenantInvited message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("TenantInvited must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process TenantInvited because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        if (await twoFactorOtpService.OtpExistsAsync(stakeholder.AppUserId, OtpIntent.TenancyInvitation, cancellationToken))
        {
            return;
        }

        var invitation = await twoFactorOtpService.GenerateOtpAsync(
            stakeholder.AppUserId,
            OtpIntent.TenancyInvitation,
            cancellationToken,
            characterLength: 32,
            isAlphaNumeric: true);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.ClientId,
                stakeholder.CountryId,
                NotificationType.UnitAllocationInvitation,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["PropertyName"] = message.PropertyName,
                        ["UnitLabel"] = message.UnitLabel,
                        ["InvitationToken"] = invitation.Code,
                        ["AcceptUrl"] = $"/tenancies/accept-invitation?email={Uri.EscapeDataString(stakeholder.EmailAddress)}&token={invitation.Code}",
                        ["InvitationExpiresAtUtc"] = DateTimeFormatter.FormatHumanReadableUtc(invitation.ExpiresAtUtc, timeProvider.GetUtcNow())
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(TenantInvited message)
    {
        yield break;
    }
}
