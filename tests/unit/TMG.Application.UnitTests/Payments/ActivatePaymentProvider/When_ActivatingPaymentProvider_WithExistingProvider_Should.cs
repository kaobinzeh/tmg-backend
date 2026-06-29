using TMG.Application.Payments.Features.ActivatePaymentProvider;
using TMG.Application.UnitTests.Payments;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.ActivatePaymentProvider;

public sealed class When_ActivatingPaymentProvider_WithExistingProvider_Should
{
    [Fact]
    public async Task SetActivation()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Credo", "credo", false);

        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(provider);

        var result = await context.CreateActivatePaymentProviderHandler().HandleAsync(
            new ActivatePaymentProviderCommand(provider.Id, true),
            CancellationToken.None);

        result.Status.ShouldBe(ActivatePaymentProviderStatus.Success);
        provider.IsActive.ShouldBeTrue();
        context.PaymentProviderRepository.Received(1).Update(provider);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
