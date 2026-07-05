using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Tenancies.Features.ListMyTenancies;

public sealed class ListMyTenanciesHandler(ITenancyReadModelRepository tenancyReadModelRepository)
{
    public async Task<ListMyTenanciesResult> HandleAsync(
        ListMyTenanciesCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } stakeholderId)
        {
            return new ListMyTenanciesResult(ListMyTenanciesStatus.NotAuthenticated, []);
        }

        var tenancies = await tenancyReadModelRepository.ListByTenantAsync(clientId, stakeholderId, cancellationToken);

        return new ListMyTenanciesResult(
            ListMyTenanciesStatus.Success,
            tenancies.Select(TenancyAllocationListItem.FromReadModel).ToList());
    }
}
