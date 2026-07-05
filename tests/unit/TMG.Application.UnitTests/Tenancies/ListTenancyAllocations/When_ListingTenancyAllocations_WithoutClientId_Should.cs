using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListTenancyAllocations;

public sealed class When_ListingTenancyAllocations_WithoutClientId_Should
{
    [Fact]
    public async Task ReturnNotAuthenticatedWithEmptyList()
    {
        var context = new TenanciesFlowTestContext();

        var result = await context.CreateListTenancyAllocationsHandler().HandleAsync(
            new ListTenancyAllocationsCommand(
                new ActorContext(Guid.CreateVersion7(), null, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListTenancyAllocationsStatus.NotAuthenticated);
        result.Allocations.ShouldBeEmpty();
        await context.TenancyReadModelRepository.DidNotReceiveWithAnyArgs()
            .ListByClientAsync(default, default(TenancyStatus?), default);
    }
}
