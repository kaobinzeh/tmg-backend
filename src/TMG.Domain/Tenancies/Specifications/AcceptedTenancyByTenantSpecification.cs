using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

public sealed class AcceptedTenancyByTenantSpecification : Specification<Tenancy>
{
    public AcceptedTenancyByTenantSpecification(Guid tenantStakeholderId)
    {
        Where(tenancy =>
            tenancy.TenantStakeholderId == tenantStakeholderId &&
            tenancy.Status == TenancyStatus.Accepted);
    }
}
