using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class PaymentProviderByIdSpecification : Specification<PaymentProvider>
{
    public PaymentProviderByIdSpecification(Guid paymentProviderId)
    {
        Where(provider => provider.Id == paymentProviderId);
        EnableTracking();
    }
}
