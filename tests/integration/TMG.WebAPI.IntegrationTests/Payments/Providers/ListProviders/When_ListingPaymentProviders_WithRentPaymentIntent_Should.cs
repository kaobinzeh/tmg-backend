using TMG.Domain.Payments;
using TMG.Domain.Payments.Entities;
using TMG.WebAPI.IntegrationTests.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace TMG.WebAPI.IntegrationTests.Payments.Providers.ListProviders;

[Collection(nameof(ContainersCollection))]
public sealed class When_ListingPaymentProviders_WithRentPaymentIntent_Should(ContainersFixture fixture)
    : PropertyManagerIntegrationTestBase(fixture)
{
    private sealed record ProviderItem(Guid Id, string Name, string PaymentMethodType, Guid CurrencyId);

    private Guid _currencyId;
    private Guid _paymentProviderId;
    private Guid _paymentProviderConfigurationId;

    [Fact]
    public async Task ReturnTheRentPaymentEnabledProvider()
    {
        await SeedRentPaymentProviderAsync();

        var response = await Client.GetAsync($"{EndpointUrl.PaymentProviders.V1}?intent=RentPayment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var providers = await response.Content.ReadFromJsonAsync<List<ProviderItem>>();
        providers.ShouldNotBeNull();

        var provider = providers.SingleOrDefault(item => item.Id == _paymentProviderId);
        provider.ShouldNotBeNull();
        provider.Name.ShouldBe("Credo");
        provider.PaymentMethodType.ShouldBe(nameof(Contracts.Payments.PaymentMethodType.PaymentLink));
        provider.CurrencyId.ShouldBe(_currencyId);
    }

    private async Task SeedRentPaymentProviderAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

        var currency = Currency.Create("ZZP", "Test Zed", true);
        var provider = PaymentProvider.Create("Credo", PaymentProviderKeys.Credo, true);
        provider.SetConfiguration(
            currency.Id,
            Contracts.Payments.PaymentIntent.RentPayment,
            Contracts.Payments.PaymentMethodType.PaymentLink,
            true);

        await dbContext.Currencies.AddAsync(currency);
        await dbContext.PaymentProviders.AddAsync(provider);
        await dbContext.SaveChangesAsync();

        _currencyId = currency.Id;
        _paymentProviderId = provider.Id;
        _paymentProviderConfigurationId = provider.Configurations.Single().Id;
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TMG.Infrastructure.Persistence.AppDbContext>();

            var configuration = await dbContext.PaymentProviderConfigurations
                .FirstOrDefaultAsync(item => item.Id == _paymentProviderConfigurationId);
            if (configuration is not null)
            {
                dbContext.PaymentProviderConfigurations.Remove(configuration);
            }

            var provider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.Id == _paymentProviderId);
            if (provider is not null)
            {
                dbContext.PaymentProviders.Remove(provider);
            }

            var currency = await dbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId);
            if (currency is not null)
            {
                dbContext.Currencies.Remove(currency);
            }

            await dbContext.SaveChangesAsync();
        }

        await base.DisposeAsync();
    }
}
