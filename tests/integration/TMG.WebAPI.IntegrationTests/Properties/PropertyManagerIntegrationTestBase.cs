using TMG.Application.Authentication.Features.SignIn;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Persistence;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.ReferenceData.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.WebAPI.Features.Authentication.Sessions;
using TMG.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Properties;

/// <summary>
/// Shared setup for property-management integration tests: provisions a verified, authenticated
/// manager under a unique client, and cleans up the manager and any property data it creates.
/// </summary>
public abstract class PropertyManagerIntegrationTestBase(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    protected const string Password = "P@ssw0rd123!";

    protected string Email { get; private set; } = string.Empty;
    protected Guid ClientId { get; private set; }
    protected Guid CountryId { get; private set; }
    protected Guid StakeholderId { get; private set; }
    protected Guid StakeholderTypeId { get; private set; }

    private bool _createdCountryForTest;

    public virtual async Task InitializeAsync()
    {
        await InitializeClientAsync();
        ClientId = Guid.CreateVersion7();
        CountryId = await ResolveCountryIdAsync();
        await CreateVerifiedManagerAsync();
        await AuthenticateAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await DeletePropertyDataAsync();
        await DeleteManagerDataAsync();
        await DisposeClientAsync();
    }

    protected async Task<Guid> SeedPropertyAsync(string name = "Lekki Court", string address = "12 Admiralty Way")
    {
        using var scope = CreateScope();
        var propertyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Property>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var property = Property.Create(ClientId, StakeholderId, name, address, null);
        await propertyRepository.AddAsync(property);
        await unitOfWork.SaveChangesAsync();

        return property.Id;
    }

    protected async Task<Guid> SeedUnitAsync(Guid propertyId, string label = "Flat 1", decimal rentAmount = 1_500_000m)
    {
        using var scope = CreateScope();
        var unitRepository = scope.ServiceProvider.GetRequiredService<IRepository<Unit>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var unit = Unit.Create(propertyId, ClientId, label, null, 2, rentAmount, Guid.CreateVersion7());
        await unitRepository.AddAsync(unit);
        await unitOfWork.SaveChangesAsync();

        return unit.Id;
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(Email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedManagerAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var firstName = WebApiIntegrationTestData.FirstName();
        var lastName = WebApiIntegrationTestData.LastName();
        Email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(Email, firstName, lastName);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(ClientId, "Manager", "manager");
        var stakeholder = Stakeholder.Create(user.Id, ClientId, CountryId, stakeholderType.Id, firstName, lastName);

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        StakeholderId = stakeholder.Id;
        StakeholderTypeId = stakeholderType.Id;
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var writeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var existing = await readRepository.ListAsync(new FirstCountrySpecification());
        if (existing.Count > 0)
        {
            return existing[0].Id;
        }

        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await writeRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;

        return country.Id;
    }

    private async Task DeletePropertyDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

        var units = await dbContext.Units.IgnoreQueryFilters().Where(unit => unit.ClientId == ClientId).ToListAsync();
        dbContext.Units.RemoveRange(units);

        var properties = await dbContext.Properties.IgnoreQueryFilters().Where(property => property.ClientId == ClientId).ToListAsync();
        dbContext.Properties.RemoveRange(properties);

        await dbContext.SaveChangesAsync();
    }

    private async Task DeleteManagerDataAsync()
    {
        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var stakeholder = await stakeholderRepository.GetByIdAsync(StakeholderId);
        if (stakeholder is not null)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        var stakeholderType = await stakeholderTypeRepository.GetByIdAsync(StakeholderTypeId);
        if (stakeholderType is not null)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(Email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(CountryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
