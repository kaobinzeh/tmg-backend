using TMG.Domain.Common.Caching;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;
using TMG.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.ReferenceData.Currencies;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingCurrencies_WithAvailableCurrencies_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    // Mirrors the cache key in GetCurrenciesHandler. Cleared around the test so a payload cached by an
    // earlier test cannot hide the seeded currency, and the seeded currency does not leak to later tests.
    private const string CurrenciesCacheKey = "reference-data:currencies";
    private const string CurrencyCode = "ZZT";

    private sealed record CurrencyView(Guid Id, string Code, string Name, string? Symbol);

    private Guid _currencyId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Client-Id", Guid.CreateVersion7().ToString());
        await SeedCurrencyAsync();
        await RemoveCachedCurrenciesAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await RemoveCachedCurrenciesAsync();
        await DeleteCurrencyAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnCurrencies()
    {
        IReadOnlyList<CurrencyView>? payload = default;

        await WhenGettingCurrencies();
        ThenTheSeededCurrencyIsReturned();

        async Task WhenGettingCurrencies()
        {
            _response = await Client.GetAsync(EndpointUrl.Currencies.V1);
            payload = await _response.Content.ReadFromJsonAsync<IReadOnlyList<CurrencyView>>();
        }

        void ThenTheSeededCurrencyIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            payload.ShouldNotBeNull();
            payload.Any(currency => currency.Id == _currencyId && currency.Code == CurrencyCode).ShouldBeTrue();
        }
    }

    private async Task SeedCurrencyAsync()
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var currency = Currency.Create(CurrencyCode, "Test Zed", true, "Z$");

        _currencyId = currency.Id;
        await repository.AddAsync(currency);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task RemoveCachedCurrenciesAsync()
    {
        using var scope = CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IJsonCache>();
        await cache.RemoveAsync(CurrenciesCacheKey);
    }

    private async Task DeleteCurrencyAsync()
    {
        if (_currencyId == Guid.Empty)
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var currency = await repository.GetByIdAsync(_currencyId);
        if (currency is null)
        {
            return;
        }

        repository.Remove(currency);
        await unitOfWork.SaveChangesAsync();
    }
}
