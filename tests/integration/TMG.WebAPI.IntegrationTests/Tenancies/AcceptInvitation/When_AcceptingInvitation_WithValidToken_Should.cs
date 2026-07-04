using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Providers.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Infrastructure.Storage;
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
        await SeedFileStorageProviderAsync();
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

    private async Task SeedFileStorageProviderAsync()
    {
        using var scope = CreateScope();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await providerRepository.AddAsync(Provider.Create(ProviderType.FileStorage, "Noop", ObjectStorageProviderKeys.Noop, true));
        await unitOfWork.SaveChangesAsync();
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
            var providers = await dbContext.Providers
                .Where(provider => provider.ProviderType == ProviderType.FileStorage && provider.ProviderKey == ObjectStorageProviderKeys.Noop)
                .ToListAsync();
            dbContext.Providers.RemoveRange(providers);
            await dbContext.SaveChangesAsync();
        }

        await base.DisposeAsync();
    }
}
