using TMG.Contracts.Commands.Payments;
using TMG.Contracts.Payments;
using TMG.Consumer.UnitTests.Payments;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace TMG.Consumer.UnitTests.Payments.SuccessfulPaymentConfirmed;

public sealed class When_HandlingSuccessfulPaymentConfirmed_WithRentPaymentIntent_Should
{
    [Fact]
    public async Task SendRecordRentPaymentCommandCarryingTheTenancyId()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var paymentTransactionId = Guid.CreateVersion7();
        var tenancyId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();

        await context.CreateSuccessfulPaymentConfirmedHandler().HandleAsync(
            new Contracts.Events.SuccessfulPaymentConfirmed
            {
                PaymentTransactionId = paymentTransactionId,
                MerchantReference = "merchant-ref",
                PaymentIntent = PaymentIntent.RentPayment,
                PaymentProviderId = Guid.CreateVersion7(),
                Amount = 1_500_000m,
                CurrencyId = currencyId,
                TenancyId = tenancyId,
                StakeholderId = stakeholderId,
                ClientId = clientId,
                FlowId = "flow-123"
            },
            CancellationToken.None);

        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<RecordRentPaymentCommand>(command =>
                command.PaymentTransactionId == paymentTransactionId &&
                command.TenancyId == tenancyId &&
                command.Amount == 1_500_000m &&
                command.CurrencyId == currencyId &&
                command.StakeholderId == stakeholderId &&
                command.ClientId == clientId),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectWhenTheTenancyIdIsMissing()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();

        await Should.ThrowAsync<CannotProcessMessageNonTransientException>(async () =>
            await context.CreateSuccessfulPaymentConfirmedHandler().HandleAsync(
                new Contracts.Events.SuccessfulPaymentConfirmed
                {
                    PaymentTransactionId = Guid.CreateVersion7(),
                    MerchantReference = "merchant-ref",
                    PaymentIntent = PaymentIntent.RentPayment,
                    PaymentProviderId = Guid.CreateVersion7(),
                    Amount = 1_500_000m,
                    CurrencyId = Guid.CreateVersion7(),
                    TenancyId = null,
                    StakeholderId = Guid.CreateVersion7(),
                    ClientId = Guid.CreateVersion7(),
                    FlowId = "flow-123"
                },
                CancellationToken.None));
    }
}
