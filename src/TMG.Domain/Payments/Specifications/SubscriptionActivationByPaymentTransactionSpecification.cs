using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class SubscriptionActivationByPaymentTransactionSpecification : Specification<SubscriptionActivation>
{
    public SubscriptionActivationByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(activation => activation.PaymentTransactionId == paymentTransactionId);
    }
}
