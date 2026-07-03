using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>Active tenancies for a client whose next rent falls due on or before the horizon, soonest first.</summary>
public sealed class UpcomingRenewalsByClientSpecification : Specification<Tenancy>
{
    public UpcomingRenewalsByClientSpecification(Guid clientId, DateTimeOffset horizonUtc)
    {
        Where(tenancy =>
            tenancy.ClientId == clientId &&
            tenancy.Status == TenancyStatus.Active &&
            tenancy.NextRentDueUtc != null &&
            tenancy.NextRentDueUtc <= horizonUtc);
        ApplyOrderBy(tenancy => tenancy.NextRentDueUtc!);
    }
}
