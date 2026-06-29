using TMG.Application.Payments.Features.GetStakeholderWalletTransactions;
using TMG.Domain.Common.Auditing;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.WalletTransactions;

public sealed class When_GettingStakeholderWalletTransactions_WithInvalidCursor_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsFlowTestContext();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateGetStakeholderWalletTransactionsHandler().HandleAsync(
                new GetStakeholderWalletTransactionsCommand(
                    20,
                    "not-a-valid-cursor",
                    new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
                CancellationToken.None));

        exception.Message.ShouldBe("Invalid cursor.");
    }
}
