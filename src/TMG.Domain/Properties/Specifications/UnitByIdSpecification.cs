using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;

namespace TMG.Domain.Properties.Specifications;

public sealed class UnitByIdSpecification : Specification<Unit>
{
    public UnitByIdSpecification(Guid unitId, Guid clientId)
    {
        Where(unit => unit.Id == unitId && unit.ClientId == clientId);
        EnableTracking();
    }
}
