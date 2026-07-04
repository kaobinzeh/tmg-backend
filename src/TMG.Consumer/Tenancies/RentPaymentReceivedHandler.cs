using System.Globalization;
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
/// Turns a <see cref="RentPaymentReceived"/> event into a receipt email to the tenant. The receipt document
/// itself is rendered and archived by the recording handler; this handler only delivers the notification.
/// </summary>
public sealed class RentPaymentReceivedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork)
    : BaseMessageHandler<RentPaymentReceived>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override async Task HandleAsyncInternal(RentPaymentReceived message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("RentPaymentReceived must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process RentPaymentReceived because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var amount = message.Amount.ToString("N2", CultureInfo.InvariantCulture);
        var paidDate = message.PaidAtUtc.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                message.ClientId,
                stakeholder.CountryId,
                NotificationType.RentPaymentReceipt,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["PropertyName"] = message.PropertyName,
                        ["UnitLabel"] = message.UnitLabel,
                        ["Amount"] = amount,
                        ["PaidDate"] = paidDate,
                        ["ReceiptNumber"] = message.ReceiptNumber
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(RentPaymentReceived message)
    {
        yield break;
    }
}
