using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListTenancyAllocations;

public sealed class When_ListingTenancyAllocations_WithStatusFilter_Should
{
    [Fact]
    public async Task ReturnMappedAllocationsAndForwardFilter()
    {
        var context = new TenanciesFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var allocation = context.CreateAllocationReadModel(TenancyStatus.Active);
        Guid? capturedClientId = null;
        TenancyStatus? capturedStatus = null;

        context.TenancyReadModelRepository
            .ListByClientAsync(
                Arg.Do<Guid>(id => capturedClientId = id),
                Arg.Do<TenancyStatus?>(status => capturedStatus = status),
                Arg.Any<CancellationToken>())
            .Returns([allocation]);

        var result = await context.CreateListTenancyAllocationsHandler().HandleAsync(
            new ListTenancyAllocationsCommand(
                new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow"),
                TenancyStatus.Active),
            CancellationToken.None);

        capturedClientId.ShouldBe(clientId);
        capturedStatus.ShouldBe(TenancyStatus.Active);
        result.Status.ShouldBe(ListTenancyAllocationsStatus.Success);
        result.Allocations.Count.ShouldBe(1);
        var item = result.Allocations[0];
        item.TenancyId.ShouldBe(allocation.TenancyId);
        item.UnitId.ShouldBe(allocation.UnitId);
        item.PropertyId.ShouldBe(allocation.PropertyId);
        item.TenantStakeholderId.ShouldBe(allocation.TenantStakeholderId);
        item.Status.ShouldBe("Active");
        item.TenantName.ShouldBe(allocation.TenantName);
        item.TenantEmail.ShouldBe(allocation.TenantEmail);
        item.UnitLabel.ShouldBe(allocation.UnitLabel);
        item.PropertyName.ShouldBe(allocation.PropertyName);
        item.RentAmount.ShouldBe(allocation.RentAmount);
        item.CurrencyId.ShouldBe(allocation.CurrencyId);
        item.CycleStartUtc.ShouldBe(allocation.CycleStartUtc);
        item.CycleEndUtc.ShouldBe(allocation.CycleEndUtc);
        item.NextRentDueUtc.ShouldBe(allocation.NextRentDueUtc);
    }
}
