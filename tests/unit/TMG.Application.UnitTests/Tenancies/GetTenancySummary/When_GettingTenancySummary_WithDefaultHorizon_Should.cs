using TMG.Application.Tenancies.Features.GetTenancySummary;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.GetTenancySummary;

public sealed class When_GettingTenancySummary_WithDefaultHorizon_Should
{
    [Fact]
    public async Task QuerySixMonthHorizonAndReturnMappedSummary()
    {
        var context = new TenanciesFlowTestContext();
        var clientId = Guid.CreateVersion7();
        Guid? capturedClientId = null;
        DateTimeOffset? capturedNowUtc = null;
        DateTimeOffset? capturedHorizonUtc = null;

        context.TenancyReadModelRepository
            .GetRentSummaryAsync(
                Arg.Do<Guid>(id => capturedClientId = id),
                Arg.Do<DateTimeOffset>(now => capturedNowUtc = now),
                Arg.Do<DateTimeOffset>(horizon => capturedHorizonUtc = horizon),
                Arg.Any<CancellationToken>())
            .Returns(new TenancyRentSummaryReadModel(3_000_000m, 500_000m, 4));

        var result = await context.CreateGetTenancySummaryHandler().HandleAsync(
            new GetTenancySummaryCommand(
                new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow"),
                WithinMonths: 0),
            CancellationToken.None);

        capturedClientId.ShouldBe(clientId);
        capturedNowUtc.ShouldBe(context.Clock.GetUtcNow());
        capturedHorizonUtc.ShouldBe(context.Clock.GetUtcNow().AddMonths(6));
        result.Status.ShouldBe(GetTenancySummaryStatus.Success);
        result.Summary.ShouldNotBeNull();
        result.Summary.CollectedAmount.ShouldBe(3_000_000m);
        result.Summary.OutstandingAmount.ShouldBe(500_000m);
        result.Summary.UpcomingRenewalsCount.ShouldBe(4);
    }
}
