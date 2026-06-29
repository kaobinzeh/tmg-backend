using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;

namespace TMG.Domain.Properties.Specifications;

public sealed class UnitsByPropertySpecification : Specification<Unit>
{
    public UnitsByPropertySpecification(Guid propertyId, Guid clientId)
    {
        Where(unit => unit.PropertyId == propertyId && unit.ClientId == clientId);
        ApplyOrderBy(unit => unit.Label);
    }
}
