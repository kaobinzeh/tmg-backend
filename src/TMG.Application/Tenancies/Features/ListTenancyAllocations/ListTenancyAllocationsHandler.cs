using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Tenancies.Features.ListTenancyAllocations;

public sealed class ListTenancyAllocationsHandler(ITenancyReadModelRepository tenancyReadModelRepository)
{
    public async Task<ListTenancyAllocationsResult> HandleAsync(
        ListTenancyAllocationsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new ListTenancyAllocationsResult(ListTenancyAllocationsStatus.NotAuthenticated, []);
        }

        var allocations = await tenancyReadModelRepository.ListByClientAsync(clientId, command.Status, cancellationToken);

        return new ListTenancyAllocationsResult(
            ListTenancyAllocationsStatus.Success,
            allocations.Select(TenancyAllocationListItem.FromReadModel).ToList());
    }
}
