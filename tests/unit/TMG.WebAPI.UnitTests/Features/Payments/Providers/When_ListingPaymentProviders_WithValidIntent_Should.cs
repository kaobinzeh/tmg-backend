using TMG.Application.Payments.Features.ActivatePaymentProvider;
using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Contracts.Payments;
using TMG.Domain.Payments.Entities;
using TMG.WebAPI.Features.Payments.Providers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Payments.Providers;

public sealed class When_ListingPaymentProviders_WithValidIntent_Should
{
    [Fact]
    public async Task ReturnProviders()
    {
        var paymentProviderRepository = Substitute.For<IRepository<PaymentProvider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currencyId = Guid.CreateVersion7();
        var provider = PaymentProvider.Create("Credo", "credo", isActive: true);
        provider.SetConfiguration(currencyId, PaymentIntent.RentPayment, PaymentMethodType.PaymentLink, isEnabled: true);

        paymentProviderRepository
            .ListAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(new List<PaymentProvider> { provider });

        var sut = new PaymentProvidersController(
            new ActivatePaymentProviderHandler(paymentProviderRepository, unitOfWork),
            new GetPaymentProvidersHandler(paymentProviderRepository),
            new GetPaymentProvidersValidator());

        var result = await sut.List(new GetPaymentProvidersRequest("RentPayment"), CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeAssignableTo<IReadOnlyList<PaymentProviderListItem>>();
        payload.ShouldNotBeNull();
        payload.Count.ShouldBe(1);
        payload[0].Id.ShouldBe(provider.Id);
        payload[0].Name.ShouldBe("Credo");
        payload[0].PaymentMethodType.ShouldBe(nameof(PaymentMethodType.PaymentLink));
        payload[0].CurrencyId.ShouldBe(currencyId);
    }
}
