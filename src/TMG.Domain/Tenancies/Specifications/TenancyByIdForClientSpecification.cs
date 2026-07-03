using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>A single tenancy scoped to the owning client, tracked for mutation (e.g. activation).</summary>
public sealed class TenancyByIdForClientSpecification : Specification<Tenancy>
{
    public TenancyByIdForClientSpecification(Guid tenancyId, Guid clientId, bool asTracking = false)
    {
        Where(tenancy => tenancy.Id == tenancyId && tenancy.ClientId == clientId);

        if (asTracking)
        {
            EnableTracking();
        }
    }
}
