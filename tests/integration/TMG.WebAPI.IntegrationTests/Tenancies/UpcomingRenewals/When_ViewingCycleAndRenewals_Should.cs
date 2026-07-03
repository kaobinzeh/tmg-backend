using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.UpcomingRenewals;

[Collection(nameof(ContainersCollection))]
public sealed class When_ViewingCycleAndRenewals_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private sealed record CycleView(Guid TenancyId, DateTimeOffset? CycleStartUtc, DateTimeOffset? CycleEndUtc, DateTimeOffset? NextRentDueUtc);

    private sealed record RenewalItem(Guid TenancyId, Guid UnitId, DateTimeOffset? NextRentDueUtc);

    [Fact]
    public async Task ReturnTheCycleAndListItAmongUpcomingRenewals()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);

        // A short term so the renewal date falls inside the default upcoming-renewals horizon.
        var leaseStart = DateTimeOffset.UtcNow;
        var activateResponse = await Client.PostAsJsonAsync(
            EndpointUrl.Tenancies.ActivateV1(tenant.TenancyId),
            new ActivateTenancyRequest(leaseStart, TermMonths: 3));
        activateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var cycleResponse = await Client.GetAsync(EndpointUrl.Tenancies.CycleV1(tenant.TenancyId));
        cycleResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cycle = await cycleResponse.Content.ReadFromJsonAsync<CycleView>();
        cycle.ShouldNotBeNull();
        cycle.NextRentDueUtc.ShouldNotBeNull();

        var renewalsResponse = await Client.GetAsync(EndpointUrl.Tenancies.UpcomingRenewalsV1);
        renewalsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renewals = await renewalsResponse.Content.ReadFromJsonAsync<List<RenewalItem>>();
        renewals.ShouldNotBeNull();
        renewals.ShouldContain(item => item.TenancyId == tenant.TenancyId);
    }
}
