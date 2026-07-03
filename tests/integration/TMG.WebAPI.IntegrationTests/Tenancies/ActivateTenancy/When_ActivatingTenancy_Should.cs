using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.ActivateTenancy;

[Collection(nameof(ContainersCollection))]
public sealed class When_ActivatingTenancy_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task StartTheRentCycleForAnAcceptedTenancy()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: true);
        var leaseStart = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        await WhenActivating();
        await ThenTheCycleIsSet();

        async Task WhenActivating()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Tenancies.ActivateV1(tenant.TenancyId),
                new ActivateTenancyRequest(leaseStart, TermMonths: 12));
        }

        async Task ThenTheCycleIsSet()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
            var tenancy = await dbContext.Tenancies.FirstAsync(item => item.Id == tenant.TenancyId);

            tenancy.Status.ShouldBe(TenancyStatus.Active);
            tenancy.CycleStartUtc.ShouldBe(leaseStart);
            tenancy.CycleEndUtc.ShouldBe(leaseStart.AddMonths(12));
            tenancy.NextRentDueUtc.ShouldBe(leaseStart.AddMonths(12));
        }
    }

    [Fact]
    public async Task RejectActivationWhenTheTenancyIsNotAccepted()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: true, acceptedTenancy: false);

        _response = await Client.PostAsJsonAsync(
            EndpointUrl.Tenancies.ActivateV1(tenant.TenancyId),
            new ActivateTenancyRequest(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), 12));

        _response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}
