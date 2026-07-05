using TMG.Application.Tenancies.Features.GetTenancySummary;
using TMG.Domain.Tenancies.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Tenancies.GetSummary;

public sealed class When_GettingSummary_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnSummary()
    {
        var context = new TenanciesControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();

        context.AuthenticateActor(stakeholderId, clientId);
        context.TenancyReadModelRepository
            .GetRentSummaryAsync(
                clientId,
                context.Clock.GetUtcNow(),
                context.Clock.GetUtcNow().AddMonths(6),
                Arg.Any<CancellationToken>())
            .Returns(new TenancyRentSummaryReadModel(750000m, 250000m, 3));

        var sut = context.CreateController();

        var result = await sut.GetSummary(6, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<TenancySummaryDto>();
        payload.CollectedAmount.ShouldBe(750000m);
        payload.OutstandingAmount.ShouldBe(250000m);
        payload.UpcomingRenewalsCount.ShouldBe(3);
    }
}
