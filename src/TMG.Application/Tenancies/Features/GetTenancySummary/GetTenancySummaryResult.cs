namespace TMG.Application.Tenancies.Features.GetTenancySummary;

public sealed record GetTenancySummaryResult(
    GetTenancySummaryStatus Status,
    TenancySummaryDto? Summary = null);

public sealed record TenancySummaryDto(
    decimal CollectedAmount,
    decimal OutstandingAmount,
    int UpcomingRenewalsCount);

public enum GetTenancySummaryStatus
{
    Success = 1,
    NotAuthenticated = 2
}
