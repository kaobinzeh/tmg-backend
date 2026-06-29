using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class ActivePaymentProviderByIdSpecification : Specification<PaymentProvider>
{
    public ActivePaymentProviderByIdSpecification(Guid paymentProviderId)
    {
        Where(provider => provider.Id == paymentProviderId && provider.IsActive);
    }
}
