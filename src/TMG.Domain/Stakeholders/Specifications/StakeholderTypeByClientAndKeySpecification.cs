using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;

namespace TMG.Domain.Stakeholders.Specifications;

public sealed class StakeholderTypeByClientAndKeySpecification : Specification<StakeholderType>
{
    public StakeholderTypeByClientAndKeySpecification(Guid clientId, string key)
    {
        var normalizedKey = key.Trim();
        Where(stakeholderType =>
            stakeholderType.ClientId == clientId &&
            stakeholderType.Key == normalizedKey);
    }
}
