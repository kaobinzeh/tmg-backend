using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Domain.Common.Auditing;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListUpcomingRenewals;

public sealed class When_ListingUpcomingRenewals_WithoutClientId_Should
{
    [Fact]
    public async Task ReturnNotAuthenticatedWithEmptyList()
    {
        var context = new TenanciesFlowTestContext();

        var result = await context.CreateListUpcomingRenewalsHandler().HandleAsync(
            new ListUpcomingRenewalsCommand(new ActorContext(Guid.CreateVersion7(), null, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListUpcomingRenewalsStatus.NotAuthenticated);
        result.Renewals.ShouldBeEmpty();
        await context.TenancyReadModelRepository.DidNotReceiveWithAnyArgs()
            .ListUpcomingRenewalsAsync(default, default, default);
    }
}
