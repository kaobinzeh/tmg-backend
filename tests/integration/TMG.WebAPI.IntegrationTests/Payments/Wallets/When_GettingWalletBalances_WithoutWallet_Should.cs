using System.Net;
using System.Net.Http.Json;
using TMG.Application.Authentication.Features.SignIn;
using TMG.Application.Payments.Features.GetStakeholderWalletBalances;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Persistence;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;
using TMG.Domain.ReferenceData.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.WebAPI.Features.Authentication.Sessions;
using TMG.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TMG.WebAPI.IntegrationTests.Payments.Wallets;

/// <summary>
/// Wallets are only created on the first successful top-up, so a stakeholder who has never topped up
/// has no wallet row. The endpoint must still report a zero balance in their country's default currency.
/// </summary>
[Collection(nameof(ContainersCollection))]
public sealed class When_GettingWalletBalances_WithoutWallet_Should(ContainersFixture fixture) : IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private const string CurrencyCode = "KES";

    private readonly CustomWebApplicationFactory _factory = new(
        fixture.PostgresConnectionString,
        fixture.RedisConnectionString);

    private HttpClient _client = default!;
    private string _email = string.Empty;
    private Guid _clientId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _currencyId;
    private Guid _countryCurrencyId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _clientId = Guid.CreateVersion7();
        await CreateVerifiedUserAsync();
        await SeedCountryDefaultCurrencyAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ReturnAZeroBalanceInTheCountryDefaultCurrency()
    {
        await WhenGettingWalletBalances();

        _response.ShouldNotBeNull();
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await _response.Content.ReadFromJsonAsync<GetStakeholderWalletBalancesResult>();
        payload.ShouldNotBeNull();
        payload.Wallets.Count.ShouldBe(1);
        payload.Wallets[0].WalletId.ShouldBeNull();
        payload.Wallets[0].CurrencyId.ShouldBe(_currencyId);
        payload.Wallets[0].CurrencyCode.ShouldBe(CurrencyCode);
        payload.Wallets[0].Balance.ShouldBe(0m);

        async Task WhenGettingWalletBalances()
        {
            _response = await _client.GetAsync(EndpointUrl.Payments.WalletsV1);
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await _client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var country = Country.Create("No Wallet Land", "NW", "+998", "https://example.com/nw.svg");
        var stakeholderType = StakeholderType.Create(_clientId, "Tenant", "tenant");
        var stakeholder = Stakeholder.Create(user.Id, _clientId, country.Id, stakeholderType.Id, "Ada", "Lovelace");

        await countryRepository.AddAsync(country);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _stakeholderId = stakeholder.Id;
    }

    private async Task SeedCountryDefaultCurrencyAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

        var currency = Currency.Create(CurrencyCode, "Shilling", true, "KSh");
        var countryCurrency = CountryCurrency.Create(_countryId, currency.Id, isDefault: true, isActive: true);

        await dbContext.Currencies.AddAsync(currency);
        await dbContext.CountryCurrencies.AddAsync(countryCurrency);
        await dbContext.SaveChangesAsync();

        _currencyId = currency.Id;
        _countryCurrencyId = countryCurrency.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        var countryCurrency = await dbContext.CountryCurrencies.FirstOrDefaultAsync(item => item.Id == _countryCurrencyId);
        if (countryCurrency is not null)
        {
            dbContext.CountryCurrencies.Remove(countryCurrency);
        }

        var currency = await dbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId);
        if (currency is not null)
        {
            dbContext.Currencies.Remove(currency);
        }

        var stakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _stakeholderId);
        if (stakeholder is not null)
        {
            dbContext.Stakeholders.Remove(stakeholder);
        }

        var stakeholderType = await dbContext.StakeholderTypes.FirstOrDefaultAsync(item => item.Id == _stakeholderTypeId);
        if (stakeholderType is not null)
        {
            dbContext.StakeholderTypes.Remove(stakeholderType);
        }

        var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == _countryId);
        if (country is not null)
        {
            dbContext.Countries.Remove(country);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        await dbContext.SaveChangesAsync();
    }

    private IServiceScope CreateScope() => _factory.Services.CreateScope();
}
