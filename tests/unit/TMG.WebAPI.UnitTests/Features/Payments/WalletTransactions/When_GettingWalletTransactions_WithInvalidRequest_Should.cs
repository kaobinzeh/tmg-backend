using TMG.WebAPI.Features.Payments.WalletTransactions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Payments.WalletTransactions;

public sealed class When_GettingWalletTransactions_WithInvalidRequest_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new WalletTransactionsController(
            context.CreateGetStakeholderWalletTransactionsHandler(),
            new GetStakeholderWalletTransactionsValidator(),
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler(),
            new GetStakeholderWalletTopUpTransactionDetailValidator(),
            context.CurrentActor);

        var result = await sut.Handle(new GetStakeholderWalletTransactionsRequest(0, null), CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
