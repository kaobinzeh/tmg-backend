using TMG.Domain.Common.Persistence;
using TMG.Domain.ReferenceData.Entities;

namespace TMG.Application.ReferenceData.Specifications;

public sealed class EnabledCountriesSpecification : Specification<Country>
{
    public EnabledCountriesSpecification() => ApplyOrderBy(country => country.Name);
}
