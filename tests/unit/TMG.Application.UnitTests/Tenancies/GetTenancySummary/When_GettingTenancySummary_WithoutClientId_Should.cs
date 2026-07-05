using TMG.Application.Tenancies.Features.GetTenancySummary;
using TMG.Domain.Common.Auditing;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.GetTenancySummary;

public sealed class When_GettingTenancySummary_WithoutClientId_Should
{
    [Fact]
    public async Task ReturnNotAuthenticated()
    {
        var context = new TenanciesFlowTestContext();

        var result = await context.CreateGetTenancySummaryHandler().HandleAsync(
            new GetTenancySummaryCommand(new ActorContext(Guid.CreateVersion7(), null, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetTenancySummaryStatus.NotAuthenticated);
        result.Summary.ShouldBeNull();
        await context.TenancyReadModelRepository.DidNotReceiveWithAnyArgs()
            .GetRentSummaryAsync(default, default, default, default);
    }
}
