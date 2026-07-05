using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Contracts.Payments;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.GetPaymentProviders;

public sealed class When_GettingPaymentProviders_WithRentPaymentIntent_Should
{
    [Fact]
    public async Task ReturnEnabledRentPaymentConfigurations()
    {
        var paymentProviderRepository = Substitute.For<IRepository<PaymentProvider>>();
        var currencyId = Guid.CreateVersion7();
        var provider = PaymentProvider.Create("Credo", "credo", isActive: true);
        provider.SetConfiguration(currencyId, PaymentIntent.RentPayment, PaymentMethodType.PaymentLink, isEnabled: true);

        paymentProviderRepository.ListAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns([provider]);

        var handler = new GetPaymentProvidersHandler(paymentProviderRepository);

        var result = await handler.HandleAsync(
            new GetPaymentProvidersCommand(PaymentIntent.RentPayment),
            CancellationToken.None);

        result.Providers.Count.ShouldBe(1);
        result.Providers[0].Id.ShouldBe(provider.Id);
        result.Providers[0].Name.ShouldBe("Credo");
        result.Providers[0].PaymentMethodType.ShouldBe("PaymentLink");
        result.Providers[0].CurrencyId.ShouldBe(currencyId);
    }
}
