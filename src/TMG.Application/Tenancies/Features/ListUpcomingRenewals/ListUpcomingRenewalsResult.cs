namespace TMG.Application.Tenancies.Features.ListUpcomingRenewals;

public sealed record ListUpcomingRenewalsResult(
    ListUpcomingRenewalsStatus Status,
    IReadOnlyList<UpcomingRenewalListItem> Renewals);

public sealed record UpcomingRenewalListItem(
    Guid TenancyId,
    Guid UnitId,
    Guid PropertyId,
    Guid TenantStakeholderId,
    string? TenantName,
    string TenantEmail,
    string UnitLabel,
    string PropertyName,
    decimal RentAmount,
    DateTimeOffset? CycleStartUtc,
    DateTimeOffset? CycleEndUtc,
    DateTimeOffset? NextRentDueUtc);

public enum ListUpcomingRenewalsStatus
{
    Success = 1,
    NotAuthenticated = 2
}
