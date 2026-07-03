using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.ListUpcomingRenewals;

public sealed class ListUpcomingRenewalsHandler(
    IRepository<Tenancy> tenancyRepository,
    TimeProvider timeProvider)
{
    public async Task<ListUpcomingRenewalsResult> HandleAsync(ListUpcomingRenewalsCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new ListUpcomingRenewalsResult(ListUpcomingRenewalsStatus.NotAuthenticated, []);
        }

        var withinMonths = command.WithinMonths <= 0 ? 6 : command.WithinMonths;
        var horizon = timeProvider.GetUtcNow().AddMonths(withinMonths);

        var tenancies = await tenancyRepository.ListAsync(
            new UpcomingRenewalsByClientSpecification(clientId, horizon),
            cancellationToken);

        var items = tenancies
            .Select(tenancy => new UpcomingRenewalListItem(
                tenancy.Id,
                tenancy.UnitId,
                tenancy.PropertyId,
                tenancy.TenantStakeholderId,
                tenancy.CycleStartUtc,
                tenancy.CycleEndUtc,
                tenancy.NextRentDueUtc))
            .ToList();

        return new ListUpcomingRenewalsResult(ListUpcomingRenewalsStatus.Success, items);
    }
}
