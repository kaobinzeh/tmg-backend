using TMG.Application.Payments.Features.ActivatePaymentProvider;
using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Domain.Payments.Entities;
using TMG.WebAPI.Features.Payments.Providers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Payments.Providers;

public sealed class When_ListingPaymentProviders_WithInvalidIntent_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var paymentProviderRepository = Substitute.For<IRepository<PaymentProvider>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var sut = new PaymentProvidersController(
            new ActivatePaymentProviderHandler(paymentProviderRepository, unitOfWork),
            new GetPaymentProvidersHandler(paymentProviderRepository),
            new GetPaymentProvidersValidator());

        var result = await sut.List(new GetPaymentProvidersRequest("not-an-intent"), CancellationToken.None);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBeOfType<ValidationProblemDetails>();
    }
}
