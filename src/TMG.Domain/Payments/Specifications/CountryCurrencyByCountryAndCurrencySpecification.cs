using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class CountryCurrencyByCountryAndCurrencySpecification : Specification<CountryCurrency>
{
    public CountryCurrencyByCountryAndCurrencySpecification(Guid countryId, Guid currencyId)
    {
        Where(mapping =>
            mapping.CountryId == countryId &&
            mapping.CurrencyId == currencyId &&
            mapping.IsActive);
    }
}
