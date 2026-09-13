using TMG.Application.Payments.Features.GetStakeholderWalletBalances;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Payments.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.Wallets;

public sealed class When_GettingStakeholderWalletBalances_Should
{
    [Fact]
    public async Task ReturnABalancePerCurrency()
    {
        var context = new PaymentsFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var walletId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        Guid capturedStakeholderId = default;

        context.WalletReadModelRepository
            .GetBalancesByStakeholderAsync(
                Arg.Do<Guid>(id => capturedStakeholderId = id),
                Arg.Any<CancellationToken>())
            .Returns([new StakeholderWalletBalanceReadModel(walletId, currencyId, "NGN", "₦", 2500m)]);

        var result = await context.CreateGetStakeholderWalletBalancesHandler().HandleAsync(
            new GetStakeholderWalletBalancesCommand(
                new ActorContext(stakeholderId, Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        capturedStakeholderId.ShouldBe(stakeholderId);
        result.Wallets.Count.ShouldBe(1);
        result.Wallets[0].WalletId.ShouldBe(walletId);
        result.Wallets[0].CurrencyId.ShouldBe(currencyId);
        result.Wallets[0].CurrencyCode.ShouldBe("NGN");
        result.Wallets[0].CurrencySymbol.ShouldBe("₦");
        result.Wallets[0].Balance.ShouldBe(2500m);
    }

    [Fact]
    public async Task ReturnAZeroBalanceWithNoWalletIdWhenNoWalletHasBeenCreatedYet()
    {
        var context = new PaymentsFlowTestContext();
        var currencyId = Guid.CreateVersion7();

        context.WalletReadModelRepository
            .GetBalancesByStakeholderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([new StakeholderWalletBalanceReadModel(null, currencyId, "NGN", "₦", 0m)]);

        var result = await context.CreateGetStakeholderWalletBalancesHandler().HandleAsync(
            new GetStakeholderWalletBalancesCommand(
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        result.Wallets.Count.ShouldBe(1);
        result.Wallets[0].WalletId.ShouldBeNull();
        result.Wallets[0].Balance.ShouldBe(0m);
        result.Wallets[0].CurrencyCode.ShouldBe("NGN");
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWithoutAnAuthenticatedStakeholder()
    {
        var context = new PaymentsFlowTestContext();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateGetStakeholderWalletBalancesHandler().HandleAsync(
                new GetStakeholderWalletBalancesCommand(
                    new ActorContext(null, Guid.CreateVersion7(), "correlation-id", "flow-id")),
                CancellationToken.None));

        exception.Message.ShouldBe("Authenticated stakeholder id is required to retrieve wallet balances.");
    }
}
