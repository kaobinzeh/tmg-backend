using TMG.Application.Tenancies.Features.ListMyTenancies;
using TMG.Domain.Common.Auditing;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListMyTenancies;

public sealed class When_ListingMyTenancies_WithoutStakeholderId_Should
{
    [Fact]
    public async Task ReturnNotAuthenticatedWithEmptyList()
    {
        var context = new TenanciesFlowTestContext();

        var result = await context.CreateListMyTenanciesHandler().HandleAsync(
            new ListMyTenanciesCommand(new ActorContext(null, Guid.CreateVersion7(), "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListMyTenanciesStatus.NotAuthenticated);
        result.Tenancies.ShouldBeEmpty();
        await context.TenancyReadModelRepository.DidNotReceiveWithAnyArgs()
            .ListByTenantAsync(default, default, default);
    }
}
