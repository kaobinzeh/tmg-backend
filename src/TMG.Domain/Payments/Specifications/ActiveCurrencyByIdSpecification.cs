using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class ActiveCurrencyByIdSpecification : Specification<Currency>
{
    public ActiveCurrencyByIdSpecification(Guid currencyId)
    {
        Where(currency => currency.Id == currencyId && currency.IsActive);
    }
}
