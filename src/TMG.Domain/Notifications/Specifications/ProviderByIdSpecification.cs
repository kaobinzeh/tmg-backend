using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;

namespace TMG.Domain.Notifications.Specifications;

public sealed class ProviderByIdSpecification : Specification<Provider>
{
    public ProviderByIdSpecification(Guid providerId)
    {
        Where(provider => provider.Id == providerId);
    }
}
