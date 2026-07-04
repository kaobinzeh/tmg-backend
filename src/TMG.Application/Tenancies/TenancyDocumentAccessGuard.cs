using TMG.Application.Authentication.Constants;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Stakeholders.Specifications;
using TMG.Domain.Tenancies.Entities;

namespace TMG.Application.Tenancies;

/// <summary>
/// Decides whether the calling stakeholder may view a tenancy's documents: a manager of the owning client can
/// see every tenancy's documents; any other stakeholder can only see the tenancy they are the tenant of.
/// </summary>
public sealed class TenancyDocumentAccessGuard(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<StakeholderType> stakeholderTypeRepository)
{
    public async Task<bool> CanAccessAsync(Guid clientId, Guid stakeholderId, Tenancy tenancy, CancellationToken cancellationToken)
    {
        if (tenancy.ClientId != clientId)
        {
            return false;
        }

        // The tenant of the tenancy always sees their own documents.
        if (tenancy.TenantStakeholderId == stakeholderId)
        {
            return true;
        }

        var caller = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (caller is null || caller.ClientId != clientId)
        {
            return false;
        }

        var managerType = await stakeholderTypeRepository.FirstOrDefaultAsync(
            new StakeholderTypeByClientAndKeySpecification(clientId, StakeholderDefaults.Types.ManagerKey),
            cancellationToken);

        return managerType is not null && caller.StakeholderTypeId == managerType.Id;
    }
}
