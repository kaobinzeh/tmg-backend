using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;

namespace TMG.Domain.Properties.Specifications;

public sealed class PropertiesByClientSpecification : Specification<Property>
{
    public PropertiesByClientSpecification(Guid clientId)
    {
        Where(property => property.ClientId == clientId);
        ApplyOrderBy(property => property.Name);
    }
}
