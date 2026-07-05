using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.ReadModels;

public interface ITenancyReadModelRepository
{
    Task<IReadOnlyList<TenancyAllocationReadModel>> ListByClientAsync(
        Guid clientId,
        TenancyStatus? status,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TenancyAllocationReadModel>> ListByTenantAsync(
        Guid clientId,
        Guid tenantStakeholderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TenancyAllocationReadModel>> ListUpcomingRenewalsAsync(
        Guid clientId,
        DateTimeOffset horizonUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UnitTenancyReadModel>> ListCurrentByPropertyAsync(
        Guid clientId,
        Guid propertyId,
        CancellationToken cancellationToken);

    Task<TenancyRentSummaryReadModel> GetRentSummaryAsync(
        Guid clientId,
        DateTimeOffset nowUtc,
        DateTimeOffset renewalHorizonUtc,
        CancellationToken cancellationToken);
}
