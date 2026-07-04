using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Tenancies;

/// <summary>
/// Turns a <see cref="TenancyAgreementReady"/> event into an email letting the tenant know their tenancy
/// agreement has been archived to their document vault. The agreement itself is rendered + stored by the
/// accept handler; this handler only delivers the notification.
/// </summary>
public sealed class TenancyAgreementReadyHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork)
    : BaseMessageHandler<TenancyAgreementReady>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override async Task HandleAsyncInternal(TenancyAgreementReady message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("TenancyAgreementReady must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process TenancyAgreementReady because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        await commandSender.SendAsync(
            new SendNotificationCommand(
                message.ClientId,
                stakeholder.CountryId,
                NotificationType.TenancyAgreementReady,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["PropertyName"] = message.PropertyName,
                        ["UnitLabel"] = message.UnitLabel
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(TenancyAgreementReady message)
    {
        yield break;
    }
}
