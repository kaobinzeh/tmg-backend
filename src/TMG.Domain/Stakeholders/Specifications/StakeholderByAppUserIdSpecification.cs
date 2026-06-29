using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;

namespace TMG.Domain.Stakeholders.Specifications;

public sealed class StakeholderByAppUserIdSpecification : Specification<Stakeholder>
{
    public StakeholderByAppUserIdSpecification(Guid appUserId)
    {
        Where(stakeholder => stakeholder.AppUserId == appUserId);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
