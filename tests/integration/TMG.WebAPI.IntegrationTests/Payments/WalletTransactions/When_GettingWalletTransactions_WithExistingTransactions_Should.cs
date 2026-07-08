using System.Net;
using System.Net.Http.Json;
using TMG.Application.Authentication.Features.SignIn;
using TMG.Application.Payments.Features.GetStakeholderWalletTransactions;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Authentication.Persistence;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments;
using TMG.Domain.Payments.Entities;
using TMG.Domain.ReferenceData.Entities;
using TMG.Domain.Stakeholders.Entities;
using TMG.WebAPI.Features.Authentication.Sessions;
using TMG.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace TMG.WebAPI.IntegrationTests.Payments.WalletTransactions;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingWalletTransactions_WithExistingTransactions_Should(ContainersFixture fixture) : IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

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
    private Guid _walletId;
    private Guid _firstWalletTransactionId;
    private Guid _secondWalletTransactionId;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _clientId = Guid.CreateVersion7();
        await CreateVerifiedUserAsync();
        await SeedWalletTransactionsAsync();
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
    public async Task ReturnStakeholderWalletTransactions()
    {
        await WhenGettingWalletTransactions();

        _response.ShouldNotBeNull();
        _response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await _response.Content.ReadFromJsonAsync<GetStakeholderWalletTransactionsResult>();
        payload.ShouldNotBeNull();
        payload.Transactions.Count.ShouldBe(1);
        payload.Transactions[0].CurrencyCode.ShouldBe("NGN");
        payload.Transactions[0].TransactionType.ShouldBe(nameof(WalletTransactionType.Credit));
        payload.Transactions[0].TransactionCategory.ShouldBe(nameof(WalletTransactionCategory.BankTransferCredit));
        payload.Transactions[0].TransactionTitle.ShouldBe(WalletTransactionTitles.BankTransferCredit);
        payload.NextCursor.ShouldNotBeNull();

        async Task WhenGettingWalletTransactions()
        {
            _response = await _client.GetAsync($"{EndpointUrl.Payments.WalletTransactionsV1}?limit=1");
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
        var countryReadRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var country = (await countryReadRepository.ListAsync(new FirstCountrySpecification())).FirstOrDefault();
        if (country is null)
        {
            country = Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg");
            await countryRepository.AddAsync(country);
            _createdCountryForTest = true;
        }
        var stakeholderType = StakeholderType.Create(_clientId, "Tenant", "tenant");
        var stakeholder = Stakeholder.Create(user.Id, _clientId, country.Id, stakeholderType.Id, "Ada", "Lovelace");

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _stakeholderId = stakeholder.Id;
    }

    private async Task SeedWalletTransactionsAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

        var currency = Currency.Create("NGN", "Naira", true);
        var wallet = Wallet.Create(_stakeholderId, _clientId, currency.Id);
        wallet.Credit(3700m);

        await dbContext.Currencies.AddAsync(currency);
        await dbContext.Wallets.AddAsync(wallet);
        await dbContext.SaveChangesAsync();

        _currencyId = currency.Id;
        _walletId = wallet.Id;

        var firstTransaction = WalletTransaction.CreateCredit(
            wallet.Id,
            Guid.CreateVersion7(),
            "merchant-ref-1",
            2500m,
            currency.Id,
            WalletTransactionCategory.WalletFunding,
            WalletTransactionNarratives.WalletFunding.Title,
            WalletTransactionNarratives.WalletFunding.CreateDescription());
        var secondTransaction = WalletTransaction.CreateCredit(
            wallet.Id,
            Guid.CreateVersion7(),
            "merchant-ref-2",
            1200m,
            currency.Id,
            WalletTransactionCategory.BankTransferCredit,
            WalletTransactionNarratives.BankTransferCredit.Title,
            WalletTransactionNarratives.BankTransferCredit.CreateDescription());

        await dbContext.WalletTransactions.AddAsync(firstTransaction);
        await dbContext.WalletTransactions.AddAsync(secondTransaction);
        await dbContext.SaveChangesAsync();

        dbContext.Entry(firstTransaction).Property(item => item.CreatedAtUtc).CurrentValue = DateTimeOffset.UtcNow;
        dbContext.Entry(secondTransaction).Property(item => item.CreatedAtUtc).CurrentValue = DateTimeOffset.UtcNow.AddMinutes(1);
        await dbContext.SaveChangesAsync();

        _firstWalletTransactionId = firstTransaction.Id;
        _secondWalletTransactionId = secondTransaction.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        var secondTransaction = await dbContext.WalletTransactions.FirstOrDefaultAsync(item => item.Id == _secondWalletTransactionId);
        if (secondTransaction is not null)
        {
            dbContext.WalletTransactions.Remove(secondTransaction);
        }

        var firstTransaction = await dbContext.WalletTransactions.FirstOrDefaultAsync(item => item.Id == _firstWalletTransactionId);
        if (firstTransaction is not null)
        {
            dbContext.WalletTransactions.Remove(firstTransaction);
        }

        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(item => item.Id == _walletId);
        if (wallet is not null)
        {
            dbContext.Wallets.Remove(wallet);
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
        if (_createdCountryForTest && country is not null)
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

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
