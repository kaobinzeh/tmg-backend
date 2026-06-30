using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Tenancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.AllocateUnit;

[Collection(nameof(ContainersCollection))]
public sealed class When_AllocatingUnit_ToExistingClientUser_Should(ContainersFixture fixture)
    : TenancyIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;

    [Fact]
    public async Task ReuseTheExistingStakeholderWithoutCreatingAnAccount()
    {
        var tenantTypeId = await SeedTenantTypeAsync();
        var (existingEmail, existingStakeholderId) = await SeedExistingTenantAsync(tenantTypeId);
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId, "Flat 21");
        AllocateUnitResponse? payload = null;

        await WhenAllocatingToExistingEmail();
        await ThenTenancyReusesExistingStakeholder();

        async Task WhenAllocatingToExistingEmail()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Tenancies.AllocationsV1,
                new AllocateUnitRequest(unitId, existingEmail, "Tobi", "Ade"));
        }

        async Task ThenTenancyReusesExistingStakeholder()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Created);

            payload = await _response.Content.ReadFromJsonAsync<AllocateUnitResponse>();
            payload.ShouldNotBeNull();

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

            var tenancy = await dbContext.Tenancies.FirstAsync(item => item.Id == payload.TenancyId);
            tenancy.TenantStakeholderId.ShouldBe(existingStakeholderId);

            var stakeholderCount = await dbContext.Stakeholders.CountAsync(item => item.ClientId == ClientId && item.StakeholderTypeId == tenantTypeId);
            stakeholderCount.ShouldBe(1);
        }
    }

    private async Task<(string Email, Guid StakeholderId)> SeedExistingTenantAsync(Guid tenantTypeId)
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(email, "Tobi", "Ade");
        (await identityService.CreateAsync(user, TenantPassword)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholder = Stakeholder.Create(user.Id, ClientId, CountryId, tenantTypeId, "Tobi", "Ade");
        stakeholder.MarkVerified();
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        return (email, stakeholder.Id);
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}
