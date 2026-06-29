using TMG.WebAPI.Features.Payments.WalletTransactions;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Payments.WalletTransactions;

public sealed class When_GettingWalletTransactions_WithoutAuthenticatedStakeholder_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsControllerTestContext();
        context.CurrentActor.ClientId.Returns(Guid.CreateVersion7());
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");

        var sut = new WalletTransactionsController(
            context.CreateGetStakeholderWalletTransactionsHandler(),
            new GetStakeholderWalletTransactionsValidator(),
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler(),
            new GetStakeholderWalletTopUpTransactionDetailValidator(),
            context.CurrentActor);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.Handle(new GetStakeholderWalletTransactionsRequest(20, null), CancellationToken.None));

        exception.Message.ShouldBe("Unable to resolve stakeholder id from actor id ''.");
    }
}
