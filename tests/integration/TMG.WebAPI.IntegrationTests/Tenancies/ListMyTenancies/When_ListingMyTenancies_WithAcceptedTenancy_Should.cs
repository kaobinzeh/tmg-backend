using TMG.WebAPI.IntegrationTests.Tenancies;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.ListMyTenancies;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingMyTenancies_WithAcceptedTenancy_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private sealed record AllocationItem(
        Guid TenancyId,
        Guid UnitId,
        string Status,
        string TenantEmail);

    [Fact]
    public async Task ReturnOnlyTheTenantsOwnTenancy()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);

        await AuthenticateAsTenantAsync(tenant.Email);

        var response = await Client.GetAsync(EndpointUrl.Tenancies.AllocationsMineV1);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tenancies = await response.Content.ReadFromJsonAsync<List<AllocationItem>>();
        tenancies.ShouldNotBeNull();

        var tenancy = tenancies.ShouldHaveSingleItem();
        tenancy.TenancyId.ShouldBe(tenant.TenancyId);
        tenancy.UnitId.ShouldBe(tenant.UnitId);
        tenancy.Status.ShouldBe("Accepted");
    }
}
