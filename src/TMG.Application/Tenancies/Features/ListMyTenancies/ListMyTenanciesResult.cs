using TMG.Application.Tenancies.Features.ListTenancyAllocations;

namespace TMG.Application.Tenancies.Features.ListMyTenancies;

public sealed record ListMyTenanciesResult(
    ListMyTenanciesStatus Status,
    IReadOnlyList<TenancyAllocationListItem> Tenancies);

public enum ListMyTenanciesStatus
{
    Success = 1,
    NotAuthenticated = 2
}
