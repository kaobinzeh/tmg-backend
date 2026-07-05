using TMG.Application.Tenancies.Features.ListMyTenancies;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListMyTenancies;

public sealed class When_ListingMyTenancies_WithAuthenticatedTenant_Should
{
    [Fact]
    public async Task ReturnMappedTenancies()
    {
        var context = new TenanciesFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var tenancy = context.CreateAllocationReadModel(TenancyStatus.Accepted);

        context.TenancyReadModelRepository
            .ListByTenantAsync(clientId, stakeholderId, Arg.Any<CancellationToken>())
            .Returns([tenancy]);

        var result = await context.CreateListMyTenanciesHandler().HandleAsync(
            new ListMyTenanciesCommand(new ActorContext(stakeholderId, clientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(ListMyTenanciesStatus.Success);
        result.Tenancies.Count.ShouldBe(1);
        var item = result.Tenancies[0];
        item.TenancyId.ShouldBe(tenancy.TenancyId);
        item.Status.ShouldBe("Accepted");
        item.TenantEmail.ShouldBe(tenancy.TenantEmail);
        item.UnitLabel.ShouldBe(tenancy.UnitLabel);
        item.PropertyName.ShouldBe(tenancy.PropertyName);
        item.RentAmount.ShouldBe(tenancy.RentAmount);
    }
}
