using TMG.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using TMG.Domain.Common.Auditing;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.WalletTransactions.TopUpDetails;

public sealed class When_GettingStakeholderWalletTopUpTransactionDetail_WithUnknownTransaction_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new PaymentsFlowTestContext();
        var walletTransactionId = Guid.CreateVersion7();

        context.WalletTransactionReadModelRepository
            .GetWalletTopUpDetailByStakeholderAsync(Arg.Any<TMG.Domain.Payments.ReadModels.StakeholderWalletTopUpTransactionDetailRequest>(), Arg.Any<CancellationToken>())
            .Returns((TMG.Domain.Payments.ReadModels.StakeholderWalletTopUpTransactionDetailReadModel?)null);

        var result = await context.CreateGetStakeholderWalletTopUpTransactionDetailHandler().HandleAsync(
            new GetStakeholderWalletTopUpTransactionDetailCommand(
                walletTransactionId,
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        result.Status.ShouldBe(GetStakeholderWalletTopUpTransactionDetailStatus.NotFound);
        result.Error.ShouldBe($"Wallet top-up transaction '{walletTransactionId}' was not found.");
    }
}
