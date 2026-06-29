using TMG.Contracts.Commands.Payments;
using TMG.Contracts.Events;
using TMG.Contracts.Payments;
using TMG.Consumer.UnitTests.Payments;

namespace TMG.Consumer.UnitTests.Payments.SuccessfulPaymentConfirmed;

public sealed class When_HandlingSuccessfulPaymentConfirmed_WithSubscriptionIntent_Should
{
    [Fact]
    public async Task SendActivateSubscriptionCommand()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var paymentTransactionId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();

        await context.CreateSuccessfulPaymentConfirmedHandler().HandleAsync(
            new Contracts.Events.SuccessfulPaymentConfirmed
                {
                    PaymentTransactionId = paymentTransactionId,
                    MerchantReference = "merchant-ref",
                PaymentIntent = PaymentIntent.Subscription,
                PaymentProviderId = Guid.CreateVersion7(),
                Amount = 1500m,
                CurrencyId = currencyId,
                StakeholderId = stakeholderId,
                ClientId = clientId,
                FlowId = "flow-123"
            },
            CancellationToken.None);

        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<ActivateSubscriptionCommand>(command =>
                command.PaymentTransactionId == paymentTransactionId &&
                command.MerchantReference == "merchant-ref" &&
                command.Amount == 1500m &&
                command.CurrencyId == currencyId &&
                command.StakeholderId == stakeholderId &&
                command.ClientId == clientId &&
                command.FlowId == "flow-123"),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
