using TMG.Application.Payments.Features.GetPaymentProviders;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Payments.Entities;
using TMG.WebAPI.Features.Payments.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Payments.Providers;

public sealed class When_SettingPaymentProviderActivation_WithUnknownProvider_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new PaymentProvidersController(
            context.CreateActivatePaymentProviderHandler(),
            new GetPaymentProvidersHandler(Substitute.For<IRepository<PaymentProvider>>()),
            new GetPaymentProvidersValidator());

        var result = await sut.SetActivation(Guid.CreateVersion7(), new SetPaymentProviderActivationRequest(true), CancellationToken.None);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
