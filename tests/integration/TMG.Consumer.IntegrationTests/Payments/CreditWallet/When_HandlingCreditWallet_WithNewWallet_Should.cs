using TMG.Consumer.IntegrationTests.Infrastructure;
using TMG.Consumer.Payments;
using TMG.Contracts.Commands.Payments;
using TMG.Domain.Payments;
using TMG.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace TMG.Consumer.IntegrationTests.Payments.CreditWallet;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingCreditWallet_WithNewWallet_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _paymentTransactionId;
    private Guid _clientId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _walletId;
    private Guid _walletTransactionId;

    protected override async Task InitializeWorkerTestAsync()
    {
        _paymentTransactionId = Guid.CreateVersion7();
        _clientId = Guid.CreateVersion7();
        _stakeholderId = Guid.CreateVersion7();

        using var scope = CreateDbContextScope();
        var currency = Currency.Create("NGN", "Naira", true);
        scope.DbContext.Currencies.Add(currency);
        await scope.DbContext.SaveChangesAsync();

        _currencyId = currency.Id;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        var walletTransaction = await scope.DbContext.WalletTransactions.FirstOrDefaultAsync(item => item.Id == _walletTransactionId);
        if (walletTransaction is not null)
        {
            scope.DbContext.WalletTransactions.Remove(walletTransaction);
        }

        var wallet = await scope.DbContext.Wallets.FirstOrDefaultAsync(item => item.Id == _walletId);
        if (wallet is not null)
        {
            scope.DbContext.Wallets.Remove(wallet);
        }

        var currency = await scope.DbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId);
        if (currency is not null)
        {
            scope.DbContext.Currencies.Remove(currency);
        }

        await scope.DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateWalletAndWalletTransaction()
    {
        await WhenPublishingCreditWalletCommand();
        await ThenTheWalletIsCredited();

        async Task WhenPublishingCreditWalletCommand()
        {
            using var scope = CreateScope();
            var messageContext = scope.ServiceProvider.GetRequiredService<Chidelu.Integration.Messaging.RabbitMQ.Consumer.IMessageContext>();

            await scope.ServiceProvider.GetRequiredService<CreditWalletHandler>().HandleAsync(
                new CreditWalletCommand(_paymentTransactionId, "merchant-ref", 2500m, _currencyId)
                {
                    StakeholderId = _stakeholderId,
                    ClientId = _clientId,
                    FlowId = "flow-123"
                },
                CancellationToken.None);
        }

        async Task ThenTheWalletIsCredited()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                return await scope.DbContext.WalletTransactions.AnyAsync(item => item.PaymentTransactionId == _paymentTransactionId);
            });

            using var scope = CreateDbContextScope();
            var walletTransaction = await scope.DbContext.WalletTransactions.FirstAsync(item => item.PaymentTransactionId == _paymentTransactionId);
            var wallet = await scope.DbContext.Wallets.FirstAsync(item => item.Id == walletTransaction.WalletId);

            _walletTransactionId = walletTransaction.Id;
            _walletId = wallet.Id;

            wallet.Balance.ShouldBe(2500m);
            wallet.StakeholderId.ShouldBe(_stakeholderId);
            walletTransaction.Amount.ShouldBe(2500m);
            walletTransaction.MerchantReference.ShouldBe("merchant-ref");
            walletTransaction.TransactionType.ShouldBe(TMG.Domain.Payments.Entities.WalletTransactionType.Credit);
            walletTransaction.TransactionCategory.ShouldBe(TMG.Domain.Payments.Entities.WalletTransactionCategory.WalletFunding);
            walletTransaction.TransactionTitle.ShouldBe(WalletTransactionTitles.WalletFunding);
        }
    }
}

