using TMG.Application.Tenancies.Features.ListTenancyAllocations;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Tenancies.ListMyTenancies;

public sealed class When_ListingMyTenancies_WithTenanciesForTenant_Should
{
    [Fact]
    public async Task ReturnTenancies()
    {
        var context = new TenanciesControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();

        context.AuthenticateActor(stakeholderId, clientId);

        var tenancy = context.CreateAllocation(TenancyStatus.Active);
        context.TenancyReadModelRepository
            .ListByTenantAsync(clientId, stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new List<TenancyAllocationReadModel> { tenancy });

        var sut = context.CreateController();

        var result = await sut.ListMyTenancies(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeAssignableTo<IReadOnlyList<TenancyAllocationListItem>>();
        payload.ShouldNotBeNull();
        payload.Count.ShouldBe(1);
        payload[0].TenancyId.ShouldBe(tenancy.TenancyId);
        payload[0].Status.ShouldBe("Active");
        payload[0].TenantEmail.ShouldBe(tenancy.TenantEmail);
    }
}
