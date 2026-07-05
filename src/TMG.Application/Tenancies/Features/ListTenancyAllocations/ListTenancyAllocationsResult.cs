using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Tenancies.Features.ListTenancyAllocations;

public sealed record ListTenancyAllocationsResult(
    ListTenancyAllocationsStatus Status,
    IReadOnlyList<TenancyAllocationListItem> Allocations);

public sealed record TenancyAllocationListItem(
    Guid TenancyId,
    Guid UnitId,
    Guid PropertyId,
    Guid TenantStakeholderId,
    string Status,
    string? TenantName,
    string TenantEmail,
    string UnitLabel,
    string PropertyName,
    decimal RentAmount,
    Guid CurrencyId,
    DateTimeOffset? CycleStartUtc,
    DateTimeOffset? CycleEndUtc,
    DateTimeOffset? NextRentDueUtc)
{
    public static TenancyAllocationListItem FromReadModel(TenancyAllocationReadModel allocation) =>
        new(
            allocation.TenancyId,
            allocation.UnitId,
            allocation.PropertyId,
            allocation.TenantStakeholderId,
            allocation.Status.ToString(),
            allocation.TenantName,
            allocation.TenantEmail,
            allocation.UnitLabel,
            allocation.PropertyName,
            allocation.RentAmount,
            allocation.CurrencyId,
            allocation.CycleStartUtc,
            allocation.CycleEndUtc,
            allocation.NextRentDueUtc);
}

public enum ListTenancyAllocationsStatus
{
    Success = 1,
    NotAuthenticated = 2
}
