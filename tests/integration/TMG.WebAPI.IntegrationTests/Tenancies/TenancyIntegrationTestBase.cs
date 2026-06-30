using TMG.Application.Authentication.Features.SignIn;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.WebAPI.Features.Authentication.Sessions;
using TMG.WebAPI.IntegrationTests.Properties;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Tenancies;

public abstract class TenancyIntegrationTestBase(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    protected const string TenantPassword = "Tenant@Pass123!";

    protected sealed record SeededTenant(string Email, Guid AppUserId, Guid StakeholderId, Guid TenancyId, Guid UnitId);

    protected async Task<Guid> SeedTenantTypeAsync()
    {
        using var scope = CreateScope();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tenantType = StakeholderType.Create(ClientId, "Tenant", "tenant");
        await stakeholderTypeRepository.AddAsync(tenantType);
        await unitOfWork.SaveChangesAsync();
        return tenantType.Id;
    }

    protected async Task<SeededTenant> SeedTenantAsync(Guid tenantTypeId, bool activeAccount, bool acceptedTenancy)
    {
        var propertyId = await SeedPropertyAsync();
        var unitId = await SeedUnitAsync(propertyId);
        var email = WebApiIntegrationTestData.Email();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitRepository = scope.ServiceProvider.GetRequiredService<IRepository<Unit>>();
        var tenancyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Tenancy>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = AppUser.Create(email, "Tobi", "Ade");
        if (activeAccount)
        {
            (await identityService.CreateAsync(user, TenantPassword)).Succeeded.ShouldBeTrue();
            user.MarkEmailVerified();
            (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        }
        else
        {
            (await identityService.CreateAsync(user)).Succeeded.ShouldBeTrue();
        }

        var stakeholder = Stakeholder.Create(user.Id, ClientId, CountryId, tenantTypeId, "Tobi", "Ade");
        if (activeAccount)
        {
            stakeholder.MarkVerified();
        }

        await stakeholderRepository.AddAsync(stakeholder);

        var unit = await unitRepository.GetByIdAsync(unitId);
        unit!.MarkOccupied();
        unitRepository.Update(unit);

        var tenancy = Tenancy.Invite(ClientId, propertyId, unitId, stakeholder.Id, email);
        if (acceptedTenancy)
        {
            tenancy.Accept(DateTimeOffset.UtcNow);
        }

        await tenancyRepository.AddAsync(tenancy);
        await unitOfWork.SaveChangesAsync();

        return new SeededTenant(email, user.Id, stakeholder.Id, tenancy.Id, unitId);
    }

    protected async Task<string> GenerateInvitationTokenAsync(Guid appUserId)
    {
        using var scope = CreateScope();
        var twoFactorOtpService = scope.ServiceProvider.GetRequiredService<ITwoFactorOtpService>();
        var invitation = await twoFactorOtpService.GenerateOtpAsync(
            appUserId,
            OtpIntent.TenancyInvitation,
            CancellationToken.None,
            characterLength: 32,
            isAlphaNumeric: true);
        return invitation.Code;
    }

    protected async Task AuthenticateAsTenantAsync(string email)
    {
        var signInResponse = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(email, TenantPassword));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }
}
