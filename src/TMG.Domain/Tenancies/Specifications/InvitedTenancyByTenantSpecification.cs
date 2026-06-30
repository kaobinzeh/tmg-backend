using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

public sealed class InvitedTenancyByTenantSpecification : Specification<Tenancy>
{
    public InvitedTenancyByTenantSpecification(Guid tenantStakeholderId)
    {
        Where(tenancy =>
            tenancy.TenantStakeholderId == tenantStakeholderId &&
            tenancy.Status == TenancyStatus.Invited);
        EnableTracking();
    }
}
