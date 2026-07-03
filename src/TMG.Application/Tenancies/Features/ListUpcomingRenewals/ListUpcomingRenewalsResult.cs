namespace TMG.Application.Tenancies.Features.ListUpcomingRenewals;

public sealed record ListUpcomingRenewalsResult(
    ListUpcomingRenewalsStatus Status,
    IReadOnlyList<UpcomingRenewalListItem> Renewals);

public sealed record UpcomingRenewalListItem(
    Guid TenancyId,
    Guid UnitId,
    Guid PropertyId,
    Guid TenantStakeholderId,
    DateTimeOffset? CycleStartUtc,
    DateTimeOffset? CycleEndUtc,
    DateTimeOffset? NextRentDueUtc);

public enum ListUpcomingRenewalsStatus
{
    Success = 1,
    NotAuthenticated = 2
}
