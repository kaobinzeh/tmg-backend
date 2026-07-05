using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class ActiveCurrenciesSpecification : Specification<Currency>
{
    public ActiveCurrenciesSpecification()
    {
        Where(currency => currency.IsActive);
        ApplyOrderBy(currency => currency.CurrencyCode);
    }
}
