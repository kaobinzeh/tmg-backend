using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.RejectInvitation;

[Collection(nameof(ContainersCollection))]
public sealed class When_RejectingInvitation_WithValidToken_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task RejectTenancyAndFreeTheUnit()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: false, acceptedTenancy: false);
        var token = await GenerateInvitationTokenAsync(tenant.AppUserId);

        await WhenRejectingInvitation();
        await ThenTheTenancyIsRejectedAndUnitFreed();

        async Task WhenRejectingInvitation()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Tenancies.RejectInvitationV1,
                new RejectTenancyInvitationRequest(tenant.Email, token));
        }

        async Task ThenTheTenancyIsRejectedAndUnitFreed()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

            var tenancy = await dbContext.Tenancies.FirstAsync(item => item.Id == tenant.TenancyId);
            tenancy.Status.ShouldBe(TenancyStatus.Rejected);

            var unit = await dbContext.Units.FirstAsync(item => item.Id == tenant.UnitId);
            unit.Status.ShouldBe(UnitStatus.Available);
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}
