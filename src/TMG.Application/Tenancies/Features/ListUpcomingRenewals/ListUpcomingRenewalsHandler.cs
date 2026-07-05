using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Tenancies.Features.ListUpcomingRenewals;

public sealed class ListUpcomingRenewalsHandler(
    ITenancyReadModelRepository tenancyReadModelRepository,
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

        var renewals = await tenancyReadModelRepository.ListUpcomingRenewalsAsync(clientId, horizon, cancellationToken);

        var items = renewals
            .Select(renewal => new UpcomingRenewalListItem(
                renewal.TenancyId,
                renewal.UnitId,
                renewal.PropertyId,
                renewal.TenantStakeholderId,
                renewal.TenantName,
                renewal.TenantEmail,
                renewal.UnitLabel,
                renewal.PropertyName,
                renewal.RentAmount,
                renewal.CycleStartUtc,
                renewal.CycleEndUtc,
                renewal.NextRentDueUtc))
            .ToList();

        return new ListUpcomingRenewalsResult(ListUpcomingRenewalsStatus.Success, items);
    }
}
