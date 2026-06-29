using TMG.Consumer.UnitTests.Payments;
using TMG.Domain.Payments.Entities;

namespace TMG.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithDuplicatePaymentTransaction_Should
{
    [Fact]
    public async Task IgnoreMessage()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateActivateSubscriptionCommand(5000m, Guid.CreateVersion7());

        context.SubscriptionActivationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionActivation>>(), Arg.Any<CancellationToken>())
            .Returns(SubscriptionActivation.Create(
                command.PaymentTransactionId,
                command.StakeholderId!.Value,
                command.ClientId,
                null,
                command.Amount,
                command.CurrencyId,
                context.Clock.GetUtcNow()));

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
