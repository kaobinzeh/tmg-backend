using TMG.Domain.Common.Exceptions;
using TMG.Domain.Payments.Entities;
using Shouldly;

namespace TMG.Domain.UnitTests.Payments.Wallets;

public sealed class When_CreditingWallet_WithNonPositiveAmount_Should
{
    [Fact]
    public void ThrowAggregateStateException()
    {
        var wallet = Wallet.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        var exception = Should.Throw<AggregateStateException>(() => wallet.Credit(0m));

        exception.Message.ShouldContain("greater than zero");
    }
}

