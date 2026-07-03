using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.ProcessRentReminders;

/// <summary>
/// Scans active tenancies whose next rent is coming due and publishes a <see cref="RentDueReminderTriggered"/>
/// event for each (the Consumer turns that into an email + archived notice). Idempotent: each reminder kind is
/// recorded on the tenancy so a subsequent scan does not re-send it.
/// </summary>
public sealed class RentReminderService(
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    IRepository<Property> propertyRepository,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RentRemindersResult> HandleAsync(
        DateTimeOffset threeMonthThresholdUtc,
        DateTimeOffset oneMonthThresholdUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var tenancies = await tenancyRepository.ListAsync(
            new ActiveTenanciesDueForReminderSpecification(threeMonthThresholdUtc, oneMonthThresholdUtc, batchSize),
            cancellationToken);
        if (tenancies.Count == 0)
        {
            return new RentRemindersResult(0);
        }

        var now = timeProvider.GetUtcNow();
        var processed = 0;
        foreach (var tenancy in tenancies)
        {
            var dueForOneMonth = tenancy.NextRentDueUtc <= oneMonthThresholdUtc && tenancy.Reminder1MonthSentAtUtc is null;
            var dueForThreeMonths = tenancy.NextRentDueUtc <= threeMonthThresholdUtc && tenancy.Reminder3MonthsSentAtUtc is null;

            RentReminderKind kind;
            if (dueForOneMonth)
            {
                kind = RentReminderKind.RentDueReminder1Month;
                // Sending the 1-month notice supersedes an unsent 3-month one so it never fires late/out of order.
                tenancy.RecordReminderSent(RentReminderKind.RentDueReminder3Months, now);
            }
            else if (dueForThreeMonths)
            {
                kind = RentReminderKind.RentDueReminder3Months;
            }
            else
            {
                continue;
            }

            tenancy.RecordReminderSent(kind, now);

            var unit = await unitRepository.GetByIdAsync(tenancy.UnitId, cancellationToken);
            var property = await propertyRepository.GetByIdAsync(tenancy.PropertyId, cancellationToken);

            await eventPublisher.PublishAsync(
                new RentDueReminderTriggered
                {
                    TenancyId = tenancy.Id,
                    UnitId = tenancy.UnitId,
                    PropertyId = tenancy.PropertyId,
                    ReminderType = ToNotificationType(kind),
                    NextRentDueUtc = tenancy.NextRentDueUtc!.Value,
                    UnitLabel = unit?.Label ?? string.Empty,
                    PropertyName = property?.Name ?? string.Empty,
                    RentAmount = unit?.RentAmount ?? 0m,
                    CurrencyId = unit?.CurrencyId ?? Guid.Empty,
                    StakeholderId = tenancy.TenantStakeholderId,
                    ClientId = tenancy.ClientId,
                    FlowId = string.Empty,
                    OccuredAt = now
                },
                cancellationToken);

            processed++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RentRemindersResult(processed);
    }

    private static NotificationType ToNotificationType(RentReminderKind kind) => kind switch
    {
        RentReminderKind.RentDueReminder3Months => NotificationType.RentDueReminder3Months,
        RentReminderKind.RentDueReminder1Month => NotificationType.RentDueReminder1Month,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown rent reminder kind.")
    };
}
