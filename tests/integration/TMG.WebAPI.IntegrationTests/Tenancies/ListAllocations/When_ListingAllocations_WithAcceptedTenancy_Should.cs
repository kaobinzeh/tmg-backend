using TMG.WebAPI.IntegrationTests.Tenancies;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.ListAllocations;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingAllocations_WithAcceptedTenancy_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private sealed record AllocationItem(
        Guid TenancyId,
        Guid UnitId,
        string Status,
        string? TenantName,
        string TenantEmail,
        string UnitLabel,
        string PropertyName,
        decimal RentAmount);

    [Fact]
    public async Task ReturnTheSeededAllocation()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);

        var response = await Client.GetAsync(EndpointUrl.Tenancies.AllocationsV1);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var allocations = await response.Content.ReadFromJsonAsync<List<AllocationItem>>();
        allocations.ShouldNotBeNull();

        var allocation = allocations.SingleOrDefault(item => item.TenancyId == tenant.TenancyId);
        allocation.ShouldNotBeNull();
        allocation.UnitId.ShouldBe(tenant.UnitId);
        allocation.Status.ShouldBe("Accepted");
        allocation.TenantEmail.ShouldNotBeNullOrWhiteSpace();
        allocation.UnitLabel.ShouldNotBeNullOrWhiteSpace();
        allocation.PropertyName.ShouldNotBeNullOrWhiteSpace();
        allocation.RentAmount.ShouldBe(1_500_000m);
    }
}
