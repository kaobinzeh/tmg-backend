using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;

namespace TMG.Domain.Properties.Specifications;

public sealed class UnitByPropertyAndLabelSpecification : Specification<Unit>
{
    public UnitByPropertyAndLabelSpecification(Guid propertyId, string label)
    {
        var normalizedLabel = label.Trim();
        Where(unit => unit.PropertyId == propertyId && unit.Label == normalizedLabel);
    }
}
