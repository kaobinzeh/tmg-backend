using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class ActivePaymentProviderByKeySpecification : Specification<PaymentProvider>
{
    public ActivePaymentProviderByKeySpecification(string providerKey)
    {
        Where(provider => provider.ProviderKey == providerKey && provider.IsActive);
    }
}
