using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class ProvidersByTypeSpecification : Specification<Provider>
{
    public ProvidersByTypeSpecification(ProviderType providerType)
    {
        Where(provider => provider.ProviderType == providerType);
    }
}
