using System.Globalization;
using System.Text;
using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Tenancies;

/// <summary>
/// Turns a <see cref="RentDueReminderTriggered"/> event into a formal notice document (archived in the per-unit
/// document vault) and a reminder email to the tenant.
/// </summary>
public sealed class RentDueReminderTriggeredHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    INoticeLetterRenderer noticeLetterRenderer,
    IObjectStorageService objectStorageService,
    IRepository<TenancyDocument> tenancyDocumentRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork)
    : BaseMessageHandler<RentDueReminderTriggered>(customTelemetryContext, currentActorAccessor, messageContext)
{
    protected override async Task HandleAsyncInternal(RentDueReminderTriggered message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("RentDueReminderTriggered must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process RentDueReminderTriggered because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var noticePeriodLabel = message.ReminderType == NotificationType.RentDueReminder1Month ? "1 month" : "3 months";
        var rentAmount = message.RentAmount.ToString("N2", CultureInfo.InvariantCulture);
        var dueDate = message.NextRentDueUtc.UtcDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        // 1) Render + archive the formal notice document in the per-unit vault.
        var noticeHtml = noticeLetterRenderer.Render(new NoticeLetterModel(
            $"{stakeholder.FirstName} {stakeholder.LastName}".Trim(),
            message.PropertyName,
            message.UnitLabel,
            message.RentAmount,
            message.NextRentDueUtc,
            noticePeriodLabel));

        var objectKey =
            $"tenants/{message.ClientId}/tenancies/{message.TenancyId}/documents/notice/{Guid.CreateVersion7():N}.html";
        using var noticeStream = new MemoryStream(Encoding.UTF8.GetBytes(noticeHtml));
        var storageKey = await objectStorageService.UploadPrivateAsync(
            new ObjectStorageUploadRequest(objectKey, noticeStream, "text/html"),
            cancellationToken);

        var document = TenancyDocument.Create(
            message.ClientId,
            message.TenancyId,
            TenancyDocumentType.Notice,
            storageKey,
            "text/html",
            uploadedByStakeholderId: null);
        await tenancyDocumentRepository.AddAsync(document, cancellationToken);

        // 2) Email the reminder to the tenant.
        await commandSender.SendAsync(
            new SendNotificationCommand(
                message.ClientId,
                stakeholder.CountryId,
                message.ReminderType,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["PropertyName"] = message.PropertyName,
                        ["UnitLabel"] = message.UnitLabel,
                        ["RentAmount"] = rentAmount,
                        ["NextRentDueDate"] = dueDate,
                        ["NoticePeriod"] = noticePeriodLabel
                    }))
            {
                StakeholderId = stakeholder.StakeholderId
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(RentDueReminderTriggered message)
    {
        yield break;
    }
}
