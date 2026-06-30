using TMG.Domain.Authentication.Persistence;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Tenancies;
using TMG.WebAPI.IntegrationTests.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies.AllocateUnit;

[Collection(nameof(ContainersCollection))]
public sealed class When_AllocatingUnit_ToNewTenantEmail_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private HttpResponseMessage? _response;
    private string _tenantEmail = string.Empty;

    [Fact]
    public async Task ProvisionPendingTenantAndInvitedTenancy()
    {
        await SeedTenantStakeholderTypeAsync();
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId, "Flat 10");
        _tenantEmail = WebApiIntegrationTestData.Email();
        AllocateUnitResponse? payload = null;

        await WhenAllocatingUnit();
        await ThenTheTenancyAndPendingAccountExist();

        async Task WhenAllocatingUnit()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Tenancies.AllocationsV1,
                new AllocateUnitRequest(unitId, _tenantEmail, "Tobi", "Ade"));
        }

        async Task ThenTheTenancyAndPendingAccountExist()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Created);

            payload = await _response.Content.ReadFromJsonAsync<AllocateUnitResponse>();
            payload.ShouldNotBeNull();

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

            var tenancy = await dbContext.Tenancies.FirstOrDefaultAsync(item => item.Id == payload.TenancyId);
            tenancy.ShouldNotBeNull();
            tenancy.Status.ShouldBe(TenancyStatus.Invited);
            tenancy.UnitId.ShouldBe(unitId);
            tenancy.ClientId.ShouldBe(ClientId);

            var unit = await dbContext.Units.FirstAsync(item => item.Id == unitId);
            unit.Status.ShouldBe(UnitStatus.Occupied);

            var tenantUser = await userRepository.GetByEmailAsync(_tenantEmail);
            tenantUser.ShouldNotBeNull();
            tenantUser.EmailConfirmed.ShouldBeFalse();
        }
    }

    private async Task SeedTenantStakeholderTypeAsync()
    {
        using var scope = CreateScope();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await stakeholderTypeRepository.AddAsync(StakeholderType.Create(ClientId, "Tenant", "tenant"));
        await unitOfWork.SaveChangesAsync();
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}
