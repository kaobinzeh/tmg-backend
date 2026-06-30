using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.AcceptInvitation;

[Collection(nameof(ContainersCollection))]
public sealed class When_AcceptingInvitation_WithValidToken_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task ActivateAccountConfirmEmailAndAcceptTenancy()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var tenant = await SeedTenantAsync(tenantTypeId, activeAccount: false, acceptedTenancy: false);
        var token = await GenerateInvitationTokenAsync(tenant.AppUserId);

        await WhenAcceptingInvitation();
        await ThenTheAccountIsActivated();

        async Task WhenAcceptingInvitation()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Tenancies.AcceptInvitationV1,
                new AcceptTenancyInvitationRequest(tenant.Email, token, TenantPassword, TenantPassword, true));
        }

        async Task ThenTheAccountIsActivated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

            var user = await identityService.FindByEmailAsync(tenant.Email);
            user.ShouldNotBeNull();
            user.EmailConfirmed.ShouldBeTrue();
            (await identityService.CheckPasswordAsync(user, TenantPassword)).ShouldBeTrue();

            var tenancy = await dbContext.Tenancies.FirstAsync(item => item.Id == tenant.TenancyId);
            tenancy.Status.ShouldBe(TenancyStatus.Accepted);
            tenancy.TermsAcceptedAtUtc.ShouldNotBeNull();
        }
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}
