using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.ReadModels;

public sealed record TenancyAllocationReadModel(
    Guid TenancyId,
    Guid UnitId,
    Guid PropertyId,
    Guid TenantStakeholderId,
    TenancyStatus Status,
    string? TenantName,
    string TenantEmail,
    string UnitLabel,
    string PropertyName,
    decimal RentAmount,
    Guid CurrencyId,
    DateTimeOffset? CycleStartUtc,
    DateTimeOffset? CycleEndUtc,
    DateTimeOffset? NextRentDueUtc);
