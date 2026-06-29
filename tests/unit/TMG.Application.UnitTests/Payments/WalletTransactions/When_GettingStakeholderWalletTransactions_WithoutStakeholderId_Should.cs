using TMG.Application.Payments.Features.GetStakeholderWalletTransactions;
using TMG.Domain.Common.Auditing;
using Shouldly;

namespace TMG.Application.UnitTests.Payments.WalletTransactions;

public sealed class When_GettingStakeholderWalletTransactions_WithoutStakeholderId_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsFlowTestContext();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateGetStakeholderWalletTransactionsHandler().HandleAsync(
                new GetStakeholderWalletTransactionsCommand(
                    20,
                    null,
                    new ActorContext(null, Guid.CreateVersion7(), "correlation-id", "flow-id")),
                CancellationToken.None));

        exception.Message.ShouldBe("Authenticated stakeholder id is required to retrieve wallet transactions.");
    }
}
