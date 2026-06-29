using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class WalletTransactionByPaymentTransactionSpecification : Specification<WalletTransaction>
{
    public WalletTransactionByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(transaction => transaction.PaymentTransactionId == paymentTransactionId);
    }
}
