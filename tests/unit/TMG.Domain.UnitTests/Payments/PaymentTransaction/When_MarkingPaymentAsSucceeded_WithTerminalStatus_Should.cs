using TMG.Contracts.Payments;
using TMG.Domain.Common.Exceptions;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests.Payments.PaymentTransactions;

public sealed class When_MarkingPaymentAsSucceeded_WithTerminalStatus_Should
{
    [Fact]
    public void ThrowAggregateStateException()
    {
        var transaction = PaymentTransaction.Create("merchant-ref", PaymentIntent.WalletTopUp, Guid.CreateVersion7(), 1000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        transaction.MarkInitiated("provider-ref", null, null, "payment_initiated");
        transaction.MarkFailed("provider-ref", "failed", "provider_failed", DateTimeOffset.UtcNow);

        Should.Throw<AggregateStateException>(() =>
            transaction.MarkSucceeded("provider-ref", "success", DateTimeOffset.UtcNow));
    }
}

