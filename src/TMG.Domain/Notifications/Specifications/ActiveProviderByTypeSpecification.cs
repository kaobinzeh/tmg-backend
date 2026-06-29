using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class ActiveProviderByTypeSpecification : Specification<Provider>
{
    public ActiveProviderByTypeSpecification(ProviderType providerType)
    {
        Where(provider => provider.ProviderType == providerType && provider.IsActive);
    }
}
