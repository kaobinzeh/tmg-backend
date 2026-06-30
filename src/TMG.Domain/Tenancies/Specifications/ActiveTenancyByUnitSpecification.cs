using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>Tenancies for a unit that are still live (invited or accepted), used to prevent double allocation.</summary>
public sealed class ActiveTenancyByUnitSpecification : Specification<Tenancy>
{
    public ActiveTenancyByUnitSpecification(Guid unitId)
    {
        Where(tenancy =>
            tenancy.UnitId == unitId &&
            (tenancy.Status == TenancyStatus.Invited || tenancy.Status == TenancyStatus.Accepted));
    }
}
