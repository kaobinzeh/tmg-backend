using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class PaymentTransactionByProviderReferenceSpecification : Specification<PaymentTransaction>
{
    public PaymentTransactionByProviderReferenceSpecification(string providerReference)
    {
        Where(transaction => transaction.ProviderReference == providerReference);
        EnableTracking();
    }
}
