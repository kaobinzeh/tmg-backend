using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Contracts.Payments;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.GetPaymentProviders;

public sealed class When_GettingPaymentProviders_WithDisabledAndOtherIntentConfigurations_Should
{
    [Fact]
    public async Task ExcludeNonMatchingConfigurations()
    {
        var paymentProviderRepository = Substitute.For<IRepository<PaymentProvider>>();
        var enabledCurrencyId = Guid.CreateVersion7();
        var disabledCurrencyId = Guid.CreateVersion7();
        var provider = PaymentProvider.Create("SafeHaven", "safe-haven", isActive: true);
        provider.SetConfiguration(enabledCurrencyId, PaymentIntent.RentPayment, PaymentMethodType.PaymentLink, isEnabled: true);
        provider.SetConfiguration(disabledCurrencyId, PaymentIntent.RentPayment, PaymentMethodType.BankTransfer, isEnabled: false);
        provider.SetConfiguration(enabledCurrencyId, PaymentIntent.WalletTopUp, PaymentMethodType.BankTransfer, isEnabled: true);

        paymentProviderRepository.ListAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns([provider]);

        var handler = new GetPaymentProvidersHandler(paymentProviderRepository);

        var result = await handler.HandleAsync(
            new GetPaymentProvidersCommand(PaymentIntent.RentPayment),
            CancellationToken.None);

        result.Providers.Count.ShouldBe(1);
        result.Providers[0].CurrencyId.ShouldBe(enabledCurrencyId);
        result.Providers[0].PaymentMethodType.ShouldBe("PaymentLink");
    }
}
