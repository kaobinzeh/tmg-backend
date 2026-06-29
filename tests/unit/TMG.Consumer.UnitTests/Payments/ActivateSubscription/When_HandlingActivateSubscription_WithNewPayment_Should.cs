using TMG.Consumer.UnitTests.Payments;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithNewPayment_Should
{
    [Fact]
    public async Task CreateActivation()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateActivateSubscriptionCommand(5000m, Guid.CreateVersion7());
        SubscriptionActivation? capturedActivation = null;

        context.SubscriptionActivationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionActivation>>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionActivation?)null);
        context.SubscriptionActivationRepository.AddAsync(Arg.Do<SubscriptionActivation>(activation => capturedActivation = activation), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        capturedActivation.ShouldNotBeNull();
        capturedActivation.PaymentTransactionId.ShouldBe(command.PaymentTransactionId);
        capturedActivation.Amount.ShouldBe(command.Amount);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
