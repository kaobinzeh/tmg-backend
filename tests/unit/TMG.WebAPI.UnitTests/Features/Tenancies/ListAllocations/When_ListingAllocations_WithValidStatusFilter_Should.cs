using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Tenancies.ListAllocations;

public sealed class When_ListingAllocations_WithValidStatusFilter_Should
{
    [Fact]
    public async Task ReturnAllocations()
    {
        var context = new TenanciesControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();

        context.AuthenticateActor(stakeholderId, clientId);

        var allocation = context.CreateAllocation(TenancyStatus.Active);
        context.TenancyReadModelRepository
            .ListByClientAsync(clientId, TenancyStatus.Active, Arg.Any<CancellationToken>())
            .Returns(new List<TenancyAllocationReadModel> { allocation });

        var sut = context.CreateController();

        var result = await sut.ListAllocations("active", CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeAssignableTo<IReadOnlyList<TenancyAllocationListItem>>();
        payload.ShouldNotBeNull();
        payload.Count.ShouldBe(1);
        payload[0].TenancyId.ShouldBe(allocation.TenancyId);
        payload[0].Status.ShouldBe("Active");
        payload[0].TenantEmail.ShouldBe(allocation.TenantEmail);
        payload[0].UnitLabel.ShouldBe(allocation.UnitLabel);
        payload[0].PropertyName.ShouldBe(allocation.PropertyName);
        payload[0].RentAmount.ShouldBe(allocation.RentAmount);
    }
}
