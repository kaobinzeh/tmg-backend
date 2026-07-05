using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.Tenancies.Features.GetTenancySummary;

public sealed class GetTenancySummaryHandler(
    ITenancyReadModelRepository tenancyReadModelRepository,
    TimeProvider timeProvider)
{
    public async Task<GetTenancySummaryResult> HandleAsync(
        GetTenancySummaryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId)
        {
            return new GetTenancySummaryResult(GetTenancySummaryStatus.NotAuthenticated);
        }

        var withinMonths = command.WithinMonths <= 0 ? 6 : command.WithinMonths;
        var nowUtc = timeProvider.GetUtcNow();
        var renewalHorizonUtc = nowUtc.AddMonths(withinMonths);

        var summary = await tenancyReadModelRepository.GetRentSummaryAsync(
            clientId,
            nowUtc,
            renewalHorizonUtc,
            cancellationToken);

        return new GetTenancySummaryResult(
            GetTenancySummaryStatus.Success,
            new TenancySummaryDto(summary.CollectedAmount, summary.OutstandingAmount, summary.UpcomingRenewalsCount));
    }
}
