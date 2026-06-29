using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class PaymentTransactionByMerchantReferenceSpecification : Specification<PaymentTransaction>
{
    public PaymentTransactionByMerchantReferenceSpecification(string merchantReference)
    {
        Where(transaction => transaction.MerchantReference == merchantReference);
        EnableTracking();
    }
}
