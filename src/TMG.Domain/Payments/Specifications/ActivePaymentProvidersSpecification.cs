using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;

namespace TMG.Domain.Payments.Specifications;

public sealed class ActivePaymentProvidersSpecification : Specification<PaymentProvider>
{
    public ActivePaymentProvidersSpecification()
    {
        Where(provider => provider.IsActive);
        AddInclude(provider => provider.Configurations);
        ApplyOrderBy(provider => provider.ProviderName);
    }
}
