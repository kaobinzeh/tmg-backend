using TMG.Application.Tenancies.Features.ListUpcomingRenewals;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Tenancies.Entities;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ListUpcomingRenewals;

public sealed class When_ListingUpcomingRenewals_WithAuthenticatedClient_Should
{
    [Fact]
    public async Task ReturnEnrichedRenewalItemsWithinHorizon()
    {
        var context = new TenanciesFlowTestContext();
        var clientId = Guid.CreateVersion7();
        var renewal = context.CreateAllocationReadModel(TenancyStatus.Active);
        Guid? capturedClientId = null;
        DateTimeOffset? capturedHorizonUtc = null;

        context.TenancyReadModelRepository
            .ListUpcomingRenewalsAsync(
                Arg.Do<Guid>(id => capturedClientId = id),
                Arg.Do<DateTimeOffset>(horizon => capturedHorizonUtc = horizon),
                Arg.Any<CancellationToken>())
            .Returns([renewal]);

        var result = await context.CreateListUpcomingRenewalsHandler().HandleAsync(
            new ListUpcomingRenewalsCommand(
                new ActorContext(Guid.CreateVersion7(), clientId, "corr", "flow"),
                WithinMonths: 3),
            CancellationToken.None);

        capturedClientId.ShouldBe(clientId);
        capturedHorizonUtc.ShouldBe(context.Clock.GetUtcNow().AddMonths(3));
        result.Status.ShouldBe(ListUpcomingRenewalsStatus.Success);
        result.Renewals.Count.ShouldBe(1);
        var item = result.Renewals[0];
        item.TenancyId.ShouldBe(renewal.TenancyId);
        item.UnitId.ShouldBe(renewal.UnitId);
        item.PropertyId.ShouldBe(renewal.PropertyId);
        item.TenantStakeholderId.ShouldBe(renewal.TenantStakeholderId);
        item.TenantName.ShouldBe(renewal.TenantName);
        item.TenantEmail.ShouldBe(renewal.TenantEmail);
        item.UnitLabel.ShouldBe(renewal.UnitLabel);
        item.PropertyName.ShouldBe(renewal.PropertyName);
        item.RentAmount.ShouldBe(renewal.RentAmount);
        item.CycleStartUtc.ShouldBe(renewal.CycleStartUtc);
        item.CycleEndUtc.ShouldBe(renewal.CycleEndUtc);
        item.NextRentDueUtc.ShouldBe(renewal.NextRentDueUtc);
    }
}
