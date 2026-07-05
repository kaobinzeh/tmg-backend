using TMG.Application.Tenancies.Features.GetTenancySummary;
using TMG.Application.Tenancies.Features.ListMyTenancies;
using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;

namespace TMG.Application.UnitTests.Tenancies;

internal sealed class TenanciesFlowTestContext
{
    public ITenancyReadModelRepository TenancyReadModelRepository { get; } = Substitute.For<ITenancyReadModelRepository>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero));

    public ListTenancyAllocationsHandler CreateListTenancyAllocationsHandler() =>
        new(TenancyReadModelRepository);

    public ListMyTenanciesHandler CreateListMyTenanciesHandler() =>
        new(TenancyReadModelRepository);

    public GetTenancySummaryHandler CreateGetTenancySummaryHandler() =>
        new(TenancyReadModelRepository, Clock);

    public ListUpcomingRenewalsHandler CreateListUpcomingRenewalsHandler() =>
        new(TenancyReadModelRepository, Clock);

    public TenancyAllocationReadModel CreateAllocationReadModel(
        TenancyStatus status = TenancyStatus.Active,
        string? tenantName = "Ada Lovelace",
        string tenantEmail = "ada@example.com",
        string unitLabel = "Flat 1",
        string propertyName = "Lekki Court") =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            status,
            tenantName,
            tenantEmail,
            unitLabel,
            propertyName,
            1_500_000m,
            Guid.CreateVersion7(),
            Clock.GetUtcNow().AddMonths(-11),
            Clock.GetUtcNow().AddMonths(1),
            Clock.GetUtcNow().AddMonths(1));

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
