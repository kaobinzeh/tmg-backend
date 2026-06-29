using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class ProviderByTypeAndKeySpecification : Specification<Provider>
{
    public ProviderByTypeAndKeySpecification(ProviderType providerType, string providerKey)
    {
        Where(provider =>
            provider.ProviderType == providerType &&
            provider.ProviderKey == providerKey);
    }
}
