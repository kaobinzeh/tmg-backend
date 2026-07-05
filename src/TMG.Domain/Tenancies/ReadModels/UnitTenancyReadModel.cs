using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.ReadModels;

public sealed record UnitTenancyReadModel(
    Guid UnitId,
    Guid TenancyId,
    TenancyStatus Status,
    string? TenantName,
    string TenantEmail);
