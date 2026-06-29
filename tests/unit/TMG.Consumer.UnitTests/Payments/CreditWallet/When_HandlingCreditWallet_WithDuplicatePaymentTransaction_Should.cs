using TMG.Consumer.UnitTests.Payments;
using TMG.Domain.Payments;
using TMG.Domain.Payments.Entities;

namespace TMG.Consumer.UnitTests.Payments.CreditWallet;

public sealed class When_HandlingCreditWallet_WithDuplicatePaymentTransaction_Should
{
    [Fact]
    public async Task IgnoreMessage()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateCreditWalletCommand(2500m, Guid.CreateVersion7());

        context.WalletTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<WalletTransaction>>(), Arg.Any<CancellationToken>())
            .Returns(WalletTransaction.CreateCredit(Guid.CreateVersion7(), command.PaymentTransactionId, command.MerchantReference, command.Amount, command.CurrencyId, WalletTransactionCategory.WalletFunding, WalletTransactionNarratives.WalletFunding.Title, WalletTransactionNarratives.WalletFunding.CreateDescription()));

        await context.CreateCreditWalletHandler().HandleAsync(command, CancellationToken.None);

        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.WalletRepository.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
        context.WalletRepository.DidNotReceive().Update(Arg.Any<Wallet>());
    }
}

