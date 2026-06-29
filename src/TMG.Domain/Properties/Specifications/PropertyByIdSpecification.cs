using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;

namespace TMG.Domain.Properties.Specifications;

public sealed class PropertyByIdSpecification : Specification<Property>
{
    public PropertyByIdSpecification(Guid propertyId, Guid clientId)
    {
        Where(property => property.Id == propertyId && property.ClientId == clientId);
        EnableTracking();
    }
}
