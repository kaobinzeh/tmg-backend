using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>
/// Active tenancies whose next rent is coming due and still need a reminder: either the 3-month reminder
/// (due within the 3-month window, not yet sent) or the 1-month reminder (due within the 1-month window, not yet sent).
/// </summary>
public sealed class ActiveTenanciesDueForReminderSpecification : Specification<Tenancy>
{
    public ActiveTenanciesDueForReminderSpecification(
        DateTimeOffset threeMonthThresholdUtc,
        DateTimeOffset oneMonthThresholdUtc,
        int batchSize)
    {
        Where(tenancy =>
            tenancy.Status == TenancyStatus.Active &&
            tenancy.NextRentDueUtc != null &&
            ((tenancy.NextRentDueUtc <= threeMonthThresholdUtc && tenancy.Reminder3MonthsSentAtUtc == null) ||
             (tenancy.NextRentDueUtc <= oneMonthThresholdUtc && tenancy.Reminder1MonthSentAtUtc == null)));
        ApplyOrderBy(tenancy => tenancy.NextRentDueUtc!);
        ApplyPaging(0, batchSize);
        EnableTracking();
    }
}
