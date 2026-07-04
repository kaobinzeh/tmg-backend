using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Domain.Tenancies.Specifications;

/// <summary>Finds the rent payment already recorded for a gateway transaction — used to keep in-app settlement idempotent.</summary>
public sealed class RentPaymentByPaymentTransactionSpecification : Specification<RentPayment>
{
    public RentPaymentByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(payment => payment.PaymentTransactionId == paymentTransactionId);
    }
}
